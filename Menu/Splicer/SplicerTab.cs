using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util.Odin.Attributes;

namespace Puppets.Render
{
	/// <summary>
	/// Contains the components needed to render the tab above
	/// the limb grid in the limb splicing menu.
	/// </summary>
	public class SplicerTab : SerializedMonoBehaviour, IPointerClickHandler
	{
		[Title("References")]
		public Image Image;
		public            Canvas             Canvas;
		public            SelectableExtender Selectable;
		[Optional] public TextMeshProUGUI    TextLabel;
		[Optional] public Image              ImageLabel;

		[Title("Config")]
		[SerializeField] private Sprite TabActiveSprite;
		[SerializeField] private float TabActiveY;
		[SerializeField] private Color TabActiveTextColor;
		[Space]
		[SerializeField] private Sprite TabInactiveSprite;
		[SerializeField] private float TabInactiveY;
		[SerializeField] private Color TabInactiveTextColor;

		[NonSerialized] public int tabIndex;

		public event Action<SplicerTab, PointerEventData> OnClicked;

		public void SetActive(bool state)
		{
			Selectable.Selected = state;

			if (state)
			{
				// The active tab!
				// Rect.anchoredPosition = Rect.anchoredPosition.Change2(y: TabActiveY);
				Image.sprite = TabActiveSprite;

				if (TextLabel)
				{
					TextLabel.color = TabActiveTextColor;
					TextLabel.SetAllDirty();
				}
				else if (ImageLabel)
				{
					ImageLabel.color = TabActiveTextColor;
				}

				Canvas.overrideSorting = true;
				Canvas.sortingOrder    = 1;
			}
			else
			{
				// Not the active tab ..
				// Rect.anchoredPosition = Rect.anchoredPosition.Change2(y: TabInactiveY);

				Image.sprite = TabInactiveSprite;
				if (TextLabel)
				{
					TextLabel.color = TabInactiveTextColor;
					TextLabel.SetAllDirty();
				}
				else if (ImageLabel)
				{
					ImageLabel.color = TabInactiveTextColor;
				}

				Canvas.overrideSorting = false;
				Canvas.sortingOrder    = 0;
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			OnClicked?.Invoke(this, eventData);
		}
	}
}