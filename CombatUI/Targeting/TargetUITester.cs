using System;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Odin.Attributes;

namespace Combat.UI
{
	public class TargetUITester : SerializedMonoBehaviour
	{
		[OnValueChanged("Refresh")]
		public SkillAsset asset;

		private async void Awake()
		{
			GameInputs.forceUnlocks.Add("target_ui_tester");
			DebugSystem.onLayout += OnLayout;
			//await Addressables.InitializeAsync();
			await GameController.TillIntialized();
			//await Lua.initTask;

			Icon_Epicenter  = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Epicenter");
			Icon_FreeTarget = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_FreeTarget");
			Icon_Move1      = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Move_One");
			Icon_Move2      = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Move_Two");
			Icon_Teleport   = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Teleport");

			Refresh();
		}

		private void OnDestroy()
		{
			GameInputs.forceUnlocks.Remove("target_ui_tester");
			DebugSystem.onLayout -= OnLayout;
		}

		[Button]
		public void Refresh()
		{
			if (!Application.isPlaying || asset == null) return;

			TargetUILua = new TargetUILua();

			SkillAsset.EvaluatedInfo info = asset.EvaluateInfo(TargetUILua);


			if (info.targetingInfo != null)
			{
				SkillAsset.TargetingInfo targeting = info.targetingInfo.Value;
			}
		}

		[ShowInPlay, NonSerialized]
		public TargetUILua TargetUILua;


		// Drawer for testing
		//----------------------------------------

		public static Color c_field_player   = ColorsXNA.SeaGreen;
		public static Color c_field_opponent = ColorsXNA.Goldenrod;

		[ShowInInspector]
		public static Sprite Icon_Epicenter;
		public static Sprite Icon_FreeTarget;
		public static Sprite Icon_Move1;
		public static Sprite Icon_Move2;
		public static Sprite Icon_Teleport;

		private void OnLayout(ref DebugSystem.State state)
		{
			if (ImGui.Begin("Target UI Tester"))
			{
				if (ImGui.Button("Refresh"))
				{
					Refresh();
				}

				ImGui.Separator();

				ImDrawListPtr g = ImGui.GetWindowDrawList();

				const float size     = 64;
				const float sep      = 8;
				Vector2     size_vec = new Vector2(size, size);
				Vector2     grid_offset;


				if (TargetUILua != null && TargetUILua.Grid != null)
				{
					ImGui.Text("Description: " + (TargetUILua._description ?? "None"));

					ImGui.Spacing();

					grid_offset = size_vec * TargetUILua.Grid.offset + new Vector2(sep, sep) * TargetUILua.Grid.offset;

					// Bounds
					Vector2 cur = ImGui.GetCursorScreenPos();
					g.AddRect(cur, cur + TargetUILua.Bounds * (size_vec + new Vector2(sep, sep)), ColorsXNA.LightPink.ToUint(), 3, ImDrawCornerFlags.All, 2);

					// Grid
					if (TargetUILua.Grid.slots != null)
					{
						for (int y = 0; y < TargetUILua.Grid.size.y; y++)
						{
							for (int x = 0; x < TargetUILua.Grid.size.x; x++)
							{
								DrawSlot(x, y, TargetUILua.Grid.slots[x, y]);
							}
						}
					}

					// Icons
					foreach (TargetUIIcon icon in TargetUILua.Grid.icons)
					{
						Vector2 pos = GetPos(icon.x, icon.y);

						DrawIconIfThere(TargetUIIconType.Epicenter, Icon_Epicenter);
						DrawIconIfThere(TargetUIIconType.Free, Icon_FreeTarget);
						DrawIconIfThere(TargetUIIconType.Move1, Icon_Move1);
						DrawIconIfThere(TargetUIIconType.Move2, Icon_Move2);
						DrawIconIfThere(TargetUIIconType.Teleport, Icon_Teleport);

						void DrawIconIfThere(TargetUIIconType _icon, Sprite sprite)
						{
							if (icon.type == _icon)
								AImgui.AddSprite(ref g, sprite, pos + size_vec / 2, size_vec, icon.rot);
						}
					}
				}

				Vector2 GetPos(int x, int y)
				{
					Vector2 cur = ImGui.GetCursorScreenPos();
					return new Vector2(cur.x + size * x + sep * x, cur.y + size * y + sep * y);
				}

				void DrawSlot(int x, int y, TargetUISlot slot)
				{
					Vector2 base_pos = grid_offset + GetPos(x, y);

					Color base_col = slot.team == TargetUITeam.Opponent ? c_field_opponent : c_field_player;


					if (slot.highlight != TargetUIHighlight.Empty)
					{
						Color fill_col;
						if (slot.highlight == TargetUIHighlight.Full)
						{
							fill_col = base_col.Brighten(0.2f);
						}
						else
						{
							fill_col = base_col.Darken(0.4f);
						}

						g.AddRectFilled(base_pos, base_pos + size_vec, fill_col.ToUint(), 3, ImDrawCornerFlags.All);
					}

					g.AddRect(base_pos, base_pos + size_vec, base_col.ToUint(), 3, ImDrawCornerFlags.All, 2);
				}
			}

			ImGui.End();
		}
	}
}