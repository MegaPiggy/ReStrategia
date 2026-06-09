using UnityEngine;

namespace ReStrategia
{
    public static class LogUtil
    {
        private const string Prefix = "[ReStrategia] ";

        public static void LogInfo(object message)
        {
            Debug.Log(Prefix + message);
        }

        public static void LogInfo(string format, params object[] args)
        {
            Debug.Log(Prefix + string.Format(format, args));
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void LogWarning(string format, params object[] args)
        {
            Debug.LogWarning(Prefix + string.Format(format, args));
        }

        public static void LogError(object message)
        {
            Debug.LogError(Prefix + message);
        }

        public static void LogError(string format, params object[] args)
        {
            Debug.LogError(Prefix + string.Format(format, args));
        }

        public static void LogDebug(object message)
        {
            Debug.Log(Prefix + message);
        }

        public static void LogDebug(string format, params object[] args)
        {
            Debug.Log(Prefix + string.Format(format, args));
        }
    }
}
