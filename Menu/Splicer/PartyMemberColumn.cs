using System;
using System.Globalization;
using Anjin.UI;
using Anjin.Util;
using API.Puppets.Components;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using Puppets;
using SaveFiles;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util.Addressable;
using Util.RenderingElements.PointBars;

/// <summary>
/// A column which displays a monster's stats
/// </summary>
public class PartyMemberColumn : SelectableExtended<PartyMemberColumn>
{
	public event Action<PartyMemberColumn> OnNameClick;

	[SerializeField] private Image PartyImage;
	[SerializeField] private PuppetUGUI Puppet;

	[Space]
	[SerializeField] public PointBar Bar_HP;

	[SerializeField] public PointBar Bar_SP;

	[Space]
	[SerializeField] public TextMeshProUGUI TMP_NanokinName;

	[SerializeField] public TextMeshProUGUI TMP_Power;
	[SerializeField] public TextMeshProUGUI TMP_Speed;
	[SerializeField] public TextMeshProUGUI TMP_Willpower;
	[SerializeField] public TextMeshProUGUI TMP_AP;
	[SerializeField] public Transform BustRoot;

	[Space]
	[SerializeField] public Transform LevelIndicator;

	[SerializeField] public Slider TMP_XP;
	[SerializeField] public TextMeshProUGUI TMP_Level;
	[SerializeField] public TextMeshProUGUI TMP_NextLevel;

	[NonSerialized] public CharacterEntry character;
	[NonSerialized] public AsyncHandles _handles;


	private AsyncOperationHandle<Sprite> _characterArt;
	private AsyncOperationHandle<GameObject> _characterBust;

	private CharacterBust _bust;
	protected override PartyMemberColumn Myself => this;

	protected override void Awake()
	{
		base.Awake();
		_handles = new AsyncHandles();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_handles.ReleaseAll();
	}

	public void ResetHandles()
	{
		_handles = new AsyncHandles();
	}

	/*private void NameClickEvent(string _)
	{
		OnNameClick?.Invoke(this);
		TMP_NanokinName.gameObject.SetActive(false);
	}*/

	public async UniTask SetCharacter(CharacterEntry kid)
	{
		if (character == kid) return;

		if (_bust != null)
		{
			_bust.gameObject.Destroy();
			_bust = null;
		}

		Addressables2.ReleaseSafe(_characterBust);

		character = kid;

		_characterBust = await Addressables2.LoadHandleAsync(kid.asset.Bust);
		GameObject bust_obj = _characterBust.Result;
		GameObject ret = Instantiate(bust_obj, BustRoot);

		_bust = ret.GetComponent<CharacterBust>();
		_bust.transform.localPosition = Vector3.zero;
		_bust.name = kid.asset.Name;
		_bust.gameObject.SetActive(true);

		var tree = NanokinLimbTree.WithAddressable(character.nanokin, _handles);
		var puppet = new PuppetState(tree);

		await puppet.AwaitLoading();

		puppet.Play("idle");
		Puppet.SetPuppet(puppet);

		RefreshUI();
	}

	public void RefreshUI()
	{
		NanokinInstance monster = character.nanokin;
		monster.RecalculateStats();

		// Points
		Pointf pt = monster.Points;
		Pointf max = monster.MaxPoints;

		Bar_HP.Set(pt.hp * max.hp, max.hp);
		Bar_SP.Set(pt.sp * max.sp, max.sp);

		// Stats
		// TMP_NanokinName.text = _bust.Name;
		var defaultNanoName = monster.Body.Asset.DisplayName;
		var actualNanoName = monster.entry.NanokinName;
		TMP_NanokinName.text = string.IsNullOrEmpty(actualNanoName) ? defaultNanoName : actualNanoName;
		TMP_Power.text = monster.Stats.power.ToString(CultureInfo.InvariantCulture);
		TMP_Speed.text = monster.Stats.speed.ToString(CultureInfo.InvariantCulture);
		TMP_Willpower.text = monster.Stats.will.ToString(CultureInfo.InvariantCulture);
		TMP_AP.text = monster.Stats.ap.ToString(CultureInfo.InvariantCulture);
		TMP_Level.text = character.Level.ToString(CultureInfo.InvariantCulture);
		TMP_NextLevel.text = (character.Level + 1).ToString(CultureInfo.InvariantCulture);
		TMP_XP.value = character.NextLevelProgress;
	}

	public async UniTask RefreshPuppet()
	{
		var tree = NanokinLimbTree.WithAddressable(character.nanokin, _handles);
		var puppet = new PuppetState(tree);
		await puppet.AwaitLoading();

		puppet.Play("idle");
		Puppet.SetPuppet(puppet);

		RefreshUI();
	}

	public void RenameMonster(string newName)
	{
		if (character.asset == null)
		{
			Dbg.LogError("Rename failed. Character does not exist.", LogContext.UI, LogPriority.Critical);
			return;
		}

		character.NanokinName = newName;
		if (newName.Length > 16)
		{
			character.NanokinName = newName.Substring(0, 16);
		}

		RefreshUI();
	}
}