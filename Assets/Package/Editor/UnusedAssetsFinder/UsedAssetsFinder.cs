using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace ShirayuriMeshibe.EditorUtils
{
    public static class UsedAssetsFinder
    {
        static public async Task<HashSet<string>> Clean(SceneAsset[] scenes, Action<float> progress, CancellationToken cancellationToken)
        {
            progress(0f);

            var guids = new HashSet<string>();

            // .unityシーンアセットも使用済みとして追加する
            var scenesPathList = new List<string>();
            foreach (var scene in scenes)
            {
                if (cancellationToken.IsCancellationRequested)
                    return guids;
                var path = AssetDatabase.GetAssetPath(scene);
                scenesPathList.Add(path);
                guids.Add(AssetDatabase.AssetPathToGUID(path));
            }

            var dependencies = AssetDatabase.GetDependencies(scenesPathList.ToArray());
            for(int i=0; i < dependencies.Length; ++i)
            {
                if (cancellationToken.IsCancellationRequested)
                    return guids;
                var dependency = dependencies[i];
                guids.Add(AssetDatabase.AssetPathToGUID(dependency));
                progress(((float)i+1)/ dependencies.Length);
                await Task.Yield();
            }

            progress(1f);
            return guids;
        }
    }
}
