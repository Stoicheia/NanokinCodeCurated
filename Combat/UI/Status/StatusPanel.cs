using System;
using Anjin.EditorUtility;
using Anjin.Util;
using Anjin.Utils;
using Combat.Components;
using Combat.Data.VFXs;
using Combat.Entities;
using Combat.Entry;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Data.Nanokin;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;
using Util.Addressable;

namespace Combat.UI
{
	public class StatusPanel : MonoBehaviour
	{
		[SerializeField] private Image             Image;
		[SerializeField] private Image             Portrait;
		[SerializeField] private Image             PortraitShadow; // BUG the portrait shadow's color affects all the graphics!
		[SerializeField] private RectTransform     TransformableRoot;
		[SerializeField] private TextMeshProMulti  TMP_Name;
		[SerializeField] private TextMeshProUGUI[] TMP_HP;
		[SerializeField] private TextMeshProUGUI[] TMP_SP;
		[SerializeField] private Graphic[]         Graphics;

		[Title("Damping")]
		[SerializeField] private float PercentDamping = 8;
		[SerializeField] private float PoisonAppearDamping  = 3;
		[SerializeField] private float PoisonExitDamping    = 8;
		[SerializeField] private float CorruptAppearDamping = 3;
		[SerializeField] private float CorruptExitDamping   = 8;
		[SerializeField] private float DrainAppearDamping   = 3;
		[SerializeField] private float DrainExitDamping     = 8;
		[SerializeField] private float HurtExitDamping      = 3;
		[SerializeField] private float BrightnessDamping    = 3;

		[Title("Colors")]
		public ODColor OpBarBg;
		public ODColor OpBar;
		public ODColor OpBarTick;
		public ODColor OpBarSeg1;
		public ODColor OpBarSeg2;

		[Title("Hurt FX")]
		public float HurtShakeAmplitude = 50;

		// State
		// ----------------------------------------

		[NonSerialized] public SelectUIObject selectState = SelectUIObject.Initial;
		[NonSerialized] public string         name;
		[NonSerialized] public bool           poisoned;
		[NonSerialized] public bool           corrupted;
		[NonSerialized] public bool           hpdraining;
		[NonSerialized] public bool           spdraining;
		[NonSerialized] public ODState        odstate;
		[NonSerialized] public int            overdriveActionNum;
		[NonSerialized] public int            overdriveSlotNum;
		[NonSerialized] public int            overdriveSlotInverse;
		[NonSerialized] public VFXManager     vfxman;
		[NonSerialized] public bool           hurting;

		private AsyncOperationHandle<Sprite> _portrait;
		private bool                         _initialized;

		private Color[]  _baseColors;
		private Material _mat;
		private Pointf   _renderedValues;
		private Pointf   _targetValues;
		private Pointf   _maxValues;
		private int      _odSegments1;
		private int      _odSegments2Offset;
		private int      _odSegments2;

		private Fighter      _fighter;
		private TimeScalable _fighterTimescale;
		private float        _effPoison;
		private float        _effCorrupt;
		private float        _effDrainhp;
		private float        _effDrainsp;
		private float        _hurt;
		private ReactVFX     _hurtvfxReact;
		private ShakeVFX     _hurtvfxShake;

