using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using UnityEngine;

namespace Anjin.UI {

	[RequireComponent(typeof(HUDElement))]
	[LuaUserdata]
	public class DialogueTextHUD : StaticBoy<DialogueTextHUD>
	{
		public enum State { Off, On, Transition }
		public State state = State.Off;

		public HUDElement           Element;
		public Transform            AdvanceMarker;
		public TextDisplaySequencer Sequencer;

		// Runtime config
		// ----------------------------------------
		[NonSerialized] public AdvanceMode    advanceMode = AdvanceMode.Manual;
		[NonSerialized] public float          autoAdvanceDelay;
		[NonSerialized] public bool           autoCloseAfterLast;
		[NonSerialized] public List<GameText> lines;

		// Runtime logic
		// ----------------------------------------
		float                   _timer;
		GameInputs.ActionButton _advanceButton;

		protected override void OnAwake()
		{
			state = State.Off;
			lines = new List<GameText>();
			if (!Sequencer)
				Sequencer = GetComponent<TextDisplaySequencer>();

			_advanceButton = GameInputs.confirm;
		}

		void Start()
		{
			Element.SetChildrenActive(true);
			Element.Alpha = 0;
			Sequencer.OnDoneSequencing.AddListener(OnSequenceDone);
		}

		public void Update()
		{
			AdvanceMarker.gameObject.SetActive(advanceMode == AdvanceMode.Manual);

			if (state != State.On)
				return;

			// We have to tell the sequencer to advance manually, since it doesn't handle that itself.
			switch (advanceMode)
			{
				case AdvanceMode.Manual:
					if (Sequencer.Sequencing && _advanceButton.IsPressedOrHeld())
					{
						Sequencer.Advance();
					}
					break;

				case AdvanceMode.AutoTimed:
					if (Sequencer.Displayer.DisplayProcessDone)
					{
						_timer -= Time.deltaTime;
						if (_timer <= 0)
						{
							Sequencer.Advance();
							_timer = autoAdvanceDelay;
						}
					}
					break;
			}

			if (autoCloseAfterLast && Sequencer.NextAdvanceFinishes)
				Sequencer.Advance();



			/*switch (state)
			{
				case State.Off:

					/*if (Input.GetKeyDown(KeyCode.I))
					{
						//AnjinTextLine line = new AnjinTextLine("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
						List<GameText> lines = new List<GameText>();
						lines.Add(new GameText("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."));
						lines.Add(new GameText("Hello, this is a test line."));

						ShowDialogueLines(lines);
					}#1#

					break;

				case State.Next:
					lineIndex++;
					if (!IsEndOfList)
					{
						TMPText.text                 = TextLines[index].GetString();
						TMPText.maxVisibleCharacters = 0;
						characterCount               = 0;
						state                        = State.Typing;
						AdvanceMarker.gameObject.SetActive(false);
					} else
					{
						state = State.End;
					}

					break;

				case State.Typing:
					bool fast = GameInputs.confirm.IsPressed;

					characterCount += (typeSpeed * (fast ? typeFastForwardMultiplier : 1)) * Time.deltaTime;

					TMPText.maxVisibleCharacters = characterCount.Floor();

					if (characterCount >= TMPText.text.Length)
					{
						TMPText.maxVisibleCharacters = TMPText.textInfo.characterCount;
						state                        = State.Waiting;
						AdvanceMarker.gameObject.SetActive(true);
					}

					break;

				case State.Waiting:

					if (GameInputs.confirm.IsPressed)
					{
						state = State.Next;
					}

					break;

				case State.End:
					TMPText.text = "";

					AdvanceMarker.gameObject.SetActive(false);
					//tChildrenActive(false);
					state                               = State.Off;
					EffectsController.Live.ShowVignette = false;

					break;
			}*/
		}

		public void Show(List<GameText> _lines)
		{
			if (state != State.Off) return;

			advanceMode = AdvanceMode.AutoTimed;

			state = State.On;
			Element.DoAlphaFade(0, 1, 0.5f);

			lines = _lines;
			Sequencer.StartSequence(lines);
		}


		public void Hide()
		{
			if (state != State.On) return;
			state = State.Off;

			Sequencer.StopSequencing();
			Element.DoAlphaFade(0, 1, 0.5f);
		}

		public void OnSequenceDone() => Hide();


		// =========================================================




		/*public override void OnEnter()
		{
			StartSequence();
		}

		public override void OnDone()
		{
			StopSequence();
			DeactivateFinish();
			UE_OnDone.Invoke();
			UE_OnDone.RemoveAllListeners();
		}

		public async void Show(float seconds)
		{
			advanceMode      = AdvanceMode.AutoTimed;
			autoAdvanceDelay = seconds;
			_timer           = seconds;
			StartActivation();
		}

		public void ShowManual()
		{
			advanceMode = AdvanceMode.Manual;
			StartActivation();
		}

		public void StartSequence()
		{
			Sequencer.StartSequence(lines);
		}

		public void StopSequence()
		{
			Sequencer.StopSequencing();
		}


		public void OnDisplayNext()
		{
			// Update size of text box
		}

		public void SetLine(GameText line)
		{
			lines.Clear();
			lines.Add(line);
		}

		public void SetLines(List<GameText> list)
		{
			lines.Clear();
			lines.AddRange(list);
		}

		public void SetLines(GameText[] list)
		{
			lines.Clear();
			lines.AddRange(list);
		}


		private void StartDialgoue()
		{
			EffectsController.Live.ShowVignette = true;

			lineIndex      = -1;
			characterCount = 0;
			state          = State.Next;

			//AdvanceDialogueIndicator.gameObject.SetActive(true);
			//SetChildrenActive(true);
		}

		public void ShowDialogueLine(GameText line, Handler _OnFinished = null)
		{
			if (state == State.Off)
			{
				TextLines = new List<GameText>();
				TextLines.Add(line);
				IsLocalizedText = true;

				StartDialgoue();
			}
		}

		public void ShowDialogueLines(List<GameText> lines, Handler _OnFinished = null)
		{
			if (state == State.Off)
			{
				TextLines       = lines;
				IsLocalizedText = true;

				StartDialgoue();
			}
		}

		public void ShowText(string line, Handler _OnFinished = null)
		{
			if (state == State.Off)
			{
				StringLines = new List<string>();
				StringLines.Add(line);
				IsLocalizedText = false;

				StartDialgoue();
			}
		}

		public void ShowText(List<String> lines, Handler _OnFinished = null)
		{
			if (state == State.Off)
			{
				StringLines     = lines;
				IsLocalizedText = false;

				StartDialgoue();
			}
		}*/
	}

}