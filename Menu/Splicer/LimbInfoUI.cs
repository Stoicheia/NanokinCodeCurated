using System.Collections.Generic;
using Anjin.Util;
using Assets.Nanokins;
using Combat;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Util.RenderingElements.PointBars;

public class LimbInfoUI : SerializedMonoBehaviour
{
	[Title("References")]
	[SerializeField] private TextMeshProUGUI TMP_Name;
	[SerializeField] private TextMeshProUGUI TMP_Mastery;
	[SerializeField] private PointBar        Bar_XP;
	[SerializeField] private SkillInfoUI[]   UI_Skills;
	[SerializeField] private UIComparator    Comparator;
	[Space]
	[SerializeField] private TextMeshProUGUI TMP_HP;
	[SerializeField] private TextMeshProUGUI TMP_SP;
	[Space]
	[SerializeField] private TextMeshProUGUI TMP_Power;
	[SerializeField] private TextMeshProUGUI TMP_Speed;
	[SerializeField] private TextMeshProUGUI TMP_Will;
	[SerializeField] private TextMeshProUGUI TMP_AP;
	[Space]
	[SerializeField] private TextMeshProUGUI TMP_Blunt;
	[SerializeField] private TextMeshProUGUI TMP_Slash;
	[SerializeField] private TextMeshProUGUI TMP_Pierce;
	[SerializeField] private TextMeshProUGUI TMP_Oida;
	[SerializeField] private TextMeshProUGUI TMP_Astra;
	[SerializeField] private TextMeshProUGUI TMP_Gaia;

	// Comparators
	// ----------------------------------------
	[SerializeField] private TextMeshProUGUI TMP_HPComparator;
	[SerializeField] private TextMeshProUGUI TMP_SPComparator;
	[Space]
	[SerializeField] private TextMeshProUGUI TMP_PowerComparator;
	[SerializeField] private TextMeshProUGUI TMP_SpeedComparator;
	[SerializeField] private TextMeshProUGUI TMP_WillComparator;
	[SerializeField] private TextMeshProUGUI TMP_APComparator;
	[Space]
	[SerializeField] private TextMeshProUGUI TMP_BluntComparator;
	[SerializeField] private TextMeshProUGUI TMP_SlashComparator;
	[SerializeField] private TextMeshProUGUI TMP_PierceComparator;
	[SerializeField] private TextMeshProUGUI TMP_OidaComparator;
	[SerializeField] private TextMeshProUGUI TMP_AstraComparator;
	[SerializeField] private TextMeshProUGUI TMP_GaiaComparator;

	// Etc.
	// ----------------------------------------

	[Title("Design")]
	[SerializeField] private Color32 ColorInactiveAP = new Color32(170, 170, 170, 250);
	[SerializeField] private Color32 ColorActiveAP = new Color32(255, 255, 255, 255);

	private int              _level;
	private NanokinLimbAsset _asset;
	private LimbInstance     _limb;

	private bool             _enableComparison;
	private string           _nameComparator;
	private Pointf           _pointComparator;
	private Statf            _statComparator;
	private Elementf         _efficiencyComparator;
	private (int, int)       _xpComparator;
	private List<SkillAsset> _skillsComparator;
	private int              _rankComparator;

	private void Awake()
	{
		RefreshUI();
	}

	public void ClearLimb()
	{
		_asset = null;
		_limb  = null;

		RefreshUI();
	}

	/// <summary>
	/// Show a limb instance in the UI.
	/// </summary>
	/// <param name="newLimb"></param>
	[Button]
	public void ChangeLimb([NotNull] LimbInstance newLimb, int level = StatConstants.MAX_LEVEL)
	{
		_asset = null;
		_limb  = newLimb;
		_level = level;

		RefreshUI();
	}

	/// <summary>
	/// Show a limb asset in they UI.
	/// </summary>
	/// <param name="asset"></param>
	/// <param name="level"></param>
	public void ChangeLimb(NanokinLimbAsset asset, int level = StatConstants.MAX_LEVEL)
	{
		_asset = asset;
		_limb  = null;
		_level = level;

		RefreshUI();
	}

