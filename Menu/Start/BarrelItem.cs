using System;
using UnityEngine;

namespace Menu.Start
{
	public class BarrelItem // TODO this should be a struct
	{
		public readonly Sprite sprite;
		public readonly string text;
		public readonly bool   selectable;
		public readonly Action onConfirmed;

		public BarrelItem(Sprite sprite, string text, bool selectable, Action onConfirmed)
		{
			this.sprite      = sprite;
			this.text        = text;
			this.selectable  = selectable;
			this.onConfirmed = onConfirmed;
		}
	}
}