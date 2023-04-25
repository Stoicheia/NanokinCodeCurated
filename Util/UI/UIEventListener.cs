using UnityEngine;
using UnityEngine.EventSystems;

namespace Anjin.EditorUtility {
	public class UIEventListener : MonoBehaviour,
	IPointerEnterHandler,
	IPointerExitHandler,
	IPointerDownHandler,
	IPointerUpHandler,
	IPointerClickHandler,
	IInitializePotentialDragHandler,
	IBeginDragHandler,
	IDragHandler,
	IEndDragHandler,
	IDropHandler,
	IScrollHandler,
	IUpdateSelectedHandler,
	ISelectHandler,
	IDeselectHandler,
	IMoveHandler,
	ISubmitHandler,
	ICancelHandler {
		public delegate void BaseEvent(BaseEventData		data);
		public delegate void PointerEvent(PointerEventData	data);


		public BaseEvent onUpdateSelected;
		public BaseEvent onSelect;
		public BaseEvent onDeselect;
		public BaseEvent onMove;
		public BaseEvent onSubmit;
		public BaseEvent onCancel;

		public PointerEvent onDrag;
		public PointerEvent onBeginDrag;
		public PointerEvent onEndDrag;
		public PointerEvent onPointerEnter;
		public PointerEvent onPointerExit;
		public PointerEvent onPointerDown;
		public PointerEvent onPointerUp;
		public PointerEvent onPointerClick;
		public PointerEvent onInitializePotentialDrag;
		public PointerEvent onDrop;
		public PointerEvent onScroll;

		public void OnDrag(						PointerEventData eventData) => onDrag?.Invoke(eventData);
		public void OnBeginDrag(				PointerEventData eventData) => onBeginDrag?.Invoke(eventData);
		public void OnEndDrag(					PointerEventData eventData) => onEndDrag?.Invoke(eventData);
		public void OnPointerEnter(				PointerEventData eventData) => onPointerEnter?.Invoke(eventData);
		public void OnPointerExit(				PointerEventData eventData) => onPointerExit?.Invoke(eventData);
		public void OnPointerDown(				PointerEventData eventData) => onPointerDown?.Invoke(eventData);
		public void OnPointerUp(				PointerEventData eventData) => onPointerUp?.Invoke(eventData);
		public void OnPointerClick(				PointerEventData eventData) => onPointerClick?.Invoke(eventData);
		public void OnInitializePotentialDrag(	PointerEventData eventData) => onInitializePotentialDrag?.Invoke(eventData);
		public void OnDrop(						PointerEventData eventData) => onDrop?.Invoke(eventData);
		public void OnScroll(					PointerEventData eventData) => onScroll?.Invoke(eventData);

		public void OnUpdateSelected(			BaseEventData eventData) => onUpdateSelected?.Invoke(eventData);
		public void OnSelect(					BaseEventData eventData) => onSelect?.Invoke(eventData);
		public void OnDeselect(					BaseEventData eventData) => onDeselect?.Invoke(eventData);
		public void OnMove(						AxisEventData eventData) => onMove?.Invoke(eventData);
		public void OnSubmit(					BaseEventData eventData) => onSubmit?.Invoke(eventData);
		public void OnCancel(					BaseEventData eventData) => onCancel?.Invoke(eventData);
	}
}