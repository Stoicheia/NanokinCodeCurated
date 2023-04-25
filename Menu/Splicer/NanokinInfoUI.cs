using System;
using Anjin.Nanokin;
using Anjin.UI;
using Anjin.Util;
using API.Puppets.Components;
using Assets.Nanokins;
using Combat.Entry;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using Puppets;
using SaveFiles;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util.Addressable;
using Util.RenderingElements.PointBars;

public class NanokinInfoUI : SerializedMonoBehaviour
{
	[Title("References")]
	public Image IMG_Nanokeeper;
	public RectTransform   BustRoot;
	public PuppetUGUI      PuppetDisplayer;
	public TextMeshProUGUI TMP_Name;
	public UIComparator    Comparator;
	[Space]
	public bool WithPoints;
	[ShowIf("WithPoints")] public PointBar        Bar_HP;
	[ShowIf("WithPoints")] public PointBar        Bar_SP;
	[ShowIf("WithPoints")] public TextMeshProUGUI TMP_MaxHP;
	[ShowIf("WithPoints")] public TextMeshProUGUI TMP_MaxSP;
	[ShowIf("WithPoints")] public TextMeshProUGUI TMP_MaxHPComparator;
	[ShowIf("WithPoints")] public TextMeshProUGUI TMP_MaxSPComparator;
	[Space]
	public bool WithStats;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_Power;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_Speed;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_Will;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_AP;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_PowerComparator;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_SpeedComparator;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_WillComparator;
	[ShowIf("WithStats")] public TextMeshProUGUI TMP_APComparator;
	[Space]
	public bool WithEfficiencies;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Blunt;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Slash;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Pierce;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Oida;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Astra;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_Gaia;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_BluntComparator;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_SlashComparator;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_PierceComparator;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_OidaComparator;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_AstraComparator;
	[ShowIf("WithEfficiencies")] public TextMeshProUGUI TMP_GaiaComparator;


	[Title("Design")]
	public bool ComparisonHideUnchanged;

	private NanokinInstance _nanokin;
	private PuppetState     _puppetState;
	private NanokinLimbTree _puppetLimbTree;

	private bool     _enableCompare;
	private Pointf   _maxPointsComparator;
	private Statf    _statComparator;
	private Elementf _efficiencyComparator;

	private AsyncOperationHandle<Sprite>     _characterArt;
	private AsyncOperationHandle<GameObject> _characterBust;

	private CharacterBust _bust;
	private AsyncHandles  _limbHandles;

	private CharacterEntry _character;


	private void Start()
	{
		_limbHandles = new AsyncHandles();
	}

	private void OnDestroy()
	{
		Addressables2.ReleaseSafe(_characterArt);

		if (_limbHandles != null)
		{
			_limbHandles.ReleaseAll();
		}
	}



	public async UniTask ChangeCharacter(CharacterEntry entry)
	{
		CharacterAsset asset = entry.asset;
		if(_bust)
			Destroy(_bust.gameObject);

		_character = entry;

		bool inLimbMenu = MenuManager.activeMenus.Contains(Menus.EquipLimb);
		bool inStickerMenu = MenuManager.activeMenus.Contains(Menus.Sticker);

		if (!inLimbMenu && !inStickerMenu)
		{
			if (IMG_Nanokeeper != null)
			{
				IMG_Nanokeeper.gameObject.SetActive(false);
			}

			_characterBust = await Addressables2.LoadHandleAsync(asset.Bust);
			GameObject bust_obj = _characterBust.Result;
			if (bust_obj != null)
			{
				_bust = bust_obj.Instantiate<CharacterBust>(BustRoot);
				_bust.transform.localPosition = Vector3.zero;
				_bust.name = asset.Name;
				_bust.gameObject.SetActive(true);
			}
		}
		else if (inLimbMenu)
		{
			if (IMG_Nanokeeper != null)
			{
				_characterArt = await Addressables2.LoadHandleAsync(asset.Art);
				IMG_Nanokeeper.sprite = _characterArt.Result;
				IMG_Nanokeeper.transform.localPosition = asset.SpliceMenuPosition; //(inLimbMenu ? asset.SpliceMenuPosition : asset.StickerMenuPosition);
				IMG_Nanokeeper.transform.localScale = asset.SpliceMenuScale; //(inLimbMenu ? asset.SpliceMenuScale : asset.StickerMenuScale);
				IMG_Nanokeeper.gameObject.SetActive(true);
			}
		}
	}

