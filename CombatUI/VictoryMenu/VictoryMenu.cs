using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Util;
using Combat.Entry;
using Data.Nanokin;
using JetBrains.Annotations;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityUtilities;
using Util;
using Util.Addressable;
using Util.UniTween.Value;

namespace Combat.Components.VictoryScreen.Menu
{
	public class VictoryMenu : StaticBoy<VictoryMenu>
	{
		[NonSerialized] public int totalCredits;

		[Title("UI References")]
		[SerializeField] private TextMeshProUGUI Label_TotalXP;

		[SerializeField]                 private TextMeshProUGUI Label_TotalCredits;
		[SerializeField]                 private RectTransform   Root_WinnerColumns;
		[SerializeField]                 private RectTransform   Root_Notifications;
		[SerializeField]                 private WinnerColumnUI  ColumnPrefab;
		[SerializeField]                 private Vector2         ColumnPivot;
		[SerializeField]                 private Vector2         ScreenPivot;
		[SerializeField]                 private VColumn         GainsColumn;
		[SerializeField]                 private Image           BackDimmer;
		[SerializeField, UsedImplicitly] private FloatTween      DimIn;

		[Title("UI Configuration")]
		[SerializeField] private float ColumnSpacing;

		[SerializeField] private float   SlantAnimationDistance;
		[SerializeField] private Vector2 ColumnSlantVector;
		[SerializeField] private float   BaseSlant;
		[SerializeField] private EaserTo IntroEase;

		[Title("Behavior")]
		[SerializeField] private ManualTimer GainTicker;

		[SerializeField] private ManualTimer ExitDelay;

		[Title("Gain Distribution")]
		[SerializeField] private float xpGainSfxPitchChange;

		[SerializeField] private float    xpGainSfxPitchMax;
		[SerializeField] private AudioDef SFX_XPGainTick;

		private States         _state = States.Inactive;
		private TweenableFloat _introProgress;
		private TweenableFloat _screenDimming;

		// Gain Distribution
		// ----------------------------------------
		private int   _totalXPLeft;
		private int   _totalLeadingZeroes;
		private float _tickSfxPitch; // The pitch of the ticking sound effect whenever a XP point is gained. Rises as more XP is gained, creating a satisfying effect.
		private int   _barsTweeningIn;

		private List<VColumn>         _columns;
		private List<NanokinInstance> _nanokins;
		private AsyncHandles          _handles;

		public int TotalXP
		{
			get => _totalXPLeft;
			set
			{
				_totalXPLeft        = value;
				_tickSfxPitch       = 1;
				_totalLeadingZeroes = Mathf.FloorToInt(Mathf.Log10(TotalXP)) + 1;
			}
		}

		public int TotalRP
		{
			get => _totalXPLeft;
			set
			{
				_totalXPLeft        = value;
				_tickSfxPitch       = 1;
				_totalLeadingZeroes = Mathf.FloorToInt(Mathf.Log10(TotalXP)) + 1;
			}
		}


		public event Action Ending;

		public override void Awake()
		{
			base.Awake();

			_handles       = new AsyncHandles();
			_nanokins      = new List<NanokinInstance>();
			_columns       = new List<VColumn>();
			_screenDimming = new TweenableFloat();
			_introProgress = new TweenableFloat();
		}

		public void ChangeState(States state)
		{
			Debug.Log($"Next Victory State: {state}");

			switch (state)
			{
				case States.Intro:
					_introProgress.FromTo(0, 1, IntroEase);

					_screenDimming.value = 0;
					_screenDimming.To(DimIn);

					BackDimmer.color = Color.clear;
					BackDimmer.gameObject.SetActive(true);

					2f.Wait(() =>
					{
						ChangeState(States.GainDistribution);
					});

					UpdateTotalLabels();
					break;

				case States.Delay1:
					break;

				case States.GainDistribution:
					GainTicker.Restart();
					break;

				case States.Delay2:
					ExitDelay.Restart();
					break;

				case States.Exit:
					Ending?.Invoke();
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}

			_state = state;
		}

