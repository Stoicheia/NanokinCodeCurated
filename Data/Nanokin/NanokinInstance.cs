using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Anjin.Util;
using Assets.Nanokins;
using Combat;
using Data.Combat;
using Data.Shops;
using JetBrains.Annotations;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.Odin.Selectors;
using Util.Odin.Selectors.File;

namespace Data.Nanokin
{
	[Serializable]
	public class NanokinInstance
	{
		[SerializeField]
		public string Name;

		public int XP;
		[SerializeField]
		public int Level;

		/// <summary>
		/// The nanokin's current points, as a percentage between 0 and 1.
		/// This is so if limbs are changed, the health remains proportionally equal.
		/// </summary>
		[SerializeField, Inline, DarkBox]
		public Pointf Points = Pointf.One;

		[SerializeField, CanBeNull]
		public LimbInstance Head;

		[SerializeField, CanBeNull]
		public LimbInstance Body;

		[SerializeField, CanBeNull]
		public LimbInstance Arm1;

		[SerializeField, CanBeNull]
		public LimbInstance Arm2;

		[CanBeNull]
		public NanokinAsset NanokinAsset;

		[SerializeField]
		public List<StickerInstance> Stickers = new List<StickerInstance>();

		[NonSerialized, CanBeNull]
		public CharacterEntry entry;

		[CanBeNull]
		[NonSerialized] // WARNING: if this is made to be serialized, check usage refs to update ?? and null checks (because Unity doesn't serialize nulls it will deserialize to empty strings)
		public string ai = null;

		public Pointf   MaxPoints    { get; private set; }
		public Statf    Stats        { get; private set; }
		public Elementf Efficiencies { get; private set; }
		// public List<SkillAsset> AvailableSkills { get; private set; }
		// public List<SkillAsset> AllSkills       { get; private set; }

		public int XPLoot => Body.Asset.monster.BaseXPLoot * Level;
		public int RPLoot => Body.Asset.monster.BaseRPLoot;

		public LootDropInfo ItemLoot => Body.Asset.Drops; //probably rework later?

		public LimbInstance[] Limbs => new[]
		{
			Head, Body, Arm1, Arm2
		};


		/// <summary>
		/// Instantiate a new Nanokin with no limbs.
		/// </summary>
		public NanokinInstance() { }

		public NanokinInstance(int level, string nanokin)
		{
			Level = level;

			NanokinAsset asset = GameAssets.GetNanokin(nanokin);
			if (asset != null)
			{
				Arm1 = new LimbInstance(asset.Arm1);
				Arm2 = new LimbInstance(asset.Arm2);
				Body = new LimbInstance(asset.Body);
				Head = new LimbInstance(asset.Head);
				Name = entry == null || entry.NanokinName == "" ? asset.DisplayName : entry.NanokinName;
			}

			RecalculateStats();
			Heal();
		}

		public NanokinInstance(
			int    level,
			string head,
			string body,
			string arm1,
			string arm2)
		{
			Level = level;

			NanokinLimbAsset ahead = GameAssets.GetLimb(head);
			NanokinLimbAsset abody = GameAssets.GetLimb(body);
			NanokinLimbAsset aarm1 = GameAssets.GetLimb(arm1);
			NanokinLimbAsset aarm2 = GameAssets.GetLimb(arm2);

			Arm1 = new LimbInstance(aarm1);
			Arm2 = new LimbInstance(aarm2);
			Body = new LimbInstance(abody);
			Head = new LimbInstance(ahead);

			RecalculateStats();
			Heal();
		}

		/// <summary>
		/// Instantiate a new Nanokin from 4 existing standard limbs.
		/// </summary>
		public NanokinInstance(
			int          level,
			LimbInstance head,
			LimbInstance body,
			LimbInstance arm1,
			LimbInstance arm2
		)
		{
			Level = level;
			Head  = head;
			Body  = body;
			Arm1  = arm1;
			Arm2  = arm2;

			RecalculateStats();
			Heal();
		}

		public LimbInstance this[LimbType type]
		{
			get
			{
				switch (type)
				{
					case LimbType.Body: return Body;
					case LimbType.Head: return Head;
					case LimbType.Arm1: return Arm1;
					case LimbType.Arm2: return Arm2;
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, null);
				}
			}

			set
			{
				switch (type)
				{
					case LimbType.Body:
						Body = value;
						break;
					case LimbType.Head:
						Head = value;
						break;
					case LimbType.Arm1:
						Arm1 = value;
						break;
					case LimbType.Arm2:
						Arm2 = value;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, null);
				}

