using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Data.Overworld;
using JetBrains.Annotations;
using Overworld.Controllers;
using Overworld.Cutscenes.SavePointUnlockCutscene;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.Components;
using Util.Extensions;

namespace Overworld.Park_Game
{
	[LuaUserdata]
	[SelectionBase]
	public class SavePoint : SerializedMonoBehaviour
	{
		public static List<SavePoint> allLoaded = new List<SavePoint>();

		[InfoBox("The ID is used to keep track of this savepoint in the savefile and must be unique.")]
		[SerializeField] public LevelID LevelID;
		[SerializeField] public string	ID;


		[SerializeField] private AnimationClip ANIM_Unlocked;
		[SerializeField] public  SpawnPoint    SpawnPoint;
		[SerializeField] public  TriggerEvents Trigger;
		[SerializeField] public  AudioDef      SFX_Use;

		public Transform UnlockFocusPoint;

		[NonSerialized, ShowInInspector]
		public VCamTarget UnlockFocusPointTarget;

		public Interactable Interactable;

		public bool Debug_AlwaysPlayUnlock = false;
		public bool Debug_NoCutscenes      = false;

		[NonSerialized, ShowInInspector]
		public bool unlocked;

		private Animation _animator;

		private bool saveInProgress;

		private void Awake()
		{
			_animator = GetComponent<Animation>();

			saveInProgress = false;

			if (SpawnPoint)
				SpawnPoint.name = $"{name} (core id: {ID})";
		}

		private void OnEnable()
		{
			allLoaded.Add(this);
		}

		private void OnDisable()
		{
			allLoaded.Remove(this);
		}

		private void Start()
		{
			Startup().ForgetWithErrors();
		}

		private async UniTask Startup()
		{
			await GameController.TillIntialized();

			SaveData save = await SaveManager.GetCurrentAsync();
			if (save.DiscoveredSavepoints.Contains(GetID()))
			{
				Unlock(false);
				/*_animator.SetToEnd(ANIM_Unlocked);
				Unlocked = true;*/
			}

			Interactable interactable = GetComponentInChildren<Interactable>();
			interactable.OnInteract.AddListener(OnInteract);

			Trigger.onTriggerEnter += OnTriggerEnter;
			Trigger.onTriggerExit  += OnTriggerExit;

			UnlockFocusPointTarget = GameCams.NewTarget(UnlockFocusPoint);
		}

		public string GetID()
		{
			if (LevelID != LevelID.None)
				return $"{Convert.ToInt32(LevelID)}_{ID}";
			else
				return ID;
		}

		[Button]
		public async void OnInteract()
		{
			//Interactable.enabled = false;

			if (saveInProgress)
			{
				return;
			}

			bool was_unlock = false;
			if (!SaveManager.current.DiscoveredSavepoints.Contains(GetID()))	{
				SaveManager.current.DiscoveredSavepoints.Add(GetID());
				was_unlock = true;
			}

			DoSave();

			if (Debug_NoCutscenes) {
				Use();
			} else if (was_unlock || !unlocked || Debug_AlwaysPlayUnlock) {

				Unlock(true, Debug_AlwaysPlayUnlock);
			} else {
				SavePointUnlockCutscene.StartCutscene(this, false);
			}
		}

		[Button]
		public void Lock()
		{
			if (!unlocked) return;
			unlocked = false;

			_animator.clip = ANIM_Unlocked;
			_animator.Play();

			foreach (AnimationState state in _animator)
			{
				state.normalizedTime = 0;
			}

			_animator.Sample();
			_animator.Stop();
		}

		[Button]
		public void Unlock(bool anim = true, bool force = false)
		{
			if (unlocked && !force) return;
			unlocked = true;

			if (anim)
			{
				SavePointUnlockCutscene.StartCutscene(this, true);
			}
			else
			{
				_animator.SetToEnd(ANIM_Unlocked);
			}
		}

		[Button]
		public void UnlockAnimation()
		{
			_animator.PlayClip(ANIM_Unlocked);
		}

		public void DoSave()
		{
			DebugLogger.Log($"Save crystal ({ID}) used!");

			if(SaveManager.HasData) {

				SaveData save = SaveManager.current;
				save.HealParty();
				save.RefillStickers();

				save.Origin = SaveData.SaveOrigin.Savepoint;

				save.Location_LastSavePoint = new SaveData.PlayerLocation {
					Level       = LevelID != LevelID.None ? LevelID : GameController.ActiveLevel.Manifest.Level,
					MostRecentSavePointID = GetID(),
				};

				SaveManager.SaveCurrent(SaveData.SaveOrigin.Savepoint);
			}
		}

		public void Recharge()
		{
			WaitToAllowNextInteract().Forget();
		}

		private async UniTask WaitToAllowNextInteract()
		{
			await UniTask.Delay(1000);

			saveInProgress = false;
		}

		public void SetSaveStatus(bool saving)
		{
			saveInProgress = saving;
		}

		[Button]
		public void Use()
		{
			BattleController.RespawnEncounters();
			GameSFX.Play(SFX_Use, transform);
		}

		/*[Button]
		public void VisualReset()
		{
			_animator.Play(ANIM_Unlocked);
		}

		[Button]
		public void VisualUnlock()
		{
		}*/

		private void OnTriggerEnter(Collider other)
		{
			/*if (other.HasComponent(out PlayerActor player))
			{
				Unlock();

				PlayerControlBrain brain = player.GetActiveBrain() as PlayerControlBrain;
				brain.nearbySavePoint = this;
			}*/
		}

		private void OnTriggerExit(Collider other)
		{
			/*if (other.HasComponent(out PlayerControlBrain player))
			{
				player.nearbySavePoint = null;
			}*/
		}

		public static SavePoint FindByID(string id)
		{
			return allLoaded.FirstOrDefault(sp => sp.ID == id);
		}
	}
}