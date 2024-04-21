using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    [FilePath("UserSettings/UnusedAssetsFinderWindow/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    public sealed class UnusedAssetsFinderWindowSettings : EditorWindowSettings<UnusedAssetsFinderWindowSettings>
    {
        [field: SerializeField] public Property<UsedAssets[]> UnusedAssetsData { get; private set; }
        UnusedAssetsFinderWindowSettings()
        {
            UnusedAssetsData = new Property<UsedAssets[]>(this, null);
        }
    }
}