		public static readonly int spPercentHP          = Shader.PropertyToID("_PercentHP");
		public static readonly int spPercentSP          = Shader.PropertyToID("_PercentSP");
		public static readonly int spOverdrivePoints    = Shader.PropertyToID("_OverdrivePoints");
		public static readonly int spOverdrivePointsMax = Shader.PropertyToID("_OverdrivePointsMax");
		public static readonly int spOverdriveFill1     = Shader.PropertyToID("_OverdriveFill1");
		public static readonly int spOverdriveFill2Offset     = Shader.PropertyToID("_OverdriveFill2Offset");
		public static readonly int spOverdriveFill2     = Shader.PropertyToID("_OverdriveFill2");
		public static readonly int spColorOPBar         = Shader.PropertyToID("_ColorOPBar");
		public static readonly int spColorOPSeg1        = Shader.PropertyToID("_ColorOPSeg1");
		public static readonly int spColorOPSeg2        = Shader.PropertyToID("_ColorOPSeg2");
		public static readonly int spColorOPTick        = Shader.PropertyToID("_ColorOPTick");
		public static readonly int spColorOPBG          = Shader.PropertyToID("_ColorOPBG");
		public static readonly int spFillColor          = Shader.PropertyToID("_FillColor");
		public static readonly int spHurtFx             = Shader.PropertyToID("_HurtEffect");
		public static readonly int spPoisonFx           = Shader.PropertyToID("_PoisonEffect");
		public static readonly int spCorruptFx          = Shader.PropertyToID("_CorruptEffect");
		public static readonly int spSPDrainFx          = Shader.PropertyToID("_SPDrainEffect");

		public bool UsingCharacterEntry = false;

		public Fighter Fighter => _fighter;

		public CharacterEntry Entry;

		public enum ODState { Normal, Entry }

		[Serializable]
		public struct ODFloat
		{
			public float Normal;
			public float Entry;

			public float Get(ODState state)
			{
				switch (state)
				{
					case ODState.Normal: return Normal;
					case ODState.Entry:  return Entry;
					default:             throw new ArgumentOutOfRangeException(nameof(state), state, null);
				}
			}
		}


		[Serializable]
		public struct ODColor
		{
			public Color Normal;
			public Color Entry;
			public float Damping;

			[NonSerialized]
			public Color current;


			public Color Get(ODState state)
			{
				switch (state)
				{
					case ODState.Normal: return Normal;
					case ODState.Entry:  return Entry;
					default:             throw new ArgumentOutOfRangeException(nameof(state), state, null);
				}
			}

			public void Update(ODState state)
			{
				Color target = Normal;

				switch (state)
				{
					case ODState.Normal:
						target = Normal;
						break;

					case ODState.Entry:
						target = Entry;
						break;
				}

				current = current.LerpDamp(target, Damping);
			}
		}

		private void Awake()
		{
			if (_initialized) return;
			_initialized = true;

			selectState.brightness = 1;

			vfxman = gameObject.AddComponent<VFXManager>();

			_mat           = Instantiate(Image.material);
			Image.material = _mat;

			_baseColors = new Color[Graphics.Length];
			for (var i = 0; i < Graphics.Length; i++)
			{
				_baseColors[i] = Graphics[i].color;
			}
		}

		private void OnDestroy()
		{
			Addressables2.ReleaseSafe(_portrait);
		}

		public async UniTask ChangeFighter(Fighter fighter)
		{
			Awake();

			_fighter = fighter;

			_targetValues   = Fighter.points;
			_renderedValues = Fighter.points;
			_maxValues      = Fighter.info.Points;

			TMP_Name.Text = Fighter.info.Name;

			Portrait.gameObject.SetActive(false);
			PortraitShadow.gameObject.SetActive(false);

			if (fighter.info is NanokinInfo nanokin && nanokin.instance.entry != null)
			{
				CharacterAsset asset = nanokin.instance.entry.asset;
				if (asset != null)
				{
					_portrait = await Addressables2.LoadHandleAsync(asset.StatBarPortrait);

					if (_portrait.Result != null)
					{
						Portrait.sprite = _portrait.Result;
						Portrait.SetNativeSize();
						Portrait.gameObject.SetActive(true);

						PortraitShadow.sprite = _portrait.Result;
						PortraitShadow.SetNativeSize();
						PortraitShadow.gameObject.SetActive(true);
					}
				}
			}


			_fighterTimescale = fighter.actor.timescale;

			UpdateFrontend(true, true);
		}

