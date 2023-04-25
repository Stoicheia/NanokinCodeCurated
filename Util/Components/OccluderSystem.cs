using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace Util.Components
{
	public class OccluderSystem
	{
		private static List<Occluder> _all = new List<Occluder>(1024);

		private static Transform _referencePoint;
		private static bool      _hasReferencePoint;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Init()
		{
			PlayerLoopInjector.Inject<OccluderSystem>(PlayerLoopTiming.PreLateUpdate, Update);
		}

		public static void SetReferencePoint(Transform transform)
		{
			if (transform == null && GameCams.Live != null) transform               = GameCams.Live.UnityCam.transform;
			if (transform == null && ActorController.playerActor != null) transform = ActorController.playerActor.transform;

			_referencePoint    = transform;
			_hasReferencePoint = transform != null;
		}

		public static void Add(Occluder    occluder) => _all.Add(occluder);
		public static void Remove(Occluder occluder) => _all.Remove(occluder);

		private static void Update()
		{
			if (!_hasReferencePoint) return;

			Vector3 refPoint = _referencePoint.position;

			Profiler.BeginSample($"Occluder System ({_all.Count} occludees)");

			int num = _all.Count;

			for (var i = 0; i < num; i++)
			{
				Occluder occ = _all[i];

				float dist;

				if (occ.Static) {
					dist = Vector3.SqrMagnitude(occ.StaticPosition - refPoint);
				} else {
					dist = Vector3.SqrMagnitude(occ.transform.position - refPoint);
				}

				bool visible = dist < occ.totalDistanceSqr;

				if (visible && !occ.visible) occ.SetState(true);
				if (!visible && occ.visible) occ.SetState(false);

				occ.visible = visible;
			}

			Profiler.EndSample();
		}
	}
}