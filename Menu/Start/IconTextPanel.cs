using UnityEngine;
using UnityEngine.Serialization;
using Util.RenderingElements.Barrel;
using Util.RenderingElements.PanelUI.Graphics;

namespace Menu.Start
{
	/// <summary>
	/// A single barrel card in the shop barrel menu.
	/// </summary>
	public class IconTextPanel : ListPanel
	{
		[FormerlySerializedAs("_label"),SerializeField]  public PanelLabel       Label;
		[FormerlySerializedAs("_sprite"),SerializeField] public PanelSpriteAddon Sprite;
	}
}