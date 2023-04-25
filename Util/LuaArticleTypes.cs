using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Anjin.Nanokin.Map;
using Anjin.Utils;
using API.PropertySheet;
using Cinemachine;
using Combat;
using Combat.Entities;
using Combat.Launch;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Timeline;
using Util;

namespace Anjin.Util
{
	public static class LuaArticleTypes
	{
		public static readonly Dictionary<string, Type> Aliases =
			new Dictionary<string, Type>
			{
				{ "byte", typeof(byte) },
				{ "sbyte", typeof(sbyte) },
				{ "short", typeof(short) },
				{ "ushort", typeof(ushort) },
				{ "int", typeof(int) },
				{ "uint", typeof(uint) },
				{ "long", typeof(long) },
				{ "ulong", typeof(ulong) },
				{ "float", typeof(float) },
				{ "double", typeof(double) },
				{ "decimal", typeof(decimal) },
				{ "object", typeof(object) },
				{ "bool", typeof(bool) },
				{ "char", typeof(char) },
				{ "string", typeof(string) },
				{ "void", typeof(void) }
			};

		public static readonly (Type, string[])[] Keywords =
		{
			(typeof(EasingFunction), new[] { "func", "function" }),
			(typeof(GameObject), new[] { "obj", "prefab" }),
			(typeof(AnimationCurve), new[] { "curve" }),
			(typeof(float), new[] { "float", "duration", "scale", "value", "num", }),
			(typeof(int), new[] { "int", "power", "turns", "count", }),
			(typeof(string), new[] { "string" }),
			(typeof(RangeOrInt), new[] { "power_range" }),
			(typeof(RangeOrFloat), new[] { "range" }),
			(typeof(API.Spritesheet.Indexing.IndexedSpritesheetAsset), new[] { "spritesheet" }),
			(typeof(PuppetAnimation), new[] { "anim", "animation" }),
			(typeof(AudioDef), new[] { "sfx" }),
			(typeof(AudioClip), new[] { "music", "clip" }),
			(typeof(Color), new[] { "color" }),
			(typeof(CinemachineVirtualCamera), new[] { "vcam" }),
			(typeof(BattleRecipeAsset), new[] { "battle" }),
			(typeof(TimelineAsset), new[] { "timeline" }),
			(typeof(ContactCallback), new[] { "contact", "ct", }),
			(typeof(DelayedContact), new[] { "dcontact" }),
			(typeof(ObjectFighterAsset), new[] { "fighter" }),
		};


		public static readonly Dictionary<string, Type> ManualTypes = new Dictionary<string, Type>();

		public static readonly Dictionary<string, Type> AllTypesAutomatic;

		static void RegManual<T>()
		{
			ManualTypes[typeof(T).Name] = typeof(T);
		}

		static LuaArticleTypes()
		{
			AllTypesAutomatic = new Dictionary<string, Type>();

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				List<Type> types = assembly.GetTypes().ToList();

				foreach (Type type in types)
				{
					if (!AllTypesAutomatic.ContainsKey(type.Name))
					{
						AllTypesAutomatic[type.Name] = type;
					}
				}
			}


			RegManual<Trigger>();
		}

		public static Type FindType([NotNull] string name)
		{
			if (Aliases.TryGetValue(name, out Type t1)) return t1;
			if (ManualTypes.TryGetValue(name, out Type t2)) return t2;
			if (AllTypesAutomatic.TryGetValue(name, out Type t3)) return t3;
			return null;
		}

		/*public static Type CreateGenericType(string name, string argument)
		{

		}*/
	}
}