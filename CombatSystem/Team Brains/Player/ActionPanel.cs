using System;
using UnityEngine;
using Util.RenderingElements.Barrel;
using Util.RenderingElements.PanelUI.Graphics;

namespace Combat
{
	public class ActionPanel : ListPanel
	{
		[SerializeField] private PanelLabel       _label;
		[SerializeField] private PanelTextureIcon _icon;
		[SerializeField] public  Texture2D        iconLocked;

		// public Handler SelectionGained;
		// public Handler SelectionLost;
		//
		// // The easiest way I know how to do this currently -C.L.
		// protected override void OnSelectionGained() => SelectionGained?.Invoke();
		// protected override void OnSelectionLost()   => SelectionLost?.Invoke();

		public Texture2D Icon
		{
			set => _icon.Override = value ? value : iconLocked;
		}

		public string Text
		{
			set => _label.Text = value;
		}

		// public SkillAsset Skill
		// {
		// 	get => _skill;
		// 	set
		// 	{
		// 		_skill = value;
		//
		// 		if (_skill != null)
		// 		{
		// 			// Some default.
		// 			_label.Text              = _skill.SkillInfo.displayName;
		// 			IsSelectable             = !_skill.SkillInfo.passive;
		// 			_icon.ReplacementTexture = null;
		// 		}
		// 		else
		// 		{
		// 			// No skill to pick.
		// 			_label.Text              = "???";
		// 			_icon.ReplacementTexture = _iconLockedTexture;
		// 			IsSelectable             = false; // Can't pick a non-existent skill.
		// 		}
		// 	}
		// }
	}
}