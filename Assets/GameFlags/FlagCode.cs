using System;
using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Core.Flags
{
	public enum FlagSaveMode
	{
		Unsaved,
		Saved
	}

	public enum FlagLayer {
		//Default, //Unneeded, just a field on the flag, since every one has a default

		//
		Savefile	= 0,

		// Editor override
		Editor		= 1,
	}

	[Serializable]
	public abstract class FlagDefinitionBase
	{
		public string ID;
		public string Name;
		public string Path;

		public bool HasEditorOverride;

		protected FlagDefinitionBase()
		{
			ID   = DataUtil.MakeShortID(9);
			Name = "";
			Path = "";
		}

		protected FlagDefinitionBase(string name) : this()
		{
			Name = name;
		}

		protected FlagDefinitionBase(string name, string path)
		{
			Name = name;
			Path = path;
		}
	}

	[Serializable]
	public abstract class FlagDefinition<T> : FlagDefinitionBase
	{
		public T DefaultValue   = default;
		public T EditorOverride = default;

		public FlagDefinition() { }
		public FlagDefinition(T val) { DefaultValue = val; }
	}

	public interface IFlag { }

	[Serializable] public class BoolFlagDef : FlagDefinition<bool>, IFlag { }

	[Serializable] public class IntFlagDef : FlagDefinition<int>, IFlag { }

	[Serializable] public class FloatFlagDef : FlagDefinition<float>, IFlag { }

	[Serializable] public class StringFlagDef : FlagDefinition<string>, IFlag { }

	public abstract class FlagStateBase
	{
		protected       string             ID;
		public abstract FlagDefinitionBase DefBase { get; }

		protected Dictionary<string, Closure> _luaListeners = new Dictionary<string, Closure>();

		protected FlagStateBase() { ID          = ""; }
		protected FlagStateBase(string id) { ID = id; }

		public abstract object GetValue();
		public abstract void   SetValue(object val, bool forceEvents = false, bool useEditorOverride = false);

		public abstract void ResetValue();

		public void AddListener(string ID, Closure closure) => _luaListeners[ID] = closure;
	}

	public abstract class Flag<Val> : FlagStateBase //where Val : FlagDefinition<Val>
	{
		public string name => Definition.Name;

		[HideInInspector]
		public Val _value;

		[ShowInInspector]
		public Val Value
		{
			get => _value;
			set
			{
				if (!value.Equals(_value))
				{
					Val prev = _value;
					_value = value;

					OnChanged(prev);
				}
			}
		}

		protected abstract void OnChangedEvent();

		public FlagDefinition<Val> Definition;

		public override FlagDefinitionBase DefBase => Definition;

		public Flag()
		{
			Value = default;
		}

		public Flag(FlagDefinition<Val> def)
		{
			Definition = def;
			ID         = def.ID;
			Value      = def.DefaultValue;
			#if UNITY_EDITOR
			if (def.HasEditorOverride)
				Value = def.EditorOverride;
			#endif
		}

		public Flag(FlagDefinition<Val> def, Val value)
		{
			Definition = def;
			ID         = def.ID;
			Value      = value;
		}

		protected Dictionary<string, Action<Val, Val>> _listeners = new Dictionary<string, Action<Val, Val>>();

		public override object GetValue() => Value;

		public override void SetValue(object val, bool forceEvents = false, bool useEditorOverride = false)
		{
			bool changed = false;

			if (useEditorOverride && Definition.HasEditorOverride)
				val = Definition.EditorOverride;

			if (val is Val v) {
				Val prev = _value;

				if (!v.Equals(_value))
				{
					_value  = v;
					changed = true;

				}

				if (forceEvents || changed) {
					OnChanged(prev);
				}
			}

		}

		public override void ResetValue()
		{
			Value = Definition.HasEditorOverride ? Definition.EditorOverride : Definition.DefaultValue;
		}

		public void AddListener([NotNull] string id, Action<Val, Val> callback) => _listeners[id] = callback;
		//public static explicit operator Val(Flag<Val> flag) => flag.Value;

		private void OnChanged(Val prev)
		{

			OnChangedEvent();

			foreach (Closure listener in _luaListeners.Values)
			{
				if (listener != null)
					listener.Call(_value, prev);
			}

			foreach (Action<Val, Val> listener in _listeners.Values)
			{
				if (listener != null)
					listener.Invoke(_value, prev);
			}

			Flags.OnAnyFlagUpdated?.Invoke(this);
		}
	}

	[LuaUserdata] public class BoolFlag : Flag<bool>
	{
		public BoolFlag(FlagDefinition<bool> def) : base(def) { }

		public static explicit operator bool([NotNull] BoolFlag flag) => flag.Value;
		protected override              void OnChangedEvent()         => Flags.boolChanged?.Invoke(this);
	}

	[LuaUserdata] public class IntFlag : Flag<int>
	{
		public IntFlag(FlagDefinition<int> def) : base(def) { }
		protected override void OnChangedEvent() => Flags.intChanged?.Invoke(this);
	}

	[LuaUserdata] public class FloatFlag : Flag<float>
	{
		public FloatFlag(FlagDefinition<float> def) : base(def) { }
		protected override void OnChangedEvent() => Flags.floatChanged?.Invoke(this);
	}

	[LuaUserdata] public class StringFlag : Flag<string>
	{
		public StringFlag(FlagDefinition<string> def) : base(def) { }
		protected override void OnChangedEvent() => Flags.stringChanged?.Invoke(this);
	}


	/*public struct FlagRef<T> where T : IFlag
	{
		public const string NULL_ID = "$NULL$";

		public static FlagRef<T> NullRef = new FlagRef<T>()
		{
			ID = NULL_ID,

		#if UNITY_EDITOR
			Name = "Unreferenced"
		#endif
		};

		public string ID;
		public bool IsNullID => ID == NULL_ID;

		#if UNITY_EDITOR
		public string Name;
		#endif

		public FlagRef(string id, string name)
		{
			ID = id;

		#if UNITY_EDITOR
			Name = name;
		#endif
		}

		public FlagRef(FlagDefinitionBase def)
		{
			if(def != null)
			{
				ID = def.ID;
				#if UNITY_EDITOR
				Name = def.Name;
				#endif
			}
			else
			{
				ID = NULL_ID;
				#if UNITY_EDITOR
				Name = "Unreferenced";
				#endif
			}
		}

		public override string ToString()
		{
			#if UNITY_EDITOR
			return $"[{ID},{Name}]";
			#endif
		}
	}*/
}