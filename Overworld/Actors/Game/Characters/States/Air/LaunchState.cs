using System;
using Anjin.Nanokin.Map;
using UnityEngine;
using Util;

namespace Anjin.Actors {

	public enum LaunchExitBehavior {
		None,
		RetainVelocity,
		Slide
	}

	public class LaunchState : StateKCC {


		public Settings settings;

		public LaunchPad LaunchPad; // Maybe other things can launch us but for now this is it

		public float   Height;
		public Vector3 Start;
		public Vector3 End;

		public float Speed;
		public float Time;

		public bool IsDone;

		protected override Vector3 TurnDirection => (End - Start).normalized;

		public LaunchExitBehavior ExitBehavior;
		public Vector3            ExitDirection;

		public LaunchState(Settings settings) {
			this.settings = settings;
		}

		public override void OnActivate()
		{
			base.OnActivate();

			IsDone = false;

			Time = 0;

			ExitDirection = Vector3.zero;

			Height       = LaunchPad.Height;
			Start        = Motor.transform.position;

			Speed        = LaunchPad.LaunchSpeed.ValueOrDefault(settings.DefaultLaunchSpeed);

			ExitBehavior = LaunchPad.ExitBehavior.ValueOrDefault(settings.DefaultExitBehavior);

			actor.ClearVelocity();
			actor.Motor.SetGroundSolvingActivation(false);

			LaunchPad.OnLaunch();
		}


		public override void OnDeactivate()
		{
			base.OnDeactivate();
			actor.Motor.SetGroundSolvingActivation(true);
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			float speed = Speed;

			Time += (speed * deltaTime) / LaunchPad.Length;

			if (Time >= 1) {
				IsDone = true;
				return;
			}

			End = LaunchPad.Target.position;

			Vector3 target = MathUtil.EvaluateParabola(Start, End, Height, Time);

			var vec = target - Motor.transform.position;

			if (vec.magnitude > Mathf.Epsilon)
				ExitDirection = vec.normalized;

			Motor.SetPosition(target);
		}

		public override void AfterCharacterUpdate(float dt)
		{
			base.AfterCharacterUpdate(dt);

			if (active) {

			}
		}

		[Serializable]
		public class Settings {
			public InertiaForce.Settings Inertia;

			public float DefaultLaunchSpeed    = 10;

			public LaunchExitBehavior DefaultExitBehavior;

			public float DefaultExitSpeed		= 15;
			public float DefaultExitSlideSpeed	= 15;
		}

	}
}