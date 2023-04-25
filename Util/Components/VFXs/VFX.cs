using System.Collections.Generic;
using Anjin.Scripting;
using API.PropertySheet;
using DG.Tweening;
using UnityEngine;

namespace Combat.Data.VFXs
{
	[LuaUserdata(Descendants = true)]
	public abstract class VFX : ICoroutineWaitable
	{
		public readonly List<Tween> tweens = new List<Tween>();

		public int  refcount;
		public bool removalStaged;

		public GameObject gameObject;
		// public Entity     view;

		// private EndCondition _endCondition;
		private bool _hasLeft;


		/// <summary>
		/// Gets a flag indicating whether or not the VFX has completely played its course
		/// and can be safely discarded by the receiving entity.
		/// </summary>
		public virtual bool IsActive => !_hasLeft;

		public virtual Vector3         VisualOffset         => Vector3.zero;
		public virtual Vector3         VisualScale          => Vector3.one;
		public virtual Color           Tint                 => Color.white;
		public virtual float           EmissionPower        => 0;
		public virtual Color           Fill                 => Color.clear;
		public virtual string          AnimSet              => null;
		public virtual PuppetAnimation PuppetSet            => null;
		public virtual string          PuppetSetMarkerStart => null;
		public virtual string          PuppetSetMarkerEnd   => null;
		public virtual bool            AnimFreeze           => false;
		public virtual float           Opacity              => 1f;

		internal virtual void Enter() { }

		internal virtual void Leave()
		{
			_hasLeft = true;
		}

		public virtual void EndPrematurely()
		{
			Leave();

			foreach (Tween tween in tweens)
			{
				tween.Complete();
			}
		}

		public virtual void Update(float dt) { }

		public virtual void Cleanup() { }

		public virtual void OnTimeScaleChanged(float scale)
		{
			foreach (Tween tween in tweens)
			{
				if (!tween.active)
					continue;

				tween.timeScale = scale;
			}
		}

		public IEnumerable<ProcEffect> EvaluateEffects(Battle battle, Fighter dealer, Fighter victim)
		{
			yield break;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup) => !justYielded && !IsActive;

		public override string ToString() => GetType().Name;

		public virtual void OnAddingOverExisting(VFX existing) { }
	}
}