using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Anjin.UI
{
	[RequireComponent(typeof(CanvasScaler))]
	public class ScaleWithDPI : MonoBehaviour
	{
		private CanvasScaler _canvasScaler;
		public bool ScalingEnabled;
		public List<RectTransform> ToScale;

		private float _dpi;
		private Action OnDPIChange;

		private Dictionary<RectTransform, Vector3> _originalScales;

		private void Start()
		{
			_canvasScaler = GetComponent<CanvasScaler>();
			_originalScales = new Dictionary<RectTransform, Vector3>();

			if (ScalingEnabled)
			{
				foreach (RectTransform t in ToScale)
				{
					_originalScales.Add(t, t.localScale);
					t.localScale = _originalScales[t] * _canvasScaler.fallbackScreenDPI / Screen.dpi;
				}
			}

			OnDPIChange += () =>
			{
				if (ScalingEnabled)
				{
					foreach (var t in ToScale)
					{
						t.localScale = _originalScales[t] * _canvasScaler.fallbackScreenDPI / Screen.dpi;
					}
				}
			};
		}

		private void Update()
		{
			if (Math.Abs(_dpi - Screen.dpi) > 0.01f)
			{
				_dpi = Screen.dpi;
				OnDPIChange?.Invoke();
			}
		}
	}
}