	public void CompareWith([NotNull] LimbInstance limbInstance, int level)
	{
		CompareWith(
			limbInstance.Asset.FullName,
			limbInstance.CalcMaxPoints(level),
			limbInstance.CalcStats(level),
			limbInstance.CalcEfficiency(level),
			(limbInstance.RP, limbInstance.NextMasteryRP),
			limbInstance.Mastery,
			limbInstance.Skills
		);
	}

	public void CompareWith(
		string           name,
		Pointf           points,
		Statf            stats,
		Elementf         efficiency,
		(int, int)       rp,
		int              rank,
		List<SkillAsset> skills
	)
	{
		_enableComparison = true;

		_nameComparator       = name;
		_pointComparator      = points;
		_statComparator       = stats;
		_efficiencyComparator = efficiency;
		_xpComparator         = rp;
		_rankComparator       = rank;
		_skillsComparator     = skills;

		RefreshUI();
	}

	public void CompareNone()
	{
		_enableComparison = false;
		RefreshUI();
	}

	private void RefreshUI()
	{
		// ReSharper disable once LocalVariableHidesMember
		string           name       = "N/A";
		Statf            stats      = Statf.Zero;
		Pointf           points     = Pointf.Zero;
		Elementf         efficiency = Elementf.Zero;
		bool             hasAP      = false;
		int              rank       = 0;
		List<SkillAsset> skills     = ListPool<SkillAsset>.Claim(UI_Skills.Length);

		if (_asset == null && _limb == null)
		{
			this.LogWarn("Cannot display anything because we have no limb asset and no limb instance to display. ");
		}
		else if (_asset != null && _limb == null)
		{
			TMP_Mastery.text = "---";
			name             = _asset.FullName;

			stats      = _asset.CalcStats(_level);
			points     = _asset.CalcPoints(_level);
			efficiency = _asset.CalcEfficiency(_level);

			hasAP = _asset.IsBody;
			rank  = 3;

			Bar_XP.gameObject.SetActive(false);

			for (int i = 0; i < UI_Skills.Length; i++)
			{
				skills.Add(_asset.Skills.SafeGet(i));
			}
		}
		else if (_limb != null)
		{
			NanokinLimbAsset asset = _limb.Asset;
			name = asset.FullName;

			points     = _limb.CalcMaxPoints(_level);
			stats      = _limb.CalcStats(_level);
			efficiency = _limb.CalcEfficiency(_level);

			hasAP = _limb.Asset.IsBody;
			rank  = _limb.Mastery;

			TMP_Mastery.text = _limb.Mastery.ToString();
			Bar_XP.gameObject.SetActive(true);
			Bar_XP.Set(_limb.RP, _limb.NextMasteryRP);

			for (int i = 0; i < UI_Skills.Length; i++)
			{
				skills.Add(asset.Skills.SafeGet(i));
			}

			// Update skill displays
			WriteUnlockedSkills(asset.Skills, _limb.Mastery, skills);
		}

		if (_enableComparison)
		{
			name = _nameComparator ?? name;
			WriteUnlockedSkills(_skillsComparator, _rankComparator, skills);
		}

		// REFRESH VALUES
		// ----------------------------------------
		TMP_Name.text = name;
		TMP_AP.color  = hasAP ? ColorActiveAP : ColorInactiveAP;

		Comparator.UpdateNumber(false, TMP_HP, points.hp, _pointComparator.hp, false);
		Comparator.UpdateNumber(false, TMP_SP, points.sp, _pointComparator.sp, false);

		Comparator.UpdateNumber(false, TMP_Power, stats.power, _statComparator.power, false);
		Comparator.UpdateNumber(false, TMP_Speed, stats.speed, _statComparator.speed, false);
		Comparator.UpdateNumber(false, TMP_Will, stats.will, _statComparator.will, false);
		Comparator.UpdateNumber(false, TMP_AP, stats.ap, _statComparator.ap, false);

		//Comparator.UpdatePercent(false, TMP_Blunt, efficiency.blunt, _efficiencyComparator.blunt, false);
		//Comparator.UpdatePercent(false, TMP_Slash, efficiency.slash, _efficiencyComparator.slash, false);
		//Comparator.UpdatePercent(false, TMP_Pierce, efficiency.pierce, _efficiencyComparator.pierce, false);
		//Comparator.UpdatePercent(false, TMP_Gaia, efficiency.gaia, _efficiencyComparator.gaia, false);
		//Comparator.UpdatePercent(false, TMP_Astra, efficiency.astra, _efficiencyComparator.astra, false);
		//Comparator.UpdatePercent(false, TMP_Oida, efficiency.oida, _efficiencyComparator.oida, false);
		Comparator.UpdateBracket(false, TMP_Blunt, efficiency.blunt, _efficiencyComparator.blunt, false);
		Comparator.UpdateBracket(false, TMP_Slash, efficiency.slash, _efficiencyComparator.slash, false);
		Comparator.UpdateBracket(false, TMP_Pierce, efficiency.pierce, _efficiencyComparator.pierce, false);
		Comparator.UpdateBracket(false, TMP_Gaia, efficiency.gaia, _efficiencyComparator.gaia, false);
		Comparator.UpdateBracket(false, TMP_Astra, efficiency.astra, _efficiencyComparator.astra, false);
		Comparator.UpdateBracket(false, TMP_Oida, efficiency.oida, _efficiencyComparator.oida, false);

		Comparator.UpdateNumber(_enableComparison, TMP_Mastery, rank, _rankComparator, false);

		Comparator.UpdateComparatorNumber(_enableComparison, TMP_HPComparator, points.hp, _pointComparator.hp);
		Comparator.UpdateComparatorNumber(_enableComparison, TMP_SPComparator, points.sp, _pointComparator.sp);

		Comparator.UpdateComparatorNumber(_enableComparison, TMP_PowerComparator, stats.power, _statComparator.power);
		Comparator.UpdateComparatorNumber(_enableComparison, TMP_SpeedComparator, stats.speed, _statComparator.speed);
		Comparator.UpdateComparatorNumber(_enableComparison, TMP_WillComparator, stats.will, _statComparator.will);
		Comparator.UpdateComparatorNumber(_enableComparison, TMP_APComparator, stats.ap, _statComparator.ap);

		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_BluntComparator, efficiency.blunt, _efficiencyComparator.blunt);
		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_SlashComparator, efficiency.slash, _efficiencyComparator.slash);
		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_PierceComparator, efficiency.pierce, _efficiencyComparator.pierce);
		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_OidaComparator, efficiency.oida, _efficiencyComparator.oida);
		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_AstraComparator, efficiency.astra, _efficiencyComparator.astra);
		//Comparator.UpdateComparatorPercent(_enableComparison, TMP_GaiaComparator, efficiency.gaia, _efficiencyComparator.gaia);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_BluntComparator, efficiency.blunt, _efficiencyComparator.blunt);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_SlashComparator, efficiency.slash, _efficiencyComparator.slash);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_PierceComparator, efficiency.pierce, _efficiencyComparator.pierce);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_OidaComparator, efficiency.oida, _efficiencyComparator.oida);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_AstraComparator, efficiency.astra, _efficiencyComparator.astra);
		Comparator.UpdateComparatorBracket(_enableComparison, TMP_GaiaComparator, efficiency.gaia, _efficiencyComparator.gaia);

		if (_enableComparison)
			Bar_XP.Set(_xpComparator.Item1, _xpComparator.Item2);

		// Skills
		for (var i = 0; ((i < UI_Skills.Length) && i < (skills.Count)); i++)
		{
			SkillAsset  skill = skills[i];
			SkillInfoUI ui    = UI_Skills[i];

			//LimbMenu.input

			ui.ChangeSkill(skill, skill == null, i + 1);
		}


		ListPool<SkillAsset>.Release(ref skills);
	}

	private void WriteUnlockedSkills(List<SkillAsset> all, int mastery, List<SkillAsset> skills)
	{
		for (var i = 0; i < UI_Skills.Length; i++)
		{
			SkillAsset skill = all.SafeGet(i);

			if (mastery < i + 1)
				skill = null; // Skill not unlocked!

			skills[i] = skill;
		}
	}
}