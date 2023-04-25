using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using Util;

public abstract class SelectableExtended<TMyself> : SelectableRoot, IRecyclable, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
	where TMyself : SelectableExtended<TMyself>
{
	[NotNull]
	protected abstract TMyself Myself { get; }

	public Action<TMyself> onPointerClick;
	public Action<TMyself> onPointerDown;
	public Action<TMyself> onPointerUp;
	public Action<TMyself> onPointerEnter;
	public Action<TMyself> onPointerExit;
	public Action<TMyself> onSelected;
	public Action<TMyself> onDeselect;

	public virtual void Recycle()
	{
		onPointerClick = null;
		onPointerDown  = null;
		onPointerUp    = null;
		onPointerEnter = null;
		onPointerExit  = null;
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		onPointerDown?.Invoke(Myself);

		if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
		{
			Debug.Log(this + ": On Pointer Click");
			onPointerClick?.Invoke(Myself);
		}
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		onPointerUp?.Invoke(Myself);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		onPointerEnter?.Invoke(Myself);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		onPointerExit?.Invoke(Myself);
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		onSelected?.Invoke(Myself);
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		onDeselect?.Invoke(Myself);
	}
}