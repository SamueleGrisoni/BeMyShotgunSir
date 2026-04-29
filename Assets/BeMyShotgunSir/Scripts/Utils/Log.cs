using System;
using System.Diagnostics;
using UnityEngine;

namespace BeMyShotgunSir.Scripts.Utils
{
    /// <summary>
    /// Branch Predictor Studios logging system.
    /// Uses Static Generic Caching to avoid Dictionary lookups and Reflection overhead at runtime.
    /// Logs are automatically stripped from non-development builds via Conditional attributes.
    /// </summary>
    public static class Log
    {
        private enum LogTypeEnum
        {
            Default,
            Warning,
            Error,
            Task
        }
        /// <summary>
        /// Internal cache that stores type-specific metadata.
        /// Creates a unique static instance of this class for every type T.
        /// </summary>
        private static class TypeData<T>
        {
            public static readonly string Name = typeof(T).Name;
            public static readonly string Color = GenerateColorForType(typeof(T));

            private static string GenerateColorForType(Type type)
            {
                // Generate a deterministic color based on the class name hash
                int hash = type.Name.GetHashCode();
                float r = (Mathf.Abs(hash & 0xFF0000) >> 16) / 255f;
                float g = (Mathf.Abs(hash & 0x00FF00) >> 8) / 255f;
                float b = Mathf.Abs(hash & 0x0000FF) / 255f;

                // Brighten the color to ensure readability on dark Editor themes
                var c = UnityEngine.Color.Lerp(new Color(r, g, b), UnityEngine.Color.white, 0.4f);
                return "#" + ColorUtility.ToHtmlStringRGB(c);
            }
        }

        private const string _DEFAULT_COLOR = "#ffffff";
        private const string _TASK_COLOR = "#B388FF";

        // --- DEBUG LOGS (D) ---

