using TMPro;
using UnityEngine;

namespace Combat.UI
{
	public class DamageNumberChild : MonoBehaviour
	{
		public TMP_Text digit1;
		public TMP_Text digit10;
		public TMP_Text digit100;
		public TMP_Text digit1000;

		public TMP_Text digitOutline1;
		public TMP_Text digitOutline10;
		public TMP_Text digitOutline100;
		public TMP_Text digitOutline1000;

		public Color digitColor;
		public Color digitOutlineColor;
		public Color digitOutlineColorTop;
		public Color digitOutlineColorBottom;

		public float spacing;
		public float fontSize;

		private void Update()
		{
			UpdateFonts();
		}

		public void SetDamageNumber(int damage)
		{
			int shownDamage = Mathf.Clamp(damage, 0, 9999);
			var text        = shownDamage.ToString();

			if (shownDamage < 10)
			{
				digit1.text                    = text.Substring(0, 1);
				digit1.transform.localPosition = new Vector3();

				digit10.text   = "";
				digit100.text  = "";
				digit1000.text = "";
			}

			else if (shownDamage < 100)
			{
				digit1.text                    = text.Substring(1, 1);
				digit1.transform.localPosition = new Vector3(spacing * 0.5f, 0, 0);

				digit10.text                    = text.Substring(0, 1);
				digit10.transform.localPosition = new Vector3(spacing * -0.5f, 0, 0);

				digit100.text  = "";
				digit1000.text = "";
			}

			else if (shownDamage < 1000)
			{
				digit1.text                    = text.Substring(2, 1);
				digit1.transform.localPosition = new Vector3(spacing, 0, 0);

				digit10.text                    = text.Substring(1, 1);
				digit10.transform.localPosition = new Vector3();

				digit100.text                    = text.Substring(0, 1);
				digit100.transform.localPosition = new Vector3(-spacing, 0, 0);

				digit1000.text = "";
			}

			else
			{
				digit1.text                    = text.Substring(3, 1);
				digit1.transform.localPosition = new Vector3(spacing * 1.5f, 0, 0);

				digit10.text                    = text.Substring(2, 1);
				digit10.transform.localPosition = new Vector3(spacing * 0.5f, 0, 0);

				digit100.text                    = text.Substring(1, 1);
				digit100.transform.localPosition = new Vector3(spacing * -0.5f, 0, 0);

				digit1000.text                    = text.Substring(0, 1);
				digit1000.transform.localPosition = new Vector3(spacing * -1.5f, 0, 0);
			}

			UpdateOutlines();
		}

		private void UpdateOutlines()
		{
			if (digitOutline1)
			{
				digitOutline1.text = digit1.text;
			}

			if (digitOutline10)
			{
				digitOutline10.text = digit10.text;
			}

			if (digitOutline100)
			{
				digitOutline100.text = digit100.text;
			}

			if (digitOutline1000)
			{
				digitOutline1000.text = digit1000.text;
			}
		}

		private void UpdateFonts()
		{
			digit1.color       = digitColor;
			digit1.fontSize    = fontSize;
			digit10.color      = digitColor;
			digit10.fontSize   = fontSize;
			digit100.color     = digitColor;
			digit100.fontSize  = fontSize;
			digit1000.color    = digitColor;
			digit1000.fontSize = fontSize;

			var gradient = new VertexGradient(digitOutlineColorTop, digitOutlineColorTop, digitOutlineColorBottom, digitOutlineColorBottom);

			if (digitOutline1)
			{
				digitOutline1.color         = digitOutlineColor;
				digitOutline1.colorGradient = gradient;
				digitOutline1.fontSize      = fontSize;
			}

			if (digitOutline10)
			{
				digitOutline10.color         = digitOutlineColor;
				digitOutline10.colorGradient = gradient;
				digitOutline10.fontSize      = fontSize;
			}

			if (digitOutline100)
			{
				digitOutline100.color         = digitOutlineColor;
				digitOutline100.colorGradient = gradient;
				digitOutline100.fontSize      = fontSize;
			}

			if (digitOutline1000)
			{
				digitOutline1000.color         = digitOutlineColor;
				digitOutline1000.colorGradient = gradient;
				digitOutline1000.fontSize      = fontSize;
			}
		}

		// private void OnValidate()
		// {
		// 	UpdateFonts();
		// }
	}
}