		private void StepXPDistribution()
		{
			if (TotalXP <= 0) return; // No XP left to distribute!

			foreach (VColumn col in _columns)
			{
				col.StepGains(ref _totalXPLeft);
			}

			// Play SFX
			_tickSfxPitch += xpGainSfxPitchChange;
			_tickSfxPitch =  _tickSfxPitch.Maximum(xpGainSfxPitchMax);

			GameSFX.PlayGlobal(SFX_XPGainTick, transform, _tickSfxPitch, -1);

			// Update Labels
			Label_TotalXP.text = _totalXPLeft.ToString().PadLeft(_totalLeadingZeroes, '0');

			// if (IsIdle)
			// BecameIdle?.Invoke();
			UpdateTotalLabels();
		}

		private void Update()
		{
			switch (_state)
			{
				case States.Intro:
					break;

				case States.Delay1:
					break;

				case States.GainDistribution:
					if (GameInputs.confirm.IsDown)
					{
						while (TotalXP > 0)
						{
							StepXPDistribution();
						}

						ChangeState(States.Delay2);
					}
					else
					{
						if (GainTicker.Update())
						{
							GainTicker.Restart();
							StepXPDistribution();

							if (TotalXP <= 0)
							{
								ChangeState(States.Delay2);
							}
						}
					}

					break;

				case States.Delay2:
					if (ExitDelay.Update())
						ChangeState(States.Exit);

					break;

				case States.Exit:
					break;
			}
		}

		private void LateUpdate()
		{
			UpdateTotalLabels();
			RefreshColumnPositions();

			BackDimmer.color = Color.black.Alpha(_screenDimming.value);
		}

		public void AddNanokin(CharacterAsset coach, [NotNull] CharacterEntry nanokin)
		{
			_nanokins.Add(nanokin.nanokin);

			WinnerColumnUI ui = Instantiate(ColumnPrefab, Root_WinnerColumns, false);
			ui.Show(coach, nanokin.nanokin);

			_columns.Add(ui);
			RefreshColumnPositions();
		}

		public void AddResults()
		{
			_columns.Add(GainsColumn);
			GainsColumn.gameObject.SetActive(true);

			RefreshColumnPositions();
		}

		[Button]
		private void RefreshColumnPositions()
		{
			float combinedWidth = 0;

			for (var i = 0; i < _columns.Count; i++)
			{
				VColumn col    = _columns[i];
				Vector2 offset = Vector3.right * combinedWidth;

				Vector2 slantVector = ColumnSlantVector;
				if (i % 2 == 1)
					slantVector = -slantVector;

				col.Rect.anchoredPosition = offset + slantVector * (SlantAnimationDistance * (1 - _introProgress.value) + BaseSlant);

				combinedWidth += col.Rect.sizeDelta.x * col.Rect.localScale.x;
				combinedWidth += ColumnSpacing;

				// Self-pivot
				// offset -= col.Rect.sizeDelta * ColumnPivot;
				// offset += Vector2.Scale(ScreenPivot, Root_WinnerColumns.rect.size);
			}

			for (var i = 0; i < _columns.Count; i++)
			{
				VColumn col = _columns[i];
				Vector2 pos = col.Rect.anchoredPosition;

				pos.x -= combinedWidth / 2f;

				col.Rect.anchoredPosition = pos;
			}
		}

		[Button]
		private void UpdateTotalLabels()
		{
			Label_TotalXP.text      = TotalXP.ToString();
			Label_TotalCredits.text = totalCredits.ToString();
		}

		public void Clear()
		{
			for (var i = 0; i < Root_WinnerColumns.childCount; i++)
			{
				GameObject obj = Root_WinnerColumns.GetChild(i).gameObject;
				Destroy(obj);
			}

			_columns.Clear();
		}

		public enum States
		{
			Inactive,
			Intro,
			Delay1,
			GainDistribution,
			Delay2,
			Exit
		}
	}
}