using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Actors.States;
using Anjin.Nanokin;
using Anjin.Util;
using DG.Tweening;
using Dreamteck.Splines;
using UnityEngine;
using UnityUtilities;
using Util.Extensions;

namespace Anjin.Minigames {

	public struct CoasterUIInputs
	{
		public enum State
		{
			Off,
			On,
			Selected
		}

		public State left_arrow;
		public State right_arrow;

		public static CoasterUIInputs Default = new CoasterUIInputs
		{
			left_arrow  = State.Off,
			right_arrow = State.Off,
		};
	}

	public partial class CoasterCarActor
	{

		public abstract class CoasterState : StateT<CoasterCarActor>
		{
			public CoasterConfig  Config   => actor.Config;
			public SplineFollower Follower => actor.Follower;

			public virtual PhysModes PhysMode => PhysModes.Follower;
		}

		public class IdleState : CoasterState
		{
			public override void OnUpdate(float dt)
			{
				base.OnUpdate(dt);
				if (!active) return;
				actor._leanNorm                = 0;

				Follower.motion.rotationOffset	= Vector3.zero;
				Follower.motion.offset			= Vector2.zero;
			}
		}

		public class RideState : CoasterState
		{
			public TrackJumpPoint LeftJP;
			public TrackJumpPoint RightJP;

			public override void OnActivate(State prev)
			{
				//actor.ChangePhysMode(PhysModes.Follower);
			}

			public override void OnDeactivate(State next)
			{
				LeftJP  = null;
				RightJP = null;
			}

			public override void OnUpdate(float dt)
			{
				base.OnUpdate(dt);

				LeftJP  = null;
				RightJP = null;

				if (!active) return;

				bool left  = actor.Inputs.leanLeft;
				bool right = actor.Inputs.leanRight;


				// Do tilt
				var targetTilt = 0;
				if (left)
					targetTilt = -1;
				else if (right)
					targetTilt = 1;

				actor._leanNorm = actor._leanNorm.Lerp(targetTilt, 0.15f);


				// Do jump point detection (for UI)
				if (actor._jumpPointsInside.Count > 0)
				{

					for (int i = 0; i < actor._jumpPointsInside.Count; i++)
					{
						TrackJumpPoint jp = actor._jumpPointsInside[i];

						// TODO (C.L. 01-22-2023): Priority system needed? Probably not for now.
						if (jp.Direction == TrackJumpPoint.Directions.Left) {
							LeftJP                    = jp;
							actor.UIInputs.left_arrow = left ? CoasterUIInputs.State.Selected : CoasterUIInputs.State.On;
						}

						if (jp.Direction == TrackJumpPoint.Directions.Right) {
							RightJP                    = jp;
							actor.UIInputs.right_arrow = right ? CoasterUIInputs.State.Selected : CoasterUIInputs.State.On;
						}
					}
				}

			}
		}

		public class JumpPointState : CoasterState
		{
			public Sequence       HopSeq;
			public TrackJumpPoint JumpPoint;
			public float          Distance;

			public override bool IsDone => HopSeq == null || !HopSeq.IsActive() || HopSeq.IsComplete();

			public override PhysModes PhysMode => PhysModes.Manual;

