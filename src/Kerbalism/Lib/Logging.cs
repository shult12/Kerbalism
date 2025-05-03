using System.Diagnostics;
using System.Reflection;

namespace KERBALISM
{
	static class Logging
	{
		internal enum LogLevel
		{
			Message,
			Warning,
			Error
		}

		static void Log(MethodBase method, string message, LogLevel level)
		{
			switch (level)
			{
				default:
					UnityEngine.Debug.Log(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Warning:
					UnityEngine.Debug.LogWarning(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
				case LogLevel.Error:
					UnityEngine.Debug.LogError(string.Format("[Kerbalism] {0}.{1} {2}", method.ReflectedType.Name, method.Name, message));
					return;
			}
		}

		///<summary>write a message to the log</summary>
		internal static void Log(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the call stack to the log</summary>
		internal static void LogStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
			UnityEngine.Debug.Log(stackTrace);
		}

		///<summary>write a message to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		internal static void LogDebug(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
		}

		///<summary>write a message and the full call stack to the log, only on DEBUG and DEVBUILD builds</summary>
		[Conditional("DEBUG"), Conditional("DEVBUILD")]
		internal static void LogDebugStack(string message, LogLevel level = LogLevel.Message, params object[] param)
		{
			StackTrace stackTrace = new StackTrace();
			Log(stackTrace.GetFrame(1).GetMethod(), string.Format(message, param), level);
			UnityEngine.Debug.Log(stackTrace);
		}
	}
}
