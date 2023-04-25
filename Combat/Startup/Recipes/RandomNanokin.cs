using System;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Util;
using Combat;
using Combat.Entities;
using Cysharp.Threading.Tasks;
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
	public class RandomNanokin : MonsterRecipe
	{
		public RangeOrInt Level = 1;

		public bool WholeNanokin;

		[InlineGroup, HideReferenceObjectPicker]
		public RandomLimb Head = new RandomLimb(3);

		[InlineGroup, HideReferenceObjectPicker]
		public RandomLimb Body = new RandomLimb(3);

		[InlineGroup, HideReferenceObjectPicker]
		public RandomLimb Arm1 = new RandomLimb(3);

		[InlineGroup, HideReferenceObjectPicker]
		public RandomLimb Arm2 = new RandomLimb(3);

		public RandomNanokin() { }

		public RandomNanokin(int level, int mastery = 3, bool wholeNanokin = true)
		{
			Level        = level;
			WholeNanokin = wholeNanokin;
			Head         = new RandomLimb(mastery);
			Body         = new RandomLimb(mastery);
			Arm1         = new RandomLimb(mastery);
			Arm2         = new RandomLimb(mastery);
		}

		public override FighterInfo CreateInfo([NotNull] AsyncHandles handles)
		{
			// Define the limb asset variables
			NanokinLimbAsset headAsset = null;
			NanokinLimbAsset bodyAsset = null;
			NanokinLimbAsset arm1Asset = null;
			NanokinLimbAsset arm2Asset = null;

			if (WholeNanokin)
			{
				// Get a random nanokin and use it for each limb
				string       addr         = GameAssets.Nanokins.Choose();
				NanokinAsset nanokinAsset = GameAssets.GetNanokin(addr);

				headAsset = nanokinAsset.Head;
				bodyAsset = nanokinAsset.Body;
				arm1Asset = nanokinAsset.Arm1;
				arm2Asset = nanokinAsset.Arm2;
			}

			bodyAsset = bodyAsset ?? GameAssets.GetLimb(Body.Address);
			headAsset = headAsset ?? GameAssets.GetLimb(Head.Address);
			arm1Asset = arm1Asset ?? GameAssets.GetLimb(Arm1.Address);
			arm2Asset = arm2Asset ?? GameAssets.GetLimb(Arm2.Address);

			var body = new LimbInstance(bodyAsset, Body.Mastery);
			var head = new LimbInstance(headAsset, Head.Mastery);
			var arm1 = new LimbInstance(arm1Asset, Arm1.Mastery);
			var arm2 = new LimbInstance(arm2Asset, Arm2.Mastery);

			var nanokin = new NanokinInstance(Level, head, body, arm1, arm2);

			return new NanokinInfo(nanokin);
		}

		[Serializable]
		public struct RandomLimb
		{
			[Tooltip("Level of the limb.")]
			public RangeOrInt Mastery;

			public bool Random;

			[AddressFilter("Limbs/", exclude: ".spritesheet")]
			public string Address;

			public RandomLimb(int mastery = 1, string addr = "")
			{
				Address = addr;
				Random  = addr.Length > 0;
				Mastery = mastery;
			}

			public void Randomize()
			{
				Address = GameAssets.Limbs.Choose();
			}
		}

#if UNITY_EDITOR
		[Button, PropertyOrder(-1)]
		private async UniTask Assign([CanBeNull] string name = "bigfoot", int mastery = 1)
		{
			if (string.IsNullOrEmpty(name))
				return;

			await NanokinLimbCatalogue.Instance.LoadAll();

			var matchedLimbs = NanokinLimbCatalogue.Instance.loadedAssets
				.Where(l => l.DisplayName.ToLower().Contains(name))
				.ToList();

			Head.Address = matchedLimbs.FirstOrDefault(l => l.Kind == LimbType.Head).GetAddressInEditor();
			Body.Address = matchedLimbs.FirstOrDefault(l => l.Kind == LimbType.Body).GetAddressInEditor();
			Arm1.Address = matchedLimbs.FirstOrDefault(l => l.Kind == LimbType.Arm1).GetAddressInEditor();
			Arm2.Address = matchedLimbs.FirstOrDefault(l => l.Kind == LimbType.Arm2).GetAddressInEditor();

			Head.Mastery = mastery;
			Body.Mastery = mastery;
			Arm1.Mastery = mastery;
			Arm2.Mastery = mastery;
		}

#endif
	}
}