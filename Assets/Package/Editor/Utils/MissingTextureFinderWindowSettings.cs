using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/MissingTextureFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class MissingTextureFinderWindowSettings : EditorWindowSettings<MissingTextureFinderWindowSettings>
    {
        [field: SerializeField] public Property<SceneAsset[]> Scenes { get; private set; }
        MissingTextureFinderWindowSettings()
        {
            Scenes = new Property<SceneAsset[]>(this, null);
        }
    }
}
