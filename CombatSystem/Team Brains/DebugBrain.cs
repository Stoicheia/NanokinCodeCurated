using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Util;
using Combat.Data;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.UnityEditor;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using Unity.QuickSearch;
using UnityEditor;

#endif

namespace Combat
{
	/// <summary>
	/// A team brain which integrates with the Unity Editor and IMGUI
	/// to send commands.
	/// </summary>
	[Serializable]
	public class DebugBrain : BattleBrain
	{
		public static SkillAsset DefaultSkill;

		public const Key SKILL_KEY = Key.Z;
		public const Key SKIP_KEY  = Key.X;
		public const Key HOLD_KEY  = Key.C;
		public const Key KILL_KEY  = Key.K;
		public const Key LOOP_KEY  = Key.K;

		public static  DebugBrain   activeBrain;
		public static  SkillAsset   overrideSkill;
		public static  BattleAnim nextAnim;
		public static  bool         waiting;
		public static  bool         showUI;
		private static bool         _go;
		private static SkillAsset   _lastsel;

		public static bool go
		{
			get => _go;
			set
			{
				_go = value;
				if (_go) waiting = true;
			}
		}

		private static bool               _overdriveEnable;
		private static bool               _overdriveKeepActions;
		private static List<BattleAnim> _overdriveActions;
		private static string             _skillSearch        = "";
		private static List<SkillAsset>   _skillSearchResults = new List<SkillAsset>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			activeBrain = null;
			showUI      = false;
			_go         = false;
			_lastsel    = null;
			waiting     = true;
		}


		public override void OnRegistered()
		{
			base.OnRegistered();

#if UNITY_EDITOR
			if (overrideSkill == null)
				overrideSkill = InternalEditorConfig.Instance.LastTestedSkill;
#endif

			waiting = true;
			showUI  = false;

			Debug.Log("EditorTeamBrain: Q skip action, W pause/unpause, SPACE skill, ARROW formation");
			Debug.Log($"EditorTeamBrain: [{ResolveSkill()}]");

			nextAnim        = null;
			_overdriveActions = _overdriveActions ?? new List<BattleAnim>();
		}

		public override async UniTask<BattleAnim> OnGrantActionAsync()
		{
			activeBrain = this;

			GameInputs.mouseUnlocks.Add("DebugBrain");
			showUI = true;
			await UniTask.WaitUntil(() => !waiting || go);
			showUI = false;
			GameInputs.mouseUnlocks.Remove("DebugBrain");

			go          = false;
			activeBrain = null;

			// single action
			if (nextAnim != null)
			{
				BattleAnim ret = nextAnim;
				ret.fighter = battle.ActiveActer as Fighter;
				nextAnim  = null;
				return ret;
			}

			return null;
		}

		private static SkillAsset ResolveSkill()
		{
			SkillAsset skill = DefaultSkill;

			Object sel = null;
#if UNITY_EDITOR
			sel = Selection.activeObject;
#endif
			bool selchanged = _lastsel != sel;
			if (selchanged && sel is SkillAsset skillsel)
			{
				DefaultSkill = skillsel;
				_lastsel     = skillsel;
			}

			if (overrideSkill)
			{
				skill        = overrideSkill;
				DefaultSkill = overrideSkill;
			}

			return skill;
		}

		public override void Update()
		{
			if (!Application.isFocused) return;
			if (activeBrain != this) return;

			if (GameInputs.IsShortcutPressed(SKIP_KEY)) ChooseSkip();
			if (GameInputs.IsShortcutPressed(SKILL_KEY)) ChooseSkill(ResolveSkill());
			if (GameInputs.IsShortcutPressed(HOLD_KEY)) ChooseHold();
			if (GameInputs.IsShortcutPressed(KILL_KEY)) ChooseKill();
			if (GameInputs.IsShortcutPressed(LOOP_KEY))
			{
				GameOptions.current.combat_use_loop.Value = !GameOptions.current.combat_use_loop.Value;
			}

			if (GameInputs.IsPressed(Key.LeftArrow)) ChooseMove(Vector2Int.left, battle);
			if (GameInputs.IsPressed(Key.RightArrow)) ChooseMove(Vector2Int.right, battle);
			if (GameInputs.IsPressed(Key.UpArrow)) ChooseMove(Vector2Int.up, battle);
			if (GameInputs.IsPressed(Key.DownArrow)) ChooseMove(Vector2Int.down, battle);
		}

	#region Choose

		public void Choose(BattleAnim anim)
		{
			if (_overdriveEnable)
			{
				_overdriveActions.Add(anim);
			}
			else
			{
				go         = true;
				nextAnim = anim;
			}
		}

		public void ChooseKill()
		{
			Choose(new KillAnim());
		}

		public void ChooseHold()
		{
			Choose(new HoldAnim());
		}

		public void ChooseSkill(SkillAsset skill)
		{
			var targeting = new Targeting();

			BattleSkill instance = battle.GetSkillOrRegister(fighter, skill);
			if (instance != null)
			{
				battle.GetSkillTargets(instance, targeting);

				if (targeting.options.SafeGet(0)?.Count == 0)
				{
					// ChooseSkip();
					Fighter enemy = battle.GetEnemyTeams(team).Choose()?.fighters.Choose();
					if (enemy != null)
					{
						targeting.AddPick(new Target(enemy));
					}
				}
				else
				{
					targeting.PickRandomly();
				}
			}

			Choose(new SkillAnim(null, instance, targeting));
		}

