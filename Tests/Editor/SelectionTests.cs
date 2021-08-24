using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

class SelectionTests
{
    class DummyTarget : ScriptableObject {}

    const int k_KnotCount = 5;

    static readonly int[] s_KnotIndex = {0, 2, 4};
    static readonly int[] s_UnusedKnotIndex = {1, 3};
    static readonly BezierTangent[] s_TangentIndex = { BezierTangent.In, BezierTangent.Out};

    ScriptableObject m_DummyTarget;
    IEditableSpline m_Spline;
    EditableSplineManager.TestManagedSpline m_ManagerSpline;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        //Do not reload the scene if already loaded which can be the case with domain reload tests
        if(EditorSceneManager.GetActiveScene().name != "SelectionTestScene")
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var activeScene = EditorSceneManager.GetActiveScene();
            activeScene.name = "SelectionTestScene";
        }
    }
    
    [SetUp]
    public void SetUp()
    {
        m_DummyTarget = ScriptableObject.CreateInstance<DummyTarget>();
        m_Spline = BuildTestPath();
        m_ManagerSpline = new EditableSplineManager.TestManagedSpline(m_DummyTarget, m_Spline);
    }

    IEditableSpline BuildTestPath()
    {
        var path = EditableSplineUtility.CreatePathOfType(SplineType.Bezier);
        ((IEditableSplineConversionData) path).conversionTarget = m_DummyTarget;
        for (int i = 0; i < k_KnotCount; ++i)
            path.AddKnot();
        return path;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(m_DummyTarget);
        SplineSelection.Clear();
        m_ManagerSpline.Dispose();
    }
    
    [Test]
    public void Knot_AddToSelection_KnotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        Assume.That(SplineSelection.Contains(knot), Is.False);

        SplineSelection.Add(knot);

        Assert.That(SplineSelection.Contains(knot), Is.True);
    }

    [Test]
    public void Knot_RemoveFromSelection_KnotNotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        SplineSelection.Remove(knot);

        Assert.That(SplineSelection.Contains(knot), Is.False);
    }

    [Test]
    public void Knot_AddToSelection_Undo_KnotNotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);

        Undo.IncrementCurrentGroup();
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(knot), Is.False);
    }

    [Test]
    public void Knot_RemoveFromSelection_Undo_KnotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        Undo.IncrementCurrentGroup();
        SplineSelection.Remove(knot);
        Assume.That(SplineSelection.Contains(knot), Is.False);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(knot), Is.True);
    }

    [Test]
    public void Knot_RemoveKnotFromPath_KnotIsStillSelected([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        Assume.That(knotIndex, Is.Not.EqualTo(1));
        m_Spline.RemoveKnotAt(1);

        Assert.That(SplineSelection.Contains(knot), Is.True);
    }

    [Test]
    public void Knot_RemoveSelectedKnotInPath_NoZombieSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        m_Spline.RemoveKnotAt(knotIndex);

        Assert.That(SplineSelection.Contains(knot), Is.False);
    }

    [Test]
    public void Knot_InsertKnotInPath_KnotIsStillSelected([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        m_Spline.InsertKnot(1);

        Assert.That(SplineSelection.Contains(knot), Is.True);
    }

    [Test]
    public void Knot_NotSelected_SetActive_AddedFirstInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        foreach (var index in s_UnusedKnotIndex)
            SplineSelection.Add(m_Spline.GetKnot(index));

        var knot = m_Spline.GetKnot(knotIndex);
        Assume.That(SplineSelection.Contains(knot), Is.False);

        SplineSelection.SetActive(knot);

        Assert.That(SplineSelection.Contains(knot), Is.True);
        Assert.That(SplineSelection.IsActiveElement(knot), Is.True);
    }

    [Test]
    public void Knot_Selected_SetActive_FirstInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        foreach (var index in s_UnusedKnotIndex)
            SplineSelection.Add(m_Spline.GetKnot(index));

        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        SplineSelection.SetActive(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        Assert.That(SplineSelection.IsActiveElement(knot), Is.True);
    }

    [Test]
    public void Tangent_AddToSelection_KnotInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        Assume.That(SplineSelection.Contains(tangent), Is.False);

        SplineSelection.Add(tangent);

        Assert.That(SplineSelection.Contains(tangent), Is.True);
    }

    [Test]
    public void Tangent_RemoveFromSelection_KnotInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        SplineSelection.Remove(tangent);

        Assert.That(SplineSelection.Contains(tangent), Is.False);
    }

    [Test]
    public void Tangent_ToSelection_Undo_KnotNotInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);

        Undo.IncrementCurrentGroup();
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(tangent), Is.False);
    }

    [Test]
    public void Tangent_Knot_RemoveFromSelection_Undo_KnotInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        Undo.IncrementCurrentGroup();
        SplineSelection.Remove(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.False);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(tangent), Is.True);
    }

    [Test]
    public void Tangent_RemoveKnotFromPath_KnotIsStillSelected(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        Assume.That(knotIndex, Is.Not.EqualTo(1));
        m_Spline.RemoveKnotAt(1);

        Assert.That(SplineSelection.Contains(tangent), Is.True);
    }

    [Test]
    public void Tangent_RemoveSelectedKnotInPath_NoZombieSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        m_Spline.RemoveKnotAt(knotIndex);

        Assert.That(SplineSelection.Contains(tangent), Is.False);
    }

    [Test]
    public void Tangent_InsertKnotInPath_KnotIsStillSelected(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        m_Spline.InsertKnot(1);

        Assert.That(SplineSelection.Contains(tangent), Is.True);
    }

    [Test]
    public void Tangent_NotSelected_SetActive_AddedToSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        foreach (var index in s_UnusedKnotIndex)
            SplineSelection.Add(m_Spline.GetKnot(index));

        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        Assume.That(SplineSelection.Contains(tangent), Is.False);

        SplineSelection.SetActive(tangent);
        Assert.That(SplineSelection.Contains(tangent), Is.True);
        Assert.That(SplineSelection.IsActiveElement(tangent), Is.True);
    }

    [Test]
    public void Tangent_Selected_SetActive_FirstInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        foreach (var index in s_UnusedKnotIndex)
            SplineSelection.Add(m_Spline.GetKnot(index));

        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        SplineSelection.SetActive(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        Assert.That(SplineSelection.IsActiveElement(tangent), Is.True);
    }

    [Test]
    public void ClearSelection_NoKnotRemains([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        SplineSelection.Clear();

        Assert.That(SplineSelection.Contains(knot), Is.False);
    }

    [Test]
    public void ClearSelection_NoTangentRemains(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        SplineSelection.Clear();

        Assert.That(SplineSelection.Contains(tangent), Is.False);
    }

    [Test]
    public void ClearSelection_Undo_KnotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);
        Undo.IncrementCurrentGroup();

        SplineSelection.Clear();
        Assume.That(SplineSelection.Contains(knot), Is.False);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(knot), Is.True);
    }

    [Test]
    public void ClearSelection_Undo_TangentInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);
        Undo.IncrementCurrentGroup();

        SplineSelection.Clear();
        Assume.That(SplineSelection.Contains(tangent), Is.False);

        Undo.PerformUndo();

        Assert.That(SplineSelection.Contains(tangent), Is.True);
    }

    [Test]
    public void SelectElementsOnOwnerObject_UpdateObjectSelectionWithoutOwner_ElementsRemovedFromSelection()
    {
        foreach (var knotIndex in s_KnotIndex)
        {
            var knot = m_Spline.GetKnot(knotIndex);
            SplineSelection.Add(knot);
            Assume.That(SplineSelection.Contains(knot), Is.True);

            foreach (var tangentIndex in s_TangentIndex)
            {
                var tangent = knot.GetTangent((int)tangentIndex);
                SplineSelection.Add(tangent);
                Assume.That(SplineSelection.Contains(tangent), Is.True);
            }
        }

        List<EditableKnot> knots = new List<EditableKnot>();
        List<EditableTangent> tangents = new List<EditableTangent>();

        var targets = new Object[] {m_DummyTarget};
        SplineSelection.GetSelectedKnots(targets, knots);
        SplineSelection.GetSelectedTangents(targets, tangents);

        Assume.That(knots, Is.Not.Empty);
        Assume.That(tangents, Is.Not.Empty);

        SplineSelection.UpdateObjectSelection(new Object[0]);

        SplineSelection.GetSelectedKnots(targets, knots);
        SplineSelection.GetSelectedTangents(targets, tangents);
        Assert.That(knots, Is.Empty);
        Assert.That(tangents, Is.Empty);
    }

    // Ignore domain reload test on Linux b/c WaitForDomainReload spins indefinitely
    [UnityTest, Platform(Exclude = "Linux")]
    public IEnumerator SelectionSurvivesDomainReload()
    {
        Selection.activeObject = m_DummyTarget;

        var knot = m_Spline.GetKnot(0);
        var tangentIn = m_Spline.GetKnot(0).GetTangent((int) BezierTangent.In);
        var tangentOut = m_Spline.GetKnot(0).GetTangent((int) BezierTangent.Out);

        SplineSelection.Add(knot);
        SplineSelection.Add(tangentIn);
        SplineSelection.Add(tangentOut);

        Assume.That(SplineSelection.Contains(knot), Is.True);
        Assume.That(SplineSelection.Contains(tangentIn), Is.True);
        Assume.That(SplineSelection.Contains(tangentOut), Is.True);

        EditorUtility.RequestScriptReload();
        yield return new WaitForDomainReload();

        Object.DestroyImmediate(m_DummyTarget); //OneTimeSetup and Setup are called again after domain reload
        m_DummyTarget = Resources.FindObjectsOfTypeAll<DummyTarget>()[0];
        m_Spline = BuildTestPath();

        Assert.That(SplineSelection.Contains(m_Spline.GetKnot(0)), Is.True);
        Assert.That(SplineSelection.Contains(m_Spline.GetKnot(0).GetTangent((int)BezierTangent.In)), Is.True);
        Assert.That(SplineSelection.Contains(m_Spline.GetKnot(0).GetTangent((int)BezierTangent.Out)), Is.True);

        Selection.activeObject = null;
    }

    [Test]
    public void SelectKnot_GetSelectedKnots_KnotInSelection([ValueSource(nameof(s_KnotIndex))] int knotIndex)
    {
        var knot = m_Spline.GetKnot(knotIndex);
        SplineSelection.Add(knot);
        Assume.That(SplineSelection.Contains(knot), Is.True);

        List<EditableKnot> knots = new List<EditableKnot>();
        SplineSelection.GetSelectedKnots(new Object[] { m_DummyTarget }, knots);

        Assert.That(knots, Does.Contain(knot));
    }

    [Test]
    public void SelectTangent_GetSelectedTangents_TangentInSelection(
        [ValueSource(nameof(s_KnotIndex))] int knotIndex,
        [ValueSource(nameof(s_TangentIndex))] BezierTangent tangentIndex)
    {
        var tangent = m_Spline.GetKnot(knotIndex).GetTangent((int)tangentIndex);
        SplineSelection.Add(tangent);
        Assume.That(SplineSelection.Contains(tangent), Is.True);

        List<EditableTangent> tangents = new List<EditableTangent>();
        SplineSelection.GetSelectedTangents(new Object[] {m_DummyTarget}, tangents);

        Assert.That(tangents, Does.Contain(tangent));
    }
}
