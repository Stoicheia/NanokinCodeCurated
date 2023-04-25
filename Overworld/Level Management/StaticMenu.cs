using System;
using Anjin.Util;
using Anjin.Utils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.Odin.Attributes;

namespace Anjin.Nanokin
{
	public abstract class StaticMenu<TMenu> : StaticBoy<TMenu>, IMenuComponent
		where TMenu : StaticMenu<TMenu>
	{
		[ShowInPlay]
		public static bool menuActive;
		public static Action<TMenu> exitHandler;

		protected SceneActivator activator;

		public override void Awake()
		{
			MenuManager.activeMenuComponents.Add(this);

			if (TryGetComponent(out activator) || gameObject.scene.FindRootComponent(out activator))
			{
				activator.Set(false);
			}

			base.Awake();

			menuActive  = false;
			exitHandler = null;
		}

		protected virtual void Start() { }

		private void OnDestroy()
		{
			MenuManager.activeMenuComponents.Remove(this);
		}

		UniTask IMenuComponent.DisableMenu()        => DisableMenu();
		UniTask IMenuComponent.SetState(bool state) => SetState(state);
		UniTask IMenuComponent.EnableMenu()         => EnableMenu();

		/// <summary>
		/// Enable the menu so it's opened and usable.
		/// Opens with animation if there are any.
		/// </summary>
		[Button]
		public static UniTask EnableMenu(RectTransform replaced = null)
		{
			if (!Exists)
			{
				DebugLogger.LogError("Cannot EnableMenu on a StaticMenu that isn't loaded in.", LogContext.UI, LogPriority.High);
				return UniTask.CompletedTask;
			}

			if (menuActive)
				return UniTask.CompletedTask;

			if (replaced != null)
			{
				replaced.SetActive(false);
			}

			if (Live.activator != null)
				Live.activator.Set(true);

			menuActive = true;
			UniTask menu = Live.enableMenu();

			return menu;
		}

		/// <summary>
		/// Disable the menu so it's closed.
		/// Closes with animation if there are any.
		/// </summary>
		/// <returns></returns>
		[Button]
		public static UniTask DisableMenu(RectTransform replaceWith = null)
		{
			if (!menuActive)
				return UniTask.CompletedTask;

			menuActive = false;
			UniTask task = Live.disableMenu();

			if (Live.activator != null)
				Live.activator.Set(false);
			if (replaceWith != null)
			{
				replaceWith.SetActive(true);
			}

			return task;
		}

		protected static void DoExitControls()
		{
			if (GameInputs.cancel.AbsorbPress() ||
			    GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
				OnExitStatic();
		}

		protected static void OnExitStatic()
		{
			Live.OnExit(); // TODO why is this necessary? disableMenu already provides this functionality
			if (SplicerHub.Lock)
			{
				DebugLogger.LogWarning("Input locked.", LogContext.UI, LogPriority.High);
				return;
			}
			if (exitHandler != null)
			{
				exitHandler?.Invoke(Live);
				exitHandler = null;
			}
			else
			{
				DisableMenu();
			}
		}

		protected virtual void OnExit() { }

		public static async UniTask SetState(bool state)
		{
			if (state)
				await EnableMenu();
			else
				await DisableMenu();
		}

		[Button]
		[ShowInPlay]
		[PropertyOrder(1)]
		protected abstract UniTask enableMenu();

		[Button]
		[ShowInPlay]
		[PropertyOrder(1)]
		protected abstract UniTask disableMenu();
	}
}