		public void ChooseSkip()
		{
			Choose(new MockAnim());
		}

		public void ChooseMove(Vector2Int dir, [NotNull] Battle battle)
		{
			// Stop core and start again with same configuration
			if (battle.ActiveActer is Fighter fighter)
			{
				Slot slot = battle.GetSlot(fighter.home.coord + dir);
				if (slot != null)
					Choose(new MoveAnim(fighter, slot, MoveSemantic.Auto));
			}
		}

		public void ChooseOverdrive()
		{
			_overdriveEnable = false;
			Choose(new OverdriveAnim(null, _overdriveActions.ToList()));
			if (!_overdriveKeepActions) _overdriveActions.Clear();
		}

	#endregion

	#region GUI

		public void OnLayoutInstance(ref DebugSystem.State state, Battle battle)
		{
			if (!showUI) return;

			if (ImGui.Begin("Debug Brain", ImGuiWindowFlags.NoFocusOnAppearing))
			{
				// Info
				{
					ImGui.Indent();

					if (battle.ActiveActer is Fighter fighter)
						ImGui.Text(fighter.Name);
					else
						ImGui.Text("No fighter");

					if (nextAnim != null)
						ImGui.Text(nextAnim.ToString());
					else
						ImGui.Text("No action");

					ImGui.Unindent();

					ImGui.NewLine();
					ImGui.Separator();
					ImGui.NewLine();
				}

				// Actions
				{
					ImGui.Indent();
					SkillAsset skill = ResolveSkill();
					if (skill != null)
					{
						if (ImGui.Button(skill.DisplayName))
						{
							ChooseSkill(ResolveSkill());
						}
					}
					else
					{
						if (ImGui.Button("no skill"))
						{
#if UNITY_EDITOR
							QuickSearch.ShowObjectPicker((o, _) => { OnObjectSelected(o); }, OnObjectSelected, null, "", "", typeof(SkillAsset));
#endif
						}
					}

					ImGui.SameLine();
					if (ImGui.InputText(String.Empty, ref _skillSearch, 255))
					{
#if UNITY_EDITOR
						EditorUtil.SetProjectSearch($"t:skillasset {_skillSearch}");
#endif

						// Set nearest
#if UNITY_EDITOR
						string[] results = AssetDatabase.FindAssets($"{_skillSearch} t:{nameof(SkillAsset)}");
						if (results.Length > 0)
						{
							SkillAsset sel = results
								.Select(AssetDatabase.GUIDToAssetPath)
								.Select(AssetDatabase.LoadAssetAtPath<SkillAsset>)
								.OrderBy(skil => skil.name.LeveinsteinDistance(_skillSearch))
								.FirstOrDefault();

							OnObjectSelected(sel);
						}
#else
						GameAssets.FindSkills(_skillSearch, _skillSearchResults);
						if (_skillSearchResults.Count > 0)
							OnObjectSelected(_skillSearchResults[0]);
#endif
					}

					if (ImGui.Button("Skip")) ChooseSkip();
					if (ImGui.Button("Kill")) ChooseKill();

					if (_overdriveActions.Count > 0)
					{
						if (ImGui.Button("Overdrive")) ChooseOverdrive();
					}


					ImGui.Unindent();

					ImGui.NewLine();
					ImGui.Separator();

					bool v = GameOptions.current.combat_use_loop.Value;
					ImGui.Checkbox("Loop", ref v);
					GameOptions.current.combat_use_loop.Value = v;

					ImGui.NewLine();
				}

				// Overdrive
				{
					ImGui.Indent();
					ImGui.Checkbox("Overdrive", ref _overdriveEnable);
					ImGui.Checkbox("Keep Actions", ref _overdriveKeepActions);
					if (_overdriveActions.Count > 0)
					{
						if (ImGui.Button("Back"))
							_overdriveActions.RemoveAt(_overdriveActions.Count - 1);

						ImGui.SameLine();
						if (ImGui.Button("Cancel"))
						{
							_overdriveActions.Clear();
							_overdriveEnable = false;
						}

						for (var i = 0; i < _overdriveActions.Count; i++)
						{
							BattleAnim anim = _overdriveActions[i];
							ImGui.Text($"{i + 1}:");
							ImGui.SameLine();
							ImGui.Text(anim.ToString());
						}
					}

					ImGui.Unindent();

					ImGui.NewLine();
					ImGui.Separator();
					ImGui.NewLine();
				}
				ImGui.End();
			}
		}

		public static void OnLayout(ref DebugSystem.State state, Battle battle)
		{
			if (activeBrain != null)
			{
				activeBrain.OnLayoutInstance(ref state, battle);
			}
		}

#if UNITY_EDITOR

		public static void OnEditorSelectionChanged()
		{
			OnObjectSelected(Selection.activeObject);
		}

#endif

		private static void OnObjectSelected(Object o)
		{
			if (o is SkillAsset skillAsset)
			{
				overrideSkill = skillAsset;

#if UNITY_EDTIOR
				InternalEditorConfig.Instance.LastTestedSkill = skillAsset;
#endif
			}
		}

	#endregion
	}
}