using System;
using Combat.Toolkit;
using Combat.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

[Serializable]
public class BattleConfig : SerializedScriptableObject
{
	[SerializeField] public OverdriveAnim.Settings Overdrive;

	// TODO move much of this to addressable usage

	[Title("Stats")]
	public DamageNumber miss;
	public DamageNumber healNumber;
	public DamageNumber healNumberSP;
	public DamageNumber damageNumberPure;
	public DamageNumber damageNumberBlunt;
	public DamageNumber damageNumberSlash;
	public DamageNumber damageNumberPierce;
	public DamageNumber damageNumberOida;
	public DamageNumber damageNumberGaia;
	public DamageNumber damageNumberAstra;
	public EfficiencyText efficiencyText;

	[Title("Player Team Controller")]
	public BattleTriangleMenuConfig playerTriangleMenu;
	public Texture2D texAct;
	public Texture2D texMove;
	public Texture2D texHold;
	public Texture2D texFlee;
	[Space]
	public AudioDef adMenuCancel;
	public AudioDef   adMenuConfirm;
	public AudioDef   adMenuError;
	public AudioDef   adMenuConfirmFinal;
	public AudioDef   adMenuScroll;
	public AudioDef   adMenuScrollActCategory;
	public AudioDef   adOverdriveGrow;
	public AudioDef   adOverdriveShrink;
	public AudioDef   adOverdrivePush;
	public AudioDef   adOverdrivePop;
	public AudioDef   adOverdriveActivate;
	public FloatRange adOverdriveEnablePitch = new FloatRange(1f, 2f);

	[Title("Victory")]
	public SceneReference VictoryScene;
	public AudioClip VictoryFanfare;
	public AudioClip VictoryMusic;
	public float     VictoryFadeOut;
	public float     VictoryFadeIn;
	public float     VictoryMusicFadeOut = 0.6f;

	[Title("Timing")]
	public bool EnableUniversalSkillDelay = true;
	[ShowIf("EnableUniversalSkillDelay")][Range(1, 30)]
	public int UniversalSkillDelayFrames = 6;

	[Serializable]
	public struct BattleTriangleMenuConfig
	{
		public SceneReference triangleMenuScene;

		[Required, AssetsOnly] public GameObject pfbAction;
		[Required, AssetsOnly] public GameObject pfbSkillCategory;
		[Required, AssetsOnly] public GameObject pfbSkill;
	}
}