using System;
using Anjin.Scripting;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Anjin.Utils
{
	/// <summary>
	/// Define some motion behaviour, the speed, animation, look and feel, etc.
	/// </summary>
	[Inline(true, true)]
	[ColorBox(0.075f)]
	[Serializable]
	[LuaUserdata]
	public struct MotionDef
	{
		public static MotionDef Default => new MotionDef
		{
			Type         = Motions.Accelerator,
			Speed        = 1.0f,
			Acceleration = 1.0f,
			Tween        = new EaserTo(0.25f, Ease.Linear)
		};

		public static MotionDef Constant => new MotionDef
		{
			Type         = Motions.Accelerator,
			Speed        = 2.5f,
			Acceleration = 0.0f,
			Tween        = new EaserTo(0.25f, Ease.Linear)
		};

		public static MotionDef Accelerator => new MotionDef
		{
			Type         = Motions.Accelerator,
			Speed        = 1.0f,
			Acceleration = 1.0f,
			Tween        = new EaserTo(0.25f, Ease.Linear)
		};

		public static MotionDef Lock => new MotionDef
		{
			Type         = Motions.Lock,
			Speed        = 1.0f,
			Acceleration = 1.0f,
			Tween        = new EaserTo(0.25f, Ease.Linear)
		};

		public static MotionDef Prev => new MotionDef
		{
			Type         = Motions.Prev,
			Speed        = 1.0f,
			Acceleration = 1.0f,
			Tween        = new EaserTo(0.25f, Ease.Linear)
		};

		public Motions Type;

		[ShowIf("IsTween")]
		public EaserTo Tween;

		[ShowIf("IsTween")]
		public bool SpeedBased;

		// Caster and first target (should only be 1)
		[FormerlySerializedAs("speed"), SerializeField]
		[ShowIf("IsAccelerator")]
		public float Speed;

		[FormerlySerializedAs("acceleration")]
		[ShowIf("IsAccelerator")]
		public float Acceleration;

		[ShowIf("IsDamp")]
		public float Damping;

		[ShowIf("IsSmoothDamp")]
		public float SmoothTime;

		[ShowIf("IsSmoothDamp")]
		public float MaxSpeed;


#if UNITY_EDITOR
		[UsedImplicitly] private bool IsAccelerator => Type == Motions.Accelerator;
		[UsedImplicitly] private bool IsDamp        => Type == Motions.Damper;
		[UsedImplicitly] private bool IsSmoothDamp  => Type == Motions.SmoothDamp;
		[UsedImplicitly] private bool IsTween       => Type == Motions.Tween;
		[UsedImplicitly] private bool IsCustom      => Type == Motions.Custom;
#endif
	}
}