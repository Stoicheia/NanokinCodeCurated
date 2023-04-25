using Combat.Components;
using Combat.Startup;

namespace Combat
{
	public class ArenaIntroChip : Chip
	{
		/// <summary>
		/// Bypass the game option that disables arena intros.
		/// </summary>
		public bool bypassOption = false;

		protected override void RegisterHandlers()
		{
			Handle(CoreOpcode.IntroduceBattle, HandleIntro);
		}

		private void HandleIntro(ref CoreInstruction obj)
		{
			if (runner.io.outcome == BattleOutcome.LoseRetry)
				return;

			if (battle.arena == null)
			{
				this.LogError("Cannot play intro anim because the arena is null.");
				return;
			}

			runner.Submit(CoreOpcode.Execute, new CoreInstruction
			{
				anim = new ArenaIntroAnim()
			});
		}
	}
}