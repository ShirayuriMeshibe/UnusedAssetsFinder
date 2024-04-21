using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/MissingShaderFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class MissingShaderFinderWindowSettings : EditorWindowSettings<MissingShaderFinderWindowSettings>
    {
        [field: SerializeField] public Property<SceneAsset[]> Scenes { get; private set; }
        MissingShaderFinderWindowSettings()
        {
            Scenes = new Property<SceneAsset[]>(this, null);
        }
    }
}
