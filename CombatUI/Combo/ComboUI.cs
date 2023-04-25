using System;
using Anjin.Nanokin;
using Anjin.Utils;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Overworld.Cutscenes;
using Overworld.Shopping;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Util;
using Util.UniTween.Value;

namespace Combat.UI
{
	public class ComboUI : StaticBoy<ComboUI>
	{
		private const string SCENE_ADDR = Addresses.Scene_UI_Overworld;

		public GameObject LabelRoot;
		public GameObject LabelPrefab;

		[SerializeField] private float                FixedPointToShow = 2;
		[SerializeField] private EaserTo              _fadeIn;
		[SerializeField] private EaserTo              _fadeOut;
		[OdinSerialize]  private ComboLabel.Animation LabelAnimation_Updating;
		[OdinSerialize]  private ComboLabel.Animation LabelAnimation_Exiting;

		private static GObjectPool _labelPool;
		private static ComboLabel  _currentLabel;

		private void OnDestroy()
		{
			_labelPool = null;
		}

		protected override void OnAwake()
		{
			_labelPool = new GObjectPool(transform)
			{
				prefab       = LabelPrefab,
				allocateTemp = true,
				initSize     = GameOptions.current.pool_on_demand ? 0 : 4,
			};
			CutsceneTrigger.OnEnterCutscene            += HideCombo;
			TextDisplaySequencer.OnStartTextSequenceUI += HideCombo;
			BattleRunner.onStartBattleSequence           += HideCombo;
			ShopMenu.OnShopMenuDisplayUI               += HideCombo;
		}

		public static void StartCombo()
		{
			_currentLabel = _labelPool.Rent<ComboLabel>();
			_currentLabel.Reset();
			_currentLabel.color.FromTo(Color.clear, Color.white, Live._fadeIn);
		}

		[ShowInInspector]
		public static async UniTask UpdateCombo(float newvalue)
		{
			await SceneLoader.GetOrLoadAsync(SCENE_ADDR);

			ComboLabel oldLabel = _currentLabel;

			if (oldLabel)
				oldLabel.Play(Live.LabelAnimation_Exiting).OnComplete(() => ReleaseLabel(oldLabel));

			if (_labelPool == null)
			{
				_labelPool = new GObjectPool(Live.transform)
				{
					prefab = Live.LabelPrefab,
					allocateTemp = true,
					initSize = GameOptions.current.pool_on_demand ? 0 : 4,
				};
			}

			_currentLabel            = _labelPool.Rent<ComboLabel>();
			_currentLabel.Label.text = $"{newvalue.ToString($"F{Live.FixedPointToShow}")}x";
			_currentLabel.Reset();

			// Show the updating of the label
			_currentLabel.Play(Live.LabelAnimation_Updating);
		}

		[ShowInInspector]
		public static async UniTask EndCombo()
		{
			if (_currentLabel == null)
				// Nothing to fade out...
				return;

			await SceneLoader.GetOrLoadAsync(SCENE_ADDR);

			ComboLabel label = _currentLabel;
			label.color.To(Color.clear, Live._fadeOut).OnComplete(() => ReleaseLabel(label));
		}

		public static void SetVisible(bool b)
		{
			if (_currentLabel == null)
				return;
			if (!b)
			{
				ReleaseLabel(_currentLabel);
				return;
			}
			StartCombo();
		}

		private void HideCombo()
		{
			SetVisible(false);
		}

		/// <summary>
		/// Release the label so it can be re-used by the pool.
		/// </summary>
		/// <param name="label"></param>
		private static void ReleaseLabel(ComboLabel label)
		{
			if (_currentLabel == label)
				_currentLabel = null;

			_labelPool.Return(label);
		}
	}
}