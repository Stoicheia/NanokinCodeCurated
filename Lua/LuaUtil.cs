// ReSharper disable UnusedMember.Local

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anjin.Actors;
using Anjin.Core.Flags;
using Anjin.Minigames;
using Anjin.Scripting.Waitables;
using Anjin.UI;
using Anjin.Util;
using Cinemachine;
using Combat;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.Features.TurnOrder.Sampling.Operations.Scoping;
using Combat.Toolkit;
using Data.Combat;
using Data.Nanokin;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using ImGuiNET;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Overworld.Cutscenes;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Util.Extensions;
using Vexe.Runtime.Extensions;
using Closure = MoonSharp.Interpreter.Closure;
using Debug = UnityEngine.Debug;
using Extensions = Util.Extensions.Extensions;

namespace Anjin.Scripting
{
	public static class LuaUtil
	{
		public static readonly DynValue[] NO_ARGS = new DynValue[0];
		public static readonly object[]   ARGS_1  = new object[1];
		public static readonly object[]   ARGS_2  = new object[2];
		public static readonly object[]   ARGS_3  = new object[3];
		public static readonly object[]   ARGS_4  = new object[4];

		private static readonly List<string>        _scratchStrings     = new List<string>();
		private static readonly List<DirectedActor> _scratchActors      = new List<DirectedActor>();
		private static readonly List<GameObject>    _scratchGameObjects = new List<GameObject>();
		private static readonly List<Fighter>       _scratchFighters    = new List<Fighter>();

		private static bool _customConversionRegistered;

		public static readonly RegexOptions MultilineCompiled = RegexOptions.Compiled | RegexOptions.Multiline;

		public static readonly Regex IdentifierRegex       = new Regex(@"[_a-zA-Z][_a-zA-Z0-9]{0,255}", MultilineCompiled);
		public static readonly Regex WordRegex             = new Regex(@"[a-zA-Z][a-zA-Z0-9]{0,255}", MultilineCompiled);
		public static readonly Regex ConfigRegex           = new Regex(@"\@(\w+)(?:\:([\w\d]+))?", MultilineCompiled);
		public static readonly Regex InputRegex            = new Regex(@"&([_a-zA-Z][_a-zA-Z0-9\.]{0,255})(?: *: *([_a-zA-Z][_a-zA-Z0-9<>\[\]]{0,255}))?", MultilineCompiled);
		public static readonly Regex WaitTimeRegex         = new Regex(@"\B\!(?=[0-9\.])(\d*(?:\.\d+)*)\b", MultilineCompiled);
		public static readonly Regex WaitRegex             = new Regex(@"\B\!\b(?<func>[A-z_][A-z_0-9]*)(?=\(| )", MultilineCompiled);
		public static readonly Regex SkipRegex             = new Regex(@"^[ \t]*\.(?<func>[A-z_][A-z_0-9]*)(?=\(| )", MultilineCompiled);
		public static readonly Regex CatchupRegex          = new Regex(@"^[ \t]*\!\!", MultilineCompiled);
		public static readonly Regex TopLevelFunctionRegex = new Regex(@"function (\w+)\(.*?\).+?\n(?:end)", MultilineCompiled | RegexOptions.Singleline | RegexOptions.RightToLeft);
		public static readonly Regex PostassignRegex       = new Regex(@"([\!|\.])(.+)\-\>\s*(\w+)", MultilineCompiled);
		// public static readonly Regex DotNumber             = new Regex(@"\b(?<=[A-Za-z])\.(\d+)\b", MultilineCompiled);

		public static readonly string[] battleRequires =
		{
			"combat-macros",
			"anim-api",
			"battle-api",
			"target-api",
			"std-camera",
			"std-anim"
		};

		public static readonly string[] battleUIRequires =
		{
			"combat-macros",
			//"anim-api",
			"battle-api",
			"target-ui-api",
			/*"std-camera",
			"std-anim"*/
		};

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			_scratchActors.Clear();
			_scratchFighters.Clear();
			_scratchGameObjects.Clear();
			_customConversionRegistered = false;
		}

		[NotNull]
		public static List<string> DiscoverRequires([NotNull] string code)
		{
			MatchCollection identifierMatches =
				Regex.Matches(code, @"require[ \(]['""](.+)['""]\)*", RegexOptions.Multiline);

			var list = new List<string>();
			foreach (Match match in identifierMatches)
				list.Add(match.Groups[1].Value);

			return list;
		}

		[NotNull]
		public static List<FunctionDeclaration> DiscoverTopLevelFunctions([NotNull] string code)
		{
			var             ret               = new List<FunctionDeclaration>();
			MatchCollection identifierMatches = TopLevelFunctionRegex.Matches(code);

			foreach (Match match in identifierMatches)
				ret.Add(new FunctionDeclaration
				{
					name = match.Groups[1].Value,
					code = match.Value
				});

			return ret;
		}

