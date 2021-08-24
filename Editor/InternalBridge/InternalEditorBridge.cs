using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Splines
{
    internal static class InternalEditorBridge
    {
        public class ShortcutContext : IShortcutToolContext
        {
            public Func<bool> isActive;
            public bool active
            {
                get
                {
                    if (isActive != null)
                        return isActive();
                    return true;
                }
            }
            public object context { get; set; }
        }

        public static void RegisterShortcutContext(ShortcutContext context)
        {
            ShortcutIntegration.instance.contextManager.RegisterToolContext(context);
        }

        public static void UnregisterShortcutContext(ShortcutContext context)
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(context);
        }
    }
}