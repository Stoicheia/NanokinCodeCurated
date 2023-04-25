using Anjin.Actors;
using Anjin.Nanokin;
using Combat.Data;
using Cysharp.Threading.Tasks;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using SaveFiles.Elements.Inventory.Items.Scripting;
using UnityEngine.InputSystem;

namespace Combat.Toolkit
{
	public class StickerAnim : BattleAnim
	{
		private readonly BattleSticker _sticker;
		private readonly Targeting     _targeting;

		public StickerAnim(Fighter fighter, BattleSticker sticker, Targeting targeting)
		{
			this.fighter = fighter;
			_sticker     = sticker;
			_targeting   = targeting;
			useInfo      = sticker;
		}

		public override void RunInstant()
		{
			BattleSticker instance = battle.GetSticker(fighter, _sticker.instance);
			if (instance == null)
				// Could not get the skill script for whatever reason.
				return;

			if (instance.instance.Charges > 0)
				instance.instance.Charges--;

			instance.battle    = battle;
			instance.user      = fighter;
			instance.targeting = _targeting;

			BattleAnim anim = instance.Use();
			if (anim != null)
			{
				RunInstant(anim);
			}
		}

		public override async UniTask RunAnimated()
		{
			BattleSticker instance = battle.GetSticker(fighter, _sticker.instance);
			if (instance == null) return;

			if (instance.instance.Charges > 0)
				instance.instance.Charges--;

			instance.battle    = battle;
			instance.user      = fighter;
			instance.targeting = _targeting;

			BattleAnim anim = instance.Use();

			if (anim == null)
				return;

			// @event = new SkillEvent(fighter, skill);

			do
			{
				// battle.Emit("start-sticker", fighter, @event);

				//fighter.NotifyCoach(AnimID.CombatAction);
				if (fighter != null)
				{
					if (fighter.coach != null)
					{
						fighter.coach.SetAction();

						await UniTask.Delay(500);

						if (fighter.coach.actor != null)
						{
							fighter.coach.actor.SignalForAction();

							await UniTask.Delay(500);
						}
					}
					else
					{			
						if (fighter.actor != null)
						{
							fighter.actor.SignalForAction();

							await UniTask.Delay(500);
						}
					}
				}

				await RunAnimated(anim);

				// battle.Emit("end-sticker", fighter, @event);
				runner.camera.PlayState(ArenaCamera.States.idle);

				if (GameOptions.current.combat_use_loop)
					await UniTask.Delay((int)(GameOptions.current.combat_use_loop_delay.Value * 1000));
			} while (GameOptions.current.combat_use_loop && GameInputs.IsPressed(Key.Tab));
		}
	}
}