		public async UniTask SetToCharacterEntry(CharacterEntry entry)
		{
			Awake();

			UsingCharacterEntry = true;
			Entry               = entry;

			UpdateInternalValues(Entry.nanokin);
			SnapToEnd();

			TMP_Name.Text = entry.asset.name;

			Portrait.gameObject.SetActive(false);
			PortraitShadow.gameObject.SetActive(false);

			if (entry.asset != null)
			{
				_portrait = await Addressables2.LoadHandleAsync(entry.asset.StatBarPortrait);

				if (_portrait.Result != null)
				{
					Portrait.sprite = _portrait.Result;
					Portrait.SetNativeSize();
					Portrait.gameObject.SetActive(true);

					PortraitShadow.sprite = _portrait.Result;
					PortraitShadow.SetNativeSize();
					PortraitShadow.gameObject.SetActive(true);
				}
			}

			_fighterTimescale = null;

			UpdateFrontend(true, true);
		}

		public void Hurt()
		{
			if (hurting)
				StopHurting();

			hurting = true;

			vfxman.Add(_hurtvfxReact = new ReactVFX
			{
				flashFrames = 7,
				shake       = false,
				recoil      = false
			});

			vfxman.Add(_hurtvfxShake = new ShakeVFX
			{
				amplitude = HurtShakeAmplitude,
			});

			_hurt = 1;
			UpdateFrontend();
		}

		private void StopHurting()
		{
			hurting = false;
			_hurtvfxShake.Leave();
		}

		public void SnapToEnd()
		{
			_renderedValues = _targetValues;
		}

		public void Update()
		{
			Awake();

			// Update values
			// ----------------------------------------

			if (UsingCharacterEntry)
			{
				if (Entry == null) return;

				UpdateInternalValues(Entry.nanokin);
			}
			else
			{
				if (Fighter == null) return;
				_targetValues = Fighter.points;
				_maxValues    = Fighter.info.Points;
			}


			float timeScale = 1;
			if (_fighterTimescale != null)
			{
				timeScale = _fighterTimescale.current;
			}


			switch (odstate)
			{
				case ODState.Normal:
					_odSegments1 = _renderedValues.op.Floor();
					_odSegments2Offset = 0;
					_odSegments2 = 0;
					break;

				case ODState.Entry:
					_odSegments1 = overdriveSlotNum;
					_odSegments2Offset = overdriveSlotInverse;
					_odSegments2 = overdriveActionNum;
					break;
			}

			if (hurting)
			{
				_hurtvfxShake.amplitude = HurtShakeAmplitude * (1 - timeScale);
				_hurtvfxReact.OnTimeScaleChanged(timeScale);
				_hurtvfxShake.OnTimeScaleChanged(timeScale);

				if (Math.Abs(_renderedValues.hp - _targetValues.hp) < 0.1f &&
				    Math.Abs(timeScale - 1f) < 0.05f)
				{
					StopHurting();
				}
			}


			// Lerp values for smooth transitions
			// ----------------------------------------

			int lastHP = Mathf.FloorToInt(_renderedValues.hp);
			int lastSP = Mathf.FloorToInt(_renderedValues.sp);

			// Lerp points  (smooth bar movements)
			_renderedValues.hp = MathUtil.LerpDamp(_renderedValues.hp, _targetValues.hp, PercentDamping, timeScale);
			_renderedValues.sp = MathUtil.LerpDamp(_renderedValues.sp, _targetValues.sp, PercentDamping, timeScale);
			_renderedValues.op = MathUtil.LerpDamp(_renderedValues.op, _targetValues.op, PercentDamping, timeScale);


			// Snap points
			const float POINT_SNAP_THRESHOLD = 0.1f;

			if (Mathf.Abs(_targetValues.hp - _renderedValues.hp) < POINT_SNAP_THRESHOLD) _renderedValues.hp = _targetValues.hp;
			if (Mathf.Abs(_targetValues.sp - _renderedValues.sp) < POINT_SNAP_THRESHOLD) _renderedValues.sp = _targetValues.sp;
			if (Mathf.Abs(_targetValues.op - _renderedValues.op) < POINT_SNAP_THRESHOLD) _renderedValues.op = _targetValues.op;

			// Lerp effects for smooth transition
			_hurt = _hurt.LerpDamp(
				hurting ? 1 : 0,
				hurting ? 1 : HurtExitDamping
			);

			_effPoison = _effPoison.LerpDamp(
				poisoned ? 1 : 0,
				poisoned ? PoisonAppearDamping : PoisonExitDamping);

			_effCorrupt = _effCorrupt.LerpDamp(
				corrupted ? 1 : 0,
				corrupted ? CorruptAppearDamping : CorruptExitDamping);

			_effDrainhp = _effDrainhp.LerpDamp(
				hpdraining ? 1 : 0,
				hpdraining ? DrainAppearDamping : DrainExitDamping);

			_effDrainsp = _effDrainsp.LerpDamp(
				spdraining ? 1 : 0,
				spdraining ? DrainAppearDamping : DrainExitDamping);

			OpBarBg.Update(odstate);
			OpBar.Update(odstate);
			OpBarTick.Update(odstate);
			OpBarSeg1.Update(odstate);
			OpBarSeg2.Update(odstate);

			// Apply visuals from state
			// ----------------------------------------

			int newHP = Mathf.FloorToInt(_renderedValues.hp);
			int newSP = Mathf.FloorToInt(_renderedValues.sp);

			UpdateFrontend(
				lastHP != newHP,
				lastSP != newSP);
		}

