using System.Linq;
using UnityEditor;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;

class SplineInstantiateGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawSplineInstantiateGizmos(SplineInstantiate scr, GizmoType gizmoType)
    {
        var instances = scr.instances;

        foreach(var instance in instances)
        {
            var pos = instance.transform.position;
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(3f,new []{ pos, pos + 0.25f * instance.transform.right });
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(3f,new []{pos, pos + 0.25f * instance.transform.up});
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(3f,new []{pos, pos + 0.25f * instance.transform.forward});
        }
    }
}

[CustomPropertyDrawer (typeof(SplineInstantiate.InstantiableItem))]
class InstantiableItemDrawer : PropertyDrawer
{
    static readonly string k_ProbabilityTooltip = L10n.Tr("Probability for that element to appear.");

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        var prefabProperty = property.FindPropertyRelative(nameof(SplineInstantiate.InstantiableItem.Prefab));
        var probaProperty = property.FindPropertyRelative(nameof(SplineInstantiate.InstantiableItem.Probability));

        var headerLine = ReserveSpace(EditorGUIUtility.singleLineHeight, ref rect);

        using(new SplineInstantiateEditor.LabelWidthScope(0f))
            EditorGUI.ObjectField(ReserveLineSpace(headerLine.width - 100, ref headerLine), prefabProperty, new GUIContent(""));

        ReserveLineSpace(10, ref headerLine);
        EditorGUI.LabelField(ReserveLineSpace(15, ref headerLine), new GUIContent("%", k_ProbabilityTooltip));
        probaProperty.floatValue = EditorGUI.FloatField(ReserveLineSpace(60, ref headerLine), probaProperty.floatValue);
    }

    static Rect ReserveSpace(float height, ref Rect total)
    {
        Rect current = total;
        current.height = height;
        total.y += height;
        return current;
    }

    static Rect ReserveLineSpace(float width, ref Rect total)
    {
        Rect current = total;
        current.width = width;
        total.x += width;
        return current;
    }
}

[CustomPropertyDrawer (typeof(SplineInstantiate.AlignAxis))]
class ItemAxisDrawer : PropertyDrawer
{
    static int s_LastUpAxis;

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        var enumValue = property.intValue;

        if(property.name == "m_Up")
        {
            property.intValue = (int)( (SplineInstantiate.AlignAxis)EditorGUI.EnumPopup(rect, label, (SplineInstantiate.AlignAxis)enumValue));
            s_LastUpAxis = property.intValue;
        }
        else
        {
            property.intValue = (int)((SplineInstantiate.AlignAxis)EditorGUI.EnumPopup(rect, label, (SplineInstantiate.AlignAxis)enumValue,
                (item) =>
                {
                    int axisItem = (int)(SplineInstantiate.AlignAxis)item;
                    return !(axisItem == s_LastUpAxis || axisItem == (s_LastUpAxis + 3) % 6);
                }));
        }
    }
}

[CustomEditor(typeof(SplineInstantiate),false)]
[CanEditMultipleObjects]
class SplineInstantiateEditor : SplineComponentEditor
{
    enum SpawnType
    {
        Exact,
        Random
    }

    SerializedProperty m_SplineContainer;

    SerializedProperty m_ItemsToInstantiate;
    SerializedProperty m_InstantiateMethod;

    SerializedProperty m_Seed;
    SerializedProperty m_Space;
    SerializedProperty m_UpAxis;
    SerializedProperty m_ForwardAxis;
    SerializedProperty m_Spacing;
    SerializedProperty m_PositionOffset;
    SerializedProperty m_RotationOffset;
    SerializedProperty m_ScaleOffset;
    SerializedProperty m_AutoRefresh;

    static readonly string[] k_SpacingTypesLabels = new []
    {
        L10n.Tr("Count"),
        L10n.Tr("Spacing (Spline)"),
        L10n.Tr("Spacing (Linear)")
    };

