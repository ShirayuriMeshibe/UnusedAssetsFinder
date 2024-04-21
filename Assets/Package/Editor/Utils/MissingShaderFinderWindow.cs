using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class MissingShaderFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Find Missing Shader")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<MissingShaderFinderWindow>("MissingShader");
        }
        public override void SaveChanges()
        {
            MissingShaderFinderWindowSettings.instance.Save();
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
            var settings = MissingShaderFinderWindowSettings.instance.Load();

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

            var scenes = new List<SceneAsset>();
            if (settings.Scenes.Value != null)
                scenes.AddRange(settings.Scenes.Value);

            var isEnabled = new Func<bool>(() =>
            {
                return 0 < scenes.Count
                    && 0 <= scenes.FindIndex(scene => scene != null);
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

                if (0 <= index && index < scenes.Count)
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

            var labelTitleMissingShaders = new Label("Objects containing a missing shader")
            {
                style =
                {
                    marginBottom = 3f,
                },
            };
            contentContainer.Add(labelTitleMissingShaders);

            var missingShaderObjects = new List<Material>();
            var listViewMissingShaders = new ListView()
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
            listViewMissingShaders.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listViewMissingShaders.showBorder = true;
            listViewMissingShaders.horizontalScrollingEnabled = false;
            listViewMissingShaders.reorderable = false;
            listViewMissingShaders.selectionType = SelectionType.Single;
            listViewMissingShaders.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listViewMissingShaders.reorderMode = ListViewReorderMode.Animated;
            listViewMissingShaders.showAddRemoveFooter = false;
            listViewMissingShaders.itemsSource = missingShaderObjects;

            listViewMissingShaders.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listViewMissingShaders.bindItem = (visualElement, index) =>
            {
                visualElement.Clear();
                visualElement.style.flexDirection = FlexDirection.Row;

                var label = new Label()
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        flexGrow = 1,
                    },
                };
                if (0 <= index && index < missingShaderObjects.Count)
                {
                    var material = missingShaderObjects[index];
                    label.text = $"{material?.name}";
                    label.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (material != null)
                        {
                            Selection.objects = new UnityEngine.Object[] { material };
                            EditorGUIUtility.PingObject(material);
                        }
                    });
                }
                else
                {
                    label.text = "Error";
                }
                visualElement.Add(label);
            };
            contentContainer.Add(listViewMissingShaders);

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

                scenes.RemoveAll(scene => scene == null);
                var scenesArray = scenes.ToArray();

                settings.Scenes.Value = scenesArray;
                SaveChanges();

                missingShaderObjects.Clear();
                listViewMissingShaders.RefreshItems();

                FindMissingShader(scenesArray,
                missingShaderObjects,
                () =>
                {
                    listViewMissingShaders.RefreshItems();
                },
                progressBar,
                () =>
                {
                    listScenes.SetEnabled(true);
                    buttonFind.SetEnabled(true);
                });
            };
        }
        private async void FindMissingShader(SceneAsset[] scenes, List<Material> materials, Action onUpdateData, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<string, float>((title, percent) =>
                {
                    progressBar.title = $"{title} {percent:##0.00%}";
                    progressBar.value = percent * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;

                await FindMissingShader(scenes, materials, onUpdateData, progress, cancellationToken);
            }
            finally
            {
                if (onCompleted != null)
                    onCompleted();
            }
        }

        private async Task FindMissingShader(SceneAsset[] scenes, List<Material> materials, Action onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            progress("Find InternalErrorShader", 0f);

            var hiddenInternalErrorShader = Shader.Find("Hidden/InternalErrorShader");
            if(hiddenInternalErrorShader == null)
            {
                Debug.LogError($"Failed to find shader(Hidden/InternalErrorShader).");
                return;
            }

            var scenesPathList = new List<string>();
            foreach (var scene in scenes)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var path = AssetDatabase.GetAssetPath(scene);
                scenesPathList.Add(path);
            }

            var dependencies = AssetDatabase.GetDependencies(scenesPathList.ToArray());
            for (int i = 0; i < dependencies.Length; ++i)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var dependency = dependencies[i];
                var asset = AssetDatabase.LoadMainAssetAtPath(dependency);

                var percent = (float)i / dependencies.Length;

                if (asset is Material)
                {
                    var material = asset as Material;
                    var shader = material.shader;
                    if (shader == hiddenInternalErrorShader)
                    {
                        materials.Add(material);
                        onUpdateData.Invoke();
                    }
                    progress($"Material:{material.name}", percent);
                }
                else if(asset is Shader)
                {
                    var shader = asset as Shader;
                    progress($"Shader:{shader.name}", percent);
                }
                else if(asset is TextAsset)
                {
                    progress($"TextAsset:{asset.name}", percent);
                }
                else
                {
                    progress($"Asset:{asset}", percent);
                }

                await Task.Yield();
            }

            progress("Completed", 1f);
        }
    }
}
