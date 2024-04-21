using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/UsedAssetsFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class UsedAssetsFinderWindowSettings : EditorWindowSettings<UsedAssetsFinderWindowSettings>
    {
        [field: SerializeField] public Property<string> OutputDirectoryPath { get; private set; }
        [field: SerializeField] public Property<SceneAsset[]> Scenes { get; private set; }
        UsedAssetsFinderWindowSettings()
        {
            OutputDirectoryPath = new Property<string>(this, string.Empty);
            Scenes = new Property<SceneAsset[]>(this, null);
        }
    }
}