			public override void OnActivate(State prev)
			{
				/*Follower.enabled = false;
				Follower.spline  = null;
				Follower.follow  = false;*/

				//actor.ChangePhysMode(PhysModes.Manual);
				//HopSeq = actor.transform.DOJump(target, 3, 1, 0.45f);

				//float startPosY  = 0.0f;
				float height     = 3;
				//bool  offsetYSet = false;

				Transform trans   = actor.transform;
				double    percent = JumpPoint.Track2.Spline.Travel(0, Distance);
				Vector3   target  = JumpPoint.Track2.Spline.EvaluatePosition(percent);
				var sample = JumpPoint.Track2.Spline.Evaluate(percent);

				Sequence  s = DOTween.Sequence();

				float dist     = Vector3.Distance(trans.position, target);
				float duration = Config.HopDurationOverDistance.EvaluateSafe(dist);

				Debug.Log($"Duration: {duration}, Track Distance: {Distance}, Jump Distance: {dist}");

				/*float test = 0;
				DOTween.To(() => test, x => test = x, 5, duration).OnUpdate(() =>
				{
					Debug.Log("Test: " + test);
				});*/

				/*Tween yTween = DOTween.To((() => trans.position), (x => trans.position = x), new Vector3(0.0f, height, 0.0f), duration)
					.SetEase(actor.Config.HopVertical)
					.SetOptions(AxisConstraint.Y)
					.SetRelative()
					.OnStart(() =>
					{
						startPosY = trans.position.y;
					});*/

				float YOffset  = 0;
				float startY   = 0;
				float targetY  = 0;
				bool  midPoint = false;

				Tween yTween = DOTween.To((() => YOffset), (v => YOffset = v), 1, duration)
					.SetEase(actor.Config.HopVertical)
					.SetRelative()
					.OnStart(() =>
					{
						startY  = trans.position.y;
						targetY = trans.position.y + height;
					});

				yTween.OnUpdate(() => {

					if (!midPoint && yTween.ElapsedPercentage() >= 0.5f) {
						midPoint = true;

						targetY  = trans.position.y;
						startY   = target.y;
					}

					Vector3 position = trans.position;
					position.y     = DOVirtual.EasedValue(startY, targetY, yTween.ElapsedPercentage(), actor.Config.HopVertical);
					trans.position = position;
				});

				float      rotProg  = 0;
				Quaternion rotStart = trans.rotation;
				Quaternion rotTarget = Quaternion.LookRotation(sample.forward, sample.up);

				Tween rotTween = DOTween.To(() => rotProg, v => rotProg = v, 1, duration)
					.SetEase(Ease.Linear);

				rotTween.OnUpdate(() => {
					trans.rotation = Quaternion.Lerp(rotStart, rotTarget, rotTween.ElapsedPercentage());
				});

				s.Append(
					DOTween.To(() => trans.position, (x => trans.position = x), new Vector3(target.x, 0.0f, 0.0f), duration)
						.SetOptions(AxisConstraint.X)
						.SetEase(actor.Config.HopLateral))
				.Join(
					DOTween.To(() => trans.position, x => trans.position = x, new Vector3(0.0f, 0.0f, target.z), duration)
						.SetOptions(AxisConstraint.Z)
						.SetEase(actor.Config.HopLateral))
				.Join(yTween)
				.Join(rotTween)
				.SetEase(Ease.Linear);

				HopSeq = s;
			}

			public override void OnDeactivate(State next)
			{
				actor.SetTrack(JumpPoint.Track2, Distance);
			}

			public JumpPointState WithJumpPoint(TrackJumpPoint jp, float t)
			{
				JumpPoint = jp;
				Distance  = Mathf.Lerp(jp.Track2_Dist1, jp.Track2_Dist2, t);

				return this;
			}


		}

		[Serializable]
		public class SwordSwingState : CoasterState
		{
			public enum Directions { Forwards, Left, Right }

			public SphereCollider[] ForwardHitboxes;
			public SphereCollider[] LeftHitboxes;
			public SphereCollider[] RightHitboxes;

			public Transform ForwardsFXRoot;
			public Transform LeftFXRoot;
			public Transform RightFXRoot;

			public Directions Direction;

			/// <summary>
			/// New hits for this frame.
			/// </summary>
			[NonSerialized] public List<Collider> newHits = new List<Collider>();

			/// <summary>
			/// The hits for this sweep. Resets once the current sword sweep ends.
			/// </summary>
			[NonSerialized] public HashSet<Collider> currentHits = new HashSet<Collider>();

			private static List<IHitHandler<SwordHit>> _hithandlers = new List<IHitHandler<SwordHit>>();

			private float[]    _baseRadiuses;
			private Collider[] _overlaps = new Collider[8];
			private Vector3    _startPosition;

