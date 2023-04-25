using System.Text;
using JetBrains.Annotations;
using UnityEngine;

public interface ILogger
{
	string LogID       { get; }
	bool   LogSilenced { get; }
}

public static class LogOP
{
	public const string None = "--";
}

/// <summary>
/// Note: Aj stands for 'Anjin'
///
/// -- Categorie --
/// Categories are particularly useful for builds where the type of log message is not preserved. (Log, LogWarning and LogError all look the same in Unity's log files)
///
///	Trace	Low level information which leaves a 'trace' of the game's calls, useful to closely monitor or follow the flow of the game.
/// Info	Higher level information messages.
/// Effect	Indicate a change to some part of the game's state.
/// Warn	Warn about a possible problem or oddity which could help in debugging.
///
/// -- Operators --
/// Operators are mostly useful in logging-heavy parts of the game, such as combat.
///
/// --	N/A
/// %%	Performance information, load times
/// ++	Combat: Something is added or registered to the state.
/// xx	Combat: Something is removed or unregistered from the state.
/// >>	Combat: Trigger invoked.
/// </summary>
public static class AjLog
{
	private static readonly StringBuilder _sb = new StringBuilder();

	/// <summary>
	/// https://github.com/JetBrains/resharper-unity/issues/1260
	/// </summary>
	[UsedImplicitly]
	public static T breakpoint<T>(T s)
	{
		Debug.Log(s);
		return s;
	}


	public static void LogEffect(string       op,  string message)            => Log("Effect", op, message);
	public static void LogEffect(this object  obj, string op, string message) => Log(obj, message, "Effect", op);
	public static void LogEffect(this ILogger obj, string op, string message) => Log(obj, "Effect", op, message);

	public static void LogTrace(this ILogger    obj, string op, string message) => Log(obj, "Trace", op, message);
	public static void LogTrace(string          op,  string message)            => Log("Trace", op, message);
	public static void LogTrace(this GameObject obj, string op, string message) => Log(obj, message, "Trace", op);
	public static void LogTrace(this object     obj, string op, string message) => Log(obj, message, "Trace", op);

	public static void LogVisual(this GameObject obj, string op, string message) => Log(obj, message, "Visual", op);
	public static void LogVisual(this object     obj, string op, string message) => Log(obj, message, "Visual", op);
	public static void LogVisual(this ILogger    obj, string op, string message) => Log(obj, "Visual", op, message);
	public static void LogVisual(string          op,  string message) => Log("Visual", op, message);

	public static void Log(this GameObject obj, string message, string category = "Info", string op = "--")
	{
		DebugLogger.Log($"[{category}] {op} ({(obj != null ? obj.name : "null")}) {message}", obj);
	}

	public static void Log(this MonoBehaviour mb, string message, string category = "Info", string op = "--")
	{
		DebugLogger.Log($"[{category}] {op} ({(mb != null ? $"{mb.GetType().Name}" : "null")}) {message}", mb);
	}

	public static void Log(this ILogger logger, string category, string op, string message)
	{
		if (logger == null) return;
		if (!logger.LogSilenced) DebugLogger.Log($"[{category}] {op} ({logger.LogID}) {message}");
	}

	public static void Log(this object obj,
		string                         message,
		string                         category = "Info",
		string                         op       = "--",
		LogContext                     ctx      = LogContext.Default,
		LogPriority                    pt       = LogPriority.Low)
	{
		DebugLogger.Log($"[{category}] {op} ({obj.GetType().Name}) {message}", ctx, pt);
	}

	public static void Log(string category,
		string                    op,
		string                    message,
		LogContext                ctx = LogContext.Default,
		LogPriority               pt  = LogPriority.Low)
	{
		DebugLogger.Log($"[{category}] {op} {message}", ctx, pt);
	}


	// WarnING
	// ----------------------------------------
	public static void LogWarn(this ScriptableObject so,
		string                                       msg,
		LogContext                                   ctx = LogContext.Default,
		LogPriority                                  pt  = LogPriority.Low)
	{
		DebugLogger.LogWarning($"[Warn] SkillAsset({so.name}): {msg}", so, ctx, pt);
	}