	public void ChangeMonster(NanokinInstance instance)
	{
		if (instance != null)
		{
			_nanokin = instance;

			if (PuppetDisplayer != null)
			{
				if (_limbHandles == null) _limbHandles = new AsyncHandles();
				_puppetLimbTree = instance.ToPuppetTree(_limbHandles);
				_puppetState    = new PuppetState(_puppetLimbTree);
				_puppetState.Play("idle");

				PuppetDisplayer.SetPuppet(_puppetState);
			}
		}
		else
		{
			_nanokin = null;
		}

		RefreshUI();
	}

	public void CompareWith(
		Pointf   maxPoints,
		Statf    stats,
		Elementf efficiency)
	{
		_enableCompare = true;

		_maxPointsComparator  = maxPoints;
		_statComparator       = stats;
		_efficiencyComparator = efficiency;

		RefreshUI();
	}

	public void CompareNone()
	{
		_enableCompare = false;
		RefreshUI();
	}

	public void OnLimbChange()
	{
		if (PuppetDisplayer == null) return;

		_puppetLimbTree.SetBody(_nanokin.Body?.Asset, _limbHandles);
		_puppetLimbTree.SetHead(_nanokin.Head?.Asset, _limbHandles);
		_puppetLimbTree.SetArm1(_nanokin.Arm1?.Asset, _limbHandles);
		_puppetLimbTree.SetArm2(_nanokin.Arm2?.Asset, _limbHandles);
		_puppetState.Play("idle");

		RefreshUI();
	}

