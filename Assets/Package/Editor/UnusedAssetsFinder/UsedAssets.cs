using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    public sealed class UsedAssets : ScriptableObject
    {
        [SerializeField] private string[] _guids = null;

        public static void Create(string directoryPath, HashSet<string> guids)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                directoryPath = "Assets/";

            var filename = $"{directoryPath}/UseAssets.asset";
            var outputPath = AssetDatabase.GenerateUniqueAssetPath(filename);

            var usedAssets = CreateInstance<UsedAssets>();
            usedAssets._guids = guids.ToArray();

            AssetDatabase.CreateAsset(usedAssets, outputPath);
            EditorGUIUtility.PingObject(usedAssets);
        }

        public string[] Guids => _guids;
    }
}