	public static void LogWarn(this GameObject go,
		string                                 msg,
		string                                 function,
		LogContext                             ctx = LogContext.Default,
		LogPriority                            pt  = LogPriority.Low)
	{
		DebugLogger.LogWarning($"[Warn] SkillAsset({go.name}).{function}: {msg}", go, ctx, pt);
	}


	public static void LogWarn(this ILogger logger,
		string                              message,
		LogContext                          ctx = LogContext.Default,
		LogPriority                         pt  = LogPriority.Low)
	{
		DebugLogger.LogWarning($"[Warn] ({logger.LogID}): {message}", ctx, pt);
	}


	public static void LogWarn(this object obj,
		string                             message,
		string                             function = null,
		LogContext                         ctx      = LogContext.Default,
		LogPriority                        pt       = LogPriority.Low)
	{
		_sb.Append("[Warn]");
		_sb.Append(" ");
		_sb.Append("(");
		_sb.Append(obj.GetType().Name);
		if (function != null)
		{
			_sb.Append(".");
			_sb.Append(function);
		}

		_sb.Append(")");
		_sb.Append(": ");
		_sb.Append(message);

		DebugLogger.LogWarning(_sb.ToString(), obj as Object, ctx, pt);
		_sb.Clear();
	}

	public static void LogWarn(string id,
		string                        message,
		LogContext                    ctx = LogContext.Default,
		LogPriority                   pt  = LogPriority.Low)
	{
		DebugLogger.LogWarning($"[Warn] ({id}): {message}", ctx, pt);
	}

	public static void LogWarn(string name,
		string                        message,
		string                        function,
		LogContext                    ctx = LogContext.Default,
		LogPriority                   pt  = LogPriority.Low)
	{
		_sb.Append("[Warn]");
		_sb.Append(" ");
		_sb.Append("(");
		_sb.Append(name);
		if (function != null)
		{
			_sb.Append(".");
			_sb.Append(function);
		}

		_sb.Append(")");
		_sb.Append(": ");
		_sb.Append(message);

		DebugLogger.LogWarning(_sb.ToString(), ctx, pt);
		_sb.Clear();
	}

	// ERROR
	// ----------------------------------------
	public static void LogError(this ScriptableObject so,
		string                                        msg,
		LogContext                                    ctx = LogContext.Default,
		LogPriority                                   pt  = LogPriority.Low)
	{
		DebugLogger.LogError($"[ERROR] ({so.name}) {msg}", so, ctx, pt);
	}

	public static void LogError(this GameObject go,
		string                                  msg,
		LogContext                              ctx = LogContext.Default,
		LogPriority                             pt  = LogPriority.Low)
	{
		DebugLogger.LogError($"[ERROR] ({go.name}) {msg}", go, ctx, pt);
	}

	public static void LogError(this MonoBehaviour mb,
		string                                     msg,
		LogContext                                 ctx = LogContext.Default,
		LogPriority                                pt  = LogPriority.Low)
	{
		DebugLogger.LogError($"[ERROR] ({mb.name}) {msg}", mb, ctx, pt);
	}

	public static void LogError(this ILogger logger,
		string                               message,
		LogContext                           ctx = LogContext.Default,
		LogPriority                          pt  = LogPriority.Low)
	{
		DebugLogger.LogError($"[ERROR] ({logger.LogID}) {message}", ctx, pt);
	}

	public static void LogError(this object obj,
		string                              message,
		[CanBeNull] string                  func = null,
		LogContext                          ctx  = LogContext.Default,
		LogPriority                         pt   = LogPriority.Low)
	{
		DebugLogger.LogError(func == null
			? $"[ERROR] ({obj}): {message}"
			: $"[ERROR] ({obj}.{func}): {message}", ctx, pt);
	}

	public static void LogError(string id,
		string                         message,
		LogContext                     ctx = LogContext.Default,
		LogPriority                    pt  = LogPriority.Low)
	{
		DebugLogger.LogError($"[ERROR] ({id}): {message}", ctx, pt);
	}

	public static void LogError(string message,
		string                         @class,
		[CanBeNull] string             func,
		LogContext                     ctx = LogContext.Default,
		LogPriority                    pt  = LogPriority.Low)
	{
		DebugLogger.LogError(func == null
			? $"[ERROR] ({@class}): {message}"
			: $"[ERROR] ({@class}.{func}): {message}", ctx, pt);
	}
}