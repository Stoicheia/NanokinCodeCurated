using System.Collections.Generic;
using API.Spritesheet.Indexing.Runtime;
using UnityEngine;

namespace Anjin.Actors
{
	public interface IRenderStateModifier
	{
		void ModifyRenderState(ActorRenderer renderer, ActorBase actor, ref RenderState state);
	}

	public struct RenderState
	{
		public enum AnimMode
		{
			Named,
			CustomFrames
		}

		public AnimMode         animMode;
		public string           animName;
		public AnimID           animID;
		public AnimationBinding animCustom;
		public float            animSpeed;
		public AnimationCurve   animSpeedCurve;
		public float            animPercent;
		public int              animRepeats;

		public Vector3                        offset;
		public float                          pitch;
		public float                          roll;
		public bool?                          loops;
		public Dictionary<int, FrameModifier> frameModifiers;

		public bool dropShadowDisable;

		public bool xFlip;
		public bool yFlip;

		public RenderState(
			AnimID  animID,
			float   animSpeed   = 1,
			Vector3 offset      = default,
			float   animPercent = -1
		)
		{
			animMode         = AnimMode.Named;
			animName         = null;
			this.animID      = animID;
			this.animPercent = animPercent;
			this.animSpeed   = animSpeed;
			this.offset      = offset;
			pitch            = 0;
			roll             = 0;
			animRepeats      = 0;
			loops            = null;
			frameModifiers   = null;
			animSpeedCurve   = null;
			animCustom       = AnimationBinding.Invalid;

			xFlip            = false;
			yFlip            = false;

			dropShadowDisable = false;
		}

		public RenderState(string animName,
			float                 animSpeed   = 1,
			Vector3               offset      = default,
			float                 animPercent = -1
		)
		{
			animMode         = AnimMode.Named;
			this.animName    = animName;
			animID           = AnimID.Stand;
			this.animPercent = animPercent;
			this.animSpeed   = animSpeed;
			this.offset      = offset;
			pitch            = 0;
			roll             = 0;
			animRepeats      = 0;
			loops            = null;
			frameModifiers   = null;
			animSpeedCurve   = null;
			animCustom       = AnimationBinding.Invalid;

			xFlip = false;
			yFlip = false;

			dropShadowDisable = false;
		}


		public RenderState(AnimationBinding animCustom,
			float                           animSpeed   = 1,
			Vector3                         offset      = default,
			float                           animPercent = -1
		)
		{
			animMode         = AnimMode.CustomFrames;
			this.animCustom  = animCustom;
			this.animSpeed   = animSpeed;
			this.animPercent = animPercent;
			this.offset      = offset;

			animName = "";
			animID   = AnimID.Stand;

			pitch          = 0;
			roll           = 0;
			animRepeats    = 0;
			loops          = null;
			frameModifiers = null;
			animSpeedCurve = null;

			xFlip = false;
			yFlip = false;

			dropShadowDisable = false;
		}
	}
}