using System;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Sticker
{
	public class GridCell : SelectableExtended<GridCell>
	{
		public Image Image;

		[NonSerialized] public     Vector2Int    coordinate;
		[NonSerialized] public new RectTransform transform;

		protected override GridCell Myself => this;

		protected override void Awake()
		{
			base.Awake();
			transform = GetComponent<RectTransform>();
		}
	}
}