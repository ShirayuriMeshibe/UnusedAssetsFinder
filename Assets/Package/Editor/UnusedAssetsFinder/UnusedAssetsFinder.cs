using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    public static class UnusedAssetsFinder
    {
        const string AssetFolderName = "Assets";

        class AssetFile
        {
            public AssetFolder Folder = null;
            public string Path = string.Empty;
            public bool CanRemove = false;
        }

        class AssetFolder
        {
            public AssetFolder Parent = null;
            public string Path = string.Empty;
            public HashSet<AssetFolder> Children = new HashSet<AssetFolder>();
            public HashSet<AssetFile> Files = new HashSet<AssetFile>();
            public bool CanRemove = true;
        }

        public static async Task Find(HashSet<string> usedGuids, Action<string, float> progress, CancellationToken cancellationToken)
        {
            progress("", 0f);
            //AssetDatabase.DisallowAutoRefresh();

            try
            {
                var hierarchy = new SortedDictionary<string, AssetFolder>();
                var leafAssetFolders = new List<AssetFolder>();

                var rootAssetFolder = new AssetFolder() { Parent = null, Path= AssetFolderName, CanRemove=false };
                hierarchy.Add(AssetFolderName, rootAssetFolder);

                await ConstructFolderHierarchy(hierarchy, leafAssetFolders, progress, cancellationToken);
                await FindUnusedFiles(usedGuids, rootAssetFolder, hierarchy, progress, cancellationToken);
                await SearchingUndeletableFolders(leafAssetFolders, progress, cancellationToken);
                await DisplayRemovableAssets(rootAssetFolder, hierarchy, progress, cancellationToken);
            }
            finally
            {
                //AssetDatabase.AllowAutoRefresh();
            }
            progress("Completed", 1f);
        }

        //------------------------------------------------
        // Assets�ɂ���t�H���_����t�H���_�\�����\�z����
        //------------------------------------------------
        static private async Task ConstructFolderHierarchy(IDictionary<string, AssetFolder> hierarchy, List<AssetFolder> leafAssetFolders, Action<string, float> progress, CancellationToken cancellationToken)
        {
            var stack = new Stack<string>();

            var folders = AssetDatabase.GetSubFolders(AssetFolderName);
            foreach (var folder in folders)
                stack.Push(folder);

            while (0 < stack.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var path = stack.Pop();
                progress($"Search target folder:{path}", 0f);

                var parentDirectoryPath = Path.GetDirectoryName(path).Replace("\\", "/");
                hierarchy.TryGetValue(parentDirectoryPath, out var parentAssetFolder);

                var assetFolder = new AssetFolder()
                {
                    Parent = parentAssetFolder,
                    Path = path,
                    CanRemove = true,
                };
                hierarchy.Add(path, assetFolder);

                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file).ToLower();

                    if(".meta" != extension)
                    {
                        var assetFilePath = file.Substring(file.IndexOf(AssetFolderName)).Replace("\\", "/");
                        assetFolder.Files.Add(new AssetFile() { Folder = assetFolder, Path = assetFilePath });
                    }
                }

                if (parentAssetFolder != null)
                    parentAssetFolder.Children.Add(assetFolder);

                folders = AssetDatabase.GetSubFolders(path);

                // ���[�t�H���_�������Ƃ�
                if(folders.Length==0)
                {
                    leafAssetFolders.Add(assetFolder);
                }
                else
                {
                    foreach (var folder in folders)
                        stack.Push(folder);
                }

                await Task.Yield();
            }
            return;
        }

        //--------------------------
        // ���g�p�t�@�C������������
        //--------------------------
        // �������珜�O����g���q��(������)
        static readonly string[] ExcludeFileExtensions = new string[]
        {
            ".shader",
            ".cginc",
            ".hlsl",
            ".asmdef",
            ".asmref",
        };
        static private async Task FindUnusedFiles(HashSet<string> usedGuids, AssetFolder rootAssetFolder, IDictionary<string, AssetFolder> hierarchy, Action<string, float> progress, CancellationToken cancellationToken)
        {
            var stack = new Stack<AssetFolder>();
            stack.Push(rootAssetFolder);

            var i = 1f;
            var count = hierarchy.Keys.Count;
            var stack2 = new Stack<AssetFolder>();
            while (0 < stack.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var folder = stack.Pop();

                progress($"Find folder:{folder.Path}", i / count);

                // Editor�t�H���_�͓���Ȃ̂ŏ��O����
                // ���̃t�H���_�����������悳����
                var splitedPath = folder.Path.Split('/');
                if (0 <= Array.FindIndex(splitedPath, name => "Editor" == name))
                {
                    // Editor�t�H���_�z���͂��ׂč폜���O�ɂ���
                    stack2.Clear();
                    stack2.Push(folder);
                    while (0 < stack2.Count)
                    {
                        var f = stack2.Pop();
                        Debug.Log($"Skip folder: {f.Path}");
                        f.CanRemove = false;
                        foreach (var child in f.Children)
                            stack2.Push(child);
                    }

                    continue;
                }

                foreach (var assetFile in folder.Files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetFile.Path);
                    var guid = AssetDatabase.AssetPathToGUID(assetFile.Path);
                    var extension = Path.GetExtension(assetFile.Path).ToLower();

                    if(0 <= Array.FindIndex(ExcludeFileExtensions, e => e == extension))
                    {
                        folder.CanRemove = false;
                        assetFile.CanRemove = false;
                        continue;
                    }

                    // �t�H���_�A�Z�b�g��r������
                    if (!AssetDatabase.IsValidFolder(assetFile.Path))
                    {
                        // ���g�p�ȃA�Z�b�g
                        if (!usedGuids.Contains(guid))
                        {
                            if(asset is not Shader)
                                assetFile.CanRemove = true;
                        }
                        // �g�p���Ă���A�Z�b�g
                        else
                            folder.CanRemove = false;
                    }
                }

                foreach (var child in folder.Children)
                    stack.Push(child);

                await Task.Yield();
                i++;
            }
        }

        //------------------------------------------------------------------------
        // ���[�t�H���_����e�t�H���_�����ǂ��č폜�ł��Ȃ��t�H���_��ݒ肵�Ă���
        //------------------------------------------------------------------------
        static private async Task SearchingUndeletableFolders(List<AssetFolder> leafAssetFolders, Action<string, float> progress, CancellationToken cancellationToken)
        {
            var i = 1f;
            var count = leafAssetFolders.Count;

            foreach (var leafAssetFolder in leafAssetFolders)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // �폜�ł��Ȃ����[�t�H���_�̂Ƃ��͐e�������̂ڂ��āA�e���폜�ł��Ȃ��悤�ɂ���
                if (!leafAssetFolder.CanRemove)
                {
                    var parent = leafAssetFolder.Parent;
                    while (parent != null)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        parent.CanRemove = false;
                        parent = parent.Parent;
                        //await Task.Yield();
                    }
                }
                progress($"Searching for undeletable folder. {leafAssetFolder.Path}", i / count);
                i++;
                await Task.Yield();
            }
        }

        //------------
        // ���ʂ̕\��
        //------------
        static private async Task DisplayRemovableAssets(AssetFolder rootAssetFolder, IDictionary<string, AssetFolder> hierarchy, Action<string, float> progress, CancellationToken cancellationToken)
        {
            var i = 1f;
            var count = hierarchy.Keys.Count;
            var stackAssetFolders = new Stack<AssetFolder>();
            foreach (var child in rootAssetFolder.Children)
                stackAssetFolders.Push(child);

            while (0 < stackAssetFolders.Count)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var assetFolder = stackAssetFolders.Pop();
                progress($"{assetFolder.Path}", i / count);

                if (assetFolder.CanRemove)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(assetFolder.Path);
                    Debug.Log($"<color=red>[Unused] Can remove folder. {assetFolder.Path}</color>", asset);
                }
                // �폜�ł��Ȃ��t�H���_�̂Ƃ��̓t�@�C����\������
                else
                {
                    Debug.Log($"[Used] {assetFolder.Path}");
                    foreach (var assetFile in assetFolder.Files)
                    {
                        if (assetFile.CanRemove)
                        {
                            var asset = AssetDatabase.LoadMainAssetAtPath(assetFile.Path);
                            if (asset is TextAsset)
                                Debug.Log($"<color=red>[Unused] Can remove file. {assetFile.Path}</color>", asset);
                            else
                                Debug.Log($"<color=red>[Unused] Can remove file. {assetFile.Path}, {asset}</color>", asset);
                        }
                        //await Task.Yield();
                    }

                    // �폜�ł��Ȃ��t�H���_�̎q�t�H���_�����\������
                    foreach (var child in assetFolder.Children)
                        stackAssetFolders.Push(child);
                }

                i++;
                await Task.Yield();
            }
        }
    }
}
