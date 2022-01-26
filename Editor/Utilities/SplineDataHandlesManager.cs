using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SettingsManagement;
using UnityEngine;
using UnityEngine.Splines;

namespace UnityEditor.Splines
{
    [InitializeOnLoad]
    static class SplineDataHandlesManager //: ScriptableSingleton<SplineDataHandlesManager>
    {
        // Using by default colors of maximal contrast from 
        // https://medium.com/@rjurney/kellys-22-colours-of-maximum-contrast-58edb70c90d1
        static UserSetting<Color> s_Color0 = new UserSetting<Color>(PathSettings.instance, "SplineData.color0", new Color32(0, 130, 200, 255), SettingsScope.User); // Blue
        static UserSetting<Color> s_Color1 = new UserSetting<Color>(PathSettings.instance, "SplineData.color1", new Color32(245, 130, 48, 255), SettingsScope.User); // Orange
        static UserSetting<Color> s_Color2 = new UserSetting<Color>(PathSettings.instance, "SplineData.color2", new Color32(60, 180, 75, 255), SettingsScope.User); // Green
        static UserSetting<Color> s_Color3 = new UserSetting<Color>(PathSettings.instance, "SplineData.color3", new Color32(255, 225, 25, 255), SettingsScope.User); // Yellow
        static UserSetting<Color> s_Color4 = new UserSetting<Color>(PathSettings.instance, "SplineData.color4", new Color32(145, 30, 180, 255), SettingsScope.User); // Purple
        static UserSetting<Color> s_Color5 = new UserSetting<Color>(PathSettings.instance, "SplineData.color5", new Color32(70, 240, 240, 255), SettingsScope.User); // Cyan
        static UserSetting<Color> s_Color6 = new UserSetting<Color>(PathSettings.instance, "SplineData.color6", new Color32(250, 190, 190, 255), SettingsScope.User); // Pink
        static UserSetting<Color> s_Color7 = new UserSetting<Color>(PathSettings.instance, "SplineData.color7", new Color32(0, 128, 128, 255), SettingsScope.User); // Teal
        static UserSetting<Color>[] s_DefaultSplineColors = null;

        static UserSetting<Color>[] defaultSplineColors
        {
            get
            {
                if(s_DefaultSplineColors == null)
                    s_DefaultSplineColors = new UserSetting<Color>[]
                    {
                        s_Color0,
                        s_Color1,
                        s_Color2,
                        s_Color3,
                        s_Color4,
                        s_Color5,
                        s_Color6,
                        s_Color7
                    };
                
                return s_DefaultSplineColors;
            }
        }

        static Dictionary<Type, ISplineDataHandle> s_DrawerTypes = null;
        
        static Dictionary<Type, ISplineDataHandle> drawerTypes
        {
            get
            {
                if(s_DrawerTypes == null)
                    InitializeDrawerTypes();
                return s_DrawerTypes;
            }
        }

        public static bool hasSplineDataIsSelection => s_SelectedSplineDataContainers.Count > 0;
        
        static List<SplineDataContainer> s_SelectedSplineDataContainers = new List<SplineDataContainer>();
        public static List<SplineDataContainer> selectedSplineDataContainers => s_SelectedSplineDataContainers;

        public static Action onSplineDataSelectionChanged;

        static List<Component> s_AllComponents = new List<Component>();
        static List<Component> s_TmpComponents = new List<Component>();

        internal class SplineDataContainer
        {
            public GameObject owner;
            public List<SplineDataElement> splineDataElements = new List<SplineDataElement>();
        }
        
        internal class SplineDataElement
        {
            public FieldInfo splineDataField;
            public MethodInfo drawMethodInfo;
            public MethodInfo drawKeyframeMethodInfo;
            public ISplineDataHandle customDrawerInstance;
            public SplineDataHandleAttribute customDrawerAttribute;
            public MethodInfo initCustomDrawMethodInfo;
            public MethodInfo customSplineDataDrawHandleMethodInfo;
            public MethodInfo customDataPointDrawHandleMethodInfo;
            public SplineContainer splineContainer = null;
            public Component component = null;
            public bool displayed = true;
            public UserSetting<Color> color;
            public SplineDataHandlesDrawer.LabelType labelType = SplineDataHandlesDrawer.LabelType.None;
        }

        static SplineDataHandlesManager()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            OnSelectionChanged();
        }
        
        static void OnSelectionChanged()
        {
            UpdateSplineDataElements();
        }
        
