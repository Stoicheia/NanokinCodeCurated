using Combat.Data.VFXs;
using DG.Tweening;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class HopVFX : VFX
	{
		public int            count;
		public float          speed;
		public float          height;

		public AnimationCurve curve;

		private TweenableVector3 _offset = new TweenableVector3();

		public override Vector3 VisualOffset => _offset;

		public override bool IsActive => _offset.IsTweenActive;

		public AudioDef sfx_jump;
		public AudioDef sfx_land;

		public HopVFX(int count, float speed, float height, AnimationCurve curve = null, AudioDef? _sfx_jump = null, AudioDef? _sfx_land = null)
		{
			this.count  = count;
			this.speed  = speed;
			this.height = height;
			this.curve  = curve;

			sfx_jump = _sfx_jump ?? GameAssets.Live.SFX_Default_Jump_Sound;
			sfx_land = _sfx_land ?? GameAssets.Live.SFX_Default_Land_Sound;
		}

		internal override void Enter()
		{
			base.Enter();
			if (curve != null)
				_offset.To(Vector3.zero, new JumperTo(count * speed, height, curve, count) {
					OnJump = () => { if(sfx_jump.IsValid) GameSFX.PlayGlobal(sfx_jump); },
					OnLand = () => { if(sfx_land.IsValid) GameSFX.PlayGlobal(sfx_land); }
				});
			else
				_offset.To(Vector3.zero, new JumperTo(count * speed, height, count) {
					OnJump = () => { if(sfx_jump.IsValid) GameSFX.PlayGlobal(sfx_jump); },
					OnLand = () => { if(sfx_land.IsValid) GameSFX.PlayGlobal(sfx_land); }
				});
		}

		public override void EndPrematurely()
		{
			base.EndPrematurely();
			_offset.CompleteIfTweening();
		}
	}
}