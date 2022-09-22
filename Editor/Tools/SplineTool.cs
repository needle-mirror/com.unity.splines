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
                yield return "Spline Tool Settings/Handle Visuals";
            }
        }
    }

    /// <summary>
    /// Base class from which all Spline tools inherit.
    /// Inherit SplineTool to author tools that behave like native spline tools. This class implements some common
    /// functionality and shortcuts specific to spline authoring.
    /// </summary>
    abstract class SplineTool : EditorTool
    {
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
        protected static SplineTool activeTool { get; private set; }

        /// <summary>
        /// Invoked after this EditorTool becomes the active tool.
        /// </summary>
        public override void OnActivated()
        {
            SplineSelection.changed += OnSplineSelectionChanged;
            Spline.afterSplineWasModified += AfterSplineWasModified;
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
            Spline.afterSplineWasModified -= AfterSplineWasModified;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Tools.pivotRotationChanged -= OnPivotRotationChanged;
            Tools.pivotModeChanged -= OnPivotModeChanged;
            handleOrientationChanged -= OnHandleOrientationChanged;
            activeTool = null;
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
            UpdateSelection();

            TransformOperation.pivotFreeze = TransformOperation.PivotFreeze.None;
            TransformOperation.UpdateHandleRotation();
            TransformOperation.UpdatePivotPosition();
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

                        if(!EditorSplineUtility.AreTangentsModifiable(previousMode))
                            continue;

                        if(previousMode == TangentMode.Mirrored)
                            newMode = TangentMode.Continuous;
                        if(previousMode == TangentMode.Continuous)
                            newMode = TangentMode.Broken;
                        if(previousMode == TangentMode.Broken)
                            newMode = TangentMode.Mirrored;

                        knot.SetTangentMode(newMode, (BezierTangent)tangent.TangentIndex);
                        TransformOperation.UpdateHandleRotation();
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