using System;
using JetBrains.Annotations;
using UnityEngine;
using Util.RenderingElements.Barrel;
using Util.RenderingElements.PanelUI.Graphics;

namespace Combat
{
	public class ActCategoryPanel : ListPanel
	{
		[SerializeField] private PanelTextureIcon _icon;
		[SerializeField] private PanelLabel       _label;

		protected override void OnValueChanged()
		{
			base.OnValueChanged();
			_label.Text = ToString((PlayerBrain.MainActions) ValueInt);
		}

		public string Text
		{
			get => _label.Text;
			set => _label.Text = value;
		}

		public Texture2D Icon
		{
			set => _icon.Override = value;
		}

		[NotNull]
		private static string ToString(PlayerBrain.MainActions v)
		{
			switch (v)
			{
				case PlayerBrain.MainActions.Act:  return "Act";
				case PlayerBrain.MainActions.Hold: return "Hold";
				case PlayerBrain.MainActions.Move: return "Move";
				case PlayerBrain.MainActions.Flee: return "Flee";
				case PlayerBrain.MainActions.Skip: return "Skip";
				default:
					throw new ArgumentOutOfRangeException(nameof(v), v, null);
			}
		}
	}
}