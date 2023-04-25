using Combat.Data;
using Combat.Toolkit;
using JetBrains.Annotations;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items.Scripting;

namespace Combat
{
	public class StickerCommand : TurnCommand
	{
		private readonly Fighter       _fighter;
		private readonly BattleSticker _sticker;
		private readonly Targeting     _targeting;

		public StickerCommand(Fighter fighter, BattleSticker sticker, Targeting targeting)
		{
			_fighter   = fighter;
			_sticker   = sticker;
			_targeting = targeting;
		}

		[NotNull]
		public override string Text => "Sticker";

		[NotNull] public override BattleAnim GetAction(Battle battle) => new StickerAnim(_fighter, _sticker, _targeting);
	}
}