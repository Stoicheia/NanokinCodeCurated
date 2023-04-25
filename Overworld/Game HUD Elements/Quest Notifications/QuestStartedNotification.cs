using System;
using Anjin.Nanokin;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.UI {
	public class QuestStartedNotification : MonoBehaviour, ICoroutineWaitable {

		public enum State {Off, Transition, On}

		public TMP_Text   TMP_QuestName;

		[ShowInPlay, NonSerialized] public State      state;
		[ShowInPlay, NonSerialized] public HUDElement Element;

		public float Duration = 3.5f;
		public float _timer;


		private void Awake()
		{
			Element       = GetComponent<HUDElement>();

			Element.Alpha = 0;
			state         = State.Off;
		}

		private void Update()
		{
			if (state == State.On) {
				_timer -= Time.deltaTime;
				if (_timer <= 0)
					Hide();
			}
		}

		public async void Show(Quests.LoadedQuest quest)
		{
			if (state != State.Off) return;
			state = State.Transition;

			TMP_QuestName.text = quest.GetName();
			_timer             = Duration;

			Element.DoAlphaFade(0, 1, 0.3f, Ease.OutQuad);
			Element.DoOffset(new Vector3(0, -30, 0), Vector3.zero, 0.6f, Ease.OutQuad);
			await UniTask.Delay(TimeSpan.FromSeconds(0.6f));
			state = State.On;
		}

		public async void Hide()
		{
			if (state != State.On) return;
			state = State.Transition;

			Element.DoAlphaFade(1, 0, 0.6f);
			Element.DoOffset(Vector3.zero, new Vector3(0, -30, 0), 0.6f);
			await UniTask.Delay(TimeSpan.FromSeconds(0.6f));
			state = State.Off;
		}

		public bool CanContinue(bool justYielded, bool isCatchup) => state == State.Off;
	}
}