using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overworld.UI.Settings {
	public class SliderControl : SettingsMenuControl<float> {

		public Slider   Slider;
		public TMP_Text ValueLabel;
		public float    Increment = 0.1f;
		public bool     SnapToInt;
		public int      DecimalPlaces = 1;

		public override void Awake()
		{
			base.Awake();

			Slider.onValueChanged.AddListener(val => {
				OnChanged?.Invoke(val);
				ValueLabel.text = val.ToString();
				val             = Mathf.Round(val / Increment) * Increment;
				Slider.SetValueWithoutNotify(val);
				UpdateLabel(val);
			});
		}

		[Button]
		public void Setup(float min, float max, float increment = 1, bool snapToInt = false, int decimalPlaces = 1)
		{
			Slider.minValue = min;
			Slider.maxValue = max;

			Increment = increment;
			SnapToInt = snapToInt;

			DecimalPlaces = decimalPlaces;
		}


		[Button]
		public override void Set(float val)
		{
			val = Mathf.Round(val / Increment) * Increment;
			Slider.SetValueWithoutNotify(val);
			UpdateLabel(val);
		}

		public void UpdateLabel(float val)
		{
			ValueLabel.text = val.ToString($"F{DecimalPlaces}");
		}

		public override Selectable Selectable => Slider;
	}
}