    //Setup Section
    static readonly string k_Setup = L10n.Tr("Instantiated Object Setup");
    static readonly string k_ObjectUp = L10n.Tr("Up Axis");
    static readonly string k_ObjectUpTooltip = L10n.Tr("Object axis to use as Up Direction when instantiating on the Spline (default is Y).");
    static readonly string k_ObjectForward = L10n.Tr("Forward Axis");
    static readonly string k_ObjectForwardTooltip = L10n.Tr("Object axis to use as Forward Direction when instantiating on the Spline (default is Z).");
    static readonly string k_AlignTo = L10n.Tr("Align To");
    static readonly string k_AlignToTooltip = L10n.Tr("Define the space to use to orientate the instantiated object.");

    static readonly string k_Instantiation = L10n.Tr("Instantiation");
    static readonly string k_Method = L10n.Tr("Instantiate Method");
    static readonly string k_MethodTooltip = L10n.Tr("How instances are generated along the spline.");
    static readonly string k_Max = L10n.Tr("Max");
    static readonly string k_Min = L10n.Tr("Min");

    SpawnType m_SpacingType;

    //Offsets
    static readonly string k_PositionOffset = L10n.Tr("Position Offset");
    static readonly string k_PositionOffsetTooltip = L10n.Tr("Whether or not to use a position offset.");
    static readonly string k_RotationOffset = L10n.Tr("Rotation Offset");
    static readonly string k_RotationOffsetTooltip = L10n.Tr("Whether or not to use a rotation offset.");
    static readonly string k_ScaleOffset = L10n.Tr("Scale Offset");
    static readonly string k_ScaleOffsetTooltip = L10n.Tr("Whether or not to use a scale offset.");

    //Generation
    static readonly string k_Generation = L10n.Tr("Generation");
    static readonly string k_AutoRefresh = L10n.Tr("Auto Refresh Generation");
    static readonly string k_AutoRefreshTooltip = L10n.Tr("Automatically refresh the instances when the spline or the values are changed.");

    static readonly string k_Seed = L10n.Tr("Randomization Seed");
    static readonly string k_SeedTooltip = L10n.Tr("Value used to initialize the pseudorandom number generator of the instances.");
    
    static readonly string k_Randomize = L10n.Tr("Randomize");
    static readonly string k_RandomizeTooltip = L10n.Tr("Compute a new randomization of the instances along the spline.");
    static readonly string k_Regenerate = L10n.Tr("Regenerate");
    static readonly string k_RegenerateTooltip = L10n.Tr("Regenerate the instances along the spline.");
    static readonly string k_Clear = L10n.Tr("Clear");
    static readonly string k_ClearTooltip = L10n.Tr("Clear the instances along the spline.");
    static readonly string k_Bake = L10n.Tr("Bake Instances");
    static readonly string k_BakeTooltip = L10n.Tr("Bake the instances in the SceneView for custom edition and destroy that SplineInstantiate component.");

    bool m_PositionFoldout;
    bool m_RotationFoldout;
    bool m_ScaleFoldout;

    enum OffsetType
    {
        Exact,
        Random
    };

    SplineInstantiate[] m_Components;

    SplineInstantiate[] components
    {
        get
        {
            //in case of multiple selection where some objects do not have a SplineInstantiate component, m_Components might be null
            if (m_Components == null)
                m_Components = targets.Select(x => x as SplineInstantiate).Where(y => y != null).ToArray();
            
            return m_Components;
        }
    }

