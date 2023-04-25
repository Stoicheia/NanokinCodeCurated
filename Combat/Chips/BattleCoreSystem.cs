using System;
using System.Collections.Generic;
using System.Diagnostics;
using Anjin.Nanokin.Core;
using UnityEngine;

namespace Combat.Components
{
	public class BattleCoreSystem
	{
		public static List<BattleRunner> activeCores = new List<BattleRunner>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			activeCores.Clear();
			PlayerLoopInjector.Inject<BattleCoreSystem>(PlayerLoopTiming.Update, Update);
		}

		private static void Update()
		{
			if (!Application.isPlaying) return;

			foreach (BattleRunner core in activeCores)
			{
				core.Update();
			}
		}
	}
}