using Anjin.Actors;
using Anjin.Util;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Util;
using Util.UniTween.Value;

namespace Anjin.Nanokin.Map
{
	public class CheckpointMinigame : SerializedMonoBehaviour
	{
		[SerializeField] private Interactable StartInteractable;
		[Space]
		[SerializeField] private GameObject UI_Root;
		[SerializeField] private TextMeshProUGUI UI_TimerLabel;
		[SerializeField] private TextMeshProUGUI UI_RecordLabel;
		[Space]
		[SerializeField] private AudioDef SFX_StartMinigame;
		[SerializeField] private AudioDef SFX_CheckpointReached;
		[SerializeField] private AudioDef SFX_Lose;
		[SerializeField] private AudioDef SFX_Win;
		[SerializeField] private AudioDef SFX_NewRecord;

		private CheckpointTrigger[] _checkpoints;
		private bool                _inProgress;
		private int                 _index;
		private ManualTimer         _checkpointTimer;
		private float               _totalTimer;
		private float               _recordTime = float.MaxValue;

		private void Awake()
		{
			_checkpointTimer = new ManualTimer();
		}

		private void Start()
		{
			_checkpoints = GetComponentsInChildren<CheckpointTrigger>();

			StartInteractable.OnInteract.AddListener(StartMinigame);
			UI_Root.SetActive(false);
			UI_RecordLabel.text = "n/a";

			foreach (CheckpointTrigger trigger in _checkpoints)
			{
				trigger.TriggerEntered += obj =>
				{
					OnTriggerEntered(trigger, obj);
				};

				trigger.gameObject.SetActive(false);
			}
		}

		private void StartMinigame()
		{
			// Start the game.
			_inProgress = true;
			_index      = 0;
			_totalTimer = 0;

			UI_Root.SetActive(true);
			GameSFX.PlayGlobal(SFX_StartMinigame, transform.position);

			StartInteractable.locks++;
			ActivateCheckpoint();

			RefreshActiveCheckpoints();
		}

		private void RefreshActiveCheckpoints()
		{
			for (int i = 0; i < _checkpoints.Length; i++)
			{
				CheckpointTrigger trigger = _checkpoints[i];
				trigger.gameObject.SetActive(_inProgress && (i == _index || i == _index + 1));
			}

			_checkpoints[(_index + 1).Clamp(0, _checkpoints.Length - 1)].Deactivated.Invoke();
			_checkpoints[_index.Clamp(0, _checkpoints.Length - 1)].Activated.Invoke();
		}

		private void OnTriggerEntered(CheckpointTrigger trigger, Collider collider)
		{
			if (!_inProgress) return;

			if (!collider.HasComponent<PlayerActor>()) // this is meh
				return;

			if (trigger == _checkpoints[_index])
			{
				// Checkpoint reached!
				_checkpoints[_index].Deactivated.Invoke();
				_index++;

				if (_index >= _checkpoints.Length)
				{
					_checkpointTimer.Stop();
					OnWin();
				}
				else
				{
					GameSFX.PlayGlobal(SFX_CheckpointReached, transform.position);
					ActivateCheckpoint();
				}

				RefreshActiveCheckpoints();
			}
		}

		private void ActivateCheckpoint()
		{
			_checkpointTimer.Duration = _checkpoints[_index].timeToReach;
			_checkpointTimer.Restart();

			_checkpoints[_index].Activated.Invoke();
		}

		private void Update()
		{
			if (!_inProgress) return;

			_checkpointTimer.Update();
			if (_checkpointTimer.IsDone)
			{
				// Ran out of time!
				OnLose();
				RefreshActiveCheckpoints();
			}
			else
			{
				_totalTimer += Time.deltaTime;
			}

			UI_TimerLabel.text = _totalTimer.ToString("F2");
		}

		private void OnWin()
		{
			_inProgress = false;

			GameSFX.PlayGlobal(SFX_Win, transform.position);

			if (_totalTimer < _recordTime)
			{
				// A new record!
				_recordTime         = _totalTimer;
				UI_RecordLabel.text = _recordTime.ToString("F2");

				GameSFX.PlayGlobal(SFX_NewRecord, transform.position);
			}

			ActorController.playerBrain.enabled = true;
			StartInteractable.locks--;
		}

		private void OnLose()
		{
			_inProgress = false;

			GameSFX.PlayGlobal(SFX_Lose, transform.position);

			ActorController.playerBrain.enabled = false;

			EaserTo ease = new EaserTo
			{
				ease     = Ease.Linear,
				duration = 0.5f
			};

			GameEffects.screenFadeOpacity.To(1, ease).OnComplete(() =>
			{
				ActorController.playerActor.Teleport(StartInteractable.transform.position);

				GameEffects.screenFadeOpacity.To(0, ease).OnComplete(() =>
				{
					ActorController.playerBrain.enabled = true;
					StartInteractable.locks--;
				});
			});
		}
	}
}