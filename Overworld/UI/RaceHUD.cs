using Anjin.Scripting;
using Anjin.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Overworld.UI
{
	[LuaUserdata(staticAuto: true)]
	public class RaceHUD : StaticBoy<RaceHUD>
	{
		public HUDElement Element;

		public TMP_Text TMP_Place;
		public int      Place;

		public bool Showing;

		void Start()
		{
			Element.Alpha  = 0;
			TMP_Place.text = "-";
		}

		void Update()
		{
			if (Showing)
				TMP_Place.text = PlaceToString(Place);

			Element.Invisible = SplicerHub.menuActive;
		}

		public static void setup(int place)
		{
			Live.Place          = place;
			Live.TMP_Place.text = Live.PlaceToString(place);
		}

		public static void set(int place)
		{
			Live.Place          = place;
			Live.TMP_Place.text = Live.PlaceToString(place);
		}

		public static void show() => Live.Show();

		public string PlaceToString(int place)
		{
			if (place > 0)
			{
				switch (place)
				{
					// TODO use gametext so we can localize
					case 1:  return "1st";
					case 2:  return "2nd";
					case 3:  return "3rd";
					case 4:  return "4th";
					default: return place.ToString();
					// TODO: This could probably be automated but for now, why?
				}
			}

			return "-";
		}

		[Button]
		public void Show()
		{
			if (Showing) return;
			Showing = true;

			/*TimerElement.DoScale(Vector3.one * 1.25f, Vector3.one, 0.5f);
			TimerElement.DoAlphaFade(0, 1, 0.5f);
			TimerElement.DoOffset(Vector3.right * 20f, Vector3.zero, 0.5f);*/

			Element.DoScale(Vector3.one * 1.15f, Vector3.one, 0.3f, Ease.OutBounce);
			Element.DoAlphaFade(0, 1, 0.2f, Ease.InCubic);
			Element.DoOffset(Vector3.up * 40f, Vector3.zero, 0.3f, Ease.OutBounce);
			Element.DoRotation(new Vector3(0, 0, -9), new Vector3(0, 0, 20), 0.6f, Ease.OutElastic);
		}

		public static void hide() => Live.Hide();

		[Button]
		public void Hide()
		{
			if (!Showing) return;
			Showing = false;

			Element.DoScale(Vector3.one, Vector3.one * 1.3f, 0.3f, Ease.InCirc);
			Element.DoAlphaFade(1, 0, 0.3f, Ease.InCirc);
			Element.DoOffset(Vector3.up * 500f, 0.3f, Ease.InCirc);
		}
	}
}