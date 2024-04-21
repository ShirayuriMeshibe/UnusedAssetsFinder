using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ShirayuriMeshibe.EditorUtils
{
    public static class TransformExtension
    {
        public static string FullPath(this Transform transform)
        {
            var parent = transform.parent;
            var stringBuilder = new StringBuilder();

            while(parent != null)
            {
                stringBuilder.Append($"/{parent.name}");
                parent = parent.parent;
            }

            if (0 < stringBuilder.Length)
                stringBuilder.Remove(0, 1);

            return stringBuilder.ToString();
        }
    }
}
