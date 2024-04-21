using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/UnusedLayerFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class UnusedLayerFinderWindowSettings : EditorWindowSettings<UnusedLayerFinderWindowSettings>
    {
        [field: SerializeField] public Property<SceneAsset[]> Scenes { get; private set; }
        UnusedLayerFinderWindowSettings()
        {
            Scenes = new Property<SceneAsset[]>(this, null);
        }
    }
}
