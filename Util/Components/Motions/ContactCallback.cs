using System;
using Anjin.Scripting;
using Combat.Data;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UnityEditor.Odin;

namespace Anjin.Utils
{
	[Flags]
	public enum ContactFeatures
	{
		Proc        = 0b0001,
		Particles   = 0b0010,
		Sound       = 0b0100,
		DestroySelf = 0b1000,
	}

	/// <summary>
	/// A bag of callbacks and behavior to run for a contact event.
	/// </summary>
	[Inline(true, true)]
	[DarkBox]
	[Serializable]
	[LuaUserdata]
	public struct ContactCallback
	{
		[EnumToggleButtons]
		[HideLabel]
		public ContactFeatures Features;

		// [ToggleButton]
		// // [HorizontalGroup("Horizontal")]
		// public bool DestroySelf; // Immediately destroy the object on reach

		[ShowIf("IsProc")]
		[Optional]
		[HideLabel]
		[Placeholder("Proc ID (optional)")]
		public string ProcID;

		[ShowIf("IsParticles")]
		[HideLabel]
		public GameObject SpawnParticles; // Spawn particles on reach (prefab)

		// [HorizontalGroup("")]
		[ShowIf("IsSound")]
		[HideLabel]
		[Inline]
		public AudioDef SFX;

		[NonSerialized]
		public Proc proc;

		[NonSerialized]
		public Closure luaClosure;

#if UNITY_EDITOR
		private bool IsProc        => (Features & ContactFeatures.Proc) != 0;
		private bool IsParticles   => (Features & ContactFeatures.Particles) != 0;
		private bool IsDestroySelf => (Features & ContactFeatures.DestroySelf) != 0;
		private bool IsSound       => (Features & ContactFeatures.Sound) != 0;
#endif
	}
}