    protected void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
        EditorSplineUtility.AfterSplineWasModified += OnSplineModified;
        SplineContainer.SplineAdded += OnContainerSplineSetModified;
        SplineContainer.SplineRemoved += OnContainerSplineSetModified;
    }

    bool Initialize()
    {
        if (m_Components != null && m_Components.Length > 0) 
            return true;
     
        m_SplineContainer = serializedObject.FindProperty("m_Container");

        m_ItemsToInstantiate = serializedObject.FindProperty("m_ItemsToInstantiate");
        m_InstantiateMethod = serializedObject.FindProperty("m_Method");

        m_Space = serializedObject.FindProperty("m_Space");
        m_UpAxis = serializedObject.FindProperty("m_Up");
        m_ForwardAxis = serializedObject.FindProperty("m_Forward");

        m_Spacing = serializedObject.FindProperty("m_Spacing");

        m_PositionOffset = serializedObject.FindProperty("m_PositionOffset");
        m_RotationOffset = serializedObject.FindProperty("m_RotationOffset");
        m_ScaleOffset = serializedObject.FindProperty("m_ScaleOffset");
        m_Seed = serializedObject.FindProperty("m_Seed");

        m_AutoRefresh = serializedObject.FindProperty("m_AutoRefresh");
        
        if (m_Spacing != null)
            m_SpacingType = Mathf.Approximately(m_Spacing.vector2Value.x, m_Spacing.vector2Value.y) ? SpawnType.Exact : SpawnType.Random;
        else
            m_SpacingType = SpawnType.Exact;

        m_Components = targets.Select(x => x as SplineInstantiate).Where(y => y != null).ToArray();

        return m_Components != null && m_Components.Length > 0;
    }

    void OnDisable()
    {
        m_Components = null;
        
        Spline.Changed -= OnSplineChanged;
        EditorSplineUtility.AfterSplineWasModified -= OnSplineModified;
        SplineContainer.SplineAdded -= OnContainerSplineSetModified;
        SplineContainer.SplineRemoved -= OnContainerSplineSetModified;
    }

    void OnSplineModified(Spline spline)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        
        foreach (var instantiate in components)
        {
            if(instantiate == null)
                continue;
            if (instantiate.Container != null && instantiate.Container.Splines.Contains(spline))
                instantiate.SetSplineDirty(spline);
        }
    }

    void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
    {
        OnSplineModified(spline);
    }

    void OnContainerSplineSetModified(SplineContainer container, int spline)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        foreach (var instantiate in components)
        {
            if (instantiate.Container == container)
                instantiate.UpdateInstances();
        }
    }

    public override void OnInspectorGUI()
    {
        if(!Initialize())
            return;
        
        serializedObject.Update();

        var splineInstantiate = ((SplineInstantiate)target);
        var dirtyInstances = false;
        var updateInstances = false;

        EditorGUILayout.PropertyField(m_SplineContainer);
        if(m_SplineContainer.objectReferenceValue == null)
            EditorGUILayout.HelpBox(k_Helpbox, MessageType.Warning);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_ItemsToInstantiate);
        dirtyInstances = EditorGUI.EndChangeCheck();
        
        DoSetupSection();
        dirtyInstances |= DoInstantiateSection();
        updateInstances |= DisplayOffsets();

        EditorGUILayout.LabelField(k_Generation, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_Seed, new GUIContent(k_Seed, k_SeedTooltip));
        var newSeed = EditorGUI.EndChangeCheck();
        dirtyInstances |= newSeed;
        updateInstances |= newSeed;
        EditorGUILayout.PropertyField(m_AutoRefresh, new GUIContent(k_AutoRefresh, k_AutoRefreshTooltip));
        EditorGUI.indentLevel--;
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent(k_Randomize, k_RandomizeTooltip), GUILayout.MaxWidth(100f)))
        {            
            Undo.RecordObjects(targets, "Changing SplineInstantiate seed");
            splineInstantiate.Randomize();
            updateInstances = true;
        }

        if (splineInstantiate.instances.Count == 0)
        {
            if (GUILayout.Button(new GUIContent(k_Regenerate, k_RegenerateTooltip), GUILayout.MaxWidth(100f)))
                updateInstances = true;
        }
        else
        {
            if (GUILayout.Button(new GUIContent(k_Clear, k_ClearTooltip), GUILayout.MaxWidth(100f)))
                splineInstantiate.Clear();
        }

        GUI.enabled = splineInstantiate.instances.Count > 0;
        if (GUILayout.Button(new GUIContent(k_Bake, k_BakeTooltip), GUILayout.MaxWidth(100f)))
            BakeInstances(splineInstantiate);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();

        if (dirtyInstances)
            splineInstantiate.SetDirty();

        if (updateInstances)
            splineInstantiate.UpdateInstances();
        
        if (dirtyInstances || updateInstances)
            SceneView.RepaintAll();
    }

    void DoSetupSection()
    {
        EditorGUILayout.LabelField(k_Setup, EditorStyles.boldLabel);
        GUILayout.Space(5f);
        EditorGUI.indentLevel++;

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(m_UpAxis, new GUIContent(k_ObjectUp, k_ObjectUpTooltip));
        EditorGUILayout.PropertyField(m_ForwardAxis, new GUIContent(k_ObjectForward, k_ObjectForwardTooltip));

        if(EditorGUI.EndChangeCheck())
        {
            //Insuring axis integrity
            if(m_ForwardAxis.intValue == m_UpAxis.intValue || m_ForwardAxis.intValue == ( m_UpAxis.intValue + 3 ) % 6)
                m_ForwardAxis.intValue = ( m_ForwardAxis.intValue + 1 ) % 6;
        }

        EditorGUILayout.PropertyField(m_Space, new GUIContent(k_AlignTo, k_AlignToTooltip));
        EditorGUI.indentLevel--;
    }

    bool DoInstantiateSection()
    {
        var dirty = false;
        Vector2 spacingV2 = m_Spacing.vector2Value;

        EditorGUILayout.LabelField(k_Instantiation, EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_InstantiateMethod, new GUIContent(k_Method, k_MethodTooltip), EditorStyles.boldFont );

        if(EditorGUI.EndChangeCheck())
        {
            if(m_SpacingType == SpawnType.Random && m_InstantiateMethod.intValue == (int)SplineInstantiate.Method.LinearDistance)
                m_Spacing.vector2Value = new Vector2(spacingV2.x, float.NaN);
            dirty = true;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent(k_SpacingTypesLabels[m_InstantiateMethod.intValue]));
        EditorGUI.indentLevel--;

        GUILayout.Space(2f);

        EditorGUI.BeginChangeCheck();

        float spacingX = m_Spacing.vector2Value.x;
        var isExact = m_SpacingType == SpawnType.Exact;
        if(isExact || m_InstantiateMethod.intValue != (int)SplineInstantiate.Method.LinearDistance)
        {
           using(new LabelWidthScope(30f))
                spacingX = (SplineInstantiate.Method)m_InstantiateMethod.intValue == SplineInstantiate.Method.InstanceCount ?
                    EditorGUILayout.IntField(new GUIContent(isExact ? string.Empty : k_Min), (int)m_Spacing.vector2Value.x, GUILayout.MinWidth(50f)) :
                    EditorGUILayout.FloatField(new GUIContent(isExact ? L10n.Tr("Dist") : k_Min), m_Spacing.vector2Value.x, GUILayout.MinWidth(50f));
        }
        if(isExact)
        {
            spacingV2 = new Vector2(spacingX, spacingX);
        }
        else if(m_InstantiateMethod.intValue != (int)SplineInstantiate.Method.LinearDistance)
        {
            using(new LabelWidthScope(30f))
            {
                var spacingY = (SplineInstantiate.Method)m_InstantiateMethod.intValue == SplineInstantiate.Method.InstanceCount ?
                    EditorGUILayout.IntField(new GUIContent(k_Max), (int)m_Spacing.vector2Value.y, GUILayout.MinWidth(50f)) :
                    EditorGUILayout.FloatField(new GUIContent(k_Max), m_Spacing.vector2Value.y, GUILayout.MinWidth(50f));

                if(spacingX > m_Spacing.vector2Value.y)
                    spacingY = spacingX;
                else if(spacingY < m_Spacing.vector2Value.x)
                    spacingX = spacingY;

                spacingV2 = new Vector2(spacingX, spacingY);
            }
        }

        if(EditorGUI.EndChangeCheck())
            m_Spacing.vector2Value = spacingV2;

        EditorGUI.BeginChangeCheck();
        if(m_InstantiateMethod.intValue != (int)SplineInstantiate.Method.LinearDistance)
            m_SpacingType = (SpawnType)EditorGUILayout.EnumPopup(m_SpacingType, GUILayout.MinWidth(30f));
        else
            m_SpacingType = (SpawnType)EditorGUILayout.Popup(m_SpacingType == SpawnType.Exact ? 0 : 1,
                new []{"Exact", "Auto"}, GUILayout.MinWidth(30f));

        if(EditorGUI.EndChangeCheck())
        {
            if(m_SpacingType == SpawnType.Exact)
                m_Spacing.vector2Value = new Vector2(spacingV2.x, spacingV2.x);
            else if(m_InstantiateMethod.intValue == (int)SplineInstantiate.Method.LinearDistance)
                m_Spacing.vector2Value = new Vector2(spacingV2.x, float.NaN);

            dirty = true;
        }

        EditorGUILayout.EndHorizontal();

        return dirty;
    }

    bool DoOffsetProperties(
        SerializedProperty offsetProperty, GUIContent content, bool foldoutValue, out bool newFoldoutValue)
    {
        bool changed = false;
        newFoldoutValue = foldoutValue;

        EditorGUILayout.BeginHorizontal();

        using(new LabelWidthScope(0f))
        {
            var setupProperty = offsetProperty.FindPropertyRelative("setup");
            var setup = (SplineInstantiate.Vector3Offset.Setup)setupProperty.intValue;

            var hasOffset = ( setup & SplineInstantiate.Vector3Offset.Setup.HasOffset ) != 0;
            EditorGUI.BeginChangeCheck();
            hasOffset = EditorGUILayout.Toggle(hasOffset, GUILayout.MaxWidth(20f));
            if(EditorGUI.EndChangeCheck())
            {
                if(hasOffset)
                    setup |= SplineInstantiate.Vector3Offset.Setup.HasOffset;
                else
                    setup &= ~SplineInstantiate.Vector3Offset.Setup.HasOffset;

                setupProperty.intValue = (int)setup;
                changed = true;
            }

            EditorGUILayout.Space(10f);
            using(new EditorGUI.DisabledScope(!hasOffset))
            {
                newFoldoutValue = Foldout(foldoutValue, content, hasOffset) && hasOffset;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if(newFoldoutValue)
                {
                    EditorGUILayout.BeginHorizontal();

                    var hasCustomSpace = ( setup & SplineInstantiate.Vector3Offset.Setup.HasCustomSpace ) != 0;
                    EditorGUI.BeginChangeCheck();
                    var space = m_Space.intValue < 1 ? "Spline Element" : m_Space.intValue == 1 ? "Spline Object" : "World";
                    hasCustomSpace = EditorGUILayout.Toggle(new GUIContent("Override space", L10n.Tr("Override current space (" + space + ")")), hasCustomSpace);
                    if(EditorGUI.EndChangeCheck())
                    {
                        if(hasCustomSpace)
                            setup |= SplineInstantiate.Vector3Offset.Setup.HasCustomSpace;
                        else
                            setup &= ~SplineInstantiate.Vector3Offset.Setup.HasCustomSpace;

                        setupProperty.intValue = (int)setup;
                        changed = true;
                    }

                    var spaceProperty = offsetProperty.FindPropertyRelative("space");
                    using(new EditorGUI.DisabledScope(!hasCustomSpace))
                    {
                        var type = (SplineInstantiate.OffsetSpace)spaceProperty.intValue;
                        EditorGUI.BeginChangeCheck();
                        type = (SplineInstantiate.OffsetSpace)EditorGUILayout.EnumPopup(type);
                        if(EditorGUI.EndChangeCheck())
                        {
                            spaceProperty.intValue = (int)type;
                            changed = true;
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    var minProperty = offsetProperty.FindPropertyRelative("min");
                    var maxProperty = offsetProperty.FindPropertyRelative("max");

                    var minPropertyValue = minProperty.vector3Value;
                    var maxPropertyValue = maxProperty.vector3Value;

                    float min, max;
                    SerializedProperty randomProperty;
                    for(int i = 0; i < 3; i++)
                    {
                        string label = i == 0 ? "X" : i == 1 ? "Y" : "Z";
                        EditorGUILayout.BeginHorizontal();
                        using(new LabelWidthScope(30f))
                            EditorGUILayout.LabelField(label);
                        randomProperty = offsetProperty.FindPropertyRelative("random"+label);
                        GUILayout.FlexibleSpace();
                        if(randomProperty.boolValue)
                        {
                            EditorGUI.BeginChangeCheck();
                            using(new LabelWidthScope(30f))
                            {
                                min = EditorGUILayout.FloatField("from", minPropertyValue[i], GUILayout.MinWidth(95f), GUILayout.MaxWidth(95f));
                                max = EditorGUILayout.FloatField("  to", maxPropertyValue[i], GUILayout.MinWidth(95f), GUILayout.MaxWidth(95f));
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                if(min > maxPropertyValue[i])
                                    maxPropertyValue[i] = min;
                                if(max < minPropertyValue[i])
                                    minPropertyValue[i] = max;

                                minPropertyValue[i] = min;
                                maxPropertyValue[i] = max;

                                minProperty.vector3Value = minPropertyValue;
                                maxProperty.vector3Value = maxPropertyValue;
                                changed = true;
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();
                            using(new LabelWidthScope(30f))
                                min = EditorGUILayout.FloatField("is ", minPropertyValue[i], GUILayout.MinWidth(193f), GUILayout.MaxWidth(193f));

                            if(EditorGUI.EndChangeCheck())
                            {
                                minPropertyValue[i] = min;
                                if(min > maxPropertyValue[i])
                                    maxPropertyValue[i] = min;

                                minProperty.vector3Value = minPropertyValue;
                                maxProperty.vector3Value = maxPropertyValue;
                                changed = true;
                            }
                        }

                        EditorGUI.BeginChangeCheck();
                        var isOffsetRandom = randomProperty.boolValue ? OffsetType.Random : OffsetType.Exact;
                        using(new LabelWidthScope(0f))
                            isOffsetRandom = (OffsetType)EditorGUILayout.EnumPopup(isOffsetRandom,GUILayout.MinWidth(100f), GUILayout.MaxWidth(200f));
                        if(EditorGUI.EndChangeCheck())
                        {
                            randomProperty.boolValue = isOffsetRandom == OffsetType.Random;
                            changed = true;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        return changed;
    }

    bool DisplayOffsets()
    {
        var updateNeeded = DoOffsetProperties(m_PositionOffset, new GUIContent(k_PositionOffset, k_PositionOffsetTooltip), m_PositionFoldout, out m_PositionFoldout);
        updateNeeded |=  DoOffsetProperties(m_RotationOffset, new GUIContent(k_RotationOffset, k_RotationOffsetTooltip), m_RotationFoldout, out m_RotationFoldout);
        updateNeeded |= DoOffsetProperties(m_ScaleOffset, new GUIContent(k_ScaleOffset, k_ScaleOffsetTooltip), m_ScaleFoldout, out m_ScaleFoldout);

        return updateNeeded;
    }
    
    /// <summary>
    /// Bake the instances into the scene and destroy this SplineInstantiate component.
    /// Making changes to the spline after baking will not affect the instances anymore.
    /// </summary>
    void BakeInstances(SplineInstantiate splineInstantiate)
    {
        Undo.SetCurrentGroupName("Baking SplineInstantiate instances");
        var group = Undo.GetCurrentGroup();
        
        splineInstantiate.UpdateInstances();
        for (int i = 0; i < splineInstantiate.instances.Count; ++i)
        {
            var newInstance = splineInstantiate.instances[i];
            newInstance.name = "Instance-" + i;
            newInstance.hideFlags = HideFlags.None;
            newInstance.transform.SetParent(((SplineInstantiate)target).gameObject.transform, true);
            
            Undo.RegisterCreatedObjectUndo(newInstance, "Baking instance");
        }
        
        splineInstantiate.instances.Clear();
        if(splineInstantiate.InstancesRoot != null)
            Undo.DestroyObjectImmediate(splineInstantiate.InstancesRoot);
        
        Undo.DestroyObjectImmediate(splineInstantiate);
        
        Undo.CollapseUndoOperations(group);
    }
}
