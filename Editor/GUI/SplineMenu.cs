using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine;
using UnityEditor.EditorTools;

namespace UnityEditor.Splines
{
    static class SplineMenu
    {
        const string k_MenuPath = "GameObject/Spline";

        internal static GameObject CreateSplineGameObject(MenuCommand menuCommand, Spline spline = null)
        {
            var name = GameObjectUtility.GetUniqueNameForSibling(null, "Spline");
            var gameObject = ObjectFactory.CreateGameObject(name, typeof(SplineContainer));

#if UNITY_2022_1_OR_NEWER
            ObjectFactory.PlaceGameObject(gameObject, menuCommand.context as GameObject);
#else
            if (menuCommand.context is GameObject go)
            {
                Undo.RecordObject(gameObject.transform, "Re-parenting");
                gameObject.transform.SetParent(go.transform);
            }
#endif

            if (spline != null)
            {
                var container = gameObject.GetComponent<SplineContainer>();
                container.Spline = spline;
            }

            Selection.activeGameObject = gameObject;
            return gameObject;
        }

        const int k_MenuPriority = 10;

        [MenuItem(k_MenuPath + "/Draw Splines Tool...", false, k_MenuPriority + 0)]
        static void CreateNewSpline(MenuCommand menuCommand)
        {
            ToolManager.SetActiveTool<CreateSplineTool>();
        }

        [MenuItem(k_MenuPath + "/Square", false, k_MenuPriority + 11)]
        static void CreateSquare(MenuCommand command)
        {
            CreateSplineGameObject(command, SplineFactory.CreateSquare(1f));
        }

        [MenuItem(k_MenuPath + "/Rounded Square", false, k_MenuPriority + 12)]
        static void CreateRoundedSquare(MenuCommand command)
        {
            CreateSplineGameObject(command, SplineFactory.CreateRoundedCornerSquare(1f, 0.25f));
        }

        [MenuItem(k_MenuPath + "/Circle", false, k_MenuPriority + 13)]
        static void CreateCircle(MenuCommand command)
        {
            CreateSplineGameObject(command, SplineFactory.CreateCircle(0.5f));
        }

        [MenuItem(k_MenuPath + "/Polygon", false, k_MenuPriority + 14)]
        static void CreatePolygon(MenuCommand command)
        {
            var edgeSize = math.sin(math.PI / 6f);
            CreateSplineGameObject(command, SplineFactory.CreatePolygon(edgeSize, 6));
        }

        [MenuItem(k_MenuPath + "/Helix", false, k_MenuPriority + 15)]
        static void CreateHelix(MenuCommand command)
        {
            CreateSplineGameObject(command, SplineFactory.CreateHelix(0.5f, 1f, 1));
        }

        [MenuItem(k_MenuPath + "/Star", false, k_MenuPriority + 16)]
        static void CreateStar(MenuCommand command)
        {
            var edgeSize = math.sin(math.PI / 5f);
            CreateSplineGameObject(command, SplineFactory.CreateStarPolygon(edgeSize, 5, 0.5f));
        }
    }
}
