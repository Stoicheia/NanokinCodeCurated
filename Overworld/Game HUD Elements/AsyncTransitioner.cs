using System;
using System.Threading;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Anjin.UI {

	public enum TransitionStates {
		Off,
		On,
		Showing,
		Hiding,
	}

	public class AsyncTransitioner {

		CancellationTokenSource cancel;


		public Action OnShow;
		public Action OnHide;

		Func<CancellationToken, UniTask> OnShowAnimated;
		Func<CancellationToken, UniTask> OnHideAnimated;

		public TransitionStates State;

		public AsyncTransitioner(Action onShow, Action onHide, Func<CancellationToken, UniTask> onShowAnimated = null, Func<CancellationToken,UniTask> onHideAnimated = null, TransitionStates initialState = TransitionStates.Off)
		{
			State = initialState;

			OnShow = onShow;
			OnHide = onHide;

			OnShowAnimated = onShowAnimated;
			OnHideAnimated = onHideAnimated;
		}

		[Button]
		public void Show(bool anim = true)
		{
			InsureCompleted();

			if (State != TransitionStates.Off) return;
			State = TransitionStates.Showing;
			if(anim) {
				cancel = new CancellationTokenSource();
				_show(cancel.Token).ForgetWithErrors();
			} else {
				if (OnShow != null) OnShow();
				State = TransitionStates.On;
			}
		}

		[Button]
		public async UniTask ShowAsync()
		{
			InsureCompleted();

			if (State != TransitionStates.Off) return;
			State = TransitionStates.Showing;

			cancel = new CancellationTokenSource();
			await _show(cancel.Token);
		}

		private async UniTask _show(CancellationToken cts)
		{
			if(OnShowAnimated != null)
				await OnShowAnimated(cts);
			else if (OnShow != null)
				OnShow();

			State = TransitionStates.On;
			cancel.Dispose();
		}

		[Button]
		public void Hide(bool anim = true)
		{
			InsureCompleted();

			if (State != TransitionStates.On) return;
			State = TransitionStates.Hiding;
			if(anim) {
				cancel = new CancellationTokenSource();
				_hide(cancel.Token).ForgetWithErrors();
			}else {
				if (OnHide != null) OnHide();
				State = TransitionStates.Off;
			}
		}

		[Button]
		public async UniTask HideAsync()
		{
			InsureCompleted();

			if (State != TransitionStates.On) return;
			State = TransitionStates.Hiding;

			cancel = new CancellationTokenSource();
			await _hide(cancel.Token);

		}

		[Button]
		public void InsureCompleted()
		{
			if (State == TransitionStates.On || State == TransitionStates.Off) return;

			if (cancel != null) {
				cancel.Cancel(false);
				cancel.Dispose();
			}

			if (State == TransitionStates.Hiding)  State = TransitionStates.Off;
			if (State == TransitionStates.Showing) State = TransitionStates.On;
		}

		private async UniTask _hide(CancellationToken cts)
		{
			if(OnHideAnimated != null)
				await OnHideAnimated(cts);
			else if (OnHide != null)
				OnHide();

			State = TransitionStates.Off;
			cancel.Dispose();
		}
	}
}