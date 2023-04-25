using System;
using Anjin.Cameras;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class ActorPlaybackBrain : ActorBrain, IAnimOverrider
	{
		[FormerlySerializedAs("System")]
		public ActorPlaybackData Data;
		public bool Loop;

		[NonSerialized] public bool finished;

		[DebugVars]
		[OnValueChanged("OnIndexChanged")]
		private int _index;
		private float _elapsed;


		[DebugVar]
		public ActorKeyframe CurrentFrame => Data.Keyframes[_index];

		public override int Priority => 100;

		public override void OnBeginControl()
		{
			OnStart();
		}

		public override void OnEndControl()
		{
			OnFinish();
		}

		private void OnStart()
		{
			actor.disablePhysics = true;

			_elapsed = 0;
			_index   = 0;
			OnKeyframe();
		}

		private void OnKeyframe()
		{
			ActorKeyframe kf = Data.Keyframes[_index];

			actor.Teleport(kf.Position);
			actor.Reorient(kf.Facing);
		}

		private void OnFinish()
		{
			finished             = true;
			actor.disablePhysics = false;
		}

		public override void OnTick(float dt)
		{
			if (finished) return;

			// Interpolation
			// ----------------------------------------
			ActorKeyframe last = Data.Keyframes[_index];
			ActorKeyframe next = Data.Keyframes[_index + 1];

			float t = Mathf.InverseLerp(last.Time, next.Time, _elapsed);

			Vector3 newpos    = actor.Position;
			Vector3 newfacing = actor.facing;

			if (next.PositionCurve.x > -1)
			{
				AnimationCurve xc = GameAssets.Live.ActorPlaybackCurves.InterpolationCurves[next.PositionCurve.x];
				newpos.x = Mathf.Lerp(last.Position.x, next.Position.x, xc.Evaluate(t));
			}

			if (next.PositionCurve.y > -1)
			{
				AnimationCurve yc = GameAssets.Live.ActorPlaybackCurves.InterpolationCurves[next.PositionCurve.y];
				newpos.y = Mathf.Lerp(last.Position.y, next.Position.y, yc.Evaluate(t));
			}

			if (next.PositionCurve.z > -1)
			{
				AnimationCurve zc = GameAssets.Live.ActorPlaybackCurves.InterpolationCurves[next.PositionCurve.z];
				newpos.z = Mathf.Lerp(last.Position.z, next.Position.z, zc.Evaluate(t));
			}

			if (next.FacingCurve > -1)
			{
				AnimationCurve curve = GameAssets.Live.ActorPlaybackCurves.InterpolationCurves[next.FacingCurve];
				newfacing = Vector3.Lerp(last.Facing, next.Facing, curve.Evaluate(t));
			}

			actor.Teleport(newpos);
			actor.Reorient(newfacing);

			// Keyframe advance
			// ----------------------------------------
			while (_elapsed >= Data.Keyframes[_index + 1].Time)
			{
				_index++;
				OnKeyframe();

				bool lastFrame = _index == Data.FrameCount - 1;
				if (lastFrame)
				{
					if (Loop)
						OnStart();
					else
						OnFinish();

					break;
				}
			}

			_elapsed += Time.deltaTime;
		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			if (!finished)
			{
#if UNITY_EDITOR
				Draw.Line(actor.Position, CurrentFrame.Position, Color.magenta);
				Draw.Circle(CurrentFrame.Position, GameCams.Live.Brain.transform.forward, 0.1f, Color.magenta);
#endif
			}
		}

		public override bool OverridesAnim(ActorBase actor)
		{
			return !finished;
		}

		public override void OnAnimEndReached(ActorBase actor) { }

		public override RenderState GetAnimOverride(ActorBase actor)
		{
			return new RenderState(Data.Keyframes[_index].State);
		}

#if UNITY_EDITOR
		private void OnIndexChanged()
		{
			_elapsed = Data.Keyframes[_index].Time;
			OnKeyframe();
		}
#endif
	}
}