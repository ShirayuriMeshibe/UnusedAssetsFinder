using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class UnusedLayerFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Find Unused Layer")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<UnusedLayerFinderWindow>("UnusedLayer");
        }
        public override void SaveChanges()
        {
            UnusedLayerFinderWindowSettings.instance.Save();
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

        class UnusedLayer
        {
            public int Layer;
            public string Name = string.Empty;
            public string DefaultName = string.Empty;
            public bool Used = false;

            public string NormalizedName() => string.IsNullOrEmpty(Name) ? DefaultName : Name;
            public string UsedToString() => Used ? "Used" : "Unused";
        }

        public void CreateGUI()
        {
            var settings = UnusedLayerFinderWindowSettings.instance.Load();

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

            var labelTitle = new Label("Find missing textures from scene assets.")
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

            var labelTitleMissingShaders = new Label("Objects containing missing textures")
            {
                style =
                {
                    marginBottom = 3f,
                },
            };
            contentContainer.Add(labelTitleMissingShaders);

            UnusedLayer[] unusedLayers = null;
            {
                var listUnusedLayers = new List<UnusedLayer>();
                for (int i=0; i<32; ++i)
                {
                    listUnusedLayers.Add(new UnusedLayer() { Layer = i, Name=LayerMask.LayerToName(i), DefaultName=$"Layer{i}" });
                }
                unusedLayers = listUnusedLayers.ToArray();
            }
            var listViewUnusedLayers = new ListView()
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
            listViewUnusedLayers.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listViewUnusedLayers.showBorder = true;
            listViewUnusedLayers.horizontalScrollingEnabled = false;
            listViewUnusedLayers.reorderable = false;
            listViewUnusedLayers.selectionType = SelectionType.Single;
            listViewUnusedLayers.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listViewUnusedLayers.reorderMode = ListViewReorderMode.Animated;
            listViewUnusedLayers.showAddRemoveFooter = false;
            listViewUnusedLayers.itemsSource = unusedLayers;

            listViewUnusedLayers.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listViewUnusedLayers.bindItem = (visualElement, index) =>
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
                if (0 <= index && index < unusedLayers.Length)
                {
                    var layer = unusedLayers[index];
                    label.text = $"{layer?.NormalizedName()} : {layer?.UsedToString()}";
                    label.style.color = layer.Used ? Color.red : Color.white;
                    //label.RegisterCallback<MouseDownEvent>(e =>
                    //{
                    //    if (layer != null)
                    //    {
                    //        Selection.objects = new UnityEngine.Object[] { layer.Material };
                    //        EditorGUIUtility.PingObject(layer.Material);
                    //    }
                    //});
                }
                else
                {
                    label.text = "Error";
                }
                visualElement.Add(label);
            };
            contentContainer.Add(listViewUnusedLayers);

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

                Array.ForEach(unusedLayers, layer => layer.Used = false);
                listViewUnusedLayers.RefreshItems();

                FindUnusedLayers(
                    scenesArray,
                    updatedLayer =>
                    {
                        //Debug.Log($"updatedLayer:{updatedLayer}");
                        Array.ForEach(unusedLayers, l =>
                        {
                            l.Used = l.Used || l.Layer == updatedLayer;
                        });
                        listViewUnusedLayers.RefreshItems();
                    },
                    progressBar,
                    () =>
                    {
                        listScenes.SetEnabled(true);
                        buttonFind.SetEnabled(true);
                    });
            };
        }
        private async void FindUnusedLayers(SceneAsset[] sceneAssets, Action<int> onUpdateData, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<string, float>((title, percent) =>
                {
                    progressBar.title = $"{title} {percent:##0.00%}";
                    progressBar.value = percent * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;

                await FindUnusedLayers(sceneAssets, onUpdateData, progress, cancellationToken);
            }
            finally
            {
                if (onCompleted != null)
                    onCompleted();
            }
        }
        private async Task FindUnusedLayers(SceneAsset[] sceneAssets, Action<int> onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            progress("Find Unused Layers", 0f);

            var alreadyOpenedScenes = new List<Scene>(EditorSceneManager.sceneCount);
            for(int i=0; i< EditorSceneManager.sceneCount; ++i)
                alreadyOpenedScenes.Add(EditorSceneManager.GetSceneAt(i));

            var i0 = 1;
            foreach(var sceneAsset in sceneAssets)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var path = AssetDatabase.GetAssetPath(sceneAsset);

                var isNewScene = false;
                var scene = alreadyOpenedScenes.Find(s => s.path == path);

                try
                {
                    if(!scene.isLoaded)
                    {
                        isNewScene = true;
                        scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    }

                    var parcent = (float)i0 / sceneAssets.Length;
                    var rootGameObjects = scene.GetRootGameObjects();

                    await FindUnusedLayersFromLight(scene.name, rootGameObjects, onUpdateData, progress, cancellationToken);
                    await FindUnusedLayers(scene.name, rootGameObjects, onUpdateData, progress, cancellationToken);
                }
                finally
                {
                    if (isNewScene)
                        EditorSceneManager.CloseScene(scene, true);
                }

                await Task.Yield();
            }

            progress("Completed", 1f);
        }

        private async Task FindUnusedLayersFromLight(string sceneName, GameObject[] gameObjects, Action<int> onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            foreach(var gameObject in gameObjects)
            {
                var lights = gameObject.GetComponentsInChildren<Light>();
                var i0 = 0;
                foreach(var light in lights)
                {
                    var percent = (float)i0 / lights.Length;
                    progress($"{sceneName}, {light.name}", percent);
                    Debug.Log($"{sceneName}:{light.name}, {light.cullingMask}", light);

                    // Not(Everything and Nothing)
                    if(light.cullingMask != -1 && light.cullingMask!=0)
                    {
                        for (int i = 0; i < 32; ++i)
                        {
                            var layer = 1 << i;
                            if((light.cullingMask & layer) != 0)
                                onUpdateData.Invoke(i);
                        }
                    }
                    await Task.Yield();
                }
            }
        }
        private async Task FindUnusedLayers(string sceneName, GameObject[] gameObjects, Action<int> onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            var stack = new Stack<Transform>();
            foreach(var gameObject in gameObjects)
                stack.Push(gameObject.transform);

            var count = stack.Count;
            while (0 < stack.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var transform = stack.Pop();
                var percent = (float)(count - stack.Count) / count;
                progress($"{sceneName}:{transform.name}", percent);
                onUpdateData.Invoke(transform.gameObject.layer);
                //Debug.Log($"{transform.name} : {transform.gameObject.layer}, {LayerMask.LayerToName(transform.gameObject.layer)}");

                foreach (Transform child in transform)
                    stack.Push(child);

                await Task.Yield();
                count = Mathf.Max(count, stack.Count);
            }
        }
    }
}
