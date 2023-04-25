using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Anjin.UI {
	public class ChoiceTextboxItem : SelectableExtended<ChoiceTextboxItem> {

		public TextMeshProUGUI text;
		public TextMeshProUGUI cursor;
		public RectTransform   rt;

		public Color Color_Inactive;
		public Color Color_Active;

		public DynamicMeshUI   highlight;   //TODO: Figure out how to separate this

		public bool cancelChoice;
		public int index;

		private FontStyles _baseFontStyles;

		private void Awake()
		{
			rt = GetComponent<RectTransform>();

			highlight.Verts.Clear();
			highlight.Verts.Add(new DynamicMeshUI.Vert{anchor = new Vector2(0, 0), offset = new Vector2(Random.Range(5, -20), Random.Range(0, -20))});
			highlight.Verts.Add(new DynamicMeshUI.Vert{anchor = new Vector2(0, 1), offset = new Vector2(Random.Range(5, -20), Random.Range(0, 20))});
			highlight.Verts.Add(new DynamicMeshUI.Vert{anchor = new Vector2(1, 1), offset = new Vector2(Random.Range(-5, 20), Random.Range(0, 20))});
			highlight.Verts.Add(new DynamicMeshUI.Vert{anchor = new Vector2(1, 0), offset = new Vector2(Random.Range(-5, 20), Random.Range(0, -20))});
			highlight.Redraw();

			highlight.gameObject.SetActive(false);
			cursor.gameObject.SetActive(false);

			text.color      = Color_Inactive;
			_baseFontStyles = text.fontStyle;
		}

		[Button]
		public void SnapMeshUITo(DynamicMeshUI mesh)
		{
			if (text != null && rt != null) {
				Bounds b = text.textBounds;

				//mesh.rectTransform.GetWorldCorners();

				//mesh.rectTransform.TransformTo(rt);

				mesh.rectTransform.localPosition = b.center;
				mesh.rectTransform.sizeDelta     = b.size;

			}
		}

		public void ShowHighlight()
		{
			/*highlight.gameObject.SetActive(true);
			SnapMeshUITo(highlight);*/
			text.color = Color_Active;
			//text.fontStyle = _baseFontStyles | FontStyles.Underline;
			cursor.gameObject.SetActive(true);
			//text.color = Color.black;
		}

		public void HideHighlight()
		{
			//highlight.gameObject.SetActive(false);
			text.color = Color_Inactive;
			//text.fontStyle = _baseFontStyles;
			cursor.gameObject.SetActive(false);
			//text.color = Color.white;
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			ShowHighlight();
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			HideHighlight();
		}

		public override void OnSelect(BaseEventData eventData)
		{
			base.OnSelect(eventData);
			ShowHighlight();
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			HideHighlight();
		}

		protected override ChoiceTextboxItem Myself => this;
	}
}