		private void UpdateInternalValues(NanokinInstance nano)
		{
			_targetValues.hp = nano.Points.hp * nano.MaxPoints.hp;

			if (_targetValues.hp < 1)
			{
				_targetValues.hp = 1;
			}

			_targetValues.sp = nano.Points.sp * nano.MaxPoints.sp;
			_targetValues.op = nano.Points.op;

			_maxValues = nano.MaxPoints;
		}

		private void UpdateFrontend(bool hptext = false, bool sptext = false)
		{
			// Labels
			if (hptext) TMP_HP.SetNumberMulti(Mathf.FloorToInt(_renderedValues.hp));
			if (sptext) TMP_SP.SetNumberMulti(Mathf.FloorToInt(_renderedValues.sp));

			// Material
			_mat.SetFloat(spPercentHP, _renderedValues.hp / _maxValues.hp);
			_mat.SetFloat(spPercentSP, _renderedValues.sp / _maxValues.sp);

			_mat.SetFloat(spOverdrivePoints, odstate == ODState.Normal ? _renderedValues.op : Mathf.Floor(_targetValues.op));
			_mat.SetFloat(spOverdrivePointsMax, Mathf.Floor(_maxValues.op));
			_mat.SetFloat(spOverdriveFill1, _odSegments1 / _maxValues.op);
			_mat.SetFloat(spOverdriveFill2Offset, _odSegments2Offset / _maxValues.op);
			_mat.SetFloat(spOverdriveFill2, _odSegments2 / _maxValues.op);

			_mat.SetColor(spColorOPBG, OpBarBg.current);
			_mat.SetColor(spColorOPBar, OpBar.current);
			_mat.SetColor(spColorOPTick, OpBarTick.current);
			_mat.SetColor(spColorOPSeg1, OpBarSeg1.current);
			_mat.SetColor(spColorOPSeg2, OpBarSeg2.current);
			_mat.SetColor(spFillColor, vfxman.state.fill);

			TransformableRoot.anchoredPosition = vfxman.state.offset;

			_mat.SetFloat(spHurtFx, _hurt);
			_mat.SetFloat(spPoisonFx, _effPoison);
			_mat.SetFloat(spCorruptFx, _effCorrupt);
			_mat.SetFloat(spSPDrainFx, _effDrainsp);

			for (var i = 0; i < Graphics.Length; i++)
			{
				var target = Color.Lerp(Color.black, _baseColors[i] * vfxman.state.tint, selectState.brightness);
				Graphics[i].color = Graphics[i].color.LerpDamp(target, BrightnessDamping);
			}
		}
	}
}