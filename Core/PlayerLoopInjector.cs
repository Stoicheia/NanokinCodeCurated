using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Anjin.Nanokin.Core
{
	public class PlayerLoopInjector
	{
		public static void Inject<TSystem>(PlayerLoopTiming timing, PlayerLoopSystem.UpdateFunction @delegate)
		{
			PlayerLoopSystem rootNode   = PlayerLoop.GetCurrentPlayerLoop();
			PlayerLoopSystem timingNode = rootNode.subSystemList[(int) timing];

			// SEARCH FOR EXISTING (for playmode without domain reload)
			// ------------------------------------------------------------
			List<PlayerLoopSystem> systems = ListPool<PlayerLoopSystem>.Claim();
			systems.AddRange(timingNode.subSystemList);

			int index = -1;
			for (var i = 0; i < systems.Count; i++)
			{
				PlayerLoopSystem sys = systems[i];
				if (sys.type == typeof(TSystem))
				{
					index = i;
					break;
				}
			}

			ListPool<PlayerLoopSystem>.Release(ref systems);

			if (index == -1)
			{
				Array.Resize(ref timingNode.subSystemList, timingNode.subSystemList.Length + 1);
				index = timingNode.subSystemList.Length - 1;
			}

			// APPLY
			// ------------------------------------------------------------

			timingNode.subSystemList[index] = new PlayerLoopSystem
			{
				type           = typeof(TSystem),
				updateDelegate = @delegate
			};

			rootNode.subSystemList[(int) timing] = timingNode;
			PlayerLoop.SetPlayerLoop(rootNode);
		}
	}
}