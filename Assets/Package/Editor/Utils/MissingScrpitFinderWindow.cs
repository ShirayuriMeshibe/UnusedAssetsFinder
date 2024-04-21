using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class MissingScrpitFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Find Missing Script From Hierarchy Scenes")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<MissingScrpitFinderWindow>("MissingScript");
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

            var buttonFind = new Button();
            buttonFind.text = "Find";
            buttonFind.style.marginBottom = 10f;

            var labelTitle = new Label("Objects containing a missing script")
            {
                style =
                {
                    marginBottom = 3f,
                },
            };
            contentContainer.Add(labelTitle);

            var missingScriptObjects = new List<GameObject>();
            var listGameObjects = new ListView()
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
            listGameObjects.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listGameObjects.showBorder = true;
            listGameObjects.horizontalScrollingEnabled = false;
            listGameObjects.reorderable = false;
            listGameObjects.selectionType = SelectionType.Single;
            listGameObjects.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listGameObjects.reorderMode = ListViewReorderMode.Animated;
            listGameObjects.showAddRemoveFooter = false;
            listGameObjects.itemsSource = missingScriptObjects;

            listGameObjects.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listGameObjects.bindItem = (visualElement, index) =>
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
                if (0 <= index && index < missingScriptObjects.Count)
                {
                    var gameObject = missingScriptObjects[index];
                    label.text = $"{gameObject?.name}";
                    label.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (gameObject != null)
                            Selection.activeGameObject = gameObject;
                    });
                }
                else
                {
                    label.text = "Error";
                }
                visualElement.Add(label);
            };
            contentContainer.Add(listGameObjects);

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

            buttonFind.clicked += () =>
            {
                buttonFind.SetEnabled(false);
                //settings.UnusedAssetsData.Value = usedAssetsList.ToArray();
                SaveChanges();

                FindMissingScriptObject(missingScriptObjects,
                () =>
                {
                    //listGameObjects.itemsSource = missingScriptObjects;
                    Debug.Log($"missingScriptObjects:{missingScriptObjects.Count}");
                    listGameObjects.RefreshItems();
                    listGameObjects.Rebuild();
                },
                () =>
                {
                    buttonFind.SetEnabled(true);
                }, progressBar, _cancellationTokenSource.Token);
            };
        }

        async void FindMissingScriptObject(List<GameObject> missingScriptObjects, Action onFindMissingScript, Action onComplete, ProgressBar progressBar, CancellationToken cancellationToken)
        {
            try
            {
                var gameObjects = new List<GameObject>();
                for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    gameObjects.AddRange(scene.GetRootGameObjects());
                }

                var stack = new Stack<GameObject>(gameObjects.Count);
                foreach (var gameObject in gameObjects)
                    stack.Push(gameObject);

                var count = stack.Count;
                while (0 < stack.Count)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var gameObject = stack.Pop();
                    var percent = 1f - (stack.Count / (float)count);
                    percent *= 100f;
                    progressBar.title = $"{gameObject.name} {percent:##0.00}%";
                    progressBar.value = percent;

                    var components = gameObject.GetComponents<Component>();

                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            Debug.Log($"Missing component. {component}", component);
                            missingScriptObjects.Add(gameObject);
                            onFindMissingScript.Invoke();
                            break;
                        }
                    }

                    foreach (Transform child in gameObject.transform)
                        stack.Push(child.gameObject);

                    await Task.Yield();
                    count = Mathf.Max(count, stack.Count);
                }

                return;
            }
            finally
            {
                if(onComplete != null)
                    onComplete.Invoke();
            }
        }
    }
}
