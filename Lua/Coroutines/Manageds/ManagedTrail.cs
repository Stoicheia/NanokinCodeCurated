using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;
using Util.RenderingElements.Trails;

namespace Overworld.Cutscenes
{
	public class ManagedTrail : CoroutineManaged
	{
		public TrailSettings settings;

		private readonly GameObject _object;

		public override bool Active => true;

		public ManagedTrail(GameObject @object, TrailSettings settings)
		{
			_object       = @object;
			this.settings = settings;
		}

		public override bool CanContinue(bool justYielded, bool isCatchup) => true;

		public override void OnStart()
		{
			List<Trail> trails = ListPool<Trail>.Claim();
			_object.GetComponentsInChildren(trails);

			foreach (Trail trail in trails)
			{
				trail.Settings = settings;
				trail.Play();
			}

			trails.Clear();
			ListPool<Trail>.Release(trails);
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			List<Trail> trails = ListPool<Trail>.Claim();
			_object.GetComponentsInChildren(trails);

			foreach (Trail trail in trails)
			{
				trail.StopProgressive();
			}

			trails.Clear();
			ListPool<Trail>.Release(trails);
		}
	}
}