using System;
using System.Collections.Generic;
using Anjin.Nanokin.SceneLoading;
using Anjin.Util;
using Core.Debug;
using ImGuiNET;
using UnityEngine;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;

namespace Anjin.Nanokin
{
	public class DebugLevelSelectMenu : StaticBoy<DebugLevelSelectMenu>, IDebugDrawer
	{
		public List<LevelManifest> Manifests;

		[NonSerialized]
		LevelManifest hovered_level;

		float max_text_size = 0;

		[NonSerialized]
		public int CurrentSelected = 0;

		bool Active;

		public GUISkin skin;

		private void Start()
		{
			GameController.Live.StateApp = GameController.AppState.Menu;
			Manifests                    = LevelManifestDatabase.LoadedDB.Manifests;
			Active                       = true;

			DebugSystem.Register(this);
		}

		private void OnEnable()
		{
			GameInputs.mouseUnlocks.Add("level_select_menu");
		}

		private void OnDisable()
		{
			GameInputs.mouseUnlocks.Remove("level_select_menu");

		}

		public void OnSelectMenuItem(int index)
		{
			if (Manifests[index].MainScene.IsInBuild || Application.isEditor)
			{
				GameSceneLoader.UnloadScenes(gameObject.scene);
				GameController.Live.StartGameplay(Manifests[index]);

				Active = false;
			}
		}

		private void Update()
		{
			if (GameInputs.jump.IsPressed || Input.GetKeyDown(KeyCode.Return))
			{
				OnSelectMenuItem(CurrentSelected);
				return;
			}

			if (GameInputs.move.down.IsPressed)
			{
				CurrentSelected++;
				if (CurrentSelected >= Manifests.Count) CurrentSelected = 0;
			}
			else if (GameInputs.move.up.IsPressed)
			{
				CurrentSelected--;
				if (CurrentSelected < 0) CurrentSelected = Manifests.Count - 1;
			}
		}

		/*private void OnGUI()
		{
			if (!Active) return;

			var oldSkin = GUI.skin;
			GUI.skin = skin;

			glo.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

			glo.BeginVertical();
			{
				glo.FlexibleSpace();
				{
					glo.BeginHorizontal();
					{
						glo.FlexibleSpace();
						glo.BeginVertical(GUI.skin.box);
						{
							InsideBoxGUI();
						}
						glo.EndVertical();
						glo.FlexibleSpace();
					}
					glo.EndHorizontal();
				}
				glo.FlexibleSpace();
			}
			glo.EndVertical();

			glo.EndArea();

			GUI.skin = oldSkin;
		}

		void InsideBoxGUI()
		{
			glo.Label("DEBUG LEVEL SELECT");

			g.FocusControl("Button" + CurrentSelected.ToString());

			for (int i = 0; i < Manifests.Count; i++)
			{
				if (!Manifests[i].MainScene.IsInBuild)
					GUI.enabled = false;

				g.SetNextControlName("Button" + i.ToString());
				if (glo.Button(Manifests[i].LevelName))
				{
					OnSelectMenuItem(i);
				}

				GUI.enabled = true;
			}
		}*/

		float intial_spacing = 0;

		public void OnLayout(ref DebugSystem.State state)
		{
			if (max_text_size == 0) {
				for (int i = 0; i < Manifests.Count; i++) {
					var size = ImGui.CalcTextSize(Manifests[i].DisplayName);
					if (size.x > max_text_size)
						max_text_size = size.x;
				}
			}

			hovered_level = null;
			ImGui.SetNextWindowPos(state.DisplaySize / 2, ImGuiCond.Always, new Vector2(0.5f,0.5f));
			ImGui.SetNextWindowSize(new Vector2(state.DisplaySize.x / 2, 0), ImGuiCond.Always);
			if (ImGui.Begin("LEVEL SELECT", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar)) {

				ImGui.Columns(2);

				if (intial_spacing < 2) {
					ImGui.SetColumnWidth(0, max_text_size + 16);
					intial_spacing++;
				}

				ImGui.PushItemWidth(-1);

				for (int i = 0; i < Manifests.Count; i++)
				{
					if (Manifests[i] != null && Manifests[i].MainScene.IsInBuild || Application.isEditor) {
						ImGui.PushID(i);
						if (ImGui.Button(Manifests[i].LevelName)) OnSelectMenuItem(i);
						if (ImGui.IsItemHovered()) hovered_level = Manifests[i];
						ImGui.PopID();
					}
				}

				if(!Application.isEditor) {
					//ImGui.Separator();
					ImGui.TextColored(Color.red.ToV4(), "Scenes Not In Build:");

					for (int i = 0; i < Manifests.Count; i++) {
						if (!Manifests[i].MainScene.IsInBuild) {
							ImGui.TextDisabled(Manifests[i].LevelName);
							//if (ImGui.IsItemHovered()) hovered_level = Manifests[i];
						}
					}
				}
				ImGui.PopItemWidth();


				ImGui.NextColumn();

				if (hovered_level != null) {
					ImGui.TextColored(ColorsXNA.Orange.ToV4(), hovered_level.name);

					ImGui.Text("Main Scene:" + hovered_level.MainScene.SceneName);
					if(hovered_level.SubScenes.Count > 0) {
						ImGui.Text("Subscenes:");

						ImGui.Indent(16);
						for (int i = 0; i < hovered_level.SubScenes.Count; i++)
							ImGui.Text($"{i}:" + hovered_level.SubScenes[i].SceneName);
						ImGui.Unindent(16);
					}



					//ImGui.Separator();

					ImGui.TextColored(ColorsXNA.Coral.ToV4(), "Actor Ref Path:" + hovered_level.ActorReferencePath);
					// ImGui.TextColored(ColorsXNA.Coral.ToV4(), "Level Script:" + (hovered_level.LevelScript == null ? "none" : hovered_level.LevelScript.Path));
					//ImGui.Separator();

					if(hovered_level.RegionGraphs.Count > 0) {
						ImGui.TextColored(ColorsXNA.Aquamarine.ToV4(), "Auto-loaded Region Graphs:");
						ImGui.Indent(16);

						for (int i = 0; i < hovered_level.RegionGraphs.Count; i++)
							ImGui.TextColored(ColorsXNA.Aquamarine.ToV4(), $"{i}:" + hovered_level.RegionGraphs[i].name);

						ImGui.Unindent(16);
					}

					//ImGui.Separator();

					var profile = hovered_level.MusicProfile;
					ImGui.TextColored(ColorsXNA.MediumPurple.ToV4(), "Music Profile:" + (profile == null ? "none" : profile.name));

					profile = hovered_level.AmbientProfile;
					ImGui.TextColored(ColorsXNA.MediumPurple.ToV4(), "Ambient Profile:" + (profile == null ? "none" : profile.name));
					/*public List<RegionGraphAsset> AutoLoadedRegionGraphs;

					public GameAudioProfile MusicProfile;
					public GameAudioProfile AmbientProfile;*/
				}

				ImGui.Columns(1);

			} ImGui.End();
		}
	}
}