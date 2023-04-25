using Combat.Data.VFXs;
using UnityEngine;
using UnityUtilities;
using Util;

namespace Combat.Toolkit
{
	/// <summary>
	/// A blinking vfx using emission and fill color.
	/// Use for invincibility, attracting attention to actors, etc.
	/// </summary>
	public class BlinkVFX : VFX
	{
		// Options
		// ----------------------------------------
		public float power;
		public float speed;
		public Color fill = Color.white;
		public Color tint = Color.clear;


		// State
		// ----------------------------------------
		public float elapsed;
		public bool  paused;

		private bool  _active;
		private Color _fill;
		private Color _tint;
		private float _emission;


		public BlinkVFX(float power = 1f, float speed = 1f, Color fill = default(Color), Color tint = default(Color))
		{
			this.power = power;
			this.speed = speed;
			this.fill = ((fill != default(Color)) ? fill : Color.white);
			this.tint = ((tint == default(Color)) ? Color.clear : tint);
		}

		public override float EmissionPower => _emission;
		public override Color Fill          => _fill;
		public override Color Tint          => _tint;

		public override bool IsActive => _active || _emission > 0 || _fill.a > 0;

		public override void Update(float dt)
		{
			if (_active && !paused)
			{
				// Blinking the fill color
				// ----------------------------------------
				elapsed += dt;

				// TODO clean up these hardcoded values
				float amp = Mathf.Sin(elapsed * 16 * speed) * 0.5f + 0.5f;

				_fill     = fill.Alpha(amp * 0.105f * power);
				_tint     = Color.Lerp(Color.white, tint, amp * 0.105f * power);
				_emission = amp * 0.525f * power;
			}
			else
			{
				// Ease down to no blinking
				// ----------------------------------------

				// FILL
				_fill = _fill.Alpha(MathUtil.LerpDamp(_fill.a, 0, 4));

				// EMISSION
				_emission = MathUtil.LerpDamp(_emission, 0, 8);

				// TINT
				Color tintDiff = Color.white - _tint;
				float tintDist = (tintDiff.r + tintDiff.g + tintDiff.b + tintDiff.a) / 4f;
				tintDist = MathUtil.LerpDamp(tintDist, 0, 4);
				_tint    = Color.Lerp(Color.white, tint, tintDist);

				// CLAMPING
				// if (_fill.a < 0.01f) _fill.a = 0;
				// if (_emission < 0.01f) _fill.a = 0;
				// if (tintDist < 0.01f) _fill = Color.white;
			}

			base.Update(dt);
		}

		internal override void Enter()
		{
			elapsed = 0;
			_active = true;
		}

		internal override void Leave()
		{
			elapsed = 0;
			_active = false;
		}
	}
}