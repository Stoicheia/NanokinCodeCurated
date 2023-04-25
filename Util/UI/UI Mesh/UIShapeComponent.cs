using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace Anjin.EditorUtility.UIShape
{
	public class UIShapeComponent : UIShapeBase
	{
		// Shape
		[TitleGroup("Shape", order: -100)]
		public UIShapeBase Source = null;

		[TitleGroup("Shape")]
		public UIShape Shape = new UIShape();

		[TitleGroup("Shape")]
		public List<UIShapeLayer> Layers = new List<UIShapeLayer>
		{
			new UIShapeLayer(UIShapeLayerType.Solid)
		};

		public override UIShape GetShape()
		{
			if (Source != null)
			{
				UIShape sourceShape = Source.GetShape();
				if (sourceShape != null)
					return Source.GetShape();
			}

			return Shape;
		}

		public override List<UIShapeLayer> GetLayers() => Layers;

		protected override bool Populate(VertexHelper vh)
		{
			UIShape shape = GetShape();
			if (shape == null || shape.Vertices.Count <= 2) return false;

			for (int i = 0; i < Layers.Count; i++)
			{
				shape.BuildLayerGeometry(Layers[i], _rectTransform, color, _uiVerts, _tris);
			}

			return true;
		}
	}
}