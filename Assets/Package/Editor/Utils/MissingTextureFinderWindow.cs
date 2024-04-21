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
    public sealed class MissingTextureFinderWindow : EditorWindow
    {
        [MenuItem("Tools/Find Missing Texture")]
        static void ShowWindow()
        {
            EditorWindow.GetWindow<MissingTextureFinderWindow>("MissingTexture");
        }
        public override void SaveChanges()
        {
            MissingTextureFinderWindowSettings.instance.Save();
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

        class MissingMaterial
        {
            public Material Material;
            public string PropertyName;
            public int Id;
        }

        public void CreateGUI()
        {
            var settings = MissingTextureFinderWindowSettings.instance.Load();

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

            var missingTextureMaterials = new List<MissingMaterial>();
            var listViewMissingTextureMaterial = new ListView()
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
            listViewMissingTextureMaterial.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listViewMissingTextureMaterial.showBorder = true;
            listViewMissingTextureMaterial.horizontalScrollingEnabled = false;
            listViewMissingTextureMaterial.reorderable = false;
            listViewMissingTextureMaterial.selectionType = SelectionType.Single;
            listViewMissingTextureMaterial.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listViewMissingTextureMaterial.reorderMode = ListViewReorderMode.Animated;
            listViewMissingTextureMaterial.showAddRemoveFooter = false;
            listViewMissingTextureMaterial.itemsSource = missingTextureMaterials;

            listViewMissingTextureMaterial.makeItem = () =>
            {
                var item = new VisualElement();
                return item;
            };

            listViewMissingTextureMaterial.bindItem = (visualElement, index) =>
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
                if (0 <= index && index < missingTextureMaterials.Count)
                {
                    var missingMaterial = missingTextureMaterials[index];
                    label.text = $"{missingMaterial?.Material?.name}, {missingMaterial.PropertyName}, {missingMaterial.Id}";
                    label.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (missingMaterial != null)
                        {
                            Selection.objects = new UnityEngine.Object[] { missingMaterial.Material };
                            EditorGUIUtility.PingObject(missingMaterial.Material);
                        }
                    });
                }
                else
                {
                    label.text = "Error";
                }
                visualElement.Add(label);
            };
            contentContainer.Add(listViewMissingTextureMaterial);

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

                missingTextureMaterials.Clear();
                listViewMissingTextureMaterial.RefreshItems();

                FindMissingTexture(
                    scenesArray,
                    missingTextureMaterials,
                    () =>
                    {
                        listViewMissingTextureMaterial.RefreshItems();
                    },
                    progressBar,
                    () =>
                    {
                        listScenes.SetEnabled(true);
                        buttonFind.SetEnabled(true);
                    });
            };
        }
        private async void FindMissingTexture(SceneAsset[] sceneAssets, List<MissingMaterial> materials, Action onUpdateData, ProgressBar progressBar, Action onCompleted)
        {
            try
            {
                var progress = new Action<string, float>((title, percent) =>
                {
                    progressBar.title = $"{title} {percent:##0.00%}";
                    progressBar.value = percent * 100f;
                });
                var cancellationToken = _cancellationTokenSource.Token;

                await FindMissingTexture(sceneAssets, materials, onUpdateData, progress, cancellationToken);
            }
            finally
            {
                if (onCompleted != null)
                    onCompleted();
            }
        }
        private async Task FindMissingTexture(SceneAsset[] sceneAssets, List<MissingMaterial> materials, Action onUpdateData, Action<string, float> progress, CancellationToken cancellationToken)
        {
            progress("Find Missing Texture", 0f);

            var scenesPathList = new List<string>();
            foreach (var scene in sceneAssets)
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

                if (asset is not Material)
                {
                    progress($"Skip. {dependency}", percent);
                    continue;
                }

                var material = asset as Material;
                var shader = material.shader;

                if(shader == null)
                {
                    progress($"Missing shaser. {dependency}", percent);
                    continue;
                }

                using var serializedObject = new SerializedObject(material);

                var textureProperties = serializedObject.FindProperty("m_SavedProperties.m_TexEnvs");
                for(var i2=0; i2< textureProperties.arraySize; ++i2)
                {
                    var element = textureProperties.GetArrayElementAtIndex(i2);
                    var firstProperty = element.FindPropertyRelative("first");
                    var secondProperty = element.FindPropertyRelative("second");
                    var textureProperty = secondProperty.FindPropertyRelative("m_Texture");
                    if(textureProperty.objectReferenceValue == null && 0 < textureProperty.objectReferenceInstanceIDValue)
                    {
                        var propertyName = firstProperty.stringValue;

                        if(0 < shader.FindPropertyIndex(propertyName))
                        {
                            Debug.Log($"Missing material. {material.name}", material);
                            materials.Add(new MissingMaterial() { Material = material, PropertyName = propertyName, Id = textureProperty.objectReferenceInstanceIDValue });
                            onUpdateData.Invoke();
                        }
                        else
                        {
                            Debug.Log($"Shader has not proerty. Property Name:{propertyName}, {material.name}", material);
                        }
                    }
                }
                progress($"{material.name}", percent);

                await Task.Yield();
            }

            progress("Completed", 1f);
        }
    }
}
