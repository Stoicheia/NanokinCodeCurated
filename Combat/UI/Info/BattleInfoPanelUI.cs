using Anjin.UI;
using Anjin.Util;
using JetBrains.Annotations;
using SaveFiles.Elements.Inventory.Items.Scripting;
using TMPro;
using UnityEngine;

namespace Combat.UI.Info
{
	public class BattleInfoPanelUI : StaticBoy<BattleInfoPanelUI>
	{
		public HUDElement SkillInfoPanel;

		public TextMeshProUGUI[] TMP_SkillInfoCost;
		public TextMeshProUGUI[] TMP_SkillInfoDescription;

		public TargetUI_UGUI Targeting;

		public bool ShowingSkillInfo;

		private void Start()
		{
			SkillInfoPanel.gameObject.SetActive(true);
			SkillInfoPanel.Alpha = 0;
		}

		public static void Show(Battle battle, [CanBeNull] BattleSkill skill)
		{
			if (skill == null || skill.asset == null) return;

			ShowSPCost((int) battle.GetSkillCost(skill).sp);

			string description = skill.Description();

			if (!skill.asset.CustomDescription)
			{
				Live.TMP_SkillInfoDescription.SetTextMulti(skill.asset.Description);
			}
			else
			{
				Live.TMP_SkillInfoDescription.SetTextMulti(skill.Description());
			}

			Live.Targeting.asset = skill.asset;
			Live.Targeting.Refresh();

			if (!Live.ShowingSkillInfo)
			{
				Live.ShowingSkillInfo = true;
				Live.SkillInfoPanel.DoAlphaFade(0, 1, 0.1f);
				Live.SkillInfoPanel.DoOffset(new Vector3(0, -30, 0), Vector3.zero, 0.1f);
			}
		}

		public static void Show(Battle battle, [CanBeNull] BattleSticker sticker)
		{
			if (sticker == null || sticker.asset == null) return;

			Live.TMP_SkillInfoCost.SetTextMulti("Free");
			Live.TMP_SkillInfoDescription.SetTextMulti(sticker.asset.Description);

			if (!Live.ShowingSkillInfo)
			{
				Live.ShowingSkillInfo = true;
				Live.SkillInfoPanel.DoAlphaFade(0, 1, 0.1f);
				Live.SkillInfoPanel.DoOffset(new Vector3(0, -30, 0), Vector3.zero, 0.1f);
			}
		}

		public static void Hide()
		{
			if (!Live.ShowingSkillInfo) return;

			Live.SkillInfoPanel.DoAlphaFade(1, 0, 0.1f);
			Live.SkillInfoPanel.DoOffset(Vector3.zero, new Vector3(0, -30, 0), 0.1f);
			Live.ShowingSkillInfo = false;
		}

		private static void ShowSPCost(int cost)
		{
			if (cost == 0)
			{
				SetFreeCost();
				return;
			}

			Live.TMP_SkillInfoCost.SetTextMulti($"{cost} sp");
		}

		private static void SetFreeCost()
		{
			Live.TMP_SkillInfoCost.SetTextMulti("Free");
		}
	}
}