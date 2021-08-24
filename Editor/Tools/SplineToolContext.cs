using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [Flags]
    enum SplineHandlesOptions
    {
        ShowTangents = 1 << 0,
        SelectableKnots = 1 << 1,
        SelectableTangents = 1 << 2,
        KnotInsert = 1 << 3,

        SelectableElements = SelectableKnots | SelectableTangents,
        None = 0,
        ManipulationDefault = ShowTangents | SelectableElements
    }

    /// <summary>
    /// Defines a tool context for editing splines. When authoring tools for splines, pass the SplineToolContext type
    /// to the EditorToolAttribute.editorToolContext parameter to register as a spline tool.
    /// </summary>
#if UNITY_2021_2_OR_NEWER
	[EditorToolContext("Spline", typeof(ISplineProvider)), Icon(k_IconPath)]
#else
    [EditorToolContext("Spline", typeof(ISplineProvider))]
#endif
	public sealed class SplineToolContext : EditorToolContext
	{
		const string k_IconPath = "Packages/com.unity.splines/Editor/Resources/Icons/SplineContext.png";

		static SplineHandlesOptions s_SplineHandlesOptions = SplineHandlesOptions.None;
		static bool s_UseCustomSplineHandles = false;

	    readonly SplineElementRectSelector m_RectSelector = new SplineElementRectSelector();
        readonly List<IEditableSpline> m_Splines = new List<IEditableSpline>();
	    readonly List<EditableKnot> m_KnotBuffer = new List<EditableKnot>();

	    internal static void SetHandlesOptions(SplineHandlesOptions options)
	    {
		    s_SplineHandlesOptions = options;
	    }
	    
	    internal static void UseCustomSplineHandles(bool useCustomSplineHandle)
	    {
		    s_UseCustomSplineHandles = useCustomSplineHandle;
	    }

	    /// <summary>
	    /// Returns the matching EditorTool type for the specified Tool given the context.
	    /// </summary>
	    /// <param name="tool">The Tool to resolve to an EditorTool type.</param>
	    /// <returns> An EditorTool type for the requested Tool.</returns>
        protected override Type GetEditorToolType(Tool tool)
		{
			if (tool == Tool.Move)
				return typeof(SplineMoveTool);
			if (tool == Tool.Rotate)
				return typeof(SplineRotateTool);
			if (tool == Tool.Scale)
				return typeof(SplineScaleTool);
			return null;
		}

        /// <summary>
        /// Invoked for each window where this context is active. The spline context uses this method to implement
        /// common functionality for working with splines, ex gizmo drawing and selection.
        /// </summary>
        /// <param name="window"></param>
	    public override void OnToolGUI(EditorWindow window)
	    {
	        m_RectSelector.OnGUI(m_Splines);

	        if(!s_UseCustomSplineHandles)
				SplineHandles.DrawSplineHandles(m_Splines, s_SplineHandlesOptions);
	        
            HandleSelectionFraming();
            HandleDeleteSelectedKnots();
        }

        /// <summary>
        /// Invoked after this EditorToolContext becomes the active tool context.
        /// </summary>
	    public override void OnActivated()
	    {
	        OnSelectionChanged();
	        Selection.selectionChanged += OnSelectionChanged;
	        Spline.afterSplineWasModified += OnSplineWasModified;
	        Undo.undoRedoPerformed += UndoRedoPerformed;

	        SplineConversionUtility.UpdateEditableSplinesForTargets(targets);
        }

        /// <summary>
        /// Invoked before this EditorToolContext stops being the active tool context.
        /// </summary>
	    public override void OnWillBeDeactivated()
	    {
	        Selection.selectionChanged -= OnSelectionChanged;
	        Spline.afterSplineWasModified -= OnSplineWasModified;
	        Undo.undoRedoPerformed -= UndoRedoPerformed;

	        EditableSplineManager.FreeEntireCache();
            SplineSelection.ClearNoUndo(false);
        }

	    void OnSplineWasModified(Spline spline) => UpdateSelection();
	    void OnSelectionChanged() => UpdateSelection();
	    void UndoRedoPerformed() => UpdateSelection();

	    void UpdateSelection()
	    {
		    EditableSplineUtility.GetSelectedSplines(targets, m_Splines);
		    EditableSplineManager.UpdateSelection(targets);
		    SplineSelection.UpdateObjectSelection(targets);
	    }

	    void HandleDeleteSelectedKnots()
	    {
	        Event evt = Event.current;
	        if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Delete)
	        {
	            SplineSelection.GetSelectedKnots(m_KnotBuffer);

	            //Sort knots index so removing them doesn't cause the rest of the indices to be invalid
	            m_KnotBuffer.Sort((a, b) => a.index.CompareTo(b.index));
	            for (int i = m_KnotBuffer.Count - 1; i >= 0; --i)
	            {
	                EditableKnot knot = m_KnotBuffer[i];
	                knot.spline.RemoveKnotAt(knot.index);
	            }
	            evt.Use();
	        }
	    }

	    void HandleSelectionFraming()
	    {
	        if (TransformOperation.canManipulate)
	        {
	            Event evt = Event.current;
	            if (evt.commandName.Equals("FrameSelected"))
	            {
	                var execute = evt.type == EventType.ExecuteCommand;

	                if (evt.type == EventType.ValidateCommand || execute)
	                {
	                    if (execute)
	                    {
	                        var selectionBounds = TransformOperation.GetSelectionBounds(false);
	                        selectionBounds.Encapsulate(TransformOperation.pivotPosition);

	                        var size = selectionBounds.size;
	                        if (selectionBounds.size.x < 1f)
	                            size.x = 1f;
	                        if (selectionBounds.size.y < 1f)
	                            size.y = 1f;
	                        if (selectionBounds.size.z < 1f)
	                            size.z = 1f;
	                        selectionBounds.size = size;

	                        SceneView.lastActiveSceneView.Frame(selectionBounds, false);
	                    }

	                    evt.Use();
	                }
	            }
	        }
	    }
    }
}