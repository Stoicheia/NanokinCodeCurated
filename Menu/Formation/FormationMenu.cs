using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Core.Flags;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Assets.Nanokins;
using Cinemachine;
using Combat.Data;
using Combat.Entities;
using Combat.UI;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Overworld.UI;
using Pathfinding.Drawing;
using Puppets;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityUtilities;
using Util.Addressable;
using Object = UnityEngine.Object;
using MenuController = Anjin.Nanokin.MenuManager;
using UnityEngine.UI;
using Combat.Toolkit;

namespace Menu.Formation
{
	public class FormationMenu : StaticMenu<FormationMenu>, ICamController
	{
		private const int GRID_W = 3;
		private const int GRID_H = 3;

		[Title("Prefabs")]
		[SerializeField] private GameObject MonsterPrefab;
		[SerializeField] private Reticle ReticlePrefab;

		[Title("References")]
		[SerializeField] private RectTransform Canvas;
		[SerializeField] private Camera Camera;
		[SerializeField]
		[FormerlySerializedAs("MonsterInfo")]
		private PartyMemberColumn UI_MonsterInfo;
		[SerializeField] private List<TMPro.TMP_Text> rowEffectDescriptions;
		[SerializeField] private Color                NormalDescriptionColor;
		[SerializeField] private Color                HighlightedDescriptionColor;
		[SerializeField] private Transform            Root_Monsters;
		[SerializeField] private Transform            Root_Reticles;
		[Space]
		[SerializeField] private CinemachineVirtualCamera VCam_MonsterSelection;
		[SerializeField] private CinemachineVirtualCamera VCam_SlotSelection;
		[SerializeField] private GameObject TutorialScreen;
		public Button BackButton;

		[Title("Config")]
		[SerializeField] private SlotTile[,] Slots = new SlotTile[GRID_W, GRID_H];
		[SerializeField] private LayerMask MonsterMouseRaycast;
		[SerializeField] private LayerMask SlotMouseRaycast;

		[Title("Audio")]
		[SerializeField] private AudioDef SFX_NavigateSlots;
		[SerializeField] private AudioDef SFX_NavigateMonster;
		[SerializeField] private AudioDef SFX_ConfirmMonster;
		[SerializeField] private AudioDef SFX_CancelSlot;
		[SerializeField] private AudioDef SFX_ConfirmSlot;
		[SerializeField] private AudioDef SFX_OpenSubmenu;
		[SerializeField] private AudioDef SFX_ExitSubmenu;

		private States                          _state;
		private List<FormationMonster>          _allMonsters;
		private Dictionary<string, FormationMonster> _monsterLookup;
		private List<Target>                    _allTargets;
		private RaycastTarget<FormationMonster> _raycastMonster;
		private RaycastTarget<SlotTile>         _raycasTile;

		private FormationMonster _selectedMonster;
		private Target           _selectedTarget;

		private Reticle		 _obsoleteReticle;
		private Reticle      _activeReticle;
		private SlotTile     _activeTile;
		private Vector2Int   _startingSlot;
		private AsyncHandles _handles;

		private StateTransition _stateTransition;

		private string _currentSelection;

		protected override void OnAwake()
		{
			base.OnAwake();

			_handles     = new AsyncHandles();
			_allMonsters = new List<FormationMonster>();
			_monsterLookup = new Dictionary<string, FormationMonster>();
			_allTargets  = new List<Target>();

			_currentSelection = "";

			for (var i = 0; i < Slots.GetLength(0); i++)
			for (var j = 0; j < Slots.GetLength(1); j++)
			{
				Slots[i, j].coord = new Vector2Int(i, j);
			}

			BackButton.onClick.AddListener(OnExitStatic);
		}

