using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEditor.SettingsManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace UnityEditor.Splines
{
    /// <summary>
    /// Base class from which all Spline tools inherit.
    /// Inherit SplineTool to author tools that behave like native spline tools. This class implements some common
    /// functionality and shortcuts specific to spline authoring.
    /// </summary>
    abstract class SplineTool : EditorTool
    {
        // TODO: These are temporary, to be removed when spline inspector is complete or when Tool Settings overlay can be extended
        [UserSetting("Manipulation", "Local is element space")]
        internal static readonly UserSetting<bool> k_LocalIsElementSpace = new UserSetting<bool>(PathSettings.instance,"Manipulation.LocalIsElementSpace", false, SettingsScope.User);
        [UserSetting("Manipulation", "Free tangents mode", "When enabled, manipulating tangents does not affect owner knot's rotation.")]
        internal static readonly UserSetting<bool> k_FreeTangentsMode = new UserSetting<bool>(PathSettings.instance, "Manipulation.FreeTangentsMode", false, SettingsScope.User);
        
        internal static InternalEditorBridge.ShortcutContext m_ShortcutContext;

        internal virtual SplineHandlesOptions handlesOptions => SplineHandlesOptions.None;
        
        void RegisterShortcuts()
        {
            m_ShortcutContext = new InternalEditorBridge.ShortcutContext()
            {
                isActive = () => { return true; },
                context = this
            };
            
            InternalEditorBridge.RegisterShortcutContext(m_ShortcutContext);
        }

        void UnregisterShortcuts()
        {
            InternalEditorBridge.UnregisterShortcutContext(m_ShortcutContext);
        }

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
            RegisterShortcuts();
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
            UnregisterShortcuts();
        }
        
        protected virtual void OnPivotRotationChanged()
        {
            TransformOperation.UpdateHandleRotation();
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
                                // To prevent mirroring tangent out against tangent in (which would desync the former from knot's rotation)
                                // the mirroring is done by first rotating the knot, switching to mirrored mode and then scaling tangent out 
                                // which then implicitly mirrors tangent in against scaled tangent out.
                                if (!k_FreeTangentsMode && tangent == owner.tangentIn)
                                {
                                    var tangentInLen = math.length(tangent.localPosition);
                                    var tangentOut = owner.tangentOut;
                                    owner.rotation = Quaternion.FromToRotation(-tangentOut.direction, tangent.direction) * owner.rotation;
                                    owner.SetMode(BezierEditableKnot.Mode.Mirrored);
                                    tangentOut.localPosition = math.normalize(tangentOut.localPosition) * tangentInLen;
                                }
                                else
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

        [Shortcut("Splines/Cycle Tangent Mode", typeof(InternalEditorBridge.ShortcutContext), KeyCode.C)]
        static void ShortcutCycleTangentMode(ShortcutArguments args)
        {
            if (args.context == m_ShortcutContext)
                (m_ShortcutContext.context as SplineTool).CycleTangentMode();
        }
    }
}
