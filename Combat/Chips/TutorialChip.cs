using System;
using Anjin.Core.Flags;
using Combat.Components;
using Cysharp.Threading.Tasks;
using Overworld.UI;
using UnityEngine;

namespace Combat
{
	public class TutorialChip : Chip
	{
		protected override int Priority => 20;

		protected override void RegisterHandlers()
		{
			Handle(CoreOpcode.PreStart, OnStartBattle);
		}

		private async UniTask OnStartBattle(CoreInstruction data)
		{
			/*Debug.Log("Tutorial Chip: Start Battle BEGIN");
			await UniTask.Delay(TimeSpan.FromSeconds(5));
			Debug.Log("Tutorial Chip: Start Battle END");*/

			if (!Flags.GetBool("tut_combat"))
			{
				Flags.SetBool("tut_combat", true);
				await UniTask2.Seconds(1f);
				await SplashScreens.ShowPrefabAsync("SplashScreens/tutorial_demo_combat");
				await UniTask.WaitUntil(() => !SplashScreens.IsActive);
				await UniTask2.Seconds(0.75f);
			}
		}
	}
}