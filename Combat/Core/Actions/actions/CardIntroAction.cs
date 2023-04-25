using Combat.Components;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat.Toolkit
{
	public class CardIntroChip : Chip
	{
		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();
			Handle(CoreOpcode.IntroduceTurns, OnIntroduceTurns);
		}

		private async UniTask OnIntroduceTurns(CoreInstruction data)
		{
			// Debug.Log("Start Cards Intro");
			TurnUI.SetVisible(true);
			await TurnUI.AnimateIntro();
			// Debug.Log("End Cards Intro");
		}

	}
}