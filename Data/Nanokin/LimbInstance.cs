using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Assets.Nanokins;
using Combat;
using Data.Combat;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Data.Nanokin
{
	[Serializable, JsonObject(MemberSerialization.OptIn)]
	public class LimbInstance
	{
		[NonSerialized, CanBeNull]
		public LimbEntry entry;

		[FormerlySerializedAs("asset")]
		[SerializeField] public NanokinLimbAsset Asset;
		[FormerlySerializedAs("mastery")] [FormerlySerializedAs("level")]
		[TableColumnWidth(50), FormerlySerializedAs("_level")]
		[SerializeField] public int Mastery;
		[FormerlySerializedAs("rp"), FormerlySerializedAs("xp"), FormerlySerializedAs("_xp")]
		[SerializeField] public int RP;
		[SerializeField] public bool Favorited;

		public List<SkillAsset> Skills            => Asset.Skills;
		public int              NextMasteryRP     => Asset.RPCurve.ClampGet(Mastery - 1);
		public int              NextMasteryRPLeft => NextMasteryRP - RP;

		public LimbInstance(int mastery = 1)
		{
			Mastery = mastery;
		}

		public LimbInstance(NanokinLimbAsset asset, int mastery = 1)
		{
			Asset   = asset;
			Mastery = mastery;
		}

		public LimbInstance(string address, RangeOrInt mastery) : this(GameAssets.GetLimb(address), mastery) { }

		public Pointf   CalcMaxPoints(int  level) => Asset.CalcPoints(level);
		public Statf    CalcStats(int      level) => Asset.CalcStats(level);
		public Elementf CalcEfficiency(int level) => Asset.CalcEfficiency(level);

		public virtual List<SkillAsset> FindUnlockedSkills()
		{
			return Asset.Skills.Where((_, i) => i + 1 <= Mastery).ToList();
		}


		public bool GainRP(int amount)
		{
			if (Mastery == StatConstants.MAX_MASTERY)
				return false;

			bool changed = false;

			if (Mastery == 0)
			{
				Mastery++;
				changed = true;
			}

			RP += amount;
			while (RP >= NextMasteryRP && Mastery < StatConstants.MAX_MASTERY)
			{
				RP -= NextMasteryRP;
				Mastery++;
				changed = true;
			}

			return changed;
		}

#if UNITY_EDITOR
		private IList<ValueDropdownItem<string>> AllLimbs => NanokinLimbCatalogue.Instance.GetOdinDropdownItems();
#endif
	}
}