			[Serializable]
			public class Settings
			{
				public AnimationCurve SweepCurve;
				public float          Duration;
			}

			public override void OnActivate(State prev)
			{
				newHits.Clear();
				currentHits.Clear();
			}

			public override void OnUpdate(float dt)
			{
				if (!active)
					return;

				SphereCollider[] hitboxes = null;
				switch (Direction)
				{
					case Directions.Forwards: hitboxes = ForwardHitboxes;break;
					case Directions.Left:     hitboxes = LeftHitboxes;break;
					case Directions.Right:    hitboxes = RightHitboxes;break;
				}

				/*for (var i = 0; i < Hitboxes.Length; i++){
					Hitboxes[i].radius = _baseRadiuses[i] * settings.SweepCurve.Evaluate(animationPercent);
				}*/

				newHits.Clear();

				foreach (SphereCollider hitbox in hitboxes) {
					Hitscan(hitbox);
				}

				foreach (Collider hit in newHits)
				{
					if (hit == null) continue;

					actor.FX_SwordHitPrefab.Instantiate(hit.transform, hit.transform.position);

					/*GameObject freezeFrameObj = new GameObject("Freeze Frame Volume");
					freezeFrameObj.transform.position = hit.transform.position;

					SphereCollider sphereCollider = freezeFrameObj.AddComponent<SphereCollider>();
					sphereCollider.isTrigger = true;
					sphereCollider.radius    = 100f;

					FreezeFrameVolume freezeFrameVolume = freezeFrameObj.AddComponent<FreezeFrameVolume>();
					freezeFrameVolume.DurationFrames = 4;

					centroid.add(hit.transform.position);*/

				}
			}

			private void Hitscan(SphereCollider hitbox)
			{
				int mask = Layers.Default.mask | Layers.Enemy.mask | Layers.Actor.mask | Layers.Interactable.mask | Layers.Collidable.mask | Layers.Projectile.mask | Layers.PlatformingPhys.mask;
				int size = Physics.OverlapSphereNonAlloc(hitbox.gameObject.transform.position, hitbox.radius, _overlaps, mask, QueryTriggerInteraction.Collide);
				for (int i = 0; i < size; i++)
				{
					Collider overlap = _overlaps[i];
					// Don't wanna hit ourselves.
					if (overlap.gameObject == actor.gameObject || overlap.attachedRigidbody != null && overlap.attachedRigidbody.gameObject == actor.gameObject)
						continue;

					// Don't wanna hit triggers.
					if (overlap.isTrigger)
						continue;

					// Already hit this collider.
					if (currentHits.Contains(overlap))
						continue;

					overlap.GetComponentsInChildren(_hithandlers);

					// Not a hit handler.
					if (_hithandlers.Count == 0)
						continue;

					SwordHit hit = new SwordHit(actor.Position.Towards(overlap.transform.position).ChangeY(0).normalized, 0);

					bool any_hit = false;

					foreach (IHitHandler<SwordHit> handler in _hithandlers) {
						if(handler.IsHittable(hit)) {
							any_hit = true;
							handler.OnHit(hit);
						}
					}

					if (any_hit) {
						currentHits.Add(overlap);
						newHits.Add(overlap);
					}
				}
			}
		}

		public class VoidedState : CoasterState
		{
			public new bool UsesFollower => false;

			public override PhysModes PhysMode => PhysModes.Manual;

			public override void OnActivate(State prev)
			{
				/*Follower.enabled = false;
				Follower.spline  = null;
				Follower.follow  = false;*/
			}

			public override void OnUpdate(float dt)
			{
				if (!active) return;

				float gravity = Config.Gravity                  * 2;
				actor.transform.Translate(Vector3.down          * actor.Speed * Time.deltaTime, Space.World);
				actor.transform.Translate(actor._velocity.x_z() * Time.deltaTime,               Space.World);

				actor._velocity *= 0.975f;

				actor.Speed += gravity * Time.deltaTime;
			}
		}

	}
}