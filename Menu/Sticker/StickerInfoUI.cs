using System;
using System.Text;
using Anjin.Util;
using JetBrains.Annotations;
using Pathfinding.Util;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Menu.Sticker
{
	/// <summary>
	/// A display which shows the sticker's name and description
	/// </summary>
	public class StickerInfoUI : SerializedMonoBehaviour
	{
		[Title("References")]
		[SerializeField] private TextMeshProUGUI TitleLabel;
		[SerializeField] private TextMeshProUGUI DescriptionLabel;
		[SerializeField] private TextMeshProUGUI EquipmentLabel;

		[Title("Design")]
		[SerializeField] private Color GainColor;
		[SerializeField] private Color LossColor;
		[Space]
		[SerializeField] private Color HpColor;
		[SerializeField] private Color SpColor;
		[Space]
		[SerializeField] private Color PowerColor;
		[SerializeField] private Color SpeedColor;
		[SerializeField] private Color WillColor;
		[Space]
		[SerializeField] private Color BluntColor;
		[SerializeField] private Color SlashColor;
		[SerializeField] private Color PierceColor;
		[SerializeField] private Color GaiaColor;
		[SerializeField] private Color AstraColor;
		[SerializeField] private Color OidaColor;

		[NonSerialized] public new RectTransform transform;

		private StickerInstance _sticker;
		private StickerAsset    _asset;
		private string          _gainColorHex, _lossColorHex;

		private void Awake()
		{
			transform = GetComponent<RectTransform>();

			_gainColorHex = ColorUtility.ToHtmlStringRGBA(GainColor);
			_lossColorHex = ColorUtility.ToHtmlStringRGBA(LossColor);

			SetNone();
		}

		public void SetNone()
		{
			TitleLabel.text       = "";
			DescriptionLabel.text = "";
			EquipmentLabel.text   = "";
		}

		public void ChangeSticker([NotNull] StickerInstance sticker)
		{
			_sticker = sticker;
			_asset   = sticker.Asset;

			TitleLabel.text       = _asset.DisplayName;
			DescriptionLabel.text = _asset.Description;

			if (_asset.IsEquipment)
			{
				EquipmentLabel.gameObject.SetActive(true);
				EquipmentLabel.text = GetEquipmentText();
			}
			else
			{
				EquipmentLabel.gameObject.SetActive(false);
			}
		}

		[NotNull] private string GetEquipmentText()
		{
			StringBuilder sb = ObjectPoolSimple<StringBuilder>.Claim();

			var count = 0;

			void AppendNumber(float value, string name)
			{
				if (Mathf.Approximately(value, 0))
					return;

				if (count > 0) sb.Append(", ");

				sb.Append("<");
				sb.Append("color=#");
				sb.Append(value > 0 ? _gainColorHex : _lossColorHex);
				sb.Append(">");
				{
					sb.Append(value > 0 ? '+' : '-');
					sb.Append(value);
					sb.Append(" ");
					sb.Append(name);
				}
				sb.Append("</color>");

				count++;
			}

			void AppendPercent(float value, string name)
			{
				if (Mathf.Approximately(value, 0))
					return;

				if (count > 0) sb.Append(", ");

				sb.Append("<");
				sb.Append("color=#");
				sb.Append(value > 0 ? _gainColorHex : _lossColorHex);
				sb.Append(">");
				{
					sb.Append(value > 0 ? '+' : '-');
					sb.Append((value * 100).ToString("F0"));
					sb.Append(" ");
					sb.Append(name);
				}
				sb.Append("</color>");

				count++;
			}


			AppendNumber(_asset.PointGain.hp, "hp");
			AppendNumber(_asset.PointGain.sp, "sp");
			AppendNumber(_asset.PointGain.op, "op");

			AppendNumber(_asset.StatGain.power, "power");
			AppendNumber(_asset.StatGain.speed, "speed");
			AppendNumber(_asset.StatGain.will, "will");

			AppendPercent(_asset.EfficiencyGain.blunt, "blunt");
			AppendPercent(_asset.EfficiencyGain.slash, "slash");
			AppendPercent(_asset.EfficiencyGain.pierce, "pierce");
			AppendPercent(_asset.EfficiencyGain.gaia, "gaia");
			AppendPercent(_asset.EfficiencyGain.astra, "astra");
			AppendPercent(_asset.EfficiencyGain.oida, "oida");

			string ret = sb.ToString();

			sb.Clear();
			ObjectPoolSimple<StringBuilder>.Release(ref sb);

			return ret;
		}
	}
}