		protected override async UniTask enableMenu()
		{
			GameInputs.SetMouseUnlock("formation_menu");



			// RESET any previous remains
			// ----------------------------------------
			foreach (FormationMonster monster in _allMonsters)
			{
				Destroy(monster.gameObject);
			}

			_allMonsters.Clear();
			_monsterLookup.Clear();


			// LOAD from the savefile
			// ----------------------------------------
			// note: the code below is a failsafe and mostly a dev convenience feature.
			// Monsters should be added to the savefile formation as soon as they are added to the team
			// during gameplay.

			SaveData savedata = await SaveManager.GetCurrentAsync();

			// Automatically add existing monsters to the grid if they have no slots yet
			foreach (CharacterEntry kid in savedata.Party)
			{
				bool overlaps = savedata.Party.Any(k => k != kid && k.FormationCoord == kid.FormationCoord);
				if (!overlaps)
					// We are the only nanokin on this slot
					continue;

				// Search for the first free slot (starting from the top row)
				for (var i = 0; i < GRID_W; i++)
				for (var j = 0; j < GRID_H; j++)
				{
					if (!IsSlotTaken(new Vector2Int(i, j)))
					{
						// Use up the first free slot
						kid.FormationCoord = new Vector2Int(i, j);

						goto after_search;
					}
				}

				after_search: ;
			}

			_allTargets.Clear();


			// SPAWN the monsters
			// ----------------------------------------------
			await UniTask.WhenAll(savedata.Party.Select(AddMonster));

			// INITIALIZE the menu
			// ----------------------------------------
			Assert.IsTrue(_allMonsters.Count > 0);
			Assert.IsTrue(_allTargets.Count > 0);
			UI_MonsterInfo.LevelIndicator.SetActive(false);

			if (_monsterLookup.ContainsKey(_currentSelection))
			{
				_selectedMonster = _monsterLookup[_currentSelection];

				//_selectedMonster.DimVFX.tint = Color.white;
				//_selectedMonster.SelectVFX.tint = _selectedMonster.ReticleHoverColor;
			}

			ChangeState(States.MonsterSelect);

			VCam_MonsterSelection.gameObject.SetActive(true);
			VCam_SlotSelection.gameObject.SetActive(true);

			GameCams.Push(this);

			if (!Flags.GetBool("tut_formation_menu"))
			{
				Flags.SetBool("tut_formation_menu", true);
				//TutorialScreen.SetActive(true);
				ShowSplashScreen("SplashScreens/Demo_FormationMenu").Forget();
			}
		}

		private async UniTask AddMonster(CharacterEntry monster)
		{
			SlotTile slot = Slots[monster.FormationCoord.x, monster.FormationCoord.y];

			GameObject go = Instantiate(MonsterPrefab, slot.transform.position, Quaternion.identity, Root_Monsters);

			// Setup the puppet
			// ----------------------------------------
			var tree = new NanokinLimbTree();
			tree.SetBody(monster.nanokin.Body.Asset, _handles);
			tree.SetHead(monster.nanokin.Head.Asset, _handles);
			tree.SetArm1(monster.nanokin.Arm1.Asset, _handles);
			tree.SetArm2(monster.nanokin.Arm2.Asset, _handles);

			var puppet = new PuppetState(tree);

			foreach (Branch branch in puppet.tree.GetBranches())
			{
				await branch.State.loadTask;
			}

			MultiSpritePuppet monopuppet = go.GetComponent<MultiSpritePuppet>();
			monopuppet.SetPuppet(puppet);
			puppet.Play("idle");
			puppet.Render();
			go.SetLayerRecursively(Layers.AboveEnv);


			// Setup the formation component
			// ----------------------------------------
			FormationMonster fmonster = go.AddComponent<FormationMonster>();
			fmonster.puppet = monopuppet;
			fmonster.slot   = monster.FormationCoord;
			fmonster.entry  = monster;
			fmonster.DimVFX = new ManualVFX { tint = Color.white };
			fmonster.puppet.vfx.Add(fmonster.DimVFX);
			fmonster.SelectVFX = new ManualVFX { tint = Color.white };
			fmonster.puppet.vfx.Add(fmonster.SelectVFX);

			var target = new Target(fmonster);
			fmonster.target = target;

			_allTargets.Add(target);
			_allMonsters.Add(fmonster);

			if (!_monsterLookup.ContainsKey(monster.Name))
			{
				_monsterLookup.Add(monster.Name, fmonster);
			}
		}

		protected override UniTask disableMenu()
		{
			GameInputs.mouseUnlocks.Add("formation_menu");

			// Write the new updated contents
			// ----------------------------------------
			foreach (FormationMonster monster in _allMonsters)
			{
				monster.entry.FormationCoord = monster.slot;
			}

			VCam_MonsterSelection.gameObject.SetActive(false);
			VCam_SlotSelection.gameObject.SetActive(false);

			GameCams.Pop(this);

			return UniTask.CompletedTask;
		}

		private void OnDestroy()
		{
			SaveManager.SaveCurrent();

			_handles.ReleaseAll();
		}

	#region API

