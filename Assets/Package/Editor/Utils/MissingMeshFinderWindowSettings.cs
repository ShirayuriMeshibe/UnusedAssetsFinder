using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/MissingMeshFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class MissingMeshFinderWindowSettings : EditorWindowSettings<MissingMeshFinderWindowSettings>
    {
        //[field: SerializeField] public Property<SceneAsset[]> Scenes { get; private set; }
        MissingMeshFinderWindowSettings()
        {
            //Scenes = new Property<SceneAsset[]>(this, null);
        }
    }
}
