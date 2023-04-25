using System;
using Anjin.Util;
using Combat;
using Combat.Entities;
using Data.Nanokin;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Assets.Nanokins
{
	[Serializable]
	public class SimpleNanokin : MonsterRecipe
	{
		[FormerlySerializedAs("nanokin")/*, HorizontalGroup*/]
		[HideLabel]
		public NanokinAsset Nanokin; // TODO this is dangerous material, needs to go away

		[Optional]
		[HideInInspector]
		public string Address;

		[FormerlySerializedAs("level")/*, HorizontalGroup(0.35f)*/]
		//[LabelText("LV")]
		[LabelWidth(56)]
		public RangeOrInt Level = 1;

		//[HorizontalGroup(0.35f)]
		//[LabelText("MSTRY")]
		[LabelWidth(56)]
		public RangeOrInt Mastery = 1;

		[CanBeNull]
		public override FighterInfo CreateInfo(AsyncHandles handles)
		{
			NanokinAsset nano;
			if (Nanokin != null)
			{
				nano = GameAssets.GetNanokin(Nanokin.name.ToLowerdash()); // TODO BAD!! TEMPORARY!!

				if (nano == null) {
					Debug.Log($"SimpleNanokin: Tried to load asset for nanokin {Nanokin.name}, failed!");
					return null;
				}
			}
			else if (Address != null)
			{
				if (Address == null)
				{
					this.LogError("SimpleNanokin: No nanokin asset or address given to SimpleNanokin.");
					return null;
				}

				var addr = $"Nanokins/{Address}";

				nano = GameAssets.GetNanokin(addr);
				if (nano == null)
				{
					Debug.LogError($"Couldn't find nanokin '{addr}'");
					return null;
				}
			}
			else
			{
				return null;
			}


			if (nano == null) {
				Debug.Log($"SimpleNanokin: Tried to load a nanokin, but failed!");
				return null;
			}

			var instance = new NanokinInstance(
				Level,
				new LimbInstance(nano.Head, Mastery),
				new LimbInstance(nano.Body, Mastery),
				new LimbInstance(nano.Arm1, Mastery),
				new LimbInstance(nano.Arm2, Mastery)
			)
			{
				Name         = nano.name,
				NanokinAsset = nano
			};

			return new NanokinInfo(instance);
		}
	}
}