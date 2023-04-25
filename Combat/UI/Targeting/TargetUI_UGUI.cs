using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Util.Odin.Attributes;

namespace Combat.UI
{
	public class TargetUI_UGUI : SerializedMonoBehaviour
	{
		[ShowInPlay] public static Sprite Icon_Space_Blank;
		[ShowInPlay] public static Sprite Icon_Space_Target_Ally;
		[ShowInPlay] public static Sprite Icon_Space_Target_Ally_Travel;
		[ShowInPlay] public static Sprite Icon_Space_Target_Enemy;
		[ShowInPlay] public static Sprite Icon_Space_Target_Enemy_Travel;

		[ShowInPlay] public static Sprite Icon_Epicenter;
		[ShowInPlay] public static Sprite Icon_FreeTarget;
		[ShowInPlay] public static Sprite Icon_Move1;
		[ShowInPlay] public static Sprite Icon_Move2;
		[ShowInPlay] public static Sprite Icon_Teleport;

		[ShowInPlay] public static Font   Font_Indicator;

		public GridLayoutGroup  Grid;
		public GridLayoutScaler Scaler;
		public CanvasGroup      Group;

		[NonSerialized] public List<Cell>  cells;
		[NonSerialized] public TargetUILua targeting;
		[NonSerialized]
		[OnValueChanged("Refresh")]
		public SkillAsset asset;

		private int  _width;
		private int  _height;
		private bool _init;

		private async void Awake()
		{
			await Init();
			Refresh();
		}

		private async UniTask Init()
		{
			if (_init) return;
			_init = true;
			cells = new List<Cell>();

			await GameController.TillIntialized();
			//await Addressables.InitializeAsync();
			//await Lua.initTask;

			Icon_Space_Blank               = await Addressables.LoadAssetAsync<Sprite>("Targeting/Space_Blank");
			Icon_Space_Target_Ally         = await Addressables.LoadAssetAsync<Sprite>("Targeting/Space_Target_Ally");
			Icon_Space_Target_Ally_Travel  = await Addressables.LoadAssetAsync<Sprite>("Targeting/Space_Target_Ally_Travel");
			Icon_Space_Target_Enemy        = await Addressables.LoadAssetAsync<Sprite>("Targeting/Space_Target_Enemy");
			Icon_Space_Target_Enemy_Travel = await Addressables.LoadAssetAsync<Sprite>("Targeting/Space_Target_Enemy_Travel");
			Font_Indicator				   = await Addressables.LoadAssetAsync<Font>("Fonts & Materials/Square");

			Icon_Epicenter  = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Epicenter");
			Icon_FreeTarget = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_FreeTarget");
			Icon_Move1      = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Move_One");
			Icon_Move2      = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Move_Two");
			Icon_Teleport   = await Addressables.LoadAssetAsync<Sprite>("Targeting/Special_Teleport");
		}

		[Button]
		public void Refresh()
		{
			if (!Application.isPlaying) return;

			targeting = null;

			if (asset != null)
			{
				targeting = new TargetUILua();
				SkillAsset.EvaluatedInfo info = asset.EvaluateInfo(targeting);
			}

			RefreshUI().Forget();
		}

		[Button]
		public async UniTaskVoid RefreshUI()
		{
			await Init();

			foreach (Cell obj in cells)
			{
				obj.Free();
			}

			cells.Clear();

			_width  = 0;
			_height = 0;

			if (targeting == null || targeting.Bounds.magnitude == 0)
				return;

			Group.alpha = 0;

			Grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
			Grid.constraintCount = targeting.Bounds.x;

			_width  = targeting.Bounds.x;
			_height = targeting.Bounds.y;

			for (int y = 0; y < _height; y++)
			{
				for (int x = 0; x < _width; x++)
				{
					Cell cell = new Cell
					{
						Root     = Grid.gameObject.AddChild($"Slot ({x}, {y})").AddComponent<RectTransform>(),
						Children = ListPool<Image>.Claim(),
						Indicators = ListPool<Text>.Claim()
					};
					cells.Add(cell);
				}
			}

			for (int y = 0; y < targeting.Grid.size.y; y++)
			{
				for (int x = 0; x < targeting.Grid.size.x; x++)
				{
					Cell cell = GetCell(targeting.Grid.offset.x + x, targeting.Grid.offset.y + y);
					cell.AddSprite(GetSpriteForSlot(targeting.Grid.slots[x, y]));
				}
			}

			for (int i = 0; i < targeting.Grid.icons.Count; i++)
			{
				TargetUIIcon icon = targeting.Grid.icons[i];
				Cell         cell = GetCell(icon.x, icon.y);

				Sprite spr = null;

				switch (icon.type)
				{
					case TargetUIIconType.Epicenter:
						spr = Icon_Epicenter;
						break;
					case TargetUIIconType.Free:
						spr = Icon_FreeTarget;
						break;
					case TargetUIIconType.Move1:
						spr = Icon_Move1;
						break;
					case TargetUIIconType.Move2:
						spr = Icon_Move2;
						break;
					case TargetUIIconType.Teleport:
						spr = Icon_Teleport;
						break;
					case TargetUIIconType.Indicator:
						spr = null;
						break;
				}

				if (spr != null)
					cell.AddSprite(spr);

				if (!string.IsNullOrEmpty(icon.indicator))
					cell.AddText(icon.indicator);
			}

			await UniTask.DelayFrame(1);

			Scaler.Fix();

			await UniTask.DelayFrame(1);

			foreach (Cell cell in cells)
			{
				foreach (var child in cell.Children)
				{
					child.rectTransform.anchorMin = Vector2.zero;
					child.rectTransform.anchorMax = Vector2.one;
					child.rectTransform.pivot     = Vector2.one * 0.5f;

					child.rectTransform.sizeDelta = Vector2.zero;
				}
			}

			Group.alpha = 1;
		}

		private Cell GetCell(int x, int y) => cells[y * _width + x];

		private Sprite GetSpriteForSlot(TargetUISlot slot)
		{
			if (slot.highlight != TargetUIHighlight.Empty)
			{
				if (slot.team == TargetUITeam.Opponent)
				{
					if (slot.highlight == TargetUIHighlight.Half)
					{
						return Icon_Space_Target_Enemy_Travel;
					}
					else
					{
						return Icon_Space_Target_Enemy;
					}
				}
				else
				{
					if (slot.highlight == TargetUIHighlight.Half)
					{
						return Icon_Space_Target_Ally_Travel;
					}
					else
					{
						return Icon_Space_Target_Ally;
					}
				}
			}

			return Icon_Space_Blank;
		}

		public struct Cell
		{
			public RectTransform Root;
			public List<Image>   Children;
			public List<Text>	 Indicators;

			public void Free()
			{
				if (Root != null)
					Destroy(Root.gameObject);

				ListPool<Image>.Release(Children);
				ListPool<Text>.Release(Indicators);
			}

			public void AddSprite(Sprite spr)
			{
				RectTransform rt    = Root.gameObject.AddChild("Grid Cell").AddComponent<RectTransform>();
				Image         image = rt.AddComponent<Image>();
				image.sprite = spr;
				image.SetNativeSize();
				Children.Add(image);
			}

			public void AddText(string str)
			{
				RectTransform rt = Root.gameObject.AddChild("Grid Cell").AddComponent<RectTransform>();
				Text text = rt.AddComponent<Text>();
				text.font = Font_Indicator;
				text.color = Color.white;
				text.text = str;
				Indicators.Add(text);
			}
		}
	}
}