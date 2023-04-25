using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Util;

namespace Util {

	[Serializable]
	public struct Option<T> where T : struct {
		public bool IsSet;
		public T    Value;

		public Option(T value) : this()
		{
			Value = value;
			IsSet = true;
		}

		public static implicit operator T(Option<T> opt) => opt.Value;
		public static implicit operator Option<T>(T opt) => new Option<T>(opt);

		public bool TryGet(out T value)
		{
			value = Value;
			return IsSet;
		}

		public T ValueOrDefault(T Default) => IsSet ? Value : Default;

		public override string ToString() => $"Option<{typeof(T).Name}>({Value}, set: {IsSet})";

		public static class Tests
		{
			[MenuItem("Anjin/Tests/Overridable")]
			public static void Test()
			{
				Overridable<float> o1 = new Overridable<float>(56);
				Option<float>      o2 = new Option<float>(9);
				Option<float>      o3 = new Option<float>(13123);
				Option<float>      o4 = new Option<float>(0.97654f);

				Debug.Log(o1.Apply(o2).Apply(o3).Apply(o4));
				o4.IsSet = false;
				Debug.Log(o1.Apply(o2).Apply(o3).Apply(o4));
			}
		}
	}

	[Serializable]
	[InlineProperty]
	//[HideLabel]
	public struct Overridable<T> where T : struct
	{
		//[LabelText("@$property.Parent.NiceName")]
		[HideLabel]
		public T Value;
		public Overridable(T value) : this() => Value = value;

		public static implicit operator T(Overridable<T> opt) => opt.Value;
		public static implicit operator Overridable<T>(T opt) => new Overridable<T>(opt);

		public OverrideResult<T> Apply(Option<T> opt) => new OverrideResult<T>(opt.IsSet ? opt.Value : Value);

		public override string ToString() => $"Overridable<{typeof(T).Name}>({Value}";
	}

	public struct OverrideResult<T> where T : struct
	{
		//public T Original;
		public T Value;
		public OverrideResult(T value) : this() => Value = value;

		public OverrideResult<T> Apply(Option<T> opt) => new OverrideResult<T>(opt.IsSet ? opt.Value : Value);

		public static implicit operator T(OverrideResult<T>                  opt) => opt.Value;
		//public static implicit operator OverrideResult<T>(T opt) => new OverrideResult<T>(opt);

		public override string ToString() => $"OverrideResult<{typeof(T).Name}>({Value})";
	}

}