		private void ChangeState(States newstate)
		{
			// Exit the old state
			States oldstate = _state;
			switch (oldstate)
			{
				case States.MonsterSelect:
					break;

				case States.SlotSelect:
					if (!_stateTransition.confirmed)
					{
						// Restore the previous setup
						_selectedMonster.slot = _startingSlot;
					}

					break;

				default:
					EnableMenu();
					break;
			}

			// Enter the new state
			_state = newstate;
			switch (_state)
			{
				case States.MonsterSelect:
					break;

				case States.SlotSelect:
					_startingSlot = _selectedMonster.slot;
					break;

				default:
					DisableMenu();
					break;
			}

			RefreshMonsterSelection();
			RefreshMonsterReticle();
			RefreshTileSelection();
			RefreshCamera();

			_stateTransition = new StateTransition();
		}

		private SlotTile GetTile(FormationMonster monster) => Slots[monster.slot.x, monster.slot.y];

		public bool IsSlotTaken(Vector2Int coord)
		{
			return _allMonsters.Any(m => m.slot == coord);
		}

		private void SelectMonster(Vector3? dir)
		{
			FormationMonster previous = _selectedMonster;

			_selectedTarget  = Targeting.FindTowards(_selectedTarget, dir.Value, _allTargets);
			_selectedMonster = _selectedTarget.all.First() as FormationMonster;

			if (_selectedMonster != previous)
			{
				if (previous != null)
				{
					previous.SelectVFX.tint = Color.white;
					previous.DimVFX.tint = Color.white;
				}

				_currentSelection = _selectedMonster.entry.Name;

				// The selection has changed. Refresh everything.
				GameSFX.PlayGlobal(SFX_NavigateMonster, transform);
				RefreshMonsterSelection();
				RefreshMonsterReticle();
			}

			_selectedMonster.DimVFX.tint = Color.white;
			_selectedMonster.SelectVFX.tint = _selectedMonster.ReticleHoverColor;
		}

		private void SelectMonster([NotNull] FormationMonster monster)
		{
			FormationMonster previous = _selectedMonster;

			_selectedMonster = monster;
			_selectedTarget  = monster.target;

			if (_selectedMonster != previous)
			{
				if (previous != null)
				{
					previous.SelectVFX.tint = Color.white;
					previous.DimVFX.tint = Color.white;
				}

				_currentSelection = _selectedMonster.entry.Name;

				// The selection has changed. Refresh everything.
				GameSFX.PlayGlobal(SFX_NavigateMonster, transform);
				RefreshMonsterSelection();
				RefreshMonsterReticle();
			}
		}

		private void ConfirmMonster(FormationMonster monster)
		{
			_selectedMonster           = monster;
			_selectedTarget            = monster.target;
			_stateTransition.confirmed = true;

			GameSFX.PlayGlobal(SFX_ConfirmMonster, transform);

			ChangeState(States.SlotSelect);
		}

		private void MoveMonsterTo(Vector2Int dest)
		{
			_selectedMonster.slot = dest;

			RefreshMonsterSelection();
			RefreshTileSelection();
			RefreshCamera();

			GameSFX.PlayGlobal(SFX_NavigateSlots, transform);
		}

		private void ConfirmSlot()
		{
			// Confirm the slot that was picked
			_stateTransition.confirmed = true;
			ChangeState(States.MonsterSelect);
			GameSFX.PlayGlobal(SFX_ConfirmSlot, transform);
		}

	#endregion

	#region State Refresh

		/// <summary>
		/// Refresh the menu's selected monster.
		/// </summary>
		private void RefreshMonsterSelection()
		{
			if (_selectedMonster == null)
			{
				_selectedMonster = _allMonsters.First();
				_currentSelection = _selectedMonster.entry.Name;
			}

			if (_selectedTarget == null) _selectedTarget   = new Target(_selectedMonster);

			if (_selectedTarget.all.Count > 0 && _selectedTarget.all[0] is FormationMonster monster)
			{
				// Update the label
				UI_MonsterInfo.SetCharacter(monster.entry).ForgetWithErrors();
			}

			// Update position to match the slot
			_selectedMonster.transform.position = GetTile(_selectedMonster).transform.position;

			_selectedMonster.DimVFX.tint = Color.white;
			_selectedMonster.SelectVFX.tint = _selectedMonster.ReticleHoverColor;

			for (int i = 0; i < GRID_W; i++)
			{
				rowEffectDescriptions[i].color = ((i != _selectedMonster.slot.x) ? NormalDescriptionColor : HighlightedDescriptionColor);
			}

			// Update the target to match the selection.
			_selectedTarget.Clear();
			_selectedTarget.Add(_selectedMonster);
		}