        /// <summary> Instance-based Lazy Log. Usage: Log.DLazy(() => "message", this); </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void DLazy<T>(Func<string> messageFactory, T sender) =>
            LogToUnity(messageFactory(), LogTypeEnum.Default, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Instance-based Lazy Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void DLazy<T>(Func<string> messageFactory, T sender, bool enabled)
        {
            if (!enabled) return;
            DLazy(messageFactory, sender);
        }

        /// <summary> Static-based Lazy Log. Usage: Log.DLazy<ClassName>(() => "message"); </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void DLazy<T>(Func<string> messageFactory) =>
            LogToUnity(messageFactory(), LogTypeEnum.Default, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Static-based Lazy Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void DLazy<T>(Func<string> messageFactory, bool enabled)
        {
            if (!enabled) return;
            DLazy<T>(messageFactory);
        }

        /// <summary> Manual Log with custom tag and color. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void D(object message, string contextTag, string customColor = _DEFAULT_COLOR) =>
            LogToUnity(message.ToString(), LogTypeEnum.Default, customColor, $"{contextTag}");

        /// <summary> Manual Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void D(object message, string contextTag, bool enabled, string customColor = _DEFAULT_COLOR)
        {
            if (!enabled) return;
            D(message, contextTag, customColor);
        }

        // --- WARNING LOGS (W) ---

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void WLazy<T>(Func<string> messageFactory, T sender) =>
            LogToUnity(messageFactory(), LogTypeEnum.Warning, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Instance-based Lazy Warning Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void WLazy<T>(Func<string> messageFactory, T sender, bool enabled)
        {
            if (!enabled) return;
            WLazy(messageFactory, sender);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void WLazy<T>(Func<string> messageFactory) =>
            LogToUnity(messageFactory(), LogTypeEnum.Warning, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Static-based Lazy Warning Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void WLazy<T>(Func<string> messageFactory, bool enabled)
        {
            if (!enabled) return;
            WLazy<T>(messageFactory);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void W(object message, string contextTag, string customColor = _DEFAULT_COLOR) =>
            LogToUnity(message.ToString(), LogTypeEnum.Warning, customColor, $"{contextTag}");

        /// <summary> Manual Warning Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void W(object message, string contextTag, bool enabled, string customColor = _DEFAULT_COLOR)
        {
            if (!enabled) return;
            W(message, contextTag, customColor);
        }

        // --- ERROR LOGS (E) ---

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ELazy<T>(Func<string> messageFactory, T sender) =>
            LogToUnity(messageFactory(), LogTypeEnum.Error, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Instance-based Lazy Error Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ELazy<T>(Func<string> messageFactory, T sender, bool enabled)
        {
            if (!enabled) return;
            ELazy(messageFactory, sender);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ELazy<T>(Func<string> messageFactory) =>
            LogToUnity(messageFactory(), LogTypeEnum.Error, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Static-based Lazy Error Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ELazy<T>(Func<string> messageFactory, bool enabled)
        {
            if (!enabled) return;
            ELazy<T>(messageFactory);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void E(object message, string contextTag, string customColor = _DEFAULT_COLOR) =>
            LogToUnity(message.ToString(), LogTypeEnum.Error, customColor, $"{contextTag}");

        /// <summary> Manual Error Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void E(object message, string contextTag, bool enabled, string customColor = _DEFAULT_COLOR)
        {
            if (!enabled) return;
            E(message, contextTag, customColor);
        }

        // --- TASK LOGS (T) ---

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void TLazy<T>(Func<string> messageFactory, T sender) =>
            LogToUnity(messageFactory(), LogTypeEnum.Task, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Instance-based Lazy Task Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void TLazy<T>(Func<string> messageFactory, T sender, bool enabled)
        {
            if (!enabled) return;
            TLazy(messageFactory, sender);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void TLazy<T>(Func<string> messageFactory) =>
            LogToUnity(messageFactory(), LogTypeEnum.Task, TypeData<T>.Color, $"{TypeData<T>.Name}");

        /// <summary> Static-based Lazy Task Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void TLazy<T>(Func<string> messageFactory, bool enabled)
        {
            if (!enabled) return;
            TLazy<T>(messageFactory);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void T(object message, string contextTag, string customColor = _TASK_COLOR) =>
            LogToUnity(message.ToString(), LogTypeEnum.Task, customColor, $"{contextTag}");

        /// <summary> Manual Task Log con flag abilitazione. </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void T(object message, string contextTag, bool enabled, string customColor = _TASK_COLOR)
        {
            if (!enabled) return;
            T(message, contextTag, customColor);
        }

        // --- INTERNAL CORE LOGIC ---

        /// <summary>
        /// Routes the formatted message to the appropriate Unity Debug method.
        /// Rich-text colors are stripped automatically when not in the Unity Editor.
        /// </summary>
        private static void LogToUnity(string message, LogTypeEnum type, string tagColor, string tagText)
        {
            string messageColor = type switch
            {
                LogTypeEnum.Warning => _DEFAULT_COLOR,
                LogTypeEnum.Error => _DEFAULT_COLOR,
                LogTypeEnum.Task => _TASK_COLOR,
                _ => _DEFAULT_COLOR
            };
#if UNITY_EDITOR
            string formattedMsg = $"<color={tagColor}>[{tagText}]</color>-<color={messageColor}>{message}</color>";
#else
            string formattedMsg = $"[{tagText}]-{message}";
#endif
            switch (type)
            {
                case LogTypeEnum.Warning: UnityEngine.Debug.LogWarning(formattedMsg); break;
                case LogTypeEnum.Error: UnityEngine.Debug.LogError(formattedMsg); break;
                case LogTypeEnum.Task: UnityEngine.Debug.Log(formattedMsg); break;
                case LogTypeEnum.Default: UnityEngine.Debug.Log(formattedMsg); break;
                default: UnityEngine.Debug.Log(formattedMsg); break;
            }
        }
    }
}
