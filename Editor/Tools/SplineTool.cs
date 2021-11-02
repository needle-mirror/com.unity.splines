using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEditor.SettingsManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine.Splines;
using UnityEditor.Overlays;

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

    abstract class SplineToolSettings : UnityEditor.Editor, ICreateToolbar
    {
        public virtual IEnumerable<string> toolbarElements
        {
            get
            {
                yield return "Tool Settings/Pivot Mode";
                yield return "Spline Tool Settings/Handle Rotation";
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
                    if (m_HandleOrientation == HandleOrientation.Local || m_HandleOrientation == HandleOrientation.Global)
                       Tools.pivotRotation = (PivotRotation)m_HandleOrientation.value;

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

        protected virtual void OnPivotRotationChanged()
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
                                owner.SetMode(BezierEditableKnot.Mode.Mirrored);
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
    }
}