		/// <summary>
		/// Refresh the menu's target reticle.
		/// </summary>
		/// <param name="justConfirmed">Whether or not the content of the reticle was confirmed.</param>
		private void RefreshMonsterReticle(bool justConfirmed = false)
		{
			if (_activeReticle)
			{
				_obsoleteReticle = _activeReticle;
				_obsoleteReticle.Disappear(justConfirmed);
				//_activeReticle = null;
			}

			if (_state == States.MonsterSelect)
			{
				_activeReticle = Instantiate(ReticlePrefab, Root_Reticles);
				_activeReticle.Appear();

				_activeReticle.Raycast.Camera = Camera;
				_activeReticle.Raycast.SetCanvasRect(Canvas);
				_activeReticle.Raycast.SetWorldPos(_selectedMonster.puppet.actor.center);
				_activeReticle.Raycast.RefreshPos();
			}
		}

		/// <summary>
		/// Refresh the menu's currently selected tile.
		/// </summary>
		private void RefreshTileSelection()
		{
			if (_activeTile == null && _selectedMonster != null)
				_activeTile = GetTile(_selectedMonster);

			if (_activeTile == null)
				return;

			switch (_state)
			{
				case States.MonsterSelect:
					_activeTile.SetHighlight(false);
					break;

				case States.SlotSelect:
					SlotTile oldtile = _activeTile;
					SlotTile newtile = GetTile(_selectedMonster);

					if (oldtile != newtile)
						oldtile.SetHighlight(false);

					newtile.SetHighlight(true);

					_activeTile = newtile;
					break;

				default:
					break;
			}
		}

		private void RefreshCamera()
		{
			/*switch (_state)
			{
				case States.MonsterSelect:
					VCam_MonsterSelection.Priority = 75;
					VCam_SlotSelection.Priority    = 70;
					break;

				case States.SlotSelect:
					VCam_MonsterSelection.Priority = 70;
					VCam_SlotSelection.Priority    = 75;

					SlotTile tile = GetTile(_selectedMonster);
					// VCam_SlotSelection.Follow = tile.transform;
					// VCam_SlotSelection.LookAt = tile.transform;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}*/
		}

	#endregion

		private void Update()
		{
			switch (_state)
			{
				// NAVIGATION
				// ----------------------------------------
				case States.MonsterSelect:
					Update_MonsterSelect();
					break;

				case States.SlotSelect:
					Update_SlotSelect();
					break;
			}

			if (_selectedMonster != null)
			{
				if (_selectedMonster.SelectVFX.tint != _selectedMonster.ReticleHoverColor)
				{
					_selectedMonster.SelectVFX.tint = _selectedMonster.ReticleHoverColor;
				}
			}
			else
			{
				RefreshMonsterSelection();
				RefreshMonsterReticle();
			}
		}

		private async UniTask ShowSplashScreen(string address)
		{
			GameSFX.PlayGlobal(SFX_OpenSubmenu, this);

			//OverworldHUD.HideCredits();

			//await GameEffects.FadeOut(ScreenFadeDuration);
			ChangeState(States.SubMenu);
			await MenuController.SetSplicerBG(true);

			await SplashScreens.ShowPrefabAsync(address, async () =>
			{
				GameSFX.PlayGlobal(SFX_ExitSubmenu, this);

				//OverworldHUD.ShowCredits();
				ChangeState(States.MonsterSelect);
			});
		}

		private void Update_MonsterSelect()
		{
			// Monster Selection (Keyboard)
			// ----------------------------------------
			if (GameInputs.menuNavigate.AnyPressed)
			{
				Camera cam = GameCams.Live.UnityCam;

				Vector3? dir = default;

				if (GameInputs.menuNavigate.up.IsPressed) dir         = cam.transform.forward.Change3(y: 0).normalized;
				else if (GameInputs.menuNavigate.down.IsPressed) dir  = -cam.transform.forward.Change3(y: 0).normalized;
				else if (GameInputs.menuNavigate.left.IsPressed) dir  = -cam.transform.right.Change3(y: 0).normalized;
				else if (GameInputs.menuNavigate.right.IsPressed) dir = cam.transform.right.Change3(y: 0).normalized;

				if (dir.HasValue)
				{
					SelectMonster(dir);
					return;
				}
			}


			// Monster Selection (Mouse)
			// ----------------------------------------
			if (GameInputs.GetMousePosition(out Vector2 mousepos))
			{
				Ray ray = Camera.ScreenPointToRay(mousepos);
				Draw.Ray(ray, 50f, Color.red);
				if (Physics.Raycast(ray, out RaycastHit rh, 50f, MonsterMouseRaycast))
				{
					FormationMonster hovered;

					if (_raycastMonster.OnEntered(rh, out hovered))
					{
						// Select on cursor entering
						SelectMonster(hovered);
					}
					else if (_raycastMonster.OnHold(rh, out hovered))
					{
						// Click to confirm
						if (Mouse.current.leftButton.wasPressedThisFrame)
						{
							ConfirmMonster(hovered);
						}
					}
				}
			}

			// Confirm selected monster
			// ----------------------------------------
			if (GameInputs.confirm.IsPressed)
			{
				ConfirmMonster(_selectedMonster);
				return;
			}

			DoExitControls();
		}

