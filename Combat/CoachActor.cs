using System;
using Anjin.Actors;

namespace Combat
{
	public class CoachActor : ActorBase
	{
		[NonSerialized]
		public AnimID state;

		protected ActorRenderer _renderer;
		protected AnimID        _idle;

		[UnityEngine.SerializeField] protected bool scaleViaHeading = false;

		[UnityEngine.SerializeField] private Assets.Scripts.Utils.ParticlePrefab FX_ActionSignal;

		public bool ScaleViaHeading => scaleViaHeading;

		protected override void Awake()
		{
			base.Awake();
			_renderer = GetComponentInChildren<ActorRenderer>();
		}

		protected virtual void Start()
		{
			SetAnim(AnimID.CombatIdle);
		}

		public virtual void SetAnim(AnimID id)
		{
			state = id;

			// Update rendering state
			// ----------------------------------------
			ref RenderState rstate = ref _renderer.state;
			_renderer.state = new RenderState(id);

			switch (id)
			{
				case AnimID.CombatIdle:
				case AnimID.CombatIdle2:
				case AnimID.CombatIdle3:
				case AnimID.CombatHurt:
				case AnimID.CombatHurt2:
				case AnimID.CombatHurt3:
					_idle = id;
					break;

				case AnimID.CombatAction:
				case AnimID.CombatAction2:
				case AnimID.CombatAction3:
				case AnimID.CombatWin:
				case AnimID.CombatWin2:
				case AnimID.CombatWin3:
				case AnimID.CombatWinGoof:
				case AnimID.CombatWinGoof2:
				case AnimID.CombatWinGoof3:
					rstate.animRepeats    = 1;
					_renderer.animRepeats = 0;
					break;
			}
		}

		public virtual void SignalForAction()
		{
			FX_ActionSignal.Instantiate(transform, parent: false);
		}

		protected virtual void Update()
		{
			ref RenderState state = ref _renderer.state;
			state.animPercent = -1;

			switch (state.animID)
			{
				case AnimID.CombatAction when _renderer.animRepeats >= 1:
					// Return to idle after attacking
					SetAnim(_idle);
					break;
			}
		}
	}
}