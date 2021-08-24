using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Splines;


namespace UnityEditor.Splines
{
    [Overlay(typeof(SceneView), "Spline Data")]
    internal class SplineDataOverlay : Overlay, ITransientOverlay
    {
        static Color[] k_DefaultSplineColors = new Color[]
        {
            Color.white,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.gray,
            Color.black
        };

        public class SplineDataContainer
        {
            public GameObject owner;
            public List<SplineDataElement> splineDataElements = new List<SplineDataElement>();
        }
        
        public class SplineDataElement
        {
            public FieldInfo splineDataField;
            public MethodInfo drawMethodInfo;
            public MethodInfo drawKeyframeMethodInfo;
            public object customDrawerInstance;
            public MethodInfo initCustomDrawMethodInfo;
            public MethodInfo customSplineDataDrawMethodInfo;
            public MethodInfo customKeyframeDrawMethodInfo;
            public SplineContainer splineContainer = null;
            public Component component = null;
            public bool displayed = true;
            public Color color = Color.white;
            public SplineDataHandles.LabelType labelType = SplineDataHandles.LabelType.None;
        }

        SplineDataList listVisualElement = new SplineDataList();
        
        List<Component> m_Components = new List<Component>();
        List<SplineDataContainer> m_SplineDataContainers = new List<SplineDataContainer>();
        
        public bool visible => m_SplineDataContainers.Count > 0;
        
        public SplineDataOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            OnSelectionChanged();
        }
        
