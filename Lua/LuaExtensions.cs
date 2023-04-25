using System;
using System.Text;
using Anjin.Util;
using Combat;
using Combat.Data;
using Combat.Data.Decorative;
using Combat.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using UnityEngine;

namespace Anjin.Scripting
{
	public static class LuaExtensions
	{
		public static float AsFloat([CanBeNull] this DynValue dv, float defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.Number) return defaultValue;

			return (float)dv.Number;
		}

		public static int AsInt([CanBeNull] this DynValue dv, int defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.Number) return defaultValue;

			return (int)dv.Number;
		}

		public static bool AsInt([CanBeNull] this DynValue dv, out int result, int defaultValue = default)
		{
			if (dv == null)
			{
				result = defaultValue;
				return false;
			}

			if (dv.Type != DataType.Number)
			{
				result = defaultValue;
				return false;
			}

			result = (int)dv.Number;
			return true;
		}


		public static bool AsBool([CanBeNull] this DynValue dv, bool defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.Boolean) return defaultValue;

			return dv.Boolean;
		}

		public static Table AsTable([CanBeNull] this DynValue dv, [CanBeNull] Table defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type == DataType.Table && dv.Table.Get("__chainctr").AsBool()) return AsTable(dv.Table.Get("value"), defaultValue);
			if (dv.Type != DataType.Table) return defaultValue;

			return dv.Table;
		}

		// OUT ARGUMENT
		// ----------------------------------------

		public static bool AsFloat([CanBeNull] this DynValue dv, out float value, float defaultValue = default)
		{
			if (dv != null && dv.Type == DataType.Number)
			{
				value = (float)dv.Number;
				return true;
			}

			value = defaultValue;
			return false;
		}

		public static bool AsBool([CanBeNull] this DynValue dv, out bool ret, bool defaultValue = default)
		{
			if (dv != null && dv.Type == DataType.Boolean)
			{
				ret = dv.Boolean;
				return true;
			}

			ret = defaultValue;
			return false;
		}

		public static bool AsTable([NotNull] this DynValue dv, out Table tbl, [CanBeNull] Table defaultValue = default)
		{
			if (dv.Type == DataType.Table && dv.Table.Get("__chainctr").AsBool())
				return AsTable(dv.Table.Get("value"), out tbl, defaultValue);

			if (dv.Type == DataType.Table)
			{
				tbl = dv.Table;
				return true;
			}

			tbl = defaultValue;
			return false;
		}

		public static bool AsString([NotNull] this DynValue dv, out string str, [CanBeNull] string defaultValue = default)
		{
			if (dv.Type == DataType.String)
			{
				str = dv.String;
				return true;
			}

			str = defaultValue;
			return false;
		}

		public static bool AsFunction([CanBeNull] this DynValue dv, out Closure closure, [CanBeNull] Closure defaultValue = default)
		{
			if (dv != null && dv.Type == DataType.Function)
			{
				closure = dv.Function;
				return true;
			}

			closure = defaultValue;
			return false;
		}

		public static string AsString([CanBeNull] this DynValue dv, [CanBeNull] string defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.String) return defaultValue;

			return dv.String;
		}

		public static Closure AsFunction([CanBeNull] this DynValue dv, [CanBeNull] Closure defaultValue = default)
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.Function) return defaultValue;

			return dv.Function;
		}

		[CanBeNull]
		public static TUserdata AsUserdata<TUserdata>([CanBeNull] this DynValue dv, [CanBeNull] TUserdata defaultValue = default)
			where TUserdata : class
		{
			if (dv == null) return defaultValue;
			if (dv.Type != DataType.UserData) return defaultValue;
			if (dv.UserData.Object is TUserdata userdata) return userdata;

			return defaultValue;
		}

		public static Vector3 AsVector3([CanBeNull] this DynValue dv, Vector3 defaultValue = default)
		{
			if (dv != null && dv.IsNotNil())
			{
				Vector3? v = LuaUtil.UserdataToPosition(dv);
				if (v != null)
				{
					return v.Value;
				}
				else
				{
					return defaultValue;
				}
			}

			return defaultValue;
		}


		public static bool AsUserdata<TUserdata>([CanBeNull] this DynValue dv, out TUserdata ret, TUserdata defaultValue = default)
		{
			if (dv?.UserData == null)
			{
				ret = defaultValue;
				return false;
			}

			if (dv != null && dv.Type == DataType.Table && dv.Table.Get("__chainctr").AsBool())
				return AsUserdata(dv.Table.Get("value"), out ret, defaultValue);

			if (dv != null && dv.Type == DataType.UserData && dv.UserData.Object is TUserdata userdata)
			{
				ret = userdata;
				return true;
			}

			ret = defaultValue;
			return false;
		}

		[CanBeNull]
		public static bool AsWorldPoint([CanBeNull] this DynValue dv, out WorldPoint ret, [CanBeNull] WorldPoint defaultValue = default)
		{
			ret = defaultValue;

			if (dv == null) return false;

			switch (dv.Type)
			{
				case DataType.Nil:      break;
				case DataType.Void:     break;
				case DataType.Boolean:  break;
				case DataType.Number:   break;
				case DataType.String:   break;
				case DataType.Function: break;
				case DataType.Table:    break;
				case DataType.Tuple:    break;

				case DataType.UserData:
					switch (dv.UserData.Object)
					{
						case GameObject go:
							ret = new WorldPoint(go);
							return true;

						case Transform tfm:
							ret = new WorldPoint(tfm.gameObject);
							return true;

						case Vector2 v2:
							ret = new WorldPoint(v2.x_y());
							return true;

						case Vector3 v3:
							ret = new WorldPoint(v3);
							return true;

						case Arena arena:
							ret = new WorldPoint(arena);
							return true;

						case Battle battle:
							ret = new WorldPoint(battle.arena);
							return true;

						case Fighter fighter:
							ret = new WorldPoint(fighter.actor);
							return true;

						case Slot slot:
							ret = new WorldPoint(slot.actor.gameObject);
							return true;

						case Target target:
							if (target.IsSingle)
							{
								ret = new WorldPoint(target.all[0].GetTargetObject());
								return true;
							}
							else
							{
								ret = new WorldPoint(target.center);
								return true;
							}

						case Targeting target:
							if (target.count == 1 && target[0].IsSingle)
							{
								ret = new WorldPoint(target[0].all[0].GetTargetObject());
								return true;
							}
							else
							{
								ret = new WorldPoint(target.Centroid);
								return true;
							}

						case CoroutineFX effectAnim:
							ret = new WorldPoint(effectAnim);
							return true;
					}

					break;

				case DataType.Thread:          break;
				case DataType.ClrFunction:     break;
				case DataType.TailCallRequest: break;
				case DataType.YieldRequest:    break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}

		public static bool AsEnum<TEnum>(this DynValue dv, out TEnum ret, TEnum defaultValue = default)
			where TEnum : struct, Enum
		{
			if (dv.AsObject(out ret))
				return true;

			if (dv.AsString(out string str))
			{
				if (Enum.TryParse(str.Replace("-", "_"), true, out TEnum val))
				{
					ret = val;
					return true;
				}
			}

			ret = defaultValue;
			return false;
		}

		public static TUserdata AsObject<TUserdata>([CanBeNull] this DynValue dv, TUserdata defaultValue = default)
		{
			if (AsObject(dv, out TUserdata outvalue, defaultValue))
				return outvalue;

			return defaultValue;
		}

		public static bool AsObject<TUserdata>([CanBeNull] this DynValue dv, out TUserdata ret, TUserdata defaultValue = default)
		{
			if (dv != null && dv.Type == DataType.Table && dv.Table.Get("__chainctr").AsBool())
				return AsObject(dv.Table.Get("value"), out ret, defaultValue);

			try
			{
				if (dv != null && dv.IsNotNil())
				{
					ret = dv.ToObject<TUserdata>();
					return true;
				}
			}
			catch (ScriptConversionException)
			{
				// ignored
			}
			catch (ScriptRuntimeException)
			{
				// ignored
			}
			catch (Exception e)
			{
				// ignored
			}

			ret = defaultValue;
			return false;
		}

		public static bool AsGameObject([CanBeNull] this DynValue dv, out GameObject ret, GameObject defaultValue = default)
		{
			if (dv != null && dv.IsNotNil())
			{
				ret = LuaUtil.UserdataToGameobject(dv);
				return true;
			}

			ret = defaultValue;
			return false;
		}

		public static bool AsVector3([CanBeNull] this DynValue dv, out Vector3 ret, Vector3 defaultValue = default)
		{
			if (dv != null && dv.IsNotNil())
			{
				Vector3? v = LuaUtil.UserdataToPosition(dv);
				if (v != null)
				{
					ret = v.Value;
					return true;
				}
				else
				{
					ret = defaultValue;
					return false;
				}
			}

			ret = defaultValue;
			return false;
		}

		public static bool AsExact<TUserdata>([CanBeNull] this DynValue dv, out TUserdata ret, TUserdata defaultValue = default)
		{
			if (dv != null && dv.Type == DataType.Table && dv.Table.Get("__chainctr").AsBool())
				return AsExact(dv.Table.Get("value"), out ret, defaultValue);

			if (dv != null && dv.Type == DataType.UserData)
			{
				if (dv.UserData.Object is TUserdata ud)
				{
					ret = ud;
					return true;
				}
			}

			ret = default;
			return false;
		}


		public static bool ContainsKey([CanBeNull] this Table tbl, string key)
		{
			if (tbl == null) return false;

			foreach (var pair in tbl.Pairs)
			{
				if (pair.Key.Type == DataType.String && pair.Key.String == key)
					return true;
			}

			return false;
		}

		public static TValue TryGet<TValue>([CanBeNull] this Table tbl, string key, TValue defaultValue = default)
		{
			if (tbl == null) return defaultValue;

			DynValue val = tbl.Get(key);
			if (val.IsNil()) return defaultValue;

			try
			{
				return val.ToObject<TValue>();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			return defaultValue;
		}

		public static bool TryGet<T>([CanBeNull] this Table tbl, string key, out T value, T default_value = default)
		{
			value = default_value;
			if (tbl == null) return false;

			DynValue val = tbl.Get(key);
			if (!val.IsNil())
			{
				try
				{
					value = val.ToObject<T>();
					return true;
				}
				catch { return false; }
			}

			return false;
		}

		public static bool TryGet<T>([CanBeNull] this Table tbl, string[] keys, out T value, T default_value = default)
		{
			value = default_value;
			if (tbl == null) return false;

			for (int i = 0; i < keys.Length; i++)
			{
				DynValue val = tbl.Get(keys[i]);
				if (val.IsNil()) continue;

				try
				{
					value = val.ToObject<T>();
					return true;
				}
				catch (Exception e)
				{
					// ignored
				}
			}

			return false;
		}

		public static void TrySetUsing<T>(this Table tbl, string name, ref T field)
		{
			if (tbl.TryGet(name, out T _field))
				field = _field;
		}

		public static bool GetInto<T>([CanBeNull] this Table tbl, string key, ref T value)
		{
			if (tbl == null) return false;

			DynValue val = tbl.Get(key);
			if (val.IsNil())
				return false;

			try
			{
				value = val.ToObject<T>();
				return true;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
		}

		public static bool TryGet([CanBeNull] this Table tbl, string[] keys, out DynValue value)
		{
			value = DynValue.Nil;
			if (tbl == null) return false;

			for (int i = 0; i < keys.Length; i++)
			{
				DynValue val = tbl.Get(keys[i]);
				if (val.IsNil()) continue;
				value = val;
				return true;
			}

			return false;
		}

		public static bool TryGet([CanBeNull] this Table tbl, string key, out DynValue value)
		{
			value = DynValue.Nil;
			if (tbl == null) return false;

			DynValue val = tbl.Get(key);
			if (val.IsNil()) return false;

			value = val;
			return true;
		}

		public static bool TryGet<T>([CanBeNull] this UserData data, out T value, T default_value = default)
		{
			value = default_value;
			if (data?.Object == null) return false;

			try
			{
				value = (T)data.Object;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static DynValue TryCall([CanBeNull] this Table tbl, Script env, string function, params object[] args)
		{
			if (tbl == null) return DynValue.Nil;
			DynValue func = tbl.Get(function);
			if (func == null || func.IsNil()) return DynValue.Nil;
			return env.Call(func, args);
		}

		public static DynValue TryCallGlobal([CanBeNull] this Script env, string function, params object[] args)
		{
			if (env == null) return DynValue.Nil;
			DynValue func = env.Globals.Get(function);
			if (func == null || func.IsNil())
			{
				Debug.LogWarning($"Tried to call script function that does not exist {function}.");
				return DynValue.Nil;
			}

			try
			{
				return env.Call(func, args);
			}
			catch (ScriptRuntimeException e)
			{
				Debug.LogError(GetPrettyString(e));
				throw;
			}
		}

		public static DynValue ExecuteString([CanBeNull] this Script env, string code)
		{
			if (env == null) return DynValue.Nil;
			return env.DoString(code);
		}

		/// <summary>
		/// Attempts to convert a DynValue to a position.
		/// </summary>
		/// <param name="val"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static bool ToPosition([NotNull] DynValue val, out Vector3 position)
		{
			position = default;
			bool success = false;

			if (val.Type == DataType.UserData)
			{
				UserData usr = val.UserData;
				if (!usr.TryGet(out position))
				{
					if (usr.TryGet(out WorldPoint wp))
						position = wp.position;
				}
			}

			return success;
		}

		private static object[] _argumentArray0 = new object[0];
		private static object[] _argumentArray1 = new object[1];
		private static object[] _argumentArray2 = new object[2];
		private static object[] _argumentArray3 = new object[3];
		private static object[] _argumentArray4 = new object[4];

		public static FunctionInvoker Function([NotNull] this Table tbl, string funcname, int nArguments = 0)
		{
			DynValue dynValue = tbl.Get(funcname);

			if (dynValue.Type != DataType.Function)
				return new FunctionInvoker(null, null);

			Closure func = dynValue.Function;

			switch (nArguments)
			{
				case 0:  return new FunctionInvoker(func, _argumentArray0);
				case 1:  return new FunctionInvoker(func, _argumentArray1);
				case 2:  return new FunctionInvoker(func, _argumentArray2);
				case 3:  return new FunctionInvoker(func, _argumentArray3);
				case 4:  return new FunctionInvoker(func, _argumentArray4);
				default: throw new NotImplementedException();
			}
		}

		public readonly struct FunctionInvoker
		{
			private readonly Closure  _closure;
			private readonly object[] _argumentArray;

			public FunctionInvoker(Closure closure, object[] argumentArray)
			{
				_closure       = closure;
				_argumentArray = argumentArray;
			}

			public FunctionInvoker Arg(int idx, int arg)
			{
				if (_argumentArray == null) return this;

				_argumentArray[idx] = arg;
				return this;
			}

			public FunctionInvoker Arg(int idx, object arg)
			{
				if (_argumentArray == null) return this;

				_argumentArray[idx] = arg;
				return this;
			}

			public bool Invoke()
			{
				return Invoke(out DynValue _);
			}

			public bool Invoke(out DynValue returnValue)
			{
				if (_closure == null)
				{
					returnValue = null;
					return false;
				}

				try
				{
					returnValue = _closure.Call(_argumentArray);
					return true;
				}
				catch (InterpreterException e)
				{
					Console.WriteLine(e.DecoratedMessage);
				}

				returnValue = null;
				return false;
			}
		}

		public static string GetPrettyString(this InterpreterException exc, Table script = null)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("MoonSharp exception:");

			string envName = script.GetEnvName();
			if (envName != null)
			{
				sb.Append("(");
				sb.Append($"env: {envName}");
				sb.Append(") ");
			}

			sb.Append(exc.Message);

			sb.AppendLine();
			sb.AppendLine();

			// Print callstack
			// ----------------------------------------
			if (exc.CallStack != null)
			{
				for (var i = 0; i < exc.CallStack.Count; i++)
				{
					WatchItem entry = exc.CallStack[i];

					if (entry.Location == null) continue;
					if (entry.Location.FromLine == 0 &&
					    entry.Location.FromChar == 0 &&
					    entry.Location.ToLine == 0 &&
					    entry.Location.ToChar == 0) continue;

					SourceCode sc = exc.Script.GetSourceCode(entry.Location.SourceIdx);
					sb.Append(sc.Name);

					if (!string.IsNullOrEmpty(entry.Name))
					{
						sb.Append('.');
						sb.Append(entry.Name ?? "(null)");
					}

					sb.Append(':');
					sb.AppendLine(entry.Location.FromLine.ToString());
				}
			}

			// Print relevant lines of code
			// ----------------------------------------
			if (Lua.Ready)
			{
				try
				{
					SourceCode src = exc.Script.GetSourceCode(exc.Location.SourceIdx);
					if (src != null)
					{
						int row1 = exc.Location.FromLine;
						int col1 = exc.Location.FromChar;
						int row2 = exc.Location.ToLine;
						int col2 = exc.Location.ToChar;

						const int CONTEXT_LINES = 1;

						string text = src.Code;

						sb.AppendLine("------------------------------");
						for (int i = 0, line = 1, col = 1; i < text.Length && line <= row2 + CONTEXT_LINES; i++)
						{
							char ch = text[i];

							bool ctx   = line >= row1 - CONTEXT_LINES && line <= row2 + CONTEXT_LINES;
							bool first = line == row1 && col == col1;
							bool last  = line == row2 && col == col2;

							if (first) sb.Append("<b>");
							if (ctx) sb.Append(ch);
							if (last) sb.Append("</b>");

							if (ch == '\n')
							{
								line++;
								col = 1;
							}
							else
							{
								col++;
							}
						}

						sb.AppendLine("------------------------------");
						sb.AppendLine();
					}
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch { }
			}

			// Print exception
			// ----------------------------------------
			sb.AppendLine();
			sb.Append(exc);
			sb.AppendLine();

			return sb.ToString();
		}

		[CanBeNull]
		public static string GetEnvName([CanBeNull] this Table table)
		{
			if (table == null) return null;
			DynValue name = table.Get("_name");
			if (name.IsNil()) return null;

			return name.String;
		}

		public static void DoFileSafe(this Script script, string name, Table into)
		{
			try
			{
				LuaChangeWatcher.Use(name);
				script.DoFile(name, into);
			}
			catch (InterpreterException e)
			{
				Debug.LogError($"Could not load '{name}':{Environment.NewLine}{e.GetPrettyString()}");
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not load '{name}':{Environment.NewLine}{e}");
			}
		}

		public static void UnexpectedTypeError(this DynValue dv, string functionName)
		{
			Debug.LogError($"Unexpected DynValue '{dv}' given to '{functionName}'! It will be ignored.");
		}

		public static string DumpTable(this Table node)
		{
			// TODO reimplement tprint from util.lua
			throw new NotImplementedException();
			// while (true)
			// {
			// 	int size = 0;
			// 	foreach (TablePair pair in node.Pairs)
			// 	{
			// 		if (pair)
			// 	}
			// }
		}

		public static void Set(this Table me, [CanBeNull] Table other)
		{
			if (other == null) return;
			foreach (TablePair pair in other.Pairs)
			{
				me.Set(pair.Key, pair.Value);
			}
		}

		public static bool IsIndex(this TablePair pair)
		{
			return pair.Key.Type == DataType.Number && pair.Key.Number >= 1;
		}
	}
}