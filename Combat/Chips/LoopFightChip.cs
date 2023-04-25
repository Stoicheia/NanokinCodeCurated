using System;
using Combat.Components;
using JetBrains.Annotations;

namespace Combat
{
	/// <summary>
	/// A chip that will restart the fight with the same configuration
	/// whenever the battle ends. (victory or lose)
	/// </summary>
	public class LoopFightChip : Chip
	{
		private readonly Action _onLoop;

		public LoopFightChip([CanBeNull] Action onLoop = null)
		{
			_onLoop = onLoop;
		}

		protected override int Priority => 100;

		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();

			Handle(CoreOpcode.LoseBattle, OnLoseBattle);
			Handle(CoreOpcode.WinBattle, OnWinBattle);
		}

		private void OnWinBattle(ref CoreInstruction obj)
		{
			runner.Restart();
			_onLoop?.Invoke();
		}

		private void OnLoseBattle(ref CoreInstruction obj)
		{
			runner.Restart();
			_onLoop?.Invoke();
		}
	}
}