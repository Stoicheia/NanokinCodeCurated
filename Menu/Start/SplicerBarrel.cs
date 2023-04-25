using System;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using Util.Extensions;
using Util.RenderingElements.Barrel;

namespace Menu.Start
{
	public class SplicerBarrel : StaticMenu<SplicerBarrel>
	{
		[SerializeField] public StartBarrel  Barrel;
		public                  Camera       RaycastCamera;
		[SerializeField] public GameObject[] VisibilityEnables;
		[Space]
		[SerializeField] private Animation Animator;
		[SerializeField] private AnimationClip             EnterClip;
		[SerializeField] private AnimationClip             ExitClip;
		[SerializeField] private BarrelAnimationProperties AnimProperties;

		[NonSerialized]
		public static MenuInteractivity interactivity;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void Init()
		{
			menuActive  = false;
			exitHandler = null;
		}

		protected override void OnAwake()
		{
			// Start with the UI invisible.
			interactivity = MenuInteractivity.None;

			AnimProperties.onUpdateMenuVisibility = SetVisibility;
			AnimProperties.onUpdateInteractivity  = SetInputLevel;

			//GameInputs.DeviceChanged += OnGameInputsOnDeviceChanged;

			Animator.SetToEnd(ExitClip);

			SetVisibility(false);
		}

		/*private void OnGameInputsOnDeviceChanged(InputDevices device)
		{
			if (Barrel == null) return;

			switch (device) {
				case InputDevices.Gamepad:
					Barrel.ChangeMode(PanelMenu.Mode.Barrel);
					break;

				case InputDevices.KeyboardAndMouse:

					Barrel.ChangeMode(PanelMenu.Mode.Flat);
					break;
			}
		}*/

		/*private void OnDestroy()
		{
			GameInputs.DeviceChanged -= OnGameInputsOnDeviceChanged;
		}*/

		public static void SetVisibility(bool enabled)
		{
			interactivity = MenuInteractivity.None;

			foreach (GameObject obj in Live.VisibilityEnables)
			{
				obj.SetActive(enabled);
			}
		}

		public void SetInputLevel(MenuInteractivity level)
		{
			interactivity = level;
		}

		private void Update()
		{
			// sync interactivity of barrel to this menu
			Barrel.interactivity = interactivity;

			// Change modes based on inputs
			// ----------------------------------------
			//if (Barrel.mode == PanelMenu.Mode.Flat)
			//{
			//	bool anyPressed = Keyboard.current.anyKey.wasPressedThisFrame;
			//	if (Gamepad.current != null)
			//	{
			//		for (int i = 0; i < Gamepad.current.allControls.Count; i++)
			//		{
			//			if (Gamepad.current.allControls[i] is ButtonControl button)
			//			{
			//				if (button.wasPressedThisFrame && !button.synthetic)
			//				{
			//					anyPressed = true;
			//					break;
			//				}
			//			}
			//		}
			//	}

			//	if (anyPressed)
			//	{
			//		Barrel.ChangeMode(PanelMenu.Mode.Barrel);
			//	}
			//}
			//else
			//{
			//	if (Mouse.current.delta.ReadValue().magnitude > Mathf.Epsilon)
			//	{
			//		Barrel.ChangeMode(PanelMenu.Mode.Flat);
			//	}
			//}


			// Exit
			// ----------------------------------------
			if (interactivity >= MenuInteractivity.Navigate)
				if (GameInputs.cancel.IsPressed || GameInputs.splicer.IsPressed)
				{
					exitHandler?.Invoke(this);
				}
		}

		protected override async UniTask enableMenu()
		{
			//GameController.Live.State_WorldPause = GameController.WorldPauseState.FullPaused;

			SetVisibility(true);
			await Animator.PlayAsync(EnterClip);
			Barrel.ChangeMode(PanelMenu.Mode.Flat);
		}

		protected override async UniTask disableMenu()
		{
			//GameController.Live.State_WorldPause = GameController.WorldPauseState.Running;

			interactivity = MenuInteractivity.None;

			await Animator.PlayAsync(ExitClip);

			menuActive = false;
		}
	}

	public enum MenuInteractivity
	{
		None,
		Navigate,
		Interact
	}
}