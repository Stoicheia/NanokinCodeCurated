using System.Threading;
using Combat.Data;
using Combat.Features.TurnOrder.Events;
using Combat.UI;
using Combat.UI.Notifications;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;

namespace Combat.Toolkit
{
	public class AdvanceTurnAnim : BattleAnim
	{
		public Trigger    trigger;
		public ITurnActer acter;

		public override bool Halts => false;

		public AdvanceTurnAnim(Trigger trigger, [CanBeNull] Tween tween = null)
		{
			this.trigger = trigger;
		}

		public AdvanceTurnAnim(ITurnActer acter, [CanBeNull] Tween tween = null)
		{
			this.acter = acter;
		}

		public override async UniTask RunAnimated()
		{
			if (acter != null)
			{
				TurnUI
					.AnimateAdvance(acter)
					.WithCancellation(cts.Token)
					// .ToUniTask(TweenCancelBehaviour.Complete, cts.Token)
					.SuppressCancellationThrow();
				// CombatNotifyUI.DoGeneralNotificationPopup(@event.TurnName).Forget(); // TODO we need a proper name
			}
			else if (trigger != null)
			{
				TurnUI
					.AnimateAdvance(trigger)
					.WithCancellation(cts.Token)
					.SuppressCancellationThrow();
				// CombatNotifyUI.DoSkillUsedPopup(trigger.id).Forget(); // TODO we need a proper name
			}
		}
	}
}