using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat;
using Combat.Entry;
using Combat.UI.TurnOrder;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using SaveFiles.Elements.Inventory.Items;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;
 
namespace Assets.Nanokins
{
	[Serializable]
	[LuaUserdata(Descendants = true)]
	public abstract class MonsterRecipe
	{
		[FormerlySerializedAs("nanokeeper"), Optional]
		public CharacterAsset Character;

		[Optional]
		[CanBeNull]
		public List<StickerAsset> stickers;

		[FormerlySerializedAs("slot"), Optional]
		public Vector2Int? slotcoord;

		public bool ClaimsAllSlots = false;
		public bool NoTurns        = false;
		//public bool Invincible     = false;

		[Optional]
		public BattleBrain brain;

		[NonSerialized]
		public string CharacterAddress = null;

		[NonSerialized]
		public GameObject CharacterPrefab = null;

		[LuaGlobalFunc, NotNull]
		public static SimpleNanokin new_monster(string addr, int level, int mastery = 1)
		{
			return new SimpleNanokin
			{
				Address = addr,
				Level   = level,
				Mastery = mastery
			};
		}

		[LuaGlobalFunc, NotNull]
		public static SimpleNanokin new_monster(Table config)
		{
			SimpleNanokin nano = new SimpleNanokin();

			config.TryGet("address",	out nano.Address);
			config.TryGet("level",		out nano.Level, 1);
			config.TryGet("mastery",	out nano.Mastery, 1);

			if (config.TryGet("coach", out DynValue coach)) {
				if (coach.AsUserdata(out CharacterAsset asset))
					nano.Character = asset;
				else if (coach.AsString(out string charAddress)) {
					nano.CharacterAddress = charAddress;
				}
				else if (coach.AsGameObject(out GameObject charPrefab))
				{
					nano.CharacterPrefab = charPrefab;
				}
			}

			return nano;
		}

		public virtual FighterInfo CreateInfo(AsyncHandles handles)
		{
			throw new NotImplementedException();
		}

		[NotNull] public MonsterRecipe Clone() => (MonsterRecipe)MemberwiseClone();
	}
}