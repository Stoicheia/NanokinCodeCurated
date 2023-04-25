using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Utils
{
	public delegate void TimeScaleRefreshHandler(float dt, float scale);

	public class TimeScalable : MonoBehaviour
	{
		public static readonly List<TimeScalable> all = new List<TimeScalable>();

		private float _current;

		public float current
		{
			get { return _current; }
			set { _current = value; }
		}

		public float deltaTime => Time.deltaTime * current;

		public TimeScaleRefreshHandler onRefresh;

		private HashSet<TimeScaleVolumeBase> _scales = new HashSet<TimeScaleVolumeBase>();

		private static readonly List<TimeScaleVolumeBase> _tmpscales = new List<TimeScaleVolumeBase>();

		private Transform[] _childTransforms;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			all.Clear();
		}

		private void Awake()
		{
			current = 1;
			all.Add(this);
			foreach (TimeScaleVolumeBase vol in TimeScaleVolumeBase.globals)
				vol.Add(this);
		}

		private void OnDestroy()
		{
			all.Remove(this);
			_tmpscales.AddRange(_scales);

			foreach (TimeScaleVolumeBase vol in _tmpscales)
			{
				vol.Remove(this);
			}

			_tmpscales.Clear();
			_scales.Clear();
		}

		private void Start()
		{
			_childTransforms = GetComponentsInChildren<Transform>();
			Refresh();
		}

		public void Add(TimeScaleVolumeBase scale)
		{
			_scales.Add(scale);
			Refresh();
		}

		public void Remove(TimeScaleVolumeBase scale)
		{
			_scales.Remove(scale);
			Refresh();
		}

		public void Refresh()
		{
			current = 1;

			int layerBit = 1 << gameObject.layer;
			foreach (TimeScaleVolumeBase timeScale in _scales)
			{
				if (timeScale != null && (layerBit & timeScale.LayerMask.value) == layerBit)
				{
					current *= timeScale.Scaling;
				}
			}

			onRefresh?.Invoke(deltaTime, current);
		}

		public static Vector3 operator *(Vector3 t, TimeScalable ts)
		{
			if (ts == null) return t;
			return t * ts.current;
		}

		public static float operator *(float t, TimeScalable ts)
		{
			if (ts == null) return t;
			return t * ts.current;
		}
	}
}