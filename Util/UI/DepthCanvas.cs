using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.EditorUtility
{
	[ExecuteAlways]
	public class DepthCanvas : SerializedMonoBehaviour
	{
		public int  Resolution = 1000;
		public bool AddScale;
		public int  BaseOrder = 1;

		private Canvas        _canvas;
		private RectTransform _rect;

		[ShowInPlay] private bool _debugMode;

		private void Awake()
		{
			_canvas = GetComponent<Canvas>();
			_rect   = GetComponent<RectTransform>();
		}

		public void LateUpdate()
		{
			_canvas.sortingOrder = BaseOrder;

			_canvas.sortingOrder += (int) _rect.position.z *  Resolution;

			if (AddScale)
				_canvas.sortingOrder += (int) (_rect.localScale.magnitude - 1) * Resolution;

			if (_debugMode)
			{
				Debug.Log(_rect.position.z);
			}
		}
	}
}