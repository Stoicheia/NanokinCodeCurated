using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Assets.Nanokins;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Combat.Startup
{
	[Serializable]
	[LuaUserdata]
	public class TeamRecipe
	{
		[FormerlySerializedAs("monsters"), SerializeField, Inline]
		[ValidateInput("@Monsters.Count > 0", "Cannot have an empty recipe.")]
		public List<MonsterRecipe> Monsters;

		public TeamRecipe()
		{
			Monsters = new List<MonsterRecipe>();
		}

		public TeamRecipe(List<MonsterRecipe> monsters)
		{
			Monsters = monsters;
		}


		// public void RandomSlots(List<Slot> allslots)
		// {
		// 	List<Slot> remaining = ListPool<Slot>.Claim(allslots.Count);
		// 	remaining.AddRange(allslots);
		//
		// 	for (var i = 0; i < Monsters.Count; i++)
		// 	{
		// 		int sel = RNG.Int(remaining.Count);
		// 		Monsters[i].slot = remaining[sel];
		// 		remaining.RemoveAt(sel);
		// 	}
		//
		// 	ListPool<Slot>.Release(ref remaining);
		// }
	}
}