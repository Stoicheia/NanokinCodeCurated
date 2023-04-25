using System;
using System.Collections.Generic;
using Anjin.Util;
using Anjin.Utils;
using Combat.Entities;
using Combat.Toolkit;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Animation;
using Util.Odin.Attributes;

namespace Combat.Data.VFXs
{
	/// <summary>
	/// A manager component to hold and manage VFX for a single object.
	/// A VFX is an object that can report some properties (check VFXState)
	/// and the manager will aggregate them all into a single VFXState.
	/// Then, external components can implement those properties in
	/// whichever way is necessary.
	/// </summary>
	public class VFXManager : SerializedMonoBehaviour
	{
		[NonSerialized, ShowInPlay] public VFXState state;

		[ShowInPlay, NonSerialized]
		public List<VFX> all;

		public int Count => all.Count;

		// private ActorView     _view;
		private TimeScalable _timescale;
		private float        _currentTimescale;
		private int          _lastCount;

		private FighterActor actor;

		private void Awake()
		{
			state = VFXState.Default;
			all   = new List<VFX>();

			_timescale           =  gameObject.GetComponentInParent<TimeScalable>() ?? gameObject.GetOrAddComponent<TimeScalable>();
			_timescale.onRefresh += OnTimescaleRefresh;

			actor = GetComponent<FighterActor>();
		}

		private void OnDestroy()
		{
			foreach (VFX vfx in all)
			{
				OnRemoving(vfx);
			}
		}

		public bool Contains(VFX vfx) => all.Contains(vfx);

		/// <summary>
		/// Add a VFX to this manager.
		/// </summary>
		/// <param name="vfx"></param>
		public void Add(VFX vfx)
		{
			if ((vfx is ManualVFX) && (actor != null) && (actor is ObjectFighterActor))
			{
				return;
			}

			VFX existing = null;
			for (int i = all.Count - 1; i >= 0; i--) // Search in reverse since more recent ones will be at the end
			{
				VFX vfxe = all[i];
				if (vfxe.GetType() == vfx.GetType())
				{
					existing = vfxe;
					break;
				}
			}

			all.Add(vfx);


			vfx.refcount++;
			vfx.gameObject = gameObject;

			vfx.Enter();

			if (existing != null)
			{
				vfx.OnAddingOverExisting(existing);
			}
		}

		public void Remove(VFX vfx)
		{
			if (vfx.refcount > 0)
				vfx.removalStaged = true;
		}

		private void OnTimescaleRefresh(float dt, float scale)
		{
			_currentTimescale = _timescale.current;

			foreach (VFX vfx in all)
			{
				vfx.OnTimeScaleChanged(_timescale.current);
			}
		}

		private void Update()
		{
			if (all.Count == 0 && _lastCount == all.Count) return;
			_lastCount = all.Count;

			// Reset the state
			state.Clear();

			float strength = 0;
			for (var i = 0; i < all.Count; i++)
			{
				VFX vfx = all[i];

				// Update
				vfx.Update(_timescale.deltaTime);

				// Sum the fill strengths
				strength += vfx.Fill.a;
			}

			for (var i = 0; i < all.Count; i++)
			{
				VFX vfx = all[i];

				// Remove inactive VFXs
				// ----------------------------------------
				if (!vfx.IsActive || vfx.removalStaged)
				{
					all.RemoveAt(i--);

					OnRemoving(vfx);
					continue;
				}

				// Combines the VFX states into one
				// ----------------------------------------
				state.offset        += vfx.VisualOffset;
				state.emissionPower += vfx.EmissionPower;
				state.tint          *= vfx.Tint;
				state.opacity       *= vfx.Opacity;
				state.opacity       *= vfx.Tint.a;
				if (vfx.AnimSet != null)
				{
					state.animSet              = vfx.AnimSet;
					state.puppetSet            = null;
					state.puppetSetMarkerStart = null;
					state.puppetSetMarkerEnd   = null;
				}

				if (vfx.PuppetSet)
				{
					state.animSet              = null;
					state.puppetSet            = vfx.PuppetSet;
					state.puppetSetMarkerStart = vfx.PuppetSetMarkerStart;
					state.puppetSetMarkerEnd   = vfx.PuppetSetMarkerEnd;
				}

				if (vfx.AnimSet == null && vfx.PuppetSet == null)
				{
					state.animSet = null;
					state.puppetSet = null;
					state.puppetSetMarkerStart = null;
					state.puppetSetMarkerEnd = null;
				}

				state.animFreeze |= vfx.AnimFreeze;
				state.scale      =  Vector3.Scale(state.scale, vfx.VisualScale);

				if (strength > 0)
				{
					// Fill has to be done a bit differently (contribution based)
					state.fill += vfx.Fill * (vfx.Fill.a / strength);
				}
			}
		}

		private void OnRemoving([NotNull] VFX vfx)
		{
			vfx.Leave();
			vfx.Cleanup();

			// A vfx can be in multiple vfxmanager
			vfx.refcount--;
			if (vfx.refcount == 0)
				vfx.removalStaged = false;
		}

		// [NonSerialized]
		// public AnimationCurve TestCurve;
		//
		// [ShowInInspector]
		// public void TestCurvedHop(int count, float speed, float height)
		// {
		// 	var vfx = new Toolkit.HopVFX(count, speed, height, TestCurve);
		// 	Add(vfx);
		// }
	}
}