using System;
using Anjin.Util;
using Combat.Data.VFXs;
using Combat.Toolkit;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;

namespace Combat.Entities
{
	public class ObjectFighterActor : FighterActor
	{
		public string Name;

		public Transform VisualCenter;

		[FormerlySerializedAs("asset")]
		public ObjectFighterAsset Asset;

		[FormerlySerializedAs("_sr"), SerializeField]
		private SpriteRenderer SpriteRenderer;

		private bool     _almostDestroyed;
		private BlinkVFX _blink1HP;

		protected override void Awake()
		{
			base.Awake();

			if (VisualCenter == null)
				VisualCenter = transform;

			_almostDestroyed = false;

			//_blink1HP = new BlinkVFX(5, 0.5f, Color.white, ColorsXNA.DarkRed);
			_blink1HP = new BlinkVFX(5f, 0.5f, ColorsXNA.DarkRed, ColorsXNA.DarkRed)
			{
				paused = true
			};
		}

		private void Update()
		{
			center = VisualCenter.position;
			if (fighter != null)
			{
				if (fighter.max_points.hp > 1 && Math.Abs(fighter.hp - 1) < Mathf.Epsilon)
				{
					if (!vfx.Contains(_blink1HP))
					{
						_almostDestroyed = true;

						_blink1HP.elapsed = 0;
						_blink1HP.paused  = false;
						vfx.Add(_blink1HP);
					}
				}
				else
				{
					_blink1HP.paused = true;
					vfx.Remove(_blink1HP);
				}
			}

			if (vfx != null)
			{
				VFXState vfxstate = vfx.state;

				Color tint     = vfxstate.tint.Alpha(vfxstate.opacity);
				Color fill     = vfxstate.fill;
				float emission = vfxstate.emissionPower;

				SpriteRenderer.color = tint;
				SpriteRenderer.ColorFill(fill);
				SpriteRenderer.EmissionPower(emission);
			}
		}
	}
}