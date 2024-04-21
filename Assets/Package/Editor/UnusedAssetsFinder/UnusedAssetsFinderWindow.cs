using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class UnusedAssetsFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Unused Assets Finder")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<UnusedAssetsFinderWindow>("UnusedAssets");
        }
        public override void SaveChanges()
        {
            UnusedAssetsFinderWindowSettings.instance.Save();
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
            var settings = UnusedAssetsFinderWindowSettings.instance.Load();

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

            var buttonFind = new Button();
            buttonFind.text = "Find";
            buttonFind.style.marginBottom = 10f;

            var usedAssetsList = new List<UsedAssets>();
            if (settings.UnusedAssetsData.Value != null)
                usedAssetsList.AddRange(settings.UnusedAssetsData.Value);

            var isEnabled = new Func<bool>(() =>
            {
                return 0 < usedAssetsList.Count
                    && 0 <= usedAssetsList.FindIndex(scene => scene != null);
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
            listScenes.selectionType = SelectionType.Single;
            listScenes.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listScenes.reorderMode = ListViewReorderMode.Animated;
            listScenes.showAddRemoveFooter = true;
            listScenes.itemsSource = usedAssetsList;

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
                objectField.objectType = typeof(UsedAssets);
                objectField.allowSceneObjects = false;
                objectField.label = "UsedAssets";
                visualElement.Add(objectField);

                if (0 <= index && index < usedAssetsList.Count)
                {
                    objectField.value = usedAssetsList[index];
                    objectField.RegisterValueChangedCallback(e =>
                    {
                        usedAssetsList[index] = e.newValue as UsedAssets;
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

            buttonFind.SetEnabled(isEnabled());

            buttonFind.clicked += () =>
            {
                listScenes.SetEnabled(false);
                buttonFind.SetEnabled(false);

                var unusedGuids = new HashSet<string>();
                usedAssetsList.ForEach(unusedAssets =>
                {
                    unusedGuids.UnionWith(unusedAssets.Guids);
                });

                settings.UnusedAssetsData.Value = usedAssetsList.ToArray();
                SaveChanges();

                FindUnusedAssets(unusedGuids, progressBar, () =>
                {
                    listScenes.SetEnabled(true);
                    buttonFind.SetEnabled(true);
                });
            };
        }

        private async void FindUnusedAssets(HashSet<string> usedGuids, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<string, float>((title, v) =>
                {
                    progressBar.title = $"{title} {v:##0.00%}";
                    progressBar.value = v * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;
                await UnusedAssetsFinder.Find(usedGuids, progress, cancellationToken);
            }
            finally
            {
                if (onCompleted != null)
                    onCompleted();
            }
        }
    }
}
