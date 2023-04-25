using System;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Anjin.UI {

	public enum TransitioningElementState {
		Off, On, Transition
	}

	public class InteractPopup : MonoBehaviour {

		public TransitioningElementState State;

		public HUDElement                Element;
		public Interactable              Interactable;

		public GameObject InteractBubble;
		public GameObject TalkBubble;

		public bool IsActive => State != TransitioningElementState.Off;

		private void Start()
		{
			Element.Alpha = 0;

			foreach (InputButtonLabel label in GetComponentsInChildren<InputButtonLabel>()) {
				label.Button = GameInputs.interact;
			}
		}

		public void Show(Interactable interactable)
		{
			Interactable = interactable;

			if (interactable == null) {
				_hideInstant();
				return;
			}

			InteractBubble.SetActive(interactable.ShowType == Interactable.Type.Action);
			TalkBubble.SetActive(interactable.ShowType     == Interactable.Type.Talk);

			Element.WorldAnchor.mode       = WorldPoint.WorldPointMode.GameObject;
			Element.WorldAnchor.gameobject = interactable.gameObject;

			if (interactable.TryGetComponent(out Actor actor))
				Element.WorldAnchorOffset = Vector3.up * actor.height;
			else
				Element.WorldAnchorOffset = Vector3.up * 1.5f;

			if (State != TransitioningElementState.Off) return;

			_startShow();
		}

		public void Hide()
		{
			if (State != TransitioningElementState.On) return;
			_startHide();
		}

		public void HideInstant() => _hideInstant();


		// NOTE: This seems weird but we have to do things this way. The State variable needs to be changed as soon as the element is shown or hidden, so the first part of it needs to not be async
		public void _startShow() {
			State = TransitioningElementState.Transition;
			_finishShow();
		}

		public async void _finishShow() {
			await _showSequence();
			State = TransitioningElementState.On;
		}

		public void _startHide() {
			State = TransitioningElementState.Transition;
			_finishHide();
		}

		public async void _finishHide() {
			await _hideSequence();
			State = TransitioningElementState.Off;
		}

		async UniTask _showSequence()
		{
			Element.DoOffset(Vector3.up * -0.3f, Vector3.zero, 0.2f);
			Element.DoScale(Vector3.one * -0.4f, Vector3.one, 0.2f);
			await Element.DoAlphaFadeBase(1, 0.5f).ToUniTask();
		}

		async UniTask _hideSequence()
		{
			Element.DoScale(Vector3.zero, Vector3.up  * -0.3f, 0.2f);
			Element.DoOffset(Vector3.one, Vector3.one * -0.4f, 0.2f);
			await Element.DoAlphaFadeBase(0, 0.2f).ToUniTask();
		}

		void _hideInstant()
		{
			Element.Alpha = 0;
			State         = TransitioningElementState.Off;
		}

	}
}