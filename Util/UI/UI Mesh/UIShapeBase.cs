using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Anjin.EditorUtility.UIShape
{
	[RequireComponent(typeof(CanvasRenderer))]
	public abstract class UIShapeBase : SerializedMaskableGraphic
	{
		// Graphic
		public Texture Texture;

		public override Texture mainTexture
		{
			get
			{
				if (Texture == null)
				{
					if (material != null && material.mainTexture != null)
					{
						return material.mainTexture;
					}

					return s_WhiteTexture;
				}

				return Texture;
			}
		}

		protected RectTransform  _rectTransform;
		protected List<int>      _tris;
		protected List<UIVertex> _uiVerts;

		public abstract UIShape            GetShape();
		public abstract List<UIShapeLayer> GetLayers();

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
			if (_rectTransform == null) return;

			if (_tris == null) _tris       = new List<int>();
			if (_uiVerts == null) _uiVerts = new List<UIVertex>();

			_tris.Clear();
			_uiVerts.Clear();

			if (!Populate(vh)) return;

			vh.AddUIVertexStream(_uiVerts, _tris);
		}

		protected abstract bool Populate(VertexHelper vh);

		[Button]
		public void Redraw()
		{
			SetVerticesDirty();
			SetMaterialDirty();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();
			SetVerticesDirty();
			SetMaterialDirty();
		}
	}
}