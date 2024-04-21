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
    public sealed class MissingMeshFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Find Missing Mesh")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<MissingMeshFinderWindow>("MissingMesh");
        }
        public override void SaveChanges()
        {
            MissingMeshFinderWindowSettings.instance.Save();
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
            var settings = MissingMeshFinderWindowSettings.instance.Load();

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

            var labelTitle = new Label("Find missing meshes in opened scenes.")
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

            var missingMeshObjects = new List<Renderer>();
            var listViewMissingMeshes = new ListView()
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
            listViewMissingMeshes.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listViewMissingMeshes.showBorder = true;
            listViewMissingMeshes.horizontalScrollingEnabled = false;
            listViewMissingMeshes.reorderable = false;
            listViewMissingMeshes.selectionType = SelectionType.Single;
            listViewMissingMeshes.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listViewMissingMeshes.reorderMode = ListViewReorderMode.Animated;
            listViewMissingMeshes.showAddRemoveFooter = false;
            listViewMissingMeshes.itemsSource = missingMeshObjects;

            listViewMissingMeshes.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listViewMissingMeshes.bindItem = (visualElement, index) =>
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
                if (0 <= index && index < missingMeshObjects.Count)
                {
                    var renderer = missingMeshObjects[index];
                    label.text = $"{renderer?.name}";
                    label.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (renderer != null)
                        {
                            Selection.activeObject = renderer;
                            EditorGUIUtility.PingObject(renderer);
                        }
                    });
                }
                else
                {
                    label.text = "Error";
                }
                visualElement.Add(label);
            };
            contentContainer.Add(listViewMissingMeshes);

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

            buttonFind.SetEnabled(true);

            buttonFind.clicked += () =>
            {
                buttonFind.SetEnabled(false);

                SaveChanges();

                missingMeshObjects.Clear();
                listViewMissingMeshes.RefreshItems();

                FindMissingMesh(
                    missingMeshObjects,
                    () =>
                    {
                        listViewMissingMeshes.RefreshItems();
                    },
                    progressBar,
                    () =>
                    {
                        buttonFind.SetEnabled(true);
                    });
            };
        }

        private async void FindMissingMesh(List<Renderer> rendererPathes, Action onUpdateData, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<string, float>((title, percent) =>
                {
                    progressBar.title = $"{title} {percent:##0.00%}";
                    progressBar.value = percent * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;

                await FindMissingMesh(rendererPathes, onUpdateData, progress, cancellationToken);
            }
            finally
            {
                if (onCompleted != null)
                    onCompleted();
            }
        }

        private async Task FindMissingMesh(List<Renderer> rendererPathes, Action onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            progress("", 0f);

            var openedScenes = new List<Scene>(EditorSceneManager.sceneCount);
            for(int i=0; i<EditorSceneManager.sceneCount; i++)
                openedScenes.Add(EditorSceneManager.GetSceneAt(i));

            var i2 = 0;
            foreach(var scene in openedScenes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    progress("Canceld", 0f);
                    return;
                }

                if (!scene.isLoaded)
                    break;

                var rootObjects = scene.GetRootGameObjects();
                foreach(var rootObject in rootObjects)
                {
                    var renderers2 = rootObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in renderers2)
                    {
                        switch(renderer)
                        {
                            case SkinnedMeshRenderer:
                                {
                                    var skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
                                    if(skinnedMeshRenderer.sharedMesh == null)
                                    {
                                        Debug.Log($"Find missing mesh. {renderer}", renderer);
                                        rendererPathes.Add(renderer);
                                        onUpdateData.Invoke();
                                    }
                                }
                                break;
                            case ParticleSystemRenderer:
                                {
                                    var particleSystemRenderer = renderer as ParticleSystemRenderer;
                                    var meshes = Array.Empty<Mesh>();
                                    var count = particleSystemRenderer.GetMeshes(meshes);
                                    for(int i=0; i<count; ++i)
                                    {
                                        var mesh = meshes[i];
                                        if(mesh == null)
                                        {
                                            Debug.Log($"Find missing mesh. {renderer}", renderer);
                                            rendererPathes.Add(renderer);
                                            onUpdateData.Invoke();
                                            break;
                                        }
                                    }
                                }
                                break;
                            case MeshRenderer:
                                {
                                    if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
                                    {
                                        if(meshFilter.sharedMesh == null)
                                        {
                                            var meshRenderer = renderer as MeshRenderer;
                                            Debug.Log($"Find missing mesh. {renderer}", renderer);
                                            rendererPathes.Add(renderer);
                                            onUpdateData.Invoke();
                                        }
                                    }
                                }
                                break;
                            default:
                                Debug.Log($"Unknow type. renderer:{renderer.GetType().Name}");
                                break;
                        }
                    }
                }

                await Task.Yield();
                i2++;
                var percent = (float)i2 / openedScenes.Count;
                progress($"Scene {scene.name}", percent);
            }

            progress("Completed", 1f);
        }
    }
}
