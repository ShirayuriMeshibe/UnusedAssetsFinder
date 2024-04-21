using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class UsedAssetsFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Used Assets Finder")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<UsedAssetsFinderWindow>("UsedAssets");
        }
        public override void SaveChanges()
        {
            UsedAssetsFinderWindowSettings.instance.Save();
            base.SaveChanges();
        }

        private CancellationTokenSource _cancellationTokenSource;
        private void OnEnable()
        {
            if (_cancellationTokenSource == null)
                _cancellationTokenSource = new CancellationTokenSource();
        }
        private void OnDestroy()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void CreateGUI()
        {
            var settings = UsedAssetsFinderWindowSettings.instance.Load();

            var contentContainer = new VisualElement
            {
                style =
                {
                    marginTop = 10f,
                    marginRight = 10f,
                    marginLeft = 10f,
                },
            };
            rootVisualElement.Add(contentContainer);

            var labelTitle = new Label("Identify assets used in this scene.")
            {
                style =
                {
                    marginBottom = 10f,
                },
            };
            contentContainer.Add(labelTitle);

            var outputPathField = new TextField("OutputDirectoryPath");
            outputPathField.value = settings.OutputDirectoryPath.Value;
            contentContainer.Add(outputPathField);

            var buttonFind = new Button();
            buttonFind.text = "Find";
            buttonFind.style.marginBottom = 10f;

            var scenes = new List<SceneAsset>();
            if(settings.Scenes.Value != null)
                scenes.AddRange(settings.Scenes.Value);

            var isEnabled = new Func<bool>(() =>
            {
                return 0 < scenes.Count
                    && 0 <= scenes.FindIndex(scene => scene != null)
                    && !string.IsNullOrWhiteSpace(outputPathField.value);
            });

            var listScenes = new ListView()
            {
                style =
                {
                    flexGrow = 1,
                    marginTop = 5,
                    marginBottom = 5,
                    marginLeft = 5,
                    marginRight = 5,
                },
            };
            listScenes.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listScenes.showBorder = true;
            listScenes.horizontalScrollingEnabled = false;
            listScenes.reorderable = true;
            listScenes.reorderable = true;
            listScenes.selectionType = SelectionType.Single;
            listScenes.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listScenes.reorderMode = ListViewReorderMode.Animated;
            listScenes.showAddRemoveFooter = true;
            listScenes.itemsSource = scenes;

            listScenes.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listScenes.bindItem = (visualElement, index) =>
            {
                visualElement.Clear();
                visualElement.style.flexDirection = FlexDirection.Row;
                var objectField = new ObjectField()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1,
                        //marginTop = 5,
                        //marginBottom = 5,
                        //marginLeft = 5,
                        //marginRight = 5,
                    },
                };
                objectField.objectType = typeof(SceneAsset);
                objectField.allowSceneObjects = false;
                objectField.label = "Scene";
                visualElement.Add(objectField);

                if(0 <= index && index < scenes.Count)
                {
                    objectField.value = scenes[index];
                    objectField.RegisterValueChangedCallback(e =>
                    {
                        scenes[index] = e.newValue as SceneAsset;
                        buttonFind.SetEnabled(isEnabled());
                    });
                }
            };

            contentContainer.Add(listScenes);

            var space = new VisualElement
            {
                style =
                {
                    flexGrow = new StyleFloat(1f),
                },
            };
            rootVisualElement.Add(space);

            var progressBar = new ProgressBar();
            progressBar.title = "0.00%";
            rootVisualElement.Add(progressBar);

            rootVisualElement.Add(buttonFind);

            //---------
            // Actions
            //---------

            outputPathField.RegisterValueChangedCallback(e =>
            {
                buttonFind.SetEnabled(isEnabled());
            });

            buttonFind.SetEnabled(isEnabled());

            buttonFind.clicked += () =>
            {
                listScenes.SetEnabled(false);
                buttonFind.SetEnabled(false);

                scenes.RemoveAll(scene => scene == null);
                var scenesArray = scenes.ToArray();

                settings.OutputDirectoryPath.Value = outputPathField.value;
                settings.Scenes.Value = scenesArray;
                SaveChanges();

                CleanUnusedAssetsFromScene(outputPathField.value, scenesArray, progressBar, () =>
                {
                    listScenes.SetEnabled(true);
                    buttonFind.SetEnabled(true);
                });
            };
        }

        private async void CleanUnusedAssetsFromScene(string outputDirectoryPath, SceneAsset[] scenes, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<float>(v =>
                {
                    progressBar.title = $"Find used assets. {v:##0.00%}";
                    progressBar.value = v * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;
                var guids = await UsedAssetsFinder.Clean(scenes, progress, cancellationToken);
                UsedAssets.Create(outputDirectoryPath, guids);
            }
            finally
            {
                if(onCompleted!=null)
                    onCompleted();
            }
        }
    }
}
