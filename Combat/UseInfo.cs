using SaveFiles.Elements.Inventory.Items.Scripting;

namespace Combat
{
	public readonly struct UseInfo
	{
		public readonly UseType       type;
		public readonly BattleSkill   skill;
		public readonly BattleSticker sticker;

		public UseInfo(BattleSkill skill) : this()
		{
			this.type  = UseType.Skill;
			this.skill = skill;
		}

		public UseInfo(BattleSticker sticker) : this()
		{
			this.type    = UseType.Sticker;
			this.sticker = sticker;
		}

		public static implicit operator UseInfo(BattleSkill sk)
		{
			return new UseInfo(sk);
		}

		public static implicit operator UseInfo(BattleSticker stick)
		{
			return new UseInfo(stick);
		}
	}
}