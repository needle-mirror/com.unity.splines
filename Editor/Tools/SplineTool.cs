using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEditor.SettingsManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine.Splines;
#if UNITY_2022_1_OR_NEWER
using UnityEditor.Overlays;
#else
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
#endif

namespace UnityEditor.Splines
{
    /// <summary>
    /// Describes how the handles are oriented.
    /// </summary>
    enum HandleOrientation
    {
        /// <summary>
        /// Tool handles are in the active object's rotation.
        /// </summary>
        Local = 0,
        /// <summary>
        /// Tool handles are in global rotation.
        /// </summary>
        Global = 1,
        /// <summary>
        /// Tool handles are in active element's parent's rotation.
        /// </summary>
        Parent = 2,
        /// <summary>
        /// Tool handles are in active element's rotation.
        /// </summary>
        Element = 3
    }

#if UNITY_2022_1_OR_NEWER
    abstract class SplineToolSettings : UnityEditor.Editor, ICreateToolbar
#else
    abstract class SplineToolSettings : UnityEditor.Editor
#endif
    {
        public virtual IEnumerable<string> toolbarElements
        {
            get
            {
                yield return "Tool Settings/Pivot Mode";
                yield return "Spline Tool Settings/Handle Rotation";
            }
        }
        
#if !UNITY_2022_1_OR_NEWER
        const string k_ElementClassName = "unity-editor-toolbar-element";
        const string k_StyleSheetsPath = "StyleSheets/Toolbars/";

        static VisualElement CreateToolbar()
        {
            var target = new VisualElement();
            var path = k_StyleSheetsPath + "EditorToolbar";

            var common = EditorGUIUtility.Load($"{path}Common.uss") as StyleSheet;
            if (common != null)
                target.styleSheets.Add(common);

            var themeSpecificName = EditorGUIUtility.isProSkin ? "Dark" : "Light";
            var themeSpecific = EditorGUIUtility.Load($"{path}{themeSpecificName}.uss") as StyleSheet;
            if (themeSpecific != null)
                target.styleSheets.Add(themeSpecific);

            target.AddToClassList("unity-toolbar-overlay");
            target.style.flexDirection = FlexDirection.Row;
            return target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = CreateToolbar();
            
            var elements = TypeCache.GetTypesWithAttribute(typeof(EditorToolbarElementAttribute));
            
            foreach (var element in toolbarElements)
            {
                var type = elements.FirstOrDefault(x =>
                {
                    var attrib = x.GetCustomAttribute<EditorToolbarElementAttribute>();
                    return attrib != null && attrib.id == element;
                });

                if (type != null)
                {
                    try
                    {
                        const BindingFlags flags =  BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.CreateInstance;

                        var ve = (VisualElement)Activator.CreateInstance(type, flags, null, null, null, null);
                        ve.AddToClassList(k_ElementClassName);
                        root.Add(ve);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed creating toolbar element from ID \"{element}\".\n{e}");
                    }
                }
            }

            EditorToolbarUtility.SetupChildrenAsButtonStrip(root);
            
            return root;
        }
#endif
    }

    /// <summary>
    /// Base class from which all Spline tools inherit.
    /// Inherit SplineTool to author tools that behave like native spline tools. This class implements some common
    /// functionality and shortcuts specific to spline authoring.
    /// </summary>
    abstract class SplineTool : EditorTool
    {
        internal virtual SplineHandlesOptions handlesOptions => SplineHandlesOptions.None;

        static UserSetting<HandleOrientation> m_HandleOrientation = new UserSetting<HandleOrientation>(PathSettings.instance, "SplineTool.HandleOrientation", HandleOrientation.Global, SettingsScope.User);

        public static HandleOrientation handleOrientation
        {
            get => m_HandleOrientation;
            set
            {
                if (m_HandleOrientation != value)
                {
                    m_HandleOrientation.SetValue(value, true);
                    if(m_HandleOrientation == HandleOrientation.Local || m_HandleOrientation == HandleOrientation.Global)
                        Tools.pivotRotation = (PivotRotation)m_HandleOrientation.value;
                    else // If setting HandleOrientation to something else, then set the PivotRotation to global, done for GridSnapping button activation 
                    {
                        Tools.pivotRotationChanged -= OnPivotRotationChanged;
                        Tools.pivotRotation = PivotRotation.Local;
                        Tools.pivotRotationChanged += OnPivotRotationChanged;
                    }
                    handleOrientationChanged?.Invoke();
                }
            }
        }

        internal static event Action handleOrientationChanged;

        // Workaround for lack of access to ShortcutContext. Use this to pass shortcut actions to tool instances.
        protected static SplineTool m_ActiveTool;

        /// <summary>
        /// Invoked after this EditorTool becomes the active tool.
        /// </summary>
        public override void OnActivated()
        {
            SplineToolContext.SetHandlesOptions(handlesOptions);
            SplineSelection.changed += OnSplineSelectionChanged;
            Spline.afterSplineWasModified += AfterSplineWasModified;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Tools.pivotRotationChanged += OnPivotRotationChanged;
            Tools.pivotModeChanged += OnPivotModeChanged;
            TransformOperation.UpdateSelection(targets);
            handleOrientationChanged += OnHandleOrientationChanged;
            m_ActiveTool = this;
        }

        /// <summary>
        /// Invoked before this EditorTool stops being the active tool.
        /// </summary>
        public override void OnWillBeDeactivated()
        {
            SplineToolContext.SetHandlesOptions(SplineHandlesOptions.None);
            SplineSelection.changed -= OnSplineSelectionChanged;
            Spline.afterSplineWasModified -= AfterSplineWasModified;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Tools.pivotRotationChanged -= OnPivotRotationChanged;
            Tools.pivotModeChanged -= OnPivotModeChanged;
            handleOrientationChanged -= OnHandleOrientationChanged;
            m_ActiveTool = null;
        }

        protected virtual void OnHandleOrientationChanged()
        {
            TransformOperation.UpdateHandleRotation();
        }

        static void OnPivotRotationChanged()
        {
            handleOrientation = (HandleOrientation)Tools.pivotRotation;
        }

        protected virtual void OnPivotModeChanged()
        {
            TransformOperation.UpdatePivotPosition();
            TransformOperation.UpdateHandleRotation();
        }

        void AfterSplineWasModified(Spline spline) => UpdateSelection();
        void UndoRedoPerformed() => UpdateSelection();

        void OnSplineSelectionChanged()
        {
            TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
            TransformOperation.UpdateHandleRotation();
            TransformOperation.UpdatePivotPosition();

            UpdateSelection();
        }

        void UpdateSelection()
        {
            TransformOperation.UpdateSelection(targets);
        }

        void CycleTangentMode()
        {
            var elementSelection = TransformOperation.elementSelection;
            foreach (var element in elementSelection)
            {
                if (element is EditableTangent tangent)
                {
                    //Do nothing on the tangent if the knot is also in the selection
                    if (elementSelection.Contains(tangent.owner))
                        continue;

                    var oppositeTangentSelected = false;
                    if (tangent.owner is BezierEditableKnot owner)
                    {
                        if(owner.TryGetOppositeTangent(tangent, out var oppositeTangent))
                        {
                            if (elementSelection.Contains(oppositeTangent))
                                oppositeTangentSelected = true;
                        }

                        if (!oppositeTangentSelected)
                        {
                            if (owner.mode == BezierEditableKnot.Mode.Broken)
                            {
                                // Mirror otherTangent against the active tangent prior to SetMode call.
                                // As SetMode always mirrors tangentOut against tangentIn, this prevents an active selection's
                                // tangentOut from shrinking or becoming zero tangent unexpectedly.
                                for (int i = 0; i < owner.tangentCount; ++i)
                                {
                                    var otherTangent = owner.GetTangent(i);
                                    if (otherTangent != tangent)
                                        otherTangent.SetLocalPositionNoNotify(-tangent.localPosition);
                                }

                                owner.SetMode(BezierEditableKnot.Mode.Mirrored);
                            }
                            else if (owner.mode == BezierEditableKnot.Mode.Mirrored)
                                owner.SetMode(BezierEditableKnot.Mode.Continuous);
                            else if (owner.mode == BezierEditableKnot.Mode.Continuous)
                                owner.SetMode(BezierEditableKnot.Mode.Broken);

                            owner.TangentChanged(tangent, owner.mode);
                            TransformOperation.UpdateHandleRotation();

                            // Ensures the tangent mode indicators refresh
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the currently selected active spline.
        /// </summary>
        /// <returns>The active spline.</returns>
        protected virtual IEditableSpline GetActiveSpline()
        {
            IReadOnlyList<IEditableSpline> paths = EditableSplineUtility.GetSelectedSpline(target);
            if (paths == null || paths.Count == 0)
                return null;

            return paths[0];
        }

        [Shortcut("Splines/Cycle Tangent Mode", typeof(SceneView), KeyCode.C)]
        static void ShortcutCycleTangentMode(ShortcutArguments args)
        {
            if(m_ActiveTool != null)
                m_ActiveTool.CycleTangentMode();
        }

        [Shortcut("Splines/Toggle Manipulation Space", typeof(SceneView), KeyCode.X)]
        static void ShortcutCycleHandleOrientation(ShortcutArguments args)
        {
            /* We're doing a switch here (instead of handleOrientation+1 and wrapping) because HandleOrientation.Global/Local values map
               to PivotRotation.Global/Local (as they should), but PivotRotation.Global = 1 when it's actually the first option and PivotRotation.Local = 0 when it's the second option. */ 
            switch (handleOrientation)
            {
                case HandleOrientation.Element:
                    handleOrientation = HandleOrientation.Global;
                    break;
                
                case HandleOrientation.Global:
                    handleOrientation = HandleOrientation.Local;
                    break;
                
                case HandleOrientation.Local:
                    handleOrientation = HandleOrientation.Parent;
                    break;
                
                case HandleOrientation.Parent:
                    handleOrientation = HandleOrientation.Element;
                    break;
                
                default:
                    Debug.LogError($"{handleOrientation} handle orientation not supported!");
                    break;
            }
        }
    }
}
