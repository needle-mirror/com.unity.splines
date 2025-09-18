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
    /// Describes how the handles are oriented. Besides the default tool handle rotation settings, Global and Local,
    /// spline elements have the Parent and Element handle rotations. When elements are selected, a tool's handle
    /// rotation setting affects the behavior of some transform tools, such as the Rotate and Scale tools.
    /// </summary>
    public enum HandleOrientation
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
    public abstract class SplineToolSettings : UnityEditor.Editor, ICreateToolbar
    {
        public IEnumerable<string> toolbarElements
        {
#else
    abstract class SplineToolSettings : CreateToolbarBase
    {
        protected override IEnumerable<string> toolbarElements
        {
#endif
            get
            {
                yield return "Tool Settings/Pivot Mode";
                yield return "Spline Tool Settings/Handle Rotation";
#if !UNITY_2022_1_OR_NEWER
                yield return "Spline Tool Settings/Handle Visuals";
#endif
            }
        }
    }

    /// <summary>
    /// Base class from which all Spline tools inherit.
    /// Inherit SplineTool to author tools that behave like native spline tools. This class implements some common
    /// functionality and shortcuts specific to spline authoring.
    /// </summary>
    public abstract class SplineTool : EditorTool
    {
        /// <summary>The current orientation of the handles for the tool in use.</summary>
        static UserSetting<HandleOrientation> m_HandleOrientation = new UserSetting<HandleOrientation>(PathSettings.instance, "SplineTool.HandleOrientation", HandleOrientation.Global, SettingsScope.User);

        /// <summary>The current orientation of the handles for the current spline tool.</summary>
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

        /// <summary>
        /// The current active SplineTool in use.
        /// </summary>
        // Workaround for lack of access to ShortcutContext. Use this to pass shortcut actions to tool instances.
        protected static SplineTool activeTool { get; private set; }

        /// <summary>
        /// The current position of the pivot regarding the selection.
        /// </summary>
        public static Vector3 pivotPosition => TransformOperation.pivotPosition;

        /// <summary>
        /// The current rotation of the handle regarding the selection and the Handle Rotation configuration.
        /// </summary>
        public static Quaternion handleRotation => TransformOperation.handleRotation;

        /// <summary>
        /// Updates the current handle rotation. This is usually called internally by callbacks.
        /// UpdateHandleRotation can be called to refresh the handle rotation after manipulating spline elements, for instance, such as rotating a knot.
        /// </summary>
        public static void UpdateHandleRotation() => TransformOperation.UpdateHandleRotation();

        /// <summary>
        /// Updates current pivot position, usually called internally by callbacks.
        /// It can be called to refresh the pivot position after manipulating spline elements, for instance, such as moving a knot.
        /// </summary>
        /// <param name="useKnotPositionForTangents">
        /// Set to true to use the knots positions to compute the pivot instead of the tangents ones. This is necessary for
        /// some tools where it is preferrable to represent the handle on the knots rather than on the tangents directly.
        /// For instance, rotating a tangent is more intuitive when the handle is on the knot.
        /// </param>
        public static void UpdatePivotPosition(bool useKnotPositionForTangents = false) => TransformOperation.UpdatePivotPosition(useKnotPositionForTangents);

        /// <summary>
        /// Invoked after this EditorTool becomes the active tool.
        /// </summary>
        public override void OnActivated()
        {
            SplineSelection.changed += OnSplineSelectionChanged;
            Spline.afterSplineWasModifiedSceneLoop += AfterSplineWasModified;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Tools.pivotRotationChanged += OnPivotRotationChanged;
            Tools.pivotModeChanged += OnPivotModeChanged;
            TransformOperation.UpdateSelection(targets);
            handleOrientationChanged += OnHandleOrientationChanged;
            activeTool = this;
        }

        /// <summary>
        /// Invoked before this EditorTool stops being the active tool.
        /// </summary>
        public override void OnWillBeDeactivated()
        {
            SplineSelection.changed -= OnSplineSelectionChanged;
            Spline.afterSplineWasModifiedSceneLoop -= AfterSplineWasModified;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Tools.pivotRotationChanged -= OnPivotRotationChanged;
            Tools.pivotModeChanged -= OnPivotModeChanged;
            handleOrientationChanged -= OnHandleOrientationChanged;

            SplineToolContext.useCustomSplineHandles = false;
            activeTool = null;
        }

        /// <summary>
        /// Callback invoked when the handle rotation configuration changes.
        /// </summary>
        protected virtual void OnHandleOrientationChanged()
        {
            UpdateHandleRotation();
        }

        static void OnPivotRotationChanged()
        {
            handleOrientation = (HandleOrientation)Tools.pivotRotation;
        }

        /// <summary>
        /// Callback invoked when the pivot mode configuration changes.
        /// </summary>
        protected virtual void OnPivotModeChanged()
        {
            UpdatePivotPosition();
            UpdateHandleRotation();
        }

        void AfterSplineWasModified(Spline spline) => UpdateSelection();

        void UndoRedoPerformed() => UpdateSelection();

        void OnSplineSelectionChanged()
        {
            UpdateSelection();

            TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
            UpdateHandleRotation();
            UpdatePivotPosition();
        }

        void UpdateSelection()
        {
            TransformOperation.UpdateSelection(targets);
        }

        static void CycleTangentMode()
        {
            var elementSelection = TransformOperation.elementSelection;
            foreach (var element in elementSelection)
            {
                var knot = EditorSplineUtility.GetKnot(element);
                if (element is SelectableTangent tangent)
                {
                    //Do nothing on the tangent if the knot is also in the selection
                    if (elementSelection.Contains(tangent.Owner))
                        continue;

                    bool oppositeTangentSelected = elementSelection.Contains(tangent.OppositeTangent);

                    if (!oppositeTangentSelected)
                    {
                        var newMode = default(TangentMode);
                        var previousMode = knot.Mode;

                        if(!SplineUtility.AreTangentsModifiable(previousMode))
                            continue;

                        if(previousMode == TangentMode.Mirrored)
                            newMode = TangentMode.Continuous;
                        if(previousMode == TangentMode.Continuous)
                            newMode = TangentMode.Broken;
                        if(previousMode == TangentMode.Broken)
                            newMode = TangentMode.Mirrored;

                        knot.SetTangentMode(newMode, (BezierTangent)tangent.TangentIndex);
                        UpdateHandleRotation();
                        // Ensures the tangent mode indicators refresh
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        [Shortcut("Splines/Cycle Tangent Mode", typeof(SceneView), KeyCode.C)]
        static void ShortcutCycleTangentMode(ShortcutArguments args)
        {
            if (activeTool != null)
                CycleTangentMode();
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
