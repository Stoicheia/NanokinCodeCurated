using System;
using Anjin.Util;
using Drawing;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Math.Splines;
using Random = UnityEngine.Random;

namespace Combat.Data
{
	public class ScatterPlotter : PlotShape
	{
		[SerializeField] private Color _gizmoColor = Color.green;
		[SerializeField] private float _gizmoSize  = 0.1f;

		[SerializeField, Space, OnValueChanged("OnChanged")]
		public int PlotCount = 10;

		[SerializeField, OnValueChanged("OnChanged")]
		public float Radius = 3;

		[SerializeField, OnValueChanged("OnChanged")]
		public bool Spherical;

		[SerializeField, OnValueChanged("OnChanged")]
		public bool GroundSnap = true;

		[SerializeField, Space, OnValueChanged("OnChanged")]
		public float Angle;

		[SerializeField, OnValueChanged("OnChanged")]
		public float AngleFuzz;

		[SerializeField, Space, OnValueChanged("OnChanged")]
		public bool FixedSeed;

		[SerializeField, ShowIf("_isFixedSeed"), OnValueChanged("OnChanged")]
		public int Seed;

		private int       _seed;
		private Vector3[] _circleBuffer;
		private Plot[]    _plots;

		private void Awake()
		{
			_seed = FixedSeed ? Seed : RNG.Int();
		}

		public override Plot Get(int index)
		{
			if (_plots == null || index >= _plots.Length)
				CreatePlots(Math.Max(PlotCount, index + 1));

			return _plots[index];
		}

		public override Plot Get(int index, int max)
		{
			if (_plots == null || max >= _plots.Length)
				CreatePlots(Math.Max(PlotCount, max));

			return _plots[index];
		}

		private void CreatePlots(int n)
		{
			Vector3 facing = Quaternion.Euler(0, Angle + AngleFuzz, 0) * transform.forward;

			Random.InitState(_seed);
			for (var i = 0; i < n; i++)
			{
				Vector3 pos        = transform.position;
				Vector2 circlePlot = Random.insideUnitCircle * Radius;

				pos += new Vector3(circlePlot.x, 0, circlePlot.y);
				if (GroundSnap)
					pos = pos.DropToGround();

				_plots[i] = new Plot(pos, facing);
			}
		}

		private void OnDrawGizmos()
		{
			for (var i = 0; i < PlotCount; i++)
			{
				Plot plot = Get(i);

				Draw2.DrawTwoToneSphere(plot.position, _gizmoSize, _gizmoColor);
				Draw2.DrawLine(plot.position, plot.position + plot.facing, _gizmoColor);
			}

			if (!Spherical)
			{
				Draw.CircleXZ(transform.position, Radius, _gizmoColor);
			}
			else
			{
				Gizmos.color = _gizmoColor;
				Gizmos.DrawWireSphere(transform.position, Radius);
			}
		}

#if UNITY_EDITOR
		[UsedImplicitly]
		private void OnChanged()
		{
			CreatePlots(PlotCount);
			_plots = null;
		}
#endif
	}
}