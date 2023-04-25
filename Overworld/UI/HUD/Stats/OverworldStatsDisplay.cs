using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.UI;
using Cysharp.Threading.Tasks;
using SaveFiles;
using Sirenix.OdinInspector;
using UniTween.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Anjin.UI
{
	public class OverworldStatsDisplay : SerializedMonoBehaviour
	{
		//public enum State { Off, On }

		public RectTransform Root;

		public HUDElement Element;

		public float Scale = 0.75f;

		[ShowInPlay]
		private StatusPanel _loadedPrefab;

		[NonSerialized]
		public StatusPanel[] SpawnedHUDS;
		public HUDXPBar[] SpawnedXPBars;

		//[NonSerialized, ShowInPlay] public State state;

		public Vector3 InactiveOffset = new Vector3(0, -30, 0);

		public Easer ShowMoveEase;
		public Easer ShowAlphaEase;

		public Easer HideMoveEase;
		public Easer HideAlphaEase;

		[NonSerialized, ShowInPlay] public bool              Ready;
		[NonSerialized, ShowInPlay] public AsyncTransitioner Transitioner;

		public TransitionStates State => Transitioner.State;

		public bool  timed;
		public float timer;
		private async void Awake()
		{
			Ready = false;
			//state         = State.Off;
			Element.Alpha = 0;
			SpawnedHUDS   = new StatusPanel[6];
			SpawnedXPBars = new HUDXPBar[6];

			_loadedPrefab = (await Addressables.LoadAssetAsync<GameObject>("Combat/UI/Monster Status Overworld")).GetComponent<StatusPanel>();

			for (int i = 0; i < SpawnedHUDS.Length; i++)
			{
				StatusPanel obj = Instantiate(_loadedPrefab, Root);
				obj.transform.localScale = new Vector3(Scale, Scale, 1);
				SpawnedHUDS[i]           = obj;
				SpawnedXPBars[i]         = obj.GetComponentInChildren<HUDXPBar>();

				obj.gameObject.SetActive(false);
			}


			Transitioner = new AsyncTransitioner(
				() => {
					UpdateUI(SaveManager.current.Party).ForgetWithErrors();
					Element.Alpha = 1;
				},

				() => {
					Element.Alpha = 0;

					foreach (StatusPanel ui in SpawnedHUDS) {
						ui.gameObject.SetActive(false);
					}
				},

				async cts => {
					await UpdateUI(SaveManager.current.Party);
					Element.DoOffset(InactiveOffset, Vector3.zero, ShowMoveEase);
					await Element.DoAlpha(0, 1, ShowAlphaEase).Token(cts);
				},

				async cts => {
					Element.DoOffset(Vector3.zero, InactiveOffset, HideMoveEase);
					await Element.DoAlpha(1, 0, HideAlphaEase).Token(cts);

					foreach (StatusPanel ui in SpawnedHUDS) {
						ui.gameObject.SetActive(false);
					}
				}
			);

			Ready = true;

		}

		private void Update()
		{
			/*if (state == State.On && timed)
			{
				timer -= Time.deltaTime;
				if (timer <= 0)
				{
					timed = false;
					Hide();
				}
			}*/
		}


		// STATBARS
		//=================================================

		public async UniTask UpdateUI(List<CharacterEntry> Characters)
		{
			UniTaskBatch batch = UniTask2.Batch();
			for (int i = 0; i < Characters.Count && i < SpawnedHUDS.Length; i++) {
				SpawnedHUDS[i].SetToCharacterEntry(Characters[i]).Batch(batch);
				SpawnedXPBars[i].SetCharacter(Characters[i]).Batch(batch);
			}

			await batch;

			for (int i = 0; i < Characters.Count && i < SpawnedHUDS.Length; i++) {
				SpawnedHUDS[i].gameObject.SetActive(true);
			}
		}

		/*[Button]
		public async UniTask ShowFromCurrentSaveFile()
			=> await Show(SaveManager.current.Party);*/




		/*public async UniTask Show(bool anim = true)
		{
			/*if (state == State.On)
			{
				await Hide(false);
			}

			state = State.On;
			timed = false;
			timer = 0;

			Element.Alpha = 0;

			await UpdateUI(SaveManager.current.Party);

			for (int i = 0; i < Characters.Count && i < SpawnedHUDS.Length; i++)
			{
				var hud = SpawnedHUDS[i];
				await hud.SetToCharacterEntry(Characters[i]);
				SpawnedXPBars[i].SetCharacter(Characters[i]);
				hud.gameObject.SetActive(true);
			}

			if (anim) {
				Element.DoOffset(new Vector3(0, -30, 0), Vector3.zero, 0.25f);
				await Element.DoAlphaFade(0, 1, 0.25f).Tween.ToUniTask();
			} else {
				Element.Alpha = 1;
			}#1#
		}

		public void HideAfter(float seconds)
		{
			/*timed = true;
			timer = seconds;#1#
		}

		//[Button]
		public async UniTask Hide(bool anim = true)
		{
			/*if (state != State.On) return;
			state = State.Off;
			_hideInternal(anim).ForgetWithErrors();#1#
		}

		public async UniTask HideAsync(bool anim)
		{
			/*if (state != State.On) return;
			state = State.Off;
			await _hideInternal(anim);#1#
		}

		private async UniTask _hideInternal(bool anim)
		{
			/*timed = false;
			timer = 0;

			if (anim) {
				Element.DoOffset(Vector3.zero, new Vector3(0, -30, 0), 0.25f);
				await Element.DoAlphaFade(1, 0, 0.25f).Tween.ToUniTask();
			} else {
				Element.Alpha = 0;
			}

			foreach (StatusPanel ui in SpawnedHUDS) {
				ui.gameObject.SetActive(false);
			}#1#
		}*/
	}
}