				RecalculateStats();
			}
		}

		/// <summary>
		/// Heals the points.
		/// </summary>
		public void Heal(float percent = 1)
		{
			RecalculateStats();
			Points += percent;
			Points.Max(MaxPoints);
		}

		public void RecalculateStats()
		{
			MaxPoints    = Pointf.Zero;
			Stats        = Statf.Zero;
			Efficiencies = Elementf.Zero;

			var allSkills       = new List<SkillAsset>();
			var availableSkills = new List<SkillAsset>();

			foreach (LimbInstance limb in Limbs)
			{
				if (limb?.Asset == null)
					// No asset on this limb.
					continue;

				MaxPoints    += limb.CalcMaxPoints(Level);
				Stats        += limb.CalcStats(Level);
				Efficiencies += limb.CalcEfficiency(Level);

				allSkills.AddRange(limb.Skills);
				availableSkills.AddRange(limb.FindUnlockedSkills());
			}

			foreach (StickerInstance sticker in Stickers)
			{
				StickerAsset asset = sticker.Asset;

				MaxPoints    += asset.PointGain;
				Stats        += asset.StatGain;
				Efficiencies += asset.EfficiencyGain;

				MaxPoints    *= asset.PointScale;
				Stats        *= asset.StatScale;
				Efficiencies *= asset.EfficiencyScale;
			}

			MaxPoints = new Pointf
			{
				hp = MaxPoints.hp.Minimum(1),
				sp = MaxPoints.sp.Minimum(1),
				op = 8,
				// op = Stats.will / 10f, // 9.9 at 99 will
			};

			// AvailableSkills = availableSkills.RemoveNulls();
			// AllSkills       = allSkills;
		}

		public void SetLimbs(
			LimbInstance limbHead,
			LimbInstance limbBody,
			LimbInstance limbArm1,
			LimbInstance limbArm2
		)
		{
			Body = limbBody;
			Head = limbHead;
			Arm1 = limbArm1;
			Arm2 = limbArm2;
			RecalculateStats();
		}

		public static NanokinInstance Create(string name, int level, List<NanokinLimbAsset> limbsall)
		{
#if UNITY_EDITOR
			limbsall = "t:NanokinLimbAsset".SearchAssetDatabase<NanokinLimbAsset>().ToList();
#endif
			// handles = null;
			//
			// if (limbsall == null)
			// {
			//
			// 	List<string> addresses = Addressables2.FindAddresses("Limbs/", notEndWith: ".spritesheet", label: "Limbs").Where(addr => addr.ToLower().Contains(name.ToLower())).ToList();
			// 	Assert.Greater(addresses.Count, 0);
			//
			// 	IEnumerable<UniTask<AsyncOperationHandle<NanokinLimbAsset>>> tasks = addresses.Select(Addressables2.LoadHandleAsync<NanokinLimbAsset>);
			// 	handles = await UniTask.WhenAll(tasks);
			//
			// 	limbsall = handles.Select (hnd => hnd.Result).ToList();
			// }

			NanokinLimbAsset arm1 = limbsall.FirstOrDefault(l => l.Kind == LimbType.Arm1);
			NanokinLimbAsset arm2 = limbsall.FirstOrDefault(l => l.Kind == LimbType.Arm2);
			NanokinLimbAsset head = limbsall.FirstOrDefault(l => l.Kind == LimbType.Head);
			NanokinLimbAsset body = limbsall.FirstOrDefault(l => l.Kind == LimbType.Body);

			return new NanokinInstance(
				level,
				new LimbInstance(head, level),
				new LimbInstance(body, level),
				new LimbInstance(arm1, level),
				new LimbInstance(arm2, level)
			);
		}

#if UNITY_EDITOR
		[Button]
		private void SetLimbsToNanokin()
		{
			var selector = new FilterSelector<NanokinLimbAsset>(entries =>
			{
				FilterEntry<NanokinLimbAsset>[] selection = entries.ToArray();
				int                             nLimbs    = selection.Length;

				foreach (FilterEntry<NanokinLimbAsset> entry in selection)
				{
					NanokinLimbAsset selectedAsset = entry.Value;
					switch (selectedAsset.Kind)
					{
						case LimbType.Body:
							Body       = Body ?? new LimbInstance();
							Body.Asset = selectedAsset;
							break;

						case LimbType.Head:
							Head       = Head ?? new LimbInstance();
							Head.Asset = selectedAsset;
							break;

						case LimbType.Arm1:
							Arm1       = Arm1 ?? new LimbInstance();
							Arm1.Asset = selectedAsset;
							break;

						case LimbType.Arm2:
							Arm2       = Arm2 ?? new LimbInstance();
							Arm2.Asset = selectedAsset;
							break;

						case LimbType.None:
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			});

			NanokinLimbCatalogue.Instance.LoadAll(list =>
				{
					foreach (NanokinLimbAsset asset in list)
					{
						selector.AddEntry(asset.DisplayName, asset);
					}

					selector.ShowContextMenuSafe();
				},
				true).ForgetWithErrors();
		}
#endif

		public class NanokinInstanceProxy : LuaProxy<NanokinInstance> {
			public void heal(float percent = 1) => proxy.Heal(percent);

			public Pointf points => proxy.Points;
		}
	}
}