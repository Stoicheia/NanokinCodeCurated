using System;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityUtilities;
using Util.Addressable;

namespace Overworld.UI {

	public interface ISplashScreen {
		UniTask OnShow();
		UniTask OnHide();
	}

	[LuaUserdata(staticAuto: true)]
	public class SplashScreens : StaticBoy<SplashScreens>, ICoroutineWaitable {

		public enum State { Off, Loading, Appear, Showing, Dissapear }

		public RectTransform UIRoot;
		public HUDElement    Element;
		public Camera        Camera;
		public Image         Backdrop;

		public GameObject	 LoadedSplashscreen;
		public ISplashScreen LoadedSplashscreenComponent;

		private AsyncOperationHandle<GameObject> _loadHandle;

		private Action<ISplashScreen> _onLoaded;
		private Action                _onFinished;

		public State state;

		public static bool IsActive  => Live.state > State.Off;
		public static bool IsShowing => Live.state == State.Showing;

		protected override void OnAwake()
		{
			base.OnAwake();
			state = State.Off;
		}


		// API
		//-----------------------------------------------

		[Button]
		public static void ShowTestScreen()
		{
			ShowPrefabAsync("SplashScreens/Test", () => Debug.Log("FINISHED"));
		}

		public static void ShowPrefab(string address, Action onFinished = null, Action<ISplashScreen> onLoaded = null)
		{
			if (Live.state != State.Off) return;

			Live.state       = State.Loading;
			Live._onFinished = onFinished;
			Live._onLoaded   = onLoaded;
			Live._loadAndShow(address).ForgetWithErrors();
		}

		[Button]
		public static async UniTask<ISplashScreen> ShowPrefabAsync(string address, Action onFinished = null)
		{
			if (Live.state != State.Off) return null;

			Live._onLoaded   = null;
			Live._onFinished = onFinished;

			var screen = await Live._loadPrefab(address);
			Live._showPrefab().ForgetWithErrors();

			return screen;
		}

		async UniTask _loadAndShow(string address)
		{
			await _loadPrefab(address);
			await _showPrefab();
		}

		async UniTask<ISplashScreen> _loadPrefab(string address)
		{
			Reset();
			Element.Alpha = 0;

			state       = State.Loading;
			_loadHandle = await Addressables2.LoadHandleAsync<GameObject>(address);
			if (_loadHandle.Result == null) {
				state = State.Off;
				return null;
			}

			LoadedSplashscreen = _loadHandle.Result.Instantiate();
			LoadedSplashscreen.transform.SetParent(UIRoot, false);

			LoadedSplashscreenComponent = _loadHandle.Result.GetComponent<ISplashScreen>();

			_onLoaded?.Invoke(LoadedSplashscreenComponent);
			_onLoaded = null;

			return LoadedSplashscreenComponent;
		}

		async UniTask _showPrefab()
		{
			if(LoadedSplashscreenComponent != null) {
				await LoadedSplashscreenComponent.OnShow();
			}

			state = State.Appear;
			Camera.gameObject.SetActive(true);
			await Element.DoAlphaFade(1, 0.3f, Ease.InQuad).Tween;
			state = State.Showing;
		}

		[Button]
		public static async UniTask Hide() => await Live._hide();

		async UniTask _hide()
		{
			if (state != State.Showing) return;

			state = State.Dissapear;
			await Element.DoAlphaFade(0, 0.3f, Ease.InQuad).Tween;
			state = State.Off;
			Camera.gameObject.SetActive(false);

			Addressables.Release(_loadHandle);

			if (LoadedSplashscreen != null) {

				if(LoadedSplashscreenComponent != null)
					await LoadedSplashscreenComponent.OnHide();

				Destroy(LoadedSplashscreen);
			}

			Reset();

			_onFinished?.Invoke();
			_onFinished = null;
		}

		public void Reset()
		{
			Element.Alpha  = 0;
			Backdrop.color = Color.clear;
		}

		public static async UniTask FadeBackdrop(Color color, float start, float end, float duration)
		{
			Live.Backdrop.color = color.Alpha(start);
			await Live.Backdrop.DOFade(end, duration).ToUniTask();
		}

		public bool CanContinue(bool justYielded, bool isCatchup) => state == State.Off;
	}
}
