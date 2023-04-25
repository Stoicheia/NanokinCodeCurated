using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.UI {
	public abstract class BustState {
        public string ID;
    }

    public class NormalState : BustState {

		public Sprite Base;

		[Title("Eyes")]
		[LabelText("Open")]		public Sprite Eyes_Open;
		[LabelText("Closed")]	public Sprite Eyes_Closed;

		[Title("Mouth")]
        [LabelText("Open")]			public Sprite Mouth_Open;
		[LabelText("Half Open")]	public Sprite Mouth_HalfOpen;
		[LabelText("Closed")]		public Sprite Mouth_Closed;

		[Title("Brows")]
		[LabelText("Main")] public Sprite Brows;
		[LabelText("Alt")]	public Sprite Brows_Alt;

		public Vector2? ImageSizeOverride;

		public bool CanBlink		=> Eyes_Open  != null && Eyes_Closed  != null;
		public bool CanBrowWiggle	=> Brows != null && Brows_Alt != null;
	}

    public class FrameSequenceState : BustState {

		public class Frame {
			public Sprite Sprite;
			[Tooltip("Set this to more than 0 to override the overall duration.")]
			public float  DurationOverride = -1;
		}

		[InfoBox("The overall frame duration in seconds.")]
		public float OverallFrameDuration = 1;

        public List<Frame> Frames = new List<Frame>();
    }

}