	public void RefreshUI()
	{
		// ReSharper disable once LocalVariableHidesMember
		string   name       = "N/A";
		Statf    stats      = Statf.Zero;
		Pointf   points     = Pointf.Zero;
		Pointf   maxpoints  = Pointf.Zero;
		Elementf efficiency = Elementf.Zero;

		NanokinInstance nano = _nanokin;
		if (nano != null)
		{
			name       = string.IsNullOrEmpty(_character.NanokinName) ? nano.Name : _character.NanokinName;
			points     = nano.Points;
			maxpoints  = nano.MaxPoints;
			stats      = nano.Stats;
			efficiency = nano.Efficiencies;
		}

		TMP_Name.text = name;
		//TMP_Name.text = _bust.Name;
		//TMP_Name.SetText(_bust.Name);

		if (WithPoints)
		{
			Pointf absolute = points * maxpoints;

			Bar_HP.Set(absolute.hp, maxpoints.hp);
			Bar_SP.Set(absolute.sp, maxpoints.sp);

			// TMP_MaxHP.color = Comparator.GetValueColor(_enableCompare, maxpoints.hp, _maxPointsComparator.hp);
			// TMP_MaxSP.color = Comparator.GetValueColor(_enableCompare, maxpoints.sp, _maxPointsComparator.sp);

			Comparator.UpdateComparatorNumber(_enableCompare, TMP_MaxHPComparator, maxpoints.hp, _maxPointsComparator.hp, ComparisonHideUnchanged);
			Comparator.UpdateComparatorNumber(_enableCompare, TMP_MaxSPComparator, maxpoints.sp, _maxPointsComparator.sp, ComparisonHideUnchanged);
		}

		if (WithStats)
		{
			Comparator.UpdateNumber(false, TMP_Power, stats.power, _statComparator.power, false);
			Comparator.UpdateNumber(false, TMP_Speed, stats.speed, _statComparator.speed, false);
			Comparator.UpdateNumber(false, TMP_Will, stats.will, _statComparator.will, false);
			Comparator.UpdateNumber(false, TMP_AP, stats.ap, _statComparator.ap);

			Comparator.UpdateComparatorNumber(_enableCompare, TMP_PowerComparator, stats.power, _statComparator.power, ComparisonHideUnchanged);
			Comparator.UpdateComparatorNumber(_enableCompare, TMP_SpeedComparator, stats.speed, _statComparator.speed, ComparisonHideUnchanged);
			Comparator.UpdateComparatorNumber(_enableCompare, TMP_WillComparator, stats.will, _statComparator.will, ComparisonHideUnchanged);
			Comparator.UpdateComparatorNumber(_enableCompare, TMP_APComparator, stats.ap, _statComparator.ap, ComparisonHideUnchanged);
		}

		if (WithEfficiencies)
		{
			//Comparator.UpdatePercent(false, TMP_Blunt, efficiency.blunt, _efficiencyComparator.blunt, false);
			//Comparator.UpdatePercent(false, TMP_Slash, efficiency.slash, _efficiencyComparator.slash, false);
			//Comparator.UpdatePercent(false, TMP_Pierce, efficiency.pierce, _efficiencyComparator.pierce, false);
			//Comparator.UpdatePercent(false, TMP_Oida, efficiency.oida, _efficiencyComparator.oida, false);
			//Comparator.UpdatePercent(false, TMP_Astra, efficiency.astra, _efficiencyComparator.astra, false);
			//Comparator.UpdatePercent(false, TMP_Gaia, efficiency.gaia, _efficiencyComparator.gaia, false);
			Comparator.UpdateBracket(false, TMP_Blunt, efficiency.blunt, _efficiencyComparator.blunt, false);
			Comparator.UpdateBracket(false, TMP_Slash, efficiency.slash, _efficiencyComparator.slash, false);
			Comparator.UpdateBracket(false, TMP_Pierce, efficiency.pierce, _efficiencyComparator.pierce, false);
			Comparator.UpdateBracket(false, TMP_Oida, efficiency.oida, _efficiencyComparator.oida, false);
			Comparator.UpdateBracket(false, TMP_Astra, efficiency.astra, _efficiencyComparator.astra, false);
			Comparator.UpdateBracket(false, TMP_Gaia, efficiency.gaia, _efficiencyComparator.gaia, false);

			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_BluntComparator, efficiency.blunt, _efficiencyComparator.blunt, ComparisonHideUnchanged);
			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_SlashComparator, efficiency.slash, _efficiencyComparator.slash, ComparisonHideUnchanged);
			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_PierceComparator, efficiency.pierce, _efficiencyComparator.pierce, ComparisonHideUnchanged);
			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_OidaComparator, efficiency.oida, _efficiencyComparator.oida, ComparisonHideUnchanged);
			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_AstraComparator, efficiency.astra, _efficiencyComparator.astra, ComparisonHideUnchanged);
			//Comparator.UpdateComparatorPercent(_enableCompare, TMP_GaiaComparator, efficiency.gaia, _efficiencyComparator.gaia, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_BluntComparator, efficiency.blunt, _efficiencyComparator.blunt, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_SlashComparator, efficiency.slash, _efficiencyComparator.slash, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_PierceComparator, efficiency.pierce, _efficiencyComparator.pierce, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_OidaComparator, efficiency.oida, _efficiencyComparator.oida, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_AstraComparator, efficiency.astra, _efficiencyComparator.astra, ComparisonHideUnchanged);
			Comparator.UpdateComparatorBracket(_enableCompare, TMP_GaiaComparator, efficiency.gaia, _efficiencyComparator.gaia, ComparisonHideUnchanged);
		}
	}




}