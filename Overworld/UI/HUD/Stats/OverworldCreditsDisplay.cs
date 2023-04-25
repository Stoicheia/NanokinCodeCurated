using System;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Anjin.UI {
	public class OverworldCreditsDisplay : SerializedMonoBehaviour {
		//public enum State { Off, On }

		public HUDElement Element;
		public TMP_Text   Text;

		//[NonSerialized, ShowInPlay] public State state;

		public Vector3 InactiveOffset = new Vector3(0, -30, 0);

		public Easer ShowMoveEase;
		public Easer ShowAlphaEase;

		public Easer HideMoveEase;
		public Easer HideAlphaEase;

		private SaveData _currentData;
		private float    _currentVal;

		[NonSerialized, ShowInPlay]
		public AsyncTransitioner Transitioner;
		public TransitionStates State => Transitioner.State;

		private void Awake()
		{

			Transitioner = new AsyncTransitioner(
				() => {
					UpdateData();
					Element.Alpha = 1;
				},

				() => { Element.Alpha = 0; },

				async cts => {
					UpdateData();
					Element.DoOffset(InactiveOffset, Vector3.zero, ShowMoveEase);
					await Element.DoAlpha(0, 1, ShowAlphaEase).Token(cts);
				},

				async cts => {
					Element.DoOffset(Vector3.zero, InactiveOffset, HideMoveEase);
					await Element.DoAlpha(1, 0, HideAlphaEase).Token(cts);
				}
			);
		}

		private void Start()
		{
			Element.Alpha = 0;
			_currentVal   = 0;
		}


		private void Update()
		{
			if (State == TransitionStates.On) {
				if(_currentData != null) {
					_currentVal = MathUtil.LerpDamp(_currentVal, _currentData.Money, 4);
					if (Mathf.Abs(_currentVal - _currentData.Money) < 1f)
						_currentVal = _currentData.Money;
				} else {
					_currentVal = 0;
				}

				UpdateText();
			}
		}

		private void UpdateData()
		{
			_currentData = SaveManager.current;
			_currentVal  = _currentData?.Money ?? 0;

			UpdateText();
		}

		private void UpdateText()
		{
			Text.text = Mathf.RoundToInt(_currentVal).ToString();
		}

		/*[Button]
		public async UniTask ShowFromCurrentSaveFile()
			=> await Show(SaveManager.current);

		public async UniTask Show(SaveData data, bool anim = true)
		{
			if (data == null) return;

			if (state == State.On) {
				await Hide(false);
			}

			_currentData = data;

			state = State.On;
			timed = false;
			timer = 0;

			_currentVal = _currentData.Money;
			Text.text   = _currentVal.ToString();

			if (anim) {
				Element.DoOffset(new Vector3(5, 10, 0), Vector3.zero, 0.15f);
				await Element.DoAlphaFade(0, 1, 0.15f).Tween.ToUniTask();
			} else {
				Element.Alpha = 1;
			}
		}

		public void HideAfter(float seconds)
		{
			timed = true;
			timer = seconds;
		}

		[Button]
		public async UniTask Hide(bool anim = true)
		{
			if (state != State.On) return;

			state = State.Off;
			timed = false;
			timer = 0;

			if (anim) {
				Element.DoOffset(Vector3.zero, new Vector3(5, 10, 0), 0.15f);
				await Element.DoAlphaFade(1, 0, 0.15f).Tween.ToUniTask();
			} else {
				Element.Alpha = 0;
			}
		}*/
	}
}