		private void Update_SlotSelect()
		{
			// Monster movement (gamepad/keyboard)
			// ----------------------------------------
			if (GameInputs.menuNavigate.AnyPressed)
			{
				Vector2Int ijoy = GameInputs.menuNavigate.Value.CeilToInt() * new Vector2Int(1, -1);

				for (var i = 0; i < Mathf.Max(GRID_W); i++)
				{
					Vector2Int dest = _selectedMonster.slot + ijoy * (i + 1);

					if (dest.x < 0 || dest.y < 0 || dest.x >= GRID_W || dest.y >= GRID_H)
						// Going out of bound; abort!
						return;

					if (_allMonsters.Any(m => m.slot == dest))
						// The slot is already in use
						continue;

					MoveMonsterTo(dest);
					return;
				}
			}

			// Monster movement (mouse)
			// ----------------------------------------
			if (GameInputs.GetMousePosition(out Vector2 mousepos))
			{
				Ray ray = Camera.ScreenPointToRay(mousepos);
				Draw.Ray(ray, 50f, Color.red);
				if (Physics.Raycast(ray, out RaycastHit rh, 50f, SlotMouseRaycast))
				{
					// Monster Selection (Mouse)
					// ----------------------------------------

					SlotTile hovered;

					if (_raycasTile.OnEntered(rh, out hovered))
					{
						// Select on cursor entering
						MoveMonsterTo(hovered.coord);
					}
					else if (_raycasTile.OnHold(rh, out hovered))
					{
						// Click to confirm
						if (Mouse.current.leftButton.wasPressedThisFrame)
						{
							MoveMonsterTo(hovered.coord);
							ConfirmSlot();
						}
					}
				}
			}

			// Cancel
			// ----------------------------------------
			if (GameInputs.cancel.IsPressed || Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
			{
				// Cancel the modification.
				ChangeState(States.MonsterSelect);
				GameSFX.PlayGlobal(SFX_CancelSlot, transform);
				return;
			}

			if (GameInputs.confirm.IsPressed)
			{
				ConfirmSlot();
			}
		}


		public enum States
		{
			MonsterSelect,
			SlotSelect,
			SubMenu
		}

		private struct StateTransition
		{
			public bool confirmed;
		}

		public void OnActivate()                                     { }
		public void OnRelease(ref CinemachineBlendDefinition? blend) { }

		public void ActiveUpdate()
		{
			switch (_state)
			{
				case States.MonsterSelect:
					VCam_MonsterSelection.Priority = GameCams.PRIORITY_ACTIVE;
					VCam_SlotSelection.Priority    = GameCams.PRIORITY_INACTIVE;
					break;

				case States.SlotSelect:
					VCam_MonsterSelection.Priority = GameCams.PRIORITY_INACTIVE;
					VCam_SlotSelection.Priority    = GameCams.PRIORITY_ACTIVE;

					//SlotTile tile = GetTile(_selectedMonster);
					// VCam_SlotSelection.Follow = tile.transform;
					// VCam_SlotSelection.LookAt = tile.transform;
					break;
			}
		}

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = GameCams.Cut;
		}

		public struct RaycastTarget<TMono>
			where TMono : Object
		{
			private int _last;

			private TMono current;

			public bool OnEntered(RaycastHit rh, out TMono ret)
			{
				int id = rh.collider.GetInstanceID();
				if (_last != id)
				{
					_last = id;
					ret   = current = rh.collider.GetComponent<TMono>();

					return true;
				}

				ret = null;
				return false;
			}

			public bool OnHold(RaycastHit rh, out TMono ret)
			{
				int id = rh.collider.GetInstanceID();
				if (_last == id)
				{
					ret = current;
					return true;
				}

				ret = null;
				return false;
			}
		}
	}
}