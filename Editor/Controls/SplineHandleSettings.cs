using System;
using UnityEditor.SettingsManagement;

namespace UnityEditor.Splines
{
    static class SplineHandleSettings
    {
        [UserSetting]
        static readonly Pref<bool> s_FlowDirectionEnabled = new Pref<bool>("Handles.FlowDirectionEnabled", true);

        [UserSetting]
        static readonly Pref<bool> s_ShowAllTangents = new Pref<bool>("Handles.ShowAllTangents", true);

        public static bool FlowDirectionEnabled
        {
            get => s_FlowDirectionEnabled;
            set
            {
                s_FlowDirectionEnabled.SetValue(value);
                Changed?.Invoke();
            }
        }

        public static bool ShowAllTangents
        {
            get => s_ShowAllTangents;
            set
            {
                s_ShowAllTangents.SetValue(value);
                Changed?.Invoke();
            }
        }

        public static event Action Changed;
    }
}