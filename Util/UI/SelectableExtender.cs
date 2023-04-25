using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;

[Obsolete]
public class SelectableExtender : SerializedMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[FormerlySerializedAs("colors")]       public StateColors Colors = StateColors.defaultStateColors;
	[FormerlySerializedAs("fadeDuration")] public float       FadeDuration;

	private Selectable _selectable;
	private bool       _selected;

	[Title("Debug")]
	[ShowInInspector]
	public bool Selected
	{
		get => _selected;
		set
		{
			_selected = value;
			OnStateChange();
		}
	}

	private bool _hover;

	[ShowInInspector]
	public bool Hover
	{
		get => _hover;
		set
		{
			_hover = value;
			OnStateChange();
		}
	}

	// Start is called before the first frame update
	private void Start()
	{
		_selectable = GetComponent<Selectable>();
	}

	public void OnStateChange()
	{
		if (_selectable && _selectable.targetGraphic)
		{
			Color col = Colors.normal;

			if (_hover) col    = Colors.hover;
			if (_selected) col = Colors.selected;

			_selectable.targetGraphic.color = col;
			//_selectable.targetGraphic.CrossFadeColor(col, FadeDuration, false, false);
		}
	}

	public void OnPointerEnter(PointerEventData eventData) => Hover = true;
	public void OnPointerExit(PointerEventData  eventData) => Hover = false;

	[VerticalLabel, InlineProperty, BoxGroup]
	public struct StateColors
	{
		public Color normal;
		public Color hover;
		public Color selected;
		public Color pressed;

		public static StateColors defaultStateColors => new StateColors
		{
			normal   = Color.white,
			hover    = Color.white,
			selected = Color.white,
			pressed  = Color.white
		};
	}
}