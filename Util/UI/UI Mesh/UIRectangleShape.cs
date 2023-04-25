using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Anjin.EditorUtility.UIShape
{
	public class UIRectangleShape : UIShapeBase
	{
		[SerializeField, HideInInspector]
		private UIShape _shape;

		public float HorizontalSkew = 0;
		public float VerticalSkew   = 0;

		public List<UIShapeLayer> Layers = new List<UIShapeLayer>
		{
			new UIShapeLayer(UIShapeLayerType.Solid)
		};

		public override UIShape GetShape()
		{
			if (_shape == null)
			{
				_shape = new UIShape();
			}

			_shape.Vertices.Clear();

			_shape.Vertices.Add(new UIShapeVert { Anchor = new Vector2(0, 0), Offset = new Vector2(HorizontalSkew, VerticalSkew) });
			_shape.Vertices.Add(new UIShapeVert { Anchor = new Vector2(0, 1), Offset = new Vector2(-HorizontalSkew, VerticalSkew) });
			_shape.Vertices.Add(new UIShapeVert { Anchor = new Vector2(1, 1), Offset = new Vector2(-HorizontalSkew, -VerticalSkew) });
			_shape.Vertices.Add(new UIShapeVert { Anchor = new Vector2(1, 0), Offset = new Vector2(HorizontalSkew, -VerticalSkew) });

			_shape.IsQuad = true;
			return _shape;
		}

		public override List<UIShapeLayer> GetLayers() => Layers;

		protected override bool Populate(VertexHelper vh)
		{
			UIShape shape = GetShape();
			if (shape == null || shape.Vertices.Count <= 2) return false;

			for (int i = 0; i < Layers.Count; i++)
			{
				_shape.BuildLayerGeometry(Layers[i], _rectTransform, color, _uiVerts, _tris);
			}

			return true;
		}
	}
}