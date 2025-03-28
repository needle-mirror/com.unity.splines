using UnityEditor.SettingsManagement;
using UnityEngine;

namespace UnityEditor.Splines
{
    sealed class PathSettings
    {
        static Settings s_SettingsInstance;

        public static Settings instance
        {
            get
            {
                if (s_SettingsInstance == null)
                    s_SettingsInstance = new Settings(new [] { new UserSettingsRepository() });
                return s_SettingsInstance;
            }
        }

        // Register a new SettingsProvider that will scrape the owning assembly for [UserSetting] marked fields.
        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider("Preferences/Splines",
                instance,
                new[] { typeof(PathSettings).Assembly });

            return provider;
        }
    }

    /// <summary>
    /// The wrapper to define user preferences through Settings Manager.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Pref<T> : UserSetting<T>
    {
        public Pref(string key, T value)
            : base(PathSettings.instance, key, value, SettingsScope.User) { }
    }
}
