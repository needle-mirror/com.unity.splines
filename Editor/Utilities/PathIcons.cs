using UnityEngine;

namespace UnityEditor.Splines
{
    static class PathIcons
    {
        public static GUIContent splineMoveTool = EditorGUIUtility.TrIconContent("MoveTool", "Spline Move Tool");
        public static GUIContent splineRotateTool = EditorGUIUtility.TrIconContent("RotateTool", "Spline Rotate Tool");
        public static GUIContent splineScaleTool = EditorGUIUtility.TrIconContent("ScaleTool", "Spline Scale Tool");

        public static Texture2D GetIcon(string name)
        {
            bool is2x = EditorGUIUtility.pixelsPerPoint > 1;
            bool darkSkin = EditorGUIUtility.isProSkin;
            string path = string.Format($"Packages/com.unity.splines/Editor/Editor Resources/Icons/{(darkSkin ? "d_" : "")}{name}{(is2x ? "@2x" : "")}.png");

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;

            path = string.Format($"Icons/{(darkSkin ? "d_" : "")}{name}");
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                return texture;

            path = string.Format($"Icons/{name}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
