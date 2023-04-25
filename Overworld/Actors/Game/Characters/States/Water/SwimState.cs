using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[Serializable]
	public class SwimState : AirState
	{
		public readonly Settings settings;

		private float _adSwimCooldown;

		public SwimState(Settings settings)
		{
			this.settings = settings;
		}

		public bool IsSwimming { get; set; }

		[NonSerialized]
		public bool isSurfaceSwim;

		public override float Gravity => actor.Gravity * settings.gravityScale;

		private AudioSource swimIdleSource;
		private AudioSource swimMoveSource;

		public override void OnActivate()
		{
			base.OnActivate();

			if (actor.velocity.y < 0)
				GameSFX.Play(settings.adLandInWater, actor.transform);

			isSurfaceSwim = false;
			actor.Motor.SetGroundSolvingActivation(false);

			if (hasPlayer) {
				swimIdleSource = GameSFX.PlayGlobal(player.Settings.SFX_WaterIdle, actor);
				if (swimIdleSource) {
					swimIdleSource.volume = 0;
					swimIdleSource.loop   = true;
				}

				swimMoveSource = GameSFX.PlayGlobal(player.Settings.SFX_Swim, actor);
				if (swimMoveSource) {
					swimMoveSource.volume = 0;
					swimMoveSource.loop   = true;
				}
			}
		}

		protected override float TurnSpeed => 10;

		public override void OnDeactivate()
		{
			actor.Motor.SetGroundSolvingActivation(true);

			if(swimIdleSource) {
				swimIdleSource.Stop();
				swimIdleSource = null;
			}

			if(swimMoveSource) {
				swimMoveSource.Stop();
				swimMoveSource = null;
			}
		}

		protected override void UpdateVertical(ref Vector3 vel, float dt)
		{
			if (justActivated)
			{
				if (vel.y < 0)
				{
					vel.y *= 1 - settings.initialDownwardVelocityReduction;
					vel.y =  vel.y.Minimum(-settings.initialDownwardVelocityMaximum);
				}
			}

			// UPDATE vertical velocity.
			float surfaceY = actor.water.bounds.max.y; // if we want to support angles in the water surface, we can calculate this by stepping up from `top` until the point no longer overlaps the water volume, then raycasting down.

			Vector3 headPoint = actor.Motor.TransientPosition + actor.Motor.Capsule.center;
			float   headDepth = surfaceY - headPoint.y;
			if (headDepth > 0)
			{
				// head below the water --> accelerate upwards because of air in the lungs.
				isSurfaceSwim =  false;
				vel.y         += settings.floatUpForce;
			}
			else
			{
				if (vel.y > 0)
				{
					vel.y *= 1 - settings.floatUpDeceleration;
				}

				Vector3 feetPoint = actor.Motor.TransientPosition + actor.Motor.Capsule.center + Vector3.down * actor.Motor.Capsule.height / 2f;
				float   feetDepth = surfaceY - feetPoint.y;
				if (feetDepth < settings.maxDepthForGravity)
				{
					base.UpdateVertical(ref vel, dt);
				}
				else
				{
					/*if (!isSurfaceSwim && vel.y > settings.adEmergeFromUnderwaterMinVelocity)
					{
						GameSFX.Play(settings.adEmergeFromUnderwater, actor.transform);
					}*/

					isSurfaceSwim = true;
				}
			}

			// if (Mathf.Abs(distanceToSurface) > 0.1f)
			// {
			// 	targetVerticalVelocity.y = Mathf.Lerp(targetVerticalVelocity.y, -Mathf.Sign(distanceToSurface) * 1.0f, 0.2f);
			// }
			// else
			// {
			// 	targetVerticalVelocity.y = 0;
			// }
		}

		protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
		{
			// Smoothly interpolate to target swimming velocity
			Vector3 targetVelocity = settings.swimSpeed * inputs.move;
			hvel = MathUtil.LerpWithSharpness(hvel, targetVelocity, settings.movementSharpness, dt);
			ReorientToGround(ref hvel);

			bool wasSwimming = IsSwimming;
			IsSwimming = inputs.move.magnitude > Mathf.Epsilon;

			if (wasSwimming != IsSwimming)
				_adSwimCooldown = settings.adSwimmingCooldown;

			if(IsSwimming) {
				if(swimIdleSource) swimIdleSource.volume = Mathf.Lerp(swimIdleSource.volume, 0,                                         0.1f);
				if(swimMoveSource) swimMoveSource.volume = Mathf.Lerp(swimMoveSource.volume, player.Settings.SFX_Swim.EvaluateVolume(), 0.1f);

				/*if (_adSwimCooldown > 0) {
					_adSwimCooldown -= dt;
				} else {
					GameSFX.Play(settings.adSwimming, actor.transform, canReuseExisting:true);
					_adSwimCooldown = settings.adSwimmingCooldown;
				}*/

			} else {
				if(swimIdleSource) swimIdleSource.volume = Mathf.Lerp(swimIdleSource.volume, player.Settings.SFX_WaterIdle.EvaluateVolume(), 0.1f);
				if(swimMoveSource) swimMoveSource.volume = Mathf.Lerp(swimMoveSource.volume, 0, 0.1f);
			}
		}

		[Serializable]
		public class Settings
		{
			[Range01]     public float     initialDownwardVelocityReduction;
			[MinValue(0)] public float     initialDownwardVelocityMaximum = 15;
			public               float     movementSharpness              = 15f;
			public               float     swimSpeed;
			[Range01] public     float     gravityScale = 0.5f;
			public               LayerMask waterVolumeLayer;
			public               float     floatUpForce        = 0.2f; // The force applied every frame to the vertical velocity when we are underwater.
			public               float     floatUpDeceleration = 0.4f;
			public               double    maxDepthForGravity  = 1f; // Max depth of the feet before gravity ceases to apply.

			[Title("Audio")]
			public AudioDef adEmergeFromUnderwater;
			public AudioDef adLandInWater;
			public AudioDef adSwimming;
			public float    adSwimmingCooldown                = 0.5f; // Duration in seconds between each adSwimming play.
			public float    adEmergeFromUnderwaterMinVelocity = 0.25f;
		}
	}
}