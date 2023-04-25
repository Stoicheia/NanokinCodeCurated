using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Anjin.UI
{
	public enum AdvanceMode
	{
		Manual,
		AutoTimed
	}

	[RequireComponent(typeof(HUDElement))]
	[LuaUserdata]
	public class SpeechBubble : HUDBubble
	{

		// Config
		// ----------------------------------------
		[FoldoutGroup("References")] public Transform            AdvanceMarker;
		[FoldoutGroup("References")] public TextDisplaySequencer TextSequencer;

		[FormerlySerializedAs("AutoTurnOffWhenDone")]
		public bool CloseOnDone = false;
		public UnityEvent UE_OnDone;

		// Runtime config
		// ----------------------------------------
		[NonSerialized] public AdvanceMode    advanceMode = AdvanceMode.Manual;
		[NonSerialized] public float          autoAdvanceDelay;
		[NonSerialized] public bool           autoCloseAfterLast;
		[NonSerialized] public List<GameText> lines;

		// Runtime logic
		// ----------------------------------------
		private float _timer;

		public const float AUTO_ADVANCE_HOLD_DURATION = 0.1f;

		public override void Awake()
		{
			base.Awake();

			lines = new List<GameText>();
			if (!TextSequencer)
				TextSequencer = GetComponent<TextDisplaySequencer>();

		}

		private void Start()
		{
			TextSequencer.OnDoneSequencing.AddListener(OnSequenceDone);
			TextSequencer.OnDisplayNext.AddListener(OnDisplayNext);
		}

		public override void Update()
		{
			base.Update();

			AdvanceMarker.gameObject.SetActive(advanceMode == AdvanceMode.Manual);

			if (state != State.On)
				return;

			// We have to tell the sequencer to advance manually, since it doesn't handle that itself.
			switch (advanceMode)
			{
				case AdvanceMode.Manual:
					if (TextSequencer.Sequencing && (GameInputs.confirm.IsPressedOrHeld() || GameInputs.cancel.IsPressedOrHeld()))
					{
						TextSequencer.Advance();
					}
					break;

				case AdvanceMode.AutoTimed:
					if (TextSequencer.Displayer.DisplayProcessDone)
					{
						_timer -= Time.deltaTime;
						if (_timer <= 0)
						{
							TextSequencer.Advance();
							_timer = autoAdvanceDelay;
						}
					}
					break;
			}

			if (autoCloseAfterLast && TextSequencer.NextAdvanceFinishes)
			{
				TextSequencer.Advance();
			}
		}

		public override void ApplySettings(Table tbl)
		{
			base.ApplySettings(tbl);

			if (tbl == null) return;

			if (TextSequencer.Displayer is TMP_Typewriter tmp)
			{
				if (tbl.TryGet("text_color", out Color col))
				{
					tmp.TMPText.color = col;
				}
			}

			autoCloseAfterLast = tbl.TryGet("auto_close", false);
		}

		public override void ResetSettings()
		{
			base.ResetSettings();

			autoCloseAfterLast = false;

			if (TextSequencer.Displayer is TMP_Typewriter tmp)
			{
				tmp.TMPText.color = Color.white;
			}
		}

		public override void OnEnter()
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
			TextSequencer.StartSequence(lines);
		}

		public void StopSequence()
		{
			TextSequencer.StopSequencing();
		}

		public void OnSequenceDone()
		{
			if (CloseOnDone)
				StartDeactivation(true);
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
	}
}