        void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            OnSelectionChanged();
        }
        
        public override void OnCreated()
        {
            Selection.selectionChanged += OnSelectionChanged;
            
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            OnSelectionChanged();
        }
            
        void OnSelectionChanged()
        {
            m_SplineDataContainers.Clear();
            if(Selection.activeGameObject == null)
                return;

            GetSplineDataElements();
            listVisualElement.RebuildMenu(m_SplineDataContainers);
        }

        void GetSplineDataElements()
        {
            var gameObjects = Selection.gameObjects;
            
            SplineDataContainer container = null;
            FieldInfo[] fieldInfos;

            int colorIndex = 0;
            foreach(GameObject go in gameObjects)
            {
                container = null;
                go.GetComponents(typeof(Component), m_Components);

                foreach(Component c in m_Components)
                {
                    if(c == null)
                        continue;
                    
                    fieldInfos = c.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    for(int i = 0; i < fieldInfos.Length; i++)
                    {
                        var fieldType = fieldInfos[i].FieldType;
                        if(fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(SplineData<>))
                        {
                            SplineContainer splineContainer = go.GetComponent<SplineContainer>();
                            
                            //If no spline container on the GameObject, check if the current Component is referencing one
                            if(splineContainer == null)
                            {
                                var fi = fieldInfos.First(field => field.FieldType == typeof(SplineContainer));
                                if(fi.FieldType == typeof(SplineContainer))
                                    splineContainer = (SplineContainer)fi.GetValue(c);
                            }

                            System.Type splineDataType = fieldInfos[i].FieldType.GetTypeInfo().GenericTypeArguments[0];
                            System.Type drawer = typeof(SplineDataHandles);
                            MethodInfo drawerMethod = drawer.GetMethod("DrawSplineDataHandles", BindingFlags.Static | BindingFlags.NonPublic);
                            MethodInfo drawerMethodInfo = drawerMethod.MakeGenericMethod(splineDataType);
                            MethodInfo kfdrawerMethod = drawer.GetMethod("DrawCustomKeyframeHandles", BindingFlags.Static | BindingFlags.NonPublic);
                            MethodInfo keyframeDrawerMethodInfo = kfdrawerMethod.MakeGenericMethod(splineDataType);
                            
                            var splineDataAttribute = fieldInfos[i].GetCustomAttribute<SplineDataDrawerAttribute>();
                            object customDrawerInstance = null;
                            MethodInfo initCustomDrawMethodInfo = null;
                            MethodInfo customSplineDataDrawerMethodInfo = null;
                            MethodInfo customKeyframeDrawerMethodInfo = null;
                            if(splineDataAttribute != null && splineDataAttribute.drawerType != null)
                            {
                                initCustomDrawMethodInfo = 
                                    drawer.GetMethod("InitCustomHandles", 
                                        BindingFlags.Static | BindingFlags.NonPublic
                                    )?.MakeGenericMethod(splineDataType);
                                
                                var customDrawerMethodInfos = splineDataAttribute.drawerType.GetMethods(BindingFlags.Instance|BindingFlags.Public);
                                customSplineDataDrawerMethodInfo = GetCustomSplineDataDrawerMethodInfo(customDrawerMethodInfos, splineDataType);
                                customKeyframeDrawerMethodInfo = GetCustomKeyframeDrawerMethodInfo(customDrawerMethodInfos, splineDataType);
                                
                                var declaringType = customSplineDataDrawerMethodInfo.DeclaringType.IsAbstract ?
                                    customKeyframeDrawerMethodInfo.DeclaringType :
                                    customSplineDataDrawerMethodInfo.DeclaringType;
                                customDrawerInstance = declaringType.Assembly.CreateInstance(declaringType.ToString());
                            }

                            var splineDataElement = new SplineDataElement()
                            {
                                splineDataField = fieldInfos[i],
                                drawMethodInfo = drawerMethodInfo, 
                                drawKeyframeMethodInfo = keyframeDrawerMethodInfo, 
                                splineContainer = splineContainer,
                                component = c,
                                customDrawerInstance = customDrawerInstance,
                                initCustomDrawMethodInfo = initCustomDrawMethodInfo,
                                customSplineDataDrawMethodInfo = customSplineDataDrawerMethodInfo,
                                customKeyframeDrawMethodInfo = customKeyframeDrawerMethodInfo,
                                color = k_DefaultSplineColors[colorIndex++ % k_DefaultSplineColors.Length]
                            };

                            if(container == null)
                            {
                                container = new SplineDataContainer() { owner = go };
                                m_SplineDataContainers.Add(container);
                            }

                            container.splineDataElements.Add(splineDataElement);
                        }
                    }
                }
            }
        }

        public override VisualElement CreatePanelContent()
        {
            return listVisualElement;
        }

        void OnSceneGUI(SceneView view)
        {
            if(!visible)
                return;
            
            foreach(var sdContainer in m_SplineDataContainers)
            {
                foreach(var splineDataElement in sdContainer.splineDataElements)
                {
                    if(!splineDataElement.displayed || splineDataElement.splineContainer == null)
                        continue;
                    
                    splineDataElement.drawMethodInfo?.Invoke(null,
                        new object[]
                        {
                            splineDataElement.splineDataField.GetValue(splineDataElement.component),
                            splineDataElement.splineContainer.Spline,
                            splineDataElement.splineContainer.transform.localToWorldMatrix,
                            splineDataElement.color,
                            splineDataElement.labelType
                        });

                    if(splineDataElement.customDrawerInstance != null)
                    {
                        var splineData = splineDataElement.splineDataField.GetValue(splineDataElement.component);
                        var spline = splineDataElement.splineContainer.Spline;
                        var localToWorld = splineDataElement.splineContainer.transform.localToWorldMatrix;
                        
                        splineDataElement.initCustomDrawMethodInfo?.Invoke(null,
                            new object[]
                            {
                                splineData,
                                splineDataElement.customDrawerInstance
                            });
                        
                        splineDataElement.customSplineDataDrawMethodInfo?.Invoke(splineDataElement.customDrawerInstance,
                            new object[]
                            {
                                splineData,
                                spline,
                                localToWorld,
                                splineDataElement.color
                            });
                        
                        if(splineDataElement.customKeyframeDrawMethodInfo != null)
                        {
                            splineDataElement.drawKeyframeMethodInfo?.Invoke(null,
                                new object[]
                                {
                                    splineData,
                                    spline,
                                    localToWorld,
                                    splineDataElement.color,
                                    splineDataElement.customDrawerInstance,
                                    splineDataElement.customKeyframeDrawMethodInfo
                                });
                        }
                    }
                }
            }
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.white;
        }

        MethodInfo GetCustomSplineDataDrawerMethodInfo(MethodInfo[] methodInfos, System.Type splineDataType)
        {
            foreach(var methodInfo in methodInfos)
            {
                //Checking if the method has the correct name
                if(!methodInfo.Name.Equals("DrawSplineData"))
                    continue;
                                    
                //Checking if the method as parameters
                var parameters = methodInfo.GetParameters();
                if(parameters.Length > 0)
                {
                    //Is the first parameter with a generic type
                    var parameterTypeInfo = parameters[0].ParameterType.GetTypeInfo();
                    if(parameterTypeInfo.IsGenericType && parameterTypeInfo.GetGenericTypeDefinition() == typeof(SplineData<>))
                    {
                        var splineDataTargetType = parameterTypeInfo.GenericTypeArguments[0];
                        if(splineDataTargetType == splineDataType)
                            return methodInfo;
                    }
                }
            }
            return null;
        }
        
        MethodInfo GetCustomKeyframeDrawerMethodInfo(MethodInfo[] methodInfos, System.Type splineDataType)
        {
            foreach(var methodInfo in methodInfos)
            {
                //Checking if the method has the correct name
                if(!methodInfo.Name.Equals("DrawKeyframe"))
                    continue;
                                    
                //Checking if the method as parameters
                var parameters = methodInfo.GetParameters();
                if(parameters.Length == 6)
                {
                    //Is the first parameter with a generic type
                    var parameterTypeInfo = parameters[4].ParameterType.GetTypeInfo();
                    if(parameterTypeInfo.IsGenericType && parameterTypeInfo.GetGenericTypeDefinition() == typeof(SplineData<>))
                    {
                        var splineDataTargetType = parameterTypeInfo.GenericTypeArguments[0];
                        if(splineDataTargetType == splineDataType)
                            return methodInfo;
                    }
                }
            }
            return null;
        }
    }
}