using System.Collections.Generic;
using Anjin.Nanokin.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace Anjin.Actors
{
	public class CharacterRigSystem
	{
		public static List<CharacterRig> spawned;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			spawned = new List<CharacterRig>();
			PlayerLoopInjector.Inject<CharacterRigSystem>(PlayerLoopTiming.PreLateUpdate, Update);
		}

		public static void Add(CharacterRig rig)
		{
			if (!Application.isPlaying) return;
			spawned.Add(rig);
		}

		public static void Remove(CharacterRig rig)
		{
			if (!Application.isPlaying) return;
			spawned.Remove(rig);
		}

		private static void Update()
		{
			// This is taking a lot of time to complete (3ms for 2000 objects)
			// I guess setting localPosition on nested transforms can be pretty expensive
			// ---
			// Currently, this is unneeded because we never change the rigs after spawning.
			// In the future, we should simply make it so that updates happen only on rigs
			// that have been changed.


			// Profiler.BeginSample("CharacterRigSystem");
			// foreach (CharacterRig rig in spawned)
			// {
			// 	Profiler.BeginSample("Rig Offset Calculation", rig.gameObject);
			// 	for (var i = 0; i < rig.Parts.Count; i++)
			// 	{
			// 		CharacterRig.Part part = rig.Parts[i];
			// 		if (part.spawned)
			// 		{
			// 			part.transform.localPosition = part.BaseOffset + part.Offset;
			// 		}
			// 	}
			// 	Profiler.EndSample();
			// }
			// Profiler.EndSample();
		}
	}
}