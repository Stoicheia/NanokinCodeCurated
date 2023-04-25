using System;
using Anjin.Util;
using UnityEngine;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Map {

	// TODO(C.L.): Accessability option to draw outlines for platforms that are off?

	[SelectionBase]
	public class TogglePlatform : MonoBehaviour {

		public const float FLASH_DURATION	= 0.5f;
		public const float FLASH_SPEED		= 0.001f;

		public Transform ActiveTransform;
		public Renderer  ActiveRenderer;

		public bool Active = true;

		public bool ToggleStateOnStart = true;

		public float OnInterval  = 1;
		public float OffInterval = 1;

		public Option<float> FlashDuration;
		public Option<float> FlashSpeed;

		[NonSerialized, ShowInPlay] public bool     ToggleState;
		[NonSerialized, ShowInPlay] public ValTimer _tmrToggle;
		[NonSerialized, ShowInPlay] public ValTimer _rendererToggle;

		private void Start()
		{
			ToggleState = ToggleStateOnStart;
			OnToggle(ToggleState);
			_tmrToggle.Set(ToggleState ? OnInterval : OffInterval, true);
		}

		public virtual void Update()
		{
			if (GameController.IsWorldPaused) return;

			if (Active) {
				if(ActiveRenderer) {
					if(ToggleState && _tmrToggle.time < FlashDuration.ValueOrDefault(FLASH_DURATION)) {
						if(_rendererToggle.Tick()) {
							ActiveRenderer.enabled = !ActiveRenderer.enabled;
							_rendererToggle.Set(FlashSpeed.ValueOrDefault(FLASH_SPEED), true);
						}
					}else {
						ActiveRenderer.enabled = true;
					}
				}

				if (_tmrToggle.Tick()) {
					ToggleState = !ToggleState;
					OnToggle(ToggleState);
					_tmrToggle.Set(ToggleState ? OnInterval : OffInterval, true);
				}
			}
		}

		public virtual void OnToggle(bool state) {
			ActiveTransform.gameObject.SetActive(state);
		}

	}
}