		private static string[] _lineEnds   = { "\r\n", "\n", "\r" };
		private static char[]   _whitespace = { ' ' };

		public static string PreprocessLua(string code)
		{
			string[] lines = code.Split(_lineEnds, StringSplitOptions.None);
			for (var i = 0; i < lines.Length; i++)
			{
				string line    = lines[i];
				string trimmed = line.TrimStart(_whitespace).TrimEnd(_whitespace);
				if (trimmed == "AUTO_COST()")
				{
					lines[i] = "--&cost:int\n";
				}
			}

			code = lines.JoinString("\n");

			return code;
		}

		/// <summary>
		/// Transpile Anjin secret flavors to Lua syntax understood by Moonsharp.
		///
		/// Anjin secret flavors ought to be a superset of Lua, so calling this
		/// function several times on the same code should not cause problems.
		/// (but is not recommended for performance reasons)
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		public static string TranspileToLua(string code)
		{
			void Apply(string name, Regex regex, string replacement)
			{
				Profiler.BeginSample(name);
				code = regex.Replace(code, replacement);
				Profiler.EndSample();
			}

			Profiler.BeginSample("To Standard Lua");

			code = PreprocessLua(code);

			// remap identifier.0 to identifier[0] for more consistent apis
			// Apply(nameof(DotNumber), DotNumber, @"[$1]");

			// Allows using &input_variables to define external inputs to the script
			Apply(nameof(InputRegex), InputRegex, "$1");

			// Allow using @name:value to expose the value for external configuration
			Apply(nameof(ConfigRegex), ConfigRegex, "$2");

			// Allow assignment after the function call
			Apply(nameof(PostassignRegex), PostassignRegex, "$1var $3 = $2");

			// Allows using !! to catchup all non-waited waitables
			Apply(nameof(CatchupRegex), CatchupRegex, "; __catchup() ;");

			// Allows using !1 or !1.0 or !.7 to wait for the number of seconds
			Apply(nameof(WaitTimeRegex), WaitTimeRegex, "; __force($1) ;");

			// Allows using !function to force
			Apply(nameof(WaitRegex), WaitRegex, "; __force() ; ${func}");

			// Allows using .function to skip it (plays but does not wait for completion)
			Apply(nameof(SkipRegex), SkipRegex, "; __skip() ; ${func}");
			Profiler.EndSample();

			return code;
		}


