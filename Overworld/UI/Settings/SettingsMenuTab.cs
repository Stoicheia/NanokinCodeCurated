using Anjin.EditorUtility.UIShape;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Overworld.UI.Settings {

	public class SettingsMenuTab : SelectableExtended<SettingsMenuTab> {
		protected override SettingsMenuTab Myself => this;

		public RectTransform rectTransform;
		public TMP_Text      Text;

		public bool Selected = false;
		public bool Hovered  = false;

		public Color normalColor 	= Color.white;
		public Color selectedColor 	= Color.black;
		public Color hoverColor 	= Color.blue;

		protected override void Update()
		{
			base.Update();

			if (Selected)
				Text.color = selectedColor;
			else if (Hovered)
				Text.color = hoverColor;
			else
				Text.color = normalColor;
		}

		[Button]
		public void SnapRectangleShapeTo(UIRectangleShape mesh)
		{
			if (Text != null) {
				Bounds b = Text.textBounds;

				mesh.rectTransform.localPosition 	= rectTransform.localPosition + b.center;
				mesh.rectTransform.sizeDelta    	= b.size;

				mesh.HorizontalSkew = Random.Range(-4, 4);
				mesh.VerticalSkew 	= Random.Range(-4, 4);

				mesh.Redraw();
			}
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			Hovered = true;
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			Hovered = false;
		}
	}
}