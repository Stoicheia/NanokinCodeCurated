using System.Collections.Generic;
using System.Linq;
using Anjin.Audio;
using Anjin.Util;
using Anjin.Utils;
using Combat.Components.VictoryScreen.Menu;
using Combat.Data;
using Combat.Entities;
using Combat.Startup;
using Combat.Toolkit;
using Combat.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using Util.Math.Splines;
using Util.UniTween.Value;

namespace Combat.Components.WinLoseHandling
{
	/// <summary>
	/// Uses UI_Victory to implement the win instruction.
	/// Shows the player's gains for the combat (exp, loots, etc.)
	/// and applies them.
	/// </summary>
	public class VictoryUIChip : Chip
	{
		private Team _playerTeam;
		//private AudioZone _music;
		private AudioSource _musicSource;

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync();

			Assert.IsFalse(!runner.animated);
		}

		protected override void RegisterHandlers()
		{
			Handle(CoreOpcode.WinBattle, HandleWin);
		}

		public async UniTask SetCoachWin()
		{
			if (battle.PlayerTeam == null) return;

			List<Coach> coaches = battle.PlayerTeam.fighters
				.Select(f => f.coach)
				.WhereNotNull()
				.ToList();

			await UniTask.Delay(500);

			while (coaches.Count > 0)
			{
				coaches.Sort(CoachDistanceComparer.instance);

				Coach furthest = coaches[coaches.Count - 1];
				coaches.RemoveAt(coaches.Count - 1);
				furthest.SetWin();

				await UniTask.Delay(500);
			}
		}

		private async UniTask PlayVictoryThemes()
		{
			GameSFX.PlayGlobal(runner.animConfig.VictoryFanfare);

			//_music = AudioManager.AddMusic(music: core.config.VictoryFanfare, loop: false);
			await UniTask2.Seconds(((int)runner.animConfig.VictoryFanfare.length) - 3);

			//AudioManager.RemoveZone(_music);

			if (runner != null) {
				_musicSource      = GameSFX.PlayGlobal(runner.animConfig.VictoryMusic);
				_musicSource.loop = true;

				//_music                                            = AudioManager.AddMusic(core.config.VictoryMusic);
			}
		}

		private async UniTask HandleWin(CoreInstruction data)
		{
			runner.io.outcome = BattleOutcome.Win;

			// Hide combat UI
			CombatUI.Live.SetVisible(false);

			// Switch to victory music
			AudioManager.RemoveZone(runner.music);
			PlayVictoryThemes().Forget();

			// LOAD UI
			// ----------------------------------------
			VictoryUI menu = await SceneLoader.GetOrLoadAsync<VictoryUI>(runner.animConfig.VictoryScene);

			// TODO do this the right way
			// battle.ClearVFX();

			menu.fighters = battle.fighters;

			foreach(Fighter fighter in menu.fighters)
			{
				if (fighter.brain != null && fighter.brain is PlayerBrain)
				{
					(fighter.brain as PlayerBrain).ClearOverdrivesFromWin();
				}
				else if (fighter.tags.Contains(Tags.FTER_OBJECT))
				{
					fighter.AddVFX(new FadeVFX(1, new EaserTo(0.5f), new EaserTo(0.5f)));
				}
			}

			// Add income, do not swap the order.
			menu.SetItemLoots(runner.io.itemLoots.GiveLoots());
			await menu.SetIncome(runner.io.xpLoots, runner.io.rpLoots);

			// Add characters
			var coachWin = SetCoachWin().Preserve();

			List<Coach> coaches = new List<Coach>();

			foreach (Fighter fter in battle.PlayerTeam.fighters)
			{
				Coach coach = fter.coach;
				if (coach != null)
					coaches.Add(coach);
			}

			PlotShape victoryShape = battle.PlayerTeam.slots.component.VictoryShape;
			for (var i = 0; i < coaches.Count; i++)
			{
				Coach coach = coaches[i];

				coach.Teleport(victoryShape.Get(i, coaches.Count));

				if (coach.character != null)
				{
					menu.AddCharacter(coach.actor.gameObject, coach.character);
				}
			}


			// menu.SetLoots(); // TODO

			// PLAY UI
			// ----------------------------------------

			runner.screenFade.DOFade(0, 0.35f); // Note: this is the out transition for the white fade on the last hit
			await menu.Play(io.arena.VictoryParams);

			await coachWin;

			// EXIT UI
			// ----------------------------------------
			// Fade out the screen & music.
			if(_musicSource)
				_musicSource.Stop();

			//AudioManager.RemoveZone(_music);

			// await GameEffects.FadeOut(core.config.VictoryFadeOut);

			// Perform unloading while the screen is blacked out.
			SceneLoader.UnloadAsync(runner.animConfig.VictoryScene).Forget();

			runner.io.outcome = BattleOutcome.Win;
			runner.Submit(CoreOpcode.Stop);
		}
	}
}