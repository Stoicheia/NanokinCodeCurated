using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Combat;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Assets.Nanokins
{
	[Serializable]
	public class Learnset //: ISerializationCallbackReceiver
	{
		[FormerlySerializedAs("set"), LabelText("Skills by level"), ListDrawerSettings(Expanded = true, CustomAddFunction = "OnAddEntry")]
		public List<Entry> Set;

		public Learnset()
		{
			Set = new List<Entry>();
		}

		public List<SkillAsset> AllSkills => Set.Select(entry => entry.Skill).WhereNotNull().ToList();

		public List<SkillAsset> GetSkillsAtLevel(int level)
		{
			return Set
				.Where(entry => entry.Level <= level || GameOptions.current.combat_skill_unlocks)
				.Select(entry => entry.Skill)
				.WhereNotNull()
				.ToList();
		}

		[Serializable]
		public class Entry
		{
			[FormerlySerializedAs("level"), HorizontalGroup("Group"), HideLabel]
			public int Level = 1;
			[FormerlySerializedAs("skill"), HorizontalGroup("Group"), HideLabel, Required]
			public SkillAsset Skill;
		}

#if UNITY_EDITOR
		private void OnAddEntry()
		{
			Set.Add(new Entry());
		}
#endif
	}
}