		public static void RegisterUserdata([NotNull] Script script)
		{
			Table globals = script.Globals;

			RegisterCustomConversions();


			UserData.RegisterAssembly(Assembly.GetAssembly(typeof(LuaUtil)));

			// Base

			UserData.RegisterType<Type>();
			UserData.RegisterType<Task>();
			UserData.RegisterType<ITuple>();

			/*UserData.RegisterType<Vector2>();
			UserData.RegisterType<Vector2Int>();
			UserData.RegisterType<Vector3>();
			UserData.RegisterType<Vector4>();
			UserData.RegisterType<Rect>();
			UserData.RegisterType<Quaternion>();
			UserData.RegisterType<Color>();*/
			UserData.RegisterType<AnimationCurve>();
			UserData.RegisterType<AudioClip>();
			UserData.RegisterType<InputsLua>();

			UserData.RegisterExtensionType(typeof(DOTweenModuleAudio));
			RegisterNamed<TweenerCore<float, float, FloatOptions>>(script);

			UserData.RegisterType<Scene>();

			UserData.RegisterType<KeyCode>(friendlyName: "keycode");
			UserData.RegisterType<KeyCode>(friendlyName: "key_code");
			globals["key_code"] = UserData.CreateStatic<KeyCode>();

			RegisterNamedStatic<Vector2>(script);
			RegisterNamedStatic<Vector2Int>(script);
			RegisterNamedStatic<Vector3>(script);
			//script.Globals["Vector3"] = UserData.CreateStatic<Vector3>();
			RegisterNamedStatic<Vector4>(script);
			RegisterNamedStatic<Quaternion>(script);
			RegisterNamedStatic<Rect>(script);
			RegisterNamedStatic<Color>(script);
			RegisterNamedStatic<Physics>(script);

			RegisterNamed<Collision>(script);
			RegisterNamed<Collider>(script);
			RegisterNamed<SphereCollider>(script);
			RegisterNamed<BoxCollider>(script);
			RegisterNamed<MeshCollider>(script);

			RegisterNamed<AudioSource>(script);

			RegisterNamed<ParticleSystem>(script);
			RegisterNamedEnum<ParticleSystemStopBehavior>(script);

			RegisterNamed<PlayableDirector>(script);

			RegisterNamed<CinemachineVirtualCamera>(script);
			RegisterNamed<CameraState>(script);
			RegisterNamed<LensSettings>(script);
			RegisterNamed<CinemachineBlendDefinition>(script);

			RegisterNamedEnum<CinemachineBlendDefinition.Style>(script, "cinemachine_blend_style", true);
			RegisterNamedEnum<ImGuiWindowFlags>(script, "imgui_window_flags", true);
			RegisterNamedEnum<ImGuiCond>(script, "imgui_cond", true);

			// Obsolete; do not use!
			RegisterNamedEnum<CinemachineBlendDefinition.Style>(script, "cm_blend", true);
			RegisterNamedEnum<CinemachineBlendDefinition.Style>(script, "cmblend", true);

			RegisterNamedEnum<Ease>(script);


			RegisterNamed<SimpleAnimation>(script);


			#region EaseTypes

			globals["linear"]       = Ease.Linear;
			globals["in_quad"]      = Ease.InQuad;
			globals["out_quad"]     = Ease.OutQuad;
			globals["in_out_quad"]  = Ease.InOutQuad;
			globals["in_sine"]      = Ease.InSine;
			globals["out_sine"]     = Ease.OutSine;
			globals["in_out_sine"]  = Ease.InOutSine;
			globals["in_flash"]     = Ease.InFlash;
			globals["out_flash"]    = Ease.OutFlash;
			globals["in_out_flash"] = Ease.InOutFlash;

			// New ease naming scheme: iease oease and ioease. Easy to remember, easy to type, fun, new age
			globals["linear"] = Ease.Linear;

			globals["iquad"]    = Ease.InQuad;
			globals["icubic"]   = Ease.InCubic;
			globals["iquart"]   = Ease.InQuart;
			globals["iquint"]   = Ease.InQuint;
			globals["isine"]    = Ease.InSine;
			globals["iosine"]   = Ease.InOutSine;
			globals["iflash"]   = Ease.InFlash;
			globals["iback"]    = Ease.InBack;
			globals["ibounce"]  = Ease.InBounce;
			globals["icirc"]    = Ease.InCirc;
			globals["ielastic"] = Ease.InElastic;
			globals["iexpo"]    = Ease.InExpo;

			globals["oquad"]    = Ease.OutQuad;
			globals["ocubic"]   = Ease.OutCubic;
			globals["oquart"]   = Ease.OutQuart;
			globals["oquint"]   = Ease.OutQuint;
			globals["osine"]    = Ease.OutSine;
			globals["oosine"]   = Ease.OutSine;
			globals["oflash"]   = Ease.OutFlash;
			globals["oback"]    = Ease.OutBack;
			globals["obounce"]  = Ease.OutBounce;
			globals["ocirc"]    = Ease.OutCirc;
			globals["oelastic"] = Ease.OutElastic;
			globals["oexpo"]    = Ease.OutExpo;

			globals["ioquad"]    = Ease.InOutQuad;
			globals["iocubic"]   = Ease.InOutCubic;
			globals["ioquart"]   = Ease.InOutQuart;
			globals["ioquint"]   = Ease.InOutQuint;
			globals["iosine"]    = Ease.InOutSine;
			globals["ioosine"]   = Ease.InOutSine;
			globals["ioflash"]   = Ease.InOutFlash;
			globals["ioback"]    = Ease.InOutBack;
			globals["iobounce"]  = Ease.InOutBounce;
			globals["iocirc"]    = Ease.InOutCirc;
			globals["ioelastic"] = Ease.InOutElastic;
			globals["ioexpo"]    = Ease.InOutExpo;

			#endregion

			// Same for cinemachine, with cm prefix
			globals["cmlinear"] = CinemachineBlendDefinition.Style.Linear;
			globals["cmiease"]  = CinemachineBlendDefinition.Style.EaseIn;
			globals["cmoease"]  = CinemachineBlendDefinition.Style.EaseOut;
			globals["cmioease"] = CinemachineBlendDefinition.Style.EaseInOut;
			globals["cmihard"]  = CinemachineBlendDefinition.Style.HardIn;
			globals["cmohard"]  = CinemachineBlendDefinition.Style.HardOut;
			globals["cmcut"]    = CinemachineBlendDefinition.Style.Cut;
			globals["cmcustom"] = CinemachineBlendDefinition.Style.Custom;


			Type[] all_types = Assembly.GetExecutingAssembly().GetTypes();

			Lua.LogTrace("--", "Get Types");
			//	Auto Register
			//---------------------------------------------
			{
				IEnumerable<Type> types = all_types.Where(x => CustomAttributeExtensions.GetCustomAttribute<LuaUserdataAttribute>(x) != null);
				foreach (Type type in types)
				{
					LuaUserdataAttribute attrib = CustomAttributeExtensions.GetCustomAttribute<LuaUserdataAttribute>(type);

					if (!type.IsInterface)
					{
						globals[attrib.TypeName ?? attrib.StaticName ?? type.Name] = type;
						UserData.RegisterType(type, InteropAccessMode.Reflection, type.Name);
						if (attrib.StaticName != null || attrib.StaticAuto)
							globals[attrib.StaticName ?? type.Name] = UserData.CreateStatic(type);
					}

					if (attrib.Descendants)
					{
						IEnumerable<Type> children = Extensions.GetChildren(type);
						foreach (Type child in children)
						{
							if (!child.IsInterface)
								UserData.RegisterType(child, InteropAccessMode.Reflection, type.Name);
						}
					}
				}
			}

			Lua.LogTrace("--", "Auto Register");

			//	Proxies
			//---------------------------------------------
			var proxies = new List<Type>();
			Assembly.GetExecutingAssembly().GetTypes().GetDerivedFromGeneric(typeof(LuaProxy<>), ref proxies);

			Lua.LogTrace("--", "Get Proxy Types");

			foreach (Type proxy in proxies)
				if (!proxy.IsGenericType)
				{
					Type target_type = proxy.BaseType.GetGenericArguments()[0];
					var  factory     = new LuaProxyFactory(target_type, proxy);
					UserData.RegisterProxyType(factory);

					globals[target_type.Name] = target_type;

					//Debug.Log("Register " + target_type.Name + " : " + target_type);

					LuaProxyTypesAttribute secondary = CustomAttributeExtensions.GetCustomAttribute<LuaProxyTypesAttribute>(proxy);
					if (secondary != null)
					{
						foreach (Type secondary_type in secondary.Types)
						{
							UserData.RegisterProxyType(new LuaProxyFactory(secondary_type, proxy));
							globals[secondary_type.Name] = secondary_type;

							// Also register descendants
							if (secondary.Descendants)
							{
								IEnumerable<Type> children = Extensions.GetChildren(secondary_type);
								foreach (Type child in children)
								{
									UserData.RegisterProxyType(new LuaProxyFactory(child, proxy));
									globals[child.Name] = child;
								}
							}
						}
					}
				} /*else {
					//Debug.Log("Generic proxy type: " + proxy);

					Type constraint_type = null;

					//TODO: This is potentially faulty, clean up
					var genericArgs = proxy.GetGenericArguments();
					var constraint = genericArgs[0].GetGenericParameterConstraints()[0];

					var target_type = constraint;
					var factory     = new LuaProxyFactory(target_type, proxy);
					UserData.RegisterProxyType(factory);

					Env.Globals[target_type.Name] = target_type;

					var secondary = proxy.GetCustomAttribute<LuaProxyTypesAttribute>();
					if (secondary != null) {
						foreach (var secondary_type in secondary.Types) {
							UserData.RegisterProxyType(new LuaProxyFactory(secondary_type, proxy));
							Env.Globals[secondary_type.Name] = secondary_type;
						}
					}
				}*/


			Lua.LogTrace("--", "Proxies");

			//	Global Methods
			//---------------------------------------------
			{
				foreach (Type type in all_types)
					RegisterGlobals(globals, type, true);
			}

			Lua.LogTrace("--", "Global Methods");

			//	Enums
			//---------------------------------------------
			{
				IEnumerable<Type> types = all_types.Where(x =>
					x.IsEnum && CustomAttributeExtensions.GetCustomAttribute<LuaEnumAttribute>(x) != null);

				foreach (Type type in types)
				{
					LuaEnumAttribute attrib = CustomAttributeExtensions.GetCustomAttribute<LuaEnumAttribute>(type);
					UserData.RegisterType(type);

					if (attrib.StringConvertible)
					{
						Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion(type, (_, v) => DynValue.NewString(v.ToString().ToLower()));
						Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, type, v => Enum.Parse(type, v.String));
					}

					globals[attrib.Name ?? type.Name] = UserData.CreateStatic(type);
				}
			}