        static void InitializeDrawerTypes()
        {
            s_DrawerTypes = new Dictionary<Type, ISplineDataHandle>();
            var drawerTypes = TypeCache.GetTypesDerivedFrom(typeof(SplineDataHandle<>));

            foreach(var drawerType in drawerTypes)
            {
                var attributes = drawerType.GetCustomAttributes(typeof(CustomSplineDataHandle));
                if(attributes.Any())
                {
                    var drawerAttribute = (attributes.First() as CustomSplineDataHandle).m_Type;
                    var drawerInstance = drawerType.Assembly.CreateInstance(drawerType.ToString()) as ISplineDataHandle;
                    s_DrawerTypes.Add(drawerAttribute, drawerInstance);
                }
            }
        }
        
        static bool VerifyComponentsIntegrity()
        {
            var selection = Selection.gameObjects;
            int componentCount = 0;

            foreach(GameObject go in selection)
            {
                go.GetComponents(typeof(Component), s_TmpComponents);
                foreach(var component in s_TmpComponents)
                {
                    if(s_AllComponents.Contains(component))
                        componentCount++;
                    else //A new component has been added
                        return false;
                }
            }

            //Check if no components has been removed
            return componentCount == s_AllComponents.Count;
        }
        
        static void UpdateSplineDataElements()
        {

            if(Selection.activeGameObject == null)
            {
                if(s_SelectedSplineDataContainers.Count > 0)
                {
                    s_SelectedSplineDataContainers.Clear();
                    onSplineDataSelectionChanged?.Invoke();
                }
                return;
            }
            
            s_SelectedSplineDataContainers.Clear();
            
            var gameObjects = Selection.gameObjects;
            
            SplineDataContainer container = null;
            FieldInfo[] fieldInfos;

            s_AllComponents.Clear();
            
            int colorIndex = 0;
            foreach(GameObject go in gameObjects)
            {
                container = null;
                go.GetComponents(typeof(Component), s_TmpComponents);
                s_AllComponents.AddRange(s_TmpComponents);

                foreach(Component component in s_TmpComponents)
                {
                    if(component == null)
                        continue;
                    
                    fieldInfos = component.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
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
                                    splineContainer = (SplineContainer)fi.GetValue(component);
                            }
                            
                            //Skip this SplineData if no splineContainer can be automatically attributed
                            if(splineContainer == null)
                                continue;
                            
                            Type splineDataType = fieldInfos[i].FieldType.GetTypeInfo().GenericTypeArguments[0];
                            Type drawer = typeof(SplineDataHandlesDrawer);
                            
                            SplineDataHandleAttribute splineDataAttribute = fieldInfos[i].GetCustomAttribute<SplineDataHandleAttribute>();
                            ISplineDataHandle customDrawerInstance = null;
                            MethodInfo initCustomDrawMethodInfo = null;
                            MethodInfo customSplineDataHandleMethodInfo = null;
                            MethodInfo customKeyframeHandleMethodInfo = null;
                            
                            if(splineDataAttribute != null)
                            {
                                if(drawerTypes.ContainsKey(splineDataAttribute.GetType()))
                                {
                                    var splineDataHandleInstance = drawerTypes[splineDataAttribute.GetType()];
                                    var splineDataHandleType = splineDataHandleInstance.GetType();
                                    var drawerDataType = splineDataHandleType.BaseType?.GenericTypeArguments[0];
                                    if(drawerDataType != splineDataType)
                                    {
                                        Debug.LogError($"The data type '{splineDataType}' used for field {fieldInfos[i].Name} in {component.GetType().Name} " +
                                            $"does not correspond to the type '{drawerDataType}' used in {splineDataHandleType.Name}");
                                    }
                                    else 
                                    {
                                        initCustomDrawMethodInfo =
                                        drawer.GetMethod("InitCustomHandles",
                                            BindingFlags.Static | BindingFlags.NonPublic
                                        )?.MakeGenericMethod(splineDataType);

                                        var customDrawerMethodInfos = splineDataHandleType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                                        customSplineDataHandleMethodInfo = GetCustomSplineDataDrawerMethodInfo(customDrawerMethodInfos, splineDataType);
                                        customKeyframeHandleMethodInfo = GetCustomKeyframeDrawerMethodInfo(customDrawerMethodInfos, splineDataType);

                                        customDrawerInstance = splineDataHandleInstance;
                                    }
                                }
                                else
                                    Debug.LogError($"No valid SplineDataHandle<> type was found for drawerID = \"{splineDataAttribute.GetType()}\" (used in {component.gameObject.name}/{component.GetType().Name})");
                            }
                            else 
                                continue;

                            MethodInfo drawerMethod = drawer.GetMethod("DrawSplineDataHandles", BindingFlags.Static | BindingFlags.NonPublic);
                            MethodInfo drawerMethodInfo = drawerMethod.MakeGenericMethod(splineDataType);
                            MethodInfo dpDrawerMethod = drawer.GetMethod("DrawCustomHandles", BindingFlags.Static | BindingFlags.NonPublic);
                            MethodInfo dataPointDrawerMethodInfo = dpDrawerMethod.MakeGenericMethod(splineDataType);
                            
                            var splineDataElement = new SplineDataElement()
                            {
                                splineDataField = fieldInfos[i],
                                drawMethodInfo = drawerMethodInfo, 
                                drawKeyframeMethodInfo = dataPointDrawerMethodInfo, 
                                splineContainer = splineContainer,
                                component = component,
                                customDrawerInstance = customDrawerInstance,
                                customDrawerAttribute = splineDataAttribute,
                                initCustomDrawMethodInfo = initCustomDrawMethodInfo,
                                customSplineDataDrawHandleMethodInfo = customSplineDataHandleMethodInfo,
                                customDataPointDrawHandleMethodInfo = customKeyframeHandleMethodInfo,
                                color = defaultSplineColors[colorIndex++ % defaultSplineColors.Length]
                            };

                            if(container == null)
                            {
                                container = new SplineDataContainer() { owner = go };
                                s_SelectedSplineDataContainers.Add(container);
                            }
                            container.splineDataElements.Add(splineDataElement);
                        }
                    }
                }
            }
            onSplineDataSelectionChanged?.Invoke();
        }
        
        static MethodInfo GetCustomSplineDataDrawerMethodInfo(MethodInfo[] methodInfos, System.Type splineDataType)
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
        
        static MethodInfo GetCustomKeyframeDrawerMethodInfo(MethodInfo[] methodInfos, System.Type splineDataType)
        {
            foreach(var methodInfo in methodInfos)
            {
                //Checking if the method has the correct name
                if(!methodInfo.Name.Equals("DrawDataPoint"))
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
        
        static void OnSceneGUI(SceneView view)
        {
            if(!VerifyComponentsIntegrity())
                OnSelectionChanged();
            
            foreach(var sdContainer in s_SelectedSplineDataContainers)
            {
                foreach(var splineDataElement in sdContainer.splineDataElements)
                {
                    if(!splineDataElement.displayed || splineDataElement.splineContainer == null)
                        continue;
                    
                    var mono = splineDataElement.component as MonoBehaviour;
                    if((mono != null && !mono.isActiveAndEnabled) || !splineDataElement.splineContainer.isActiveAndEnabled)
                        continue;
                    
                    splineDataElement.drawMethodInfo?.Invoke(null,
                        new object[]
                        {
                            splineDataElement.splineDataField.GetValue(splineDataElement.component),
                            splineDataElement.component,
                            splineDataElement.splineContainer.Spline,
                            splineDataElement.splineContainer.transform.localToWorldMatrix,
                            splineDataElement.color.value,
                            splineDataElement.labelType
                        });

                    if(splineDataElement.customDrawerInstance != null)
                    {
                        splineDataElement.customDrawerInstance.SetAttribute(splineDataElement.customDrawerAttribute);
                        
                        var splineData = splineDataElement.splineDataField.GetValue(splineDataElement.component);
                        var spline = splineDataElement.splineContainer.Spline;
                        var localToWorld = splineDataElement.splineContainer.transform.localToWorldMatrix;
                        
                        splineDataElement.initCustomDrawMethodInfo?.Invoke(null,
                            new object[]
                            {
                                splineData,
                                splineDataElement.customDrawerInstance
                            });

                        splineDataElement.drawKeyframeMethodInfo?.Invoke(null,
                            new object[]
                            {
                                splineData,
                                splineDataElement.component,
                                spline,
                                localToWorld,
                                splineDataElement.color.value,
                                splineDataElement.customDrawerInstance,
                                splineDataElement.customSplineDataDrawHandleMethodInfo,
                                splineDataElement.customDataPointDrawHandleMethodInfo
                            });
                    }
                }
            }
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.white;
        }
        
    }
}
