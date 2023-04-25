using System;
using Animancer;
using Anjin.Actors;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtilities;
using Util;
using Util.Components;
using Util.Components.Timers;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Map {

	public enum LaunchPadBehavior {
		OnPlayerContact = 0,
		OnInterval = 1,
		OnManual = 2,			// Planned for scripts triggering
	}

	[SelectionBase]
	public class LaunchPad : AnjinBehaviour {


		public const float AUTO_HEIGHT_MULTIPLIER		= 0.35f;
		public const float AUTO_LAUNCH_ACTIVE_WINDOW	= 0.15f;

		[Title("Configuration")]

		public bool Active;

		public Transform Target;

		public bool      OrientVisualsTowardsTarget;
		public Transform OrientRoot;

		public LaunchPadBehavior Behavior = LaunchPadBehavior.OnPlayerContact;
		public float         LaunchInterval = 4;
		public Option<float> AutomaticLaunchActiveWindow;

		public Option<float>              HeightOverride;
		public Option<float>              LaunchSpeed;

		public Option<LaunchExitBehavior> ExitBehavior;

		[ShowIf("@ExitBehavior != LaunchExitBehavior.None")]
		public Option<float>              ExitSpeed;

		public ClipTransition Anim_Idle;
		public ClipTransition Anim_Launch;

		[Title("References")]
		public Collider Collider;
		public AnimancerComponent Animator;

		[Title("Debug")]
		[ShowInPlay]
		private bool _hasAnimancer;

		[ShowInPlay]
		private ValTimer _intervalTimer;

		[ShowInPlay]
		private ValTimer _activeTimer;


		[ShowInPlay]
		private bool _launched;

		[ShowInInspector]
		public float Distance {
			get {
				if (Target == null) return 0;
				return Vector3.Distance(transform.position, Target.position);
			}
		}

		[ShowInInspector]
		public float Length {
			get {

				if (Target == null) return 0;

				return MathUtil.ParabolaLength(Height, Distance);

				/*float a = Height;
				float b = Distance;


				return 0.5f * Mathf.Sqrt(b * b + 16f * a * a) +
					   (b * b) / (8f * a) *
					   Mathf.Log((4f * a + Mathf.Sqrt(b * b + 16f * a * a) ) / b);*/
			}
		}

		[ShowInInspector]
		public float Height {
			get {
				return HeightOverride.ValueOrDefault(Distance * AUTO_HEIGHT_MULTIPLIER);
			}
		}

		public bool CanLaunchPlayerIfContacting
		{
			get {
				switch (Behavior) {
					case LaunchPadBehavior.OnInterval:
						return _launched;

					case LaunchPadBehavior.OnManual:
						break;
				}

				return Active;
			}
		}

		private void OnValidate()
		{
			Collider  = GetComponent<Collider>();
			Animator = GetComponent<AnimancerComponent>();
		}

		private void Start()
		{
			_hasAnimancer = Animator != null;

			_intervalTimer = new ValTimer(LaunchInterval);

			if(_hasAnimancer) {
				Animator.Play(Anim_Idle);
				Anim_Launch.Events.OnEnd = () => Animator.Play(Anim_Idle).Time = 0;
			}

			if (Target && OrientRoot & OrientVisualsTowardsTarget) {
				Vector3 towards = (Target.transform.position - transform.position).ChangeY(0).normalized;
				OrientRoot.rotation = Quaternion.LookRotation(towards);
			}
		}

		private void Update()
		{
			if (!Active) return;

			if (Behavior == LaunchPadBehavior.OnInterval) {
				if (!_launched) {
					if (_intervalTimer.Tick()) {
						_intervalTimer.Set(LaunchInterval);
						_activeTimer.Set(AutomaticLaunchActiveWindow.ValueOrDefault(AUTO_LAUNCH_ACTIVE_WINDOW));
						OnLaunch();
						_launched = true;
					}
				} else {
					if (_activeTimer.Tick())
						_launched = false;
				}
			}
		}

		public void OnLaunch()
		{
			if (!Active) return;

			if (_hasAnimancer) {
				Animator.Play(Anim_Launch).Time = 0;
			}
		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			if(Target != null) {
				Draw.editor.Parabola(transform.position, Target.position, Height, ColorsXNA.Goldenrod);
			}
		}

	}
}