			Lua.LogTrace("--", "Enums");

			globals["Flags"] = new FlagsLua();


			// Common character IDs
			globals["c_nas"]    = UserData.Create(Character.Nas);
			globals["c_jatz"]   = UserData.Create(Character.Jatz);
			globals["c_serio"]  = UserData.Create(Character.Serio);
			globals["c_peggie"] = UserData.Create(Character.Peggie);
			globals["c_david"]  = UserData.Create(Character.David);
			globals["c_neai"]   = UserData.Create(Character.NeAi);
			globals["c_koa"]    = UserData.Create(Character.Koa);
		}

		public static void RegisterGlobals(Table globals, Type type, bool attribute_only)
		{
			foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public |
			                                              BindingFlags.NonPublic))
			{
				LuaGlobalFuncAttribute attr = method.GetCustomAttribute<LuaGlobalFuncAttribute>();
				if (!attribute_only || attr != null)
					globals[attr?.Name ?? method.Name] = method;
			}
		}


		//TODO: Add to_number method?
		public static void RegisterNamedEnum<E>(Script script,
			[CanBeNull] string                         lua_name            = null,
			bool                                       convert_value_names = false)
			where E : Enum
		{
			Type type = typeof(E);
			if (!type.IsEnum) return;

			UserData.RegisterType<E>();

			if (lua_name == null)
				lua_name = type.Name;

			var table = new Table(script);
			script.Globals[lua_name] = table;

			var values = (E[])type.GetEnumValues();

			for (var i = 0; i < values.Length; i++)
			{
				E      val  = values[i];
				string name = type.GetEnumName(val);

				if (convert_value_names)
					//name = name.InsertCharacterBeforeUpperCaseLetters('_');
					name = name.ToLower();

				table[name] = val; //DynValue.NewNumber(Convert.ToInt32(val));
			}
		}

		public static void RegisterNamed<TType>(Script script, bool name = true)
		{
			UserData.RegisterType<TType>();
			if (name)
				script.Globals[typeof(TType).Name] = typeof(TType);
		}

		public static void RegisterNamedStatic<TType>(Script script, bool name = true)
		{
			UserData.RegisterType<TType>();
			if (name)
				script.Globals[typeof(TType).Name] = UserData.CreateStatic<TType>();
		}


		//	UNITY TYPE CONSTRUCTORS
		//----------------------------------------------------------------------------------

		public static void RegisterConstructors([NotNull] Table tbl)
		{
			tbl["vec2"] = (Func<DynValue, DynValue, Vector2>)new_vec2;
			tbl["vec3"] = (Func<DynValue, DynValue, DynValue, Vector3>)new_vec3;
			tbl["vec4"] = (Func<DynValue, DynValue, DynValue, DynValue, Vector4>)new_vec4;

			tbl["vec2int"] = (Func<DynValue, DynValue, Vector2Int>)new_vec2int;

			tbl["rect"] = (Func<DynValue, DynValue, DynValue, DynValue, Rect>)new_rect;

			tbl["color"]     = (Func<DynValue, DynValue, DynValue, DynValue, Color>)new_color;
			tbl["color_hsv"] = (Func<DynValue, DynValue, DynValue, DynValue, Color>)new_color_hsv;

			tbl["blend_def"]    = (Func<CinemachineBlendDefinition.Style, float, CinemachineBlendDefinition>)new_blend_def;
			tbl["cm_blend_def"] = tbl["blend_def"];
		}

		public static Vector2 new_vec2([NotNull] DynValue arg1, DynValue arg2)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("x", out float x);
				tbl.TryGet("y", out float y);

				return new Vector2(x, y);
			}
			else
			{
				double? x = arg1.CastToNumber();
				double? y = arg2.CastToNumber();
				return new Vector2(
					(float)x.GetValueOrDefault(0),
					(float)y.GetValueOrDefault(0));
			}
		}

		public static Vector3 new_vec3([NotNull] DynValue arg1, DynValue arg2, DynValue arg3)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("x", out float x);
				tbl.TryGet("y", out float y);
				tbl.TryGet("z", out float z);

				return new Vector3(x, y, z);
			}
			else
			{
				double? x = arg1.CastToNumber();
				double? y = arg2.CastToNumber();
				double? z = arg3.CastToNumber();
				return new Vector3(
					(float)x.GetValueOrDefault(0),
					(float)y.GetValueOrDefault(0),
					(float)z.GetValueOrDefault(0));
			}
		}

		public static Vector4 new_vec4([NotNull] DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("x", out float x);
				tbl.TryGet("y", out float y);
				tbl.TryGet("z", out float z);
				tbl.TryGet("w", out float w);

				return new Vector4(x, y, z, w);
			}
			else
			{
				double? x = arg1.CastToNumber();
				double? y = arg2.CastToNumber();
				double? z = arg3.CastToNumber();
				double? w = arg4.CastToNumber();
				return new Vector4(
					(float)x.GetValueOrDefault(0),
					(float)y.GetValueOrDefault(0),
					(float)z.GetValueOrDefault(0),
					(float)w.GetValueOrDefault(0));
			}
		}

		public static Vector2Int new_vec2int([NotNull] DynValue arg1, DynValue arg2)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("x", out int x);
				tbl.TryGet("y", out int y);

				return new Vector2Int(x, y);
			}
			else
			{
				double? x = arg1.CastToNumber();
				double? y = arg2.CastToNumber();
				return new Vector2Int(
					(int)x.GetValueOrDefault(0),
					(int)y.GetValueOrDefault(0));
			}
		}

		public static Rect new_rect([NotNull] DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;

				tbl.TryGet("x", out float x);
				tbl.TryGet("y", out float y);
				tbl.TryGet("width", out float width);
				tbl.TryGet("height", out float height);

				bool is_pos  = tbl.TryGet("position", out Vector2 position);
				bool is_size = tbl.TryGet("size", out Vector2 size);

				if (!is_pos && !is_size)
					return new Rect(x, y, width, height);
				if (is_pos && is_size)
					return new Rect(position, size);
				if (is_pos)
					return new Rect(position, new Vector2(width, height));
				if (is_size)
					return new Rect(new Vector2(x, y), size);

				return new Rect();
			}

			if (arg1.Type == DataType.UserData)
			{
				UserData data = arg1.UserData;
				data.TryGet(out Vector2 pos);
				data.TryGet(out Vector2 size);

				return new Rect(pos, size);
			}

			{
				double? x = arg1.CastToNumber();
				double? y = arg2.CastToNumber();
				double? w = arg3.CastToNumber();
				double? h = arg4.CastToNumber();
				return new Rect(
					(float)x.GetValueOrDefault(0),
					(float)y.GetValueOrDefault(0),
					(float)w.GetValueOrDefault(0),
					(float)h.GetValueOrDefault(0));
			}
		}

		public static Color new_color([NotNull] DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("r", out float r);
				tbl.TryGet("g", out float g);
				tbl.TryGet("b", out float b);

				bool is_hsv = tbl.TryGet("h", out float h) |
				              tbl.TryGet("s", out float s) |
				              tbl.TryGet("v", out float v);

				tbl.TryGet("a", out float a, 1);

				if (is_hsv)
				{
					var c = Color.HSVToRGB(h, s, v);
					c.a = a;
					return c;
				}

				return new Color(r, g, b, a);
			}
			else
			{
				double? r = arg1.CastToNumber();
				double? g = arg2.CastToNumber();
				double? b = arg3.CastToNumber();
				double? a = arg4.CastToNumber();
				return new Color(
					(float)r.GetValueOrDefault(1),
					(float)g.GetValueOrDefault(1),
					(float)b.GetValueOrDefault(1),
					(float)a.GetValueOrDefault(1));
			}
		}

		public static Color new_color_hsv([NotNull] DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
		{
			if (arg1.Type == DataType.Table)
			{
				Table tbl = arg1.Table;
				tbl.TryGet("h", out float h);
				tbl.TryGet("s", out float s);
				tbl.TryGet("v", out float v);

				tbl.TryGet("a", out float a, 1);

				var c = Color.HSVToRGB(h, s, v);
				c.a = a;
				return c;
			}
			else
			{
				double? h = arg1.CastToNumber();
				double? s = arg2.CastToNumber();
				double? v = arg3.CastToNumber();
				double? a = arg4.CastToNumber();

				var c = Color.HSVToRGB(
					(float)h.GetValueOrDefault(0),
					(float)s.GetValueOrDefault(0),
					(float)v.GetValueOrDefault(1));

				c.a = (float)a.GetValueOrDefault(1);
				return c;
			}
		}

		public static CinemachineBlendDefinition new_blend_def(
			CinemachineBlendDefinition.Style style = CinemachineBlendDefinition.Style.Cut,
			float                            time  = 0
		)
		{
			return new CinemachineBlendDefinition(style, time);
		}

		public static void RegisterCustomConversions()
		{
			if (_customConversionRegistered) return;
			_customConversionRegistered = true;

			#region Core Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(SceneReference), v => new SceneReference(v.String));

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
				DataType.UserData,
				typeof(bool),
				v => v.UserData.Object is BoolFlag bf ? (object)bf.Value : null);

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
				DataType.Table,
				typeof(ICoroutineWaitable),
				dv =>
				{
					var waitables = new List<ICoroutineWaitable>();

					for (var i = 1; i <= dv.Table.Length; i++)
					{
						if (dv.Table.Get(i).UserData.Object is ICoroutineWaitable waitable)
						{
							waitables.Add(waitable);
						}
					}

					return new ManagedWaitableGroup(waitables);
				});

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(WorldPoint), UserdataToWorldpoint);

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(List<string>), v =>
			{
				_scratchStrings.ClearAndAdd(v.String);
				return _scratchStrings;
			});

			Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<GameText>((_, v) => DynValue.NewString(v.GetString()));

			#endregion

			#region Unity Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(GameObject), UserdataToGameobject);
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(Transform), UserdataToTransform);
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(Vector3), dv => UserdataToPosition(dv) ?? null);

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData,
				typeof(List<GameObject>), v =>
				{
					_scratchGameObjects.Clear();
					_scratchGameObjects.Add(UserdataToGameobject(v));
					return _scratchGameObjects;
				});

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table,
				typeof(List<GameObject>), v =>
				{
					_scratchGameObjects.Clear();

					for (var i = 1; i <= v.Table.Length; i++)
					{
						DynValue elem = v.Table.Get(i);
						_scratchGameObjects.Add(UserdataToGameobject(elem));
					}

					return _scratchGameObjects;
				});

			#endregion

			#region Minigame Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(MinigameResults), MinigameResults.FromTable);

			#endregion

			#region Coplayer Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData,
				typeof(List<DirectedActor>), v =>
				{
					_scratchActors.Clear();
					_scratchActors.Add((DirectedActor)v.UserData.Object);
					return _scratchActors;
				});

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table,
				typeof(List<DirectedActor>), v =>
				{
					_scratchActors.Clear();

					for (var i = 1; i <= v.Table.Length; i++)
					{
						DynValue elem = v.Table.Get(i);
						_scratchActors.Add((DirectedActor)elem.UserData.Object);
					}

					return _scratchActors;
				});

			#endregion


			#region GameObject Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(List<GameObject>), v =>
			{
				_scratchGameObjects.Clear();

				for (var i = 1; i <= v.Table.Length; i++)
				{
					DynValue elem = v.Table.Get(i);
					_scratchGameObjects.Add(((DirectedActor)elem.UserData.Object).gameObject);
				}

				return _scratchGameObjects;
			});

			#endregion

			#region Actor Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(Actor), v => ActorRegistry.FindByPath(v.String));
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(Actor), v => ActorRegistry.FindByPath(v.String));
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(Actor), v =>
			{
				if (v.UserData.Object is DirectedActor directedActor)
					return directedActor.actor;

				return null;
			});

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(Actor), v => ActorRegistry.FindByPath(v.String));
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(Actor), v => ActorRegistry.FindByPath(v.String));

			#endregion

			#region Combat Conversions

			RegisterEnumStringConverter<LimbType>();

			// Combat
			// ----------------------------------------

			RegisterEnumStringConverter<Elements>();
			RegisterEnumStringConverter<Natures>();
			RegisterEnumStringConverter<BattleBrains>();

			// Turn order
			RegisterEnumStringConverter<ActionMarker>();
			RegisterEnumStringConverter<RelativeLR>();
			RegisterEnumStringConverter<ListSegments>();
			RegisterEnumStringConverter<Signals>();
			RegisterEnumStringConverter<ProcStat>();

			// Coplayer
			RegisterEnumStringConverter<CoroutineMove.LookDir>();

			#region Fighter Conversions

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.UserData, typeof(Fighter), UserdataToFighter);

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
				DataType.UserData,
				typeof(List<Fighter>), dv =>
				{
					_scratchFighters.Clear();

					switch (dv.UserData.Object)
					{
						case Targeting targeting: return targeting.fighters;
						case Target target:       return target.fighters;
						default:
						{
							if (UserdataToFighter(dv) is Fighter fighter)
								_scratchFighters.Add(fighter);

							return _scratchFighters;
						}
					}
				});

			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(
				DataType.Table,
				typeof(List<Fighter>), dv =>
				{
					_scratchFighters.Clear();

					Table tbl = dv.Table;
					for (var i = 1; i <= tbl.Length; i++)
					{
						if (UserdataToFighter(tbl.Get(i)) is Fighter fighter)
							_scratchFighters.Add(fighter);
					}

					return _scratchFighters;
				});

			#endregion

			#endregion
		}

		private static void RegisterEnumStringConverter<TEnum>()
			where TEnum : struct, Enum
		{
			Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion(typeof(TEnum), (_, v) => DynValue.NewString(v.ToString().ToLower()));
			Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.String, typeof(TEnum), v =>
			{
				if (ParseEnum(v, out TEnum val))
					return val;

				throw new ArgumentException($"String {v.String} could not be parsed to {typeof(TEnum).Name}!");
			});
		}

		public static bool ParseEnum<TEnum>([NotNull] DynValue v, out TEnum val) where TEnum : struct, Enum
		{
			return ParseEnum(v.String, out val);
		}

		public static bool ParseEnum<TEnum>(string v, out TEnum val) where TEnum : struct, Enum
		{
			string str = v;
			if (str.Contains("-"))
				str = v.Replace("-", "_");

			return Enum.TryParse(str, out val);
		}

		[CanBeNull]
		public static object UserdataToWorldpoint([NotNull] DynValue v)
		{
			if (v.AsWorldPoint(out WorldPoint wp))
				return wp;

			return null;
		}

		[CanBeNull]
		public static ProcEffect DynvalueToProcEffect([NotNull] DynValue dv)
		{
			// ReSharper disable once InlineOutVariableDeclaration
			ProcEffect ret;

			if (dv.AsExact(out ret)) return ret;
			else if (dv.AsExact(out State buf)) return new AddState(buf);
			else if (dv.AsExact(out TurnFunc cl)) return new TurnFuncEffect(cl.closure);
			else if (dv.AsExact(out Closure func)) return new DynamicEffect(func);

			return null;
		}

		public static Fighter UserdataToFighter([NotNull] DynValue v)
		{
			object userData = v.UserData.Object;
			switch (userData)
			{
				// case Slot slot:           return slot.owner;
				case GameObject go:       return go.GetComponent<Fighter>();
				case Targeting targeting: return targeting.fighters.First();
				case Target target:       return target.fighters.First();
				case Fighter fighter:     return fighter;
				default:
					return null;
			}
		}

		public static GameObject UserdataToGameobject([NotNull] DynValue v)
		{
			object userData = v.UserData.Object;
			switch (userData)
			{
				case GameObject go:
					return go.gameObject;
				case MonoBehaviour behavior:
					return behavior.gameObject;
				case DirectedBase cutMember:
					return cutMember.gameObject;
				// Combat
				case Battle battle:
					return battle.arena.gameObject;
				case Fighter fighter:
					return fighter.actor.gameObject;
				case Target target:
					return target.fighters[0].actor.gameObject;
				case Targeting targeting:
					return targeting.fighters[0].actor.gameObject;
				case Slot slot:
					return slot.actor.gameObject;
				case Proc proc:
					switch (proc.kind)
					{
						case ProcKinds.Fighter:
							return proc.fighters[0].actor.gameObject;
						case ProcKinds.Slot:
							return proc.slots[0].actor != null
								? proc.slots[0].actor.gameObject
								: null;
						case ProcKinds.Default:
							return proc.fighters[0].actor.gameObject;
						default:
							throw new ArgumentOutOfRangeException();
					}

					return null;
				default:
					return null;
			}
		}

		[CanBeNull]
		public static Transform UserdataToTransform([NotNull] DynValue v)
		{
			object userData = v.UserData.Object;
			switch (userData)
			{
				case GameObject go:          return go.transform;
				case MonoBehaviour behavior: return behavior.transform;
				case DirectedBase cutMember: return cutMember.gameObject.transform;
				// Combat
				case Battle battle:       return battle.arena.transform;
				case Fighter fighter:     return fighter.actor.transform;
				case Slot slot:           return slot.actor.transform;
				case Target target:       return target.fighters[0].actor.transform;
				case Targeting targeting: return targeting.fighters[0].actor.transform;
				default:
					return null;
			}
		}

		public static Vector3? UserdataToPosition([NotNull] DynValue v)
		{
			if (v.Type != DataType.UserData) return null;
			object userData = v.UserData.Object;
			switch (userData)
			{
				case Vector3 v3: return v3;

				case WorldPoint wp:
					if (wp.TryGet(out Vector3 p))
						return p;
					break;

				case GameObject go:          return go.transform.position;
				case Transform tfm:          return tfm.position;
				case MonoBehaviour behavior: return behavior.transform.position;
				case DirectedBase cutMember: return cutMember.gameObject.transform.position;
				// Combat
				case Fighter fighter:     return fighter.actor.transform.position;
				case Target target:       return target.position;
				case Targeting targeting: return targeting.Centroid;
				default:                  return null;
			}

			return null;
		}

		public struct FunctionDeclaration
		{
			public string name;
			public string code;
		}

		public class LuaProxyFactory : IProxyFactory
		{
			public LuaProxyFactory(Type targetType, Type proxyType)
			{
				TargetType = targetType;
				ProxyType  = proxyType;
			}

			public object CreateProxyObject(object o)
			{
				object target     = o;
				Type   proxy_type = ProxyType;

				object proxy;

				if (!ProxyType.IsGenericType)
				{
					proxy = Activator.CreateInstance(ProxyType);
				}
				else
				{
					proxy_type = ProxyType.MakeGenericType(TargetType);
					proxy      = Activator.CreateInstance(proxy_type);
				}

				FieldInfo field =
					proxy_type.GetField("proxy", BindingFlags.NonPublic | BindingFlags.Instance);
				field.SetValue(proxy, target);

				return proxy;
			}

			public Type TargetType { get; }
			public Type ProxyType  { get; }
		}

		public static void LoadBattleRequires(Table table)
		{
			foreach (string require in battleRequires)
			{
				Lua.LoadFileInto(require, table);
			}
		}

		public static State DynvalToBuff(object userDataObject) => throw new NotImplementedException();

		[NotNull]
		public static object[] Args(object o1)
		{
			ARGS_1[0] = o1;
			return ARGS_1;
		}

		[NotNull]
		public static object[] Args(object o1, object o2)
		{
			ARGS_2[0] = o1;
			ARGS_2[1] = o2;
			return ARGS_2;
		}

		[NotNull]
		public static object[] Args(object o1, object o2, object o3)
		{
			ARGS_3[0] = o1;
			ARGS_3[1] = o2;
			ARGS_3[2] = o3;
			return ARGS_3;
		}

		[NotNull]
		public static object[] Args(object o1, object o2, object o3, object o4)
		{
			ARGS_4[0] = o1;
			ARGS_4[1] = o2;
			ARGS_4[2] = o3;
			ARGS_4[3] = o4;
			return ARGS_4;
		}

		[LuaGlobalFunc]
		public static void debug_break()
		{
#if UNITY_EDITOR
			Debug.Log("DEBUG BREAK (EDITOR PAUSED)");
			Debug.Break();
#endif
		}
	}
}