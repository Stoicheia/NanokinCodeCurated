using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

[Flags]
public enum LogPriority
{
	Critical = 1, High = 2, Low = 4, Temp = 8
}

[Flags]
public enum LogContext
{
	Editor      = 1, Overworld = 2, Combat    = 4, Lua     = 8, Data = 16,
	Pathfinding = 32, Hardware = 64, Coplayer = 128, Audio = 256, UI = 512, Graphics = 1024, Core = 2048,
	Default     = 16384
}

public static class DebugLogger
{
	private const string CONFIG_ADDRESS = "Debug/log_settings";

	private static readonly Dictionary<LogType, Action<string, bool>> _getLoggingFunc =
		new Dictionary<LogType, Action<string, bool>>
		{
			{ LogType.Log, (x,     y) => LogMessage(x, null, y) },
			{ LogType.Warning, (x, y) => LogWarningMessage(x, null, y) },
			{ LogType.Error, (x,   y) => LogErrorMessage(x, null, y) },
			{ LogType.Assert, (x,  y) => LogAssertionMessage(x, null, y) }
		};

	private static readonly Dictionary<LogType, Action<string, Object, bool>> _getLoggingFuncWithContext =
		new Dictionary<LogType, Action<string, Object, bool>>
		{
			{ LogType.Log, LogMessage },
			{ LogType.Warning, LogWarningMessage },
			{ LogType.Error, LogErrorMessage },
			{ LogType.Assert, LogAssertionMessage }
		};

	private static DebugLoggerConfig _config;

	private static async void LoadConfig()
	{
		_config = await Addressables.LoadAssetAsync<DebugLoggerConfig>(CONFIG_ADDRESS);
	}

	private static void LogOfType(LogType t, string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		Action<string, bool> logFunc = _getLoggingFunc[t];
		if (_config == null) //log normally when config is not loaded.
		{
			LoadConfig();
			logFunc(message, false);
			return;
		}

		if ((context & _config.EnabledContexts) != 0 && (priority & _config.EnabledPriorities) != 0)
			logFunc(message, true);
	}

	private static void LogOfType(LogType t, string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		Action<string, Object, bool> logFunc = _getLoggingFuncWithContext[t];

		if (_config == null) //log normally when config is not loaded.
		{
			LoadConfig();
			logFunc(message, ctx, false);
			return;
		}

		if ((context & _config.EnabledContexts) != 0 && (priority & _config.EnabledPriorities) != 0)
			logFunc(message, ctx, true);
	}

	private static void LogMessage(string message, Object ctx, bool check)
	{
		if (check && !_config.LogMessages) return;
		if (ctx == null)
		{
			Debug.Log(message);
		}
		else
		{
			Debug.Log(message, ctx);
		}
	}

	private static void LogWarningMessage(string message, Object ctx, bool check)
	{
		if (check && !_config.LogWarnings) return;
		if (ctx == null)
		{
			Debug.LogWarning(message);
		}
		else
		{
			Debug.LogWarning(message, ctx);
		}
	}

	private static void LogErrorMessage(string message, Object ctx, bool check)
	{
		if (check && !_config.LogErrors) return;
		if (ctx == null)
		{
			Debug.LogError(message);
		}
		else
		{
			Debug.LogError(message, ctx);
		}
	}

	private static void LogAssertionMessage(string message, Object ctx, bool check)
	{
		if (check && !_config.LogAssertions) return;
		if (ctx == null)
		{
			Debug.LogAssertion(message);
		}
		else
		{
			Debug.LogAssertion(message, ctx);
		}
	}

	public static void Log(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Log, message, context, priority);
	}

	public static void LogWarning(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Warning, message, context, priority);
	}

	public static void LogError(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Error, message, context, priority);
	}

	public static void LogAssertion(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Assert, message, context, priority);
	}

	public static void LogException(Exception e)
	{
		if (_config == null || _config.LogExceptions)
		{
			Debug.LogException(e);
		}
	}

	public static void Log(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Log, message, ctx, context, priority);
	}

	public static void LogWarning(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Warning, message, ctx, context, priority);
	}

	public static void LogError(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Error, message, ctx, context, priority);
	}

	public static void LogAssertion(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		LogOfType(LogType.Assert, message, ctx, context, priority);
	}

	public static void LogException(Exception e, Object ctx)
	{
		if (_config.LogExceptions)
		{
			Debug.LogException(e, ctx);
		}
	}
}

public static class Dbg
{
	public static void Log(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.Log(message, context, priority);
	}

	public static void LogWarning(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogWarning(message, context, priority);
	}

	public static void LogError(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogError(message, context, priority);
	}

	public static void LogAssertion(string message, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogAssertion(message, context, priority);
	}

	public static void LogException(Exception e)
	{
		DebugLogger.LogException(e);
	}

	public static void Log(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.Log(message, ctx, context, priority);
	}

	public static void LogWarning(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogWarning(message, ctx, context, priority);
	}

	public static void LogError(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogError(message, ctx, context, priority);
	}

	public static void LogAssertion(string message, Object ctx, LogContext context = LogContext.Default, LogPriority priority = LogPriority.High)
	{
		DebugLogger.LogAssertion(message, ctx, context, priority);
	}

	public static void LogException(Exception e, Object ctx)
	{
		DebugLogger.LogException(e, ctx);
	}
}