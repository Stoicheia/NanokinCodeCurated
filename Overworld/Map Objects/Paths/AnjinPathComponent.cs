using System;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Components;

namespace Anjin.Nanokin.Map {

	[AddComponentMenu("Anjin/Anjin Path")]
	public class AnjinPathComponent : AnjinBehaviour, IAnjinPathHolder {

		[SerializeField, ShowInInspector, InlineProperty, HideReferenceObjectPicker, HideLabel]
		private AnjinPath _path;
		public AnjinPath Path      => _path;
		public Matrix4x4 Matrix => transform.localToWorldMatrix;

		private void Awake()
		{
			_path.OnEnterPlaymode();
			_path.EnsureVertsUpToDate();
		}

		private void OnValidate()
		{
			if (_path == null) {
				_path = new AnjinPath(this);
			} else {
				_path.Holder = this;
			}
		}

		private void Update()
		{
			if (!Application.isPlaying || !GameController.DebugMode || _path == null) return;

			CommandBuilder cb = Draw.ingame;

			Vector3 TO_WORLD(Vector3 pos)
			{
				if (_path.CalcSpace == AnjinPath.CalculationSpace.Absolute)
					return pos;

				return _path.BaseMatrix.MultiplyPoint3x4(pos);
			}

			AnjinPath.Point p1, p2;

			for (int i = 0; i < _path.Points.Count; i++)
			{
				bool at_end = i >= _path.Points.Count - 1;

				p1 = _path.Points[i];
				if (_path.Closed && at_end)
				{
					p2 = _path.Points[0];
				}
				else if (!at_end)
					p2 = _path.Points[i + 1];
				else
					p2 = null;

				Vector3 p1_pos = TO_WORLD(p1.position);
				Vector3 p1_rh  = TO_WORLD(p1.position + p1.right_handle);
				Vector3 p2_pos = Vector3.zero, p2_lh = Vector3.zero;

				if (p2 != null)
				{
					p2_pos = TO_WORLD(p2.position);
					p2_lh  = TO_WORLD(p2.position + p2.left_handle);
				}

				cb.WireSphere(p1_pos, 0.05f, ColorsXNA.CornflowerBlue);
				cb.Bezier(p1_pos, p1_rh, p2_lh, p2_pos, ColorsXNA.IndianRed);
			}
		}
	}
}