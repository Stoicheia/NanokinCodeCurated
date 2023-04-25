using Anjin.Scripting;
using Anjin.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Overworld.UI {
    [LuaUserdata(StaticAuto = true)]
    public class TimerHUD : StaticBoy<TimerHUD> {

        public HUDElement TimerElement;
        public TMP_Text   Label;
        public TMP_Text   TimerLabel;
        public int        TimerAmount;

		private const string DEFAULT_LABEL = "Time Left:";

        public bool Showing;
        void Start()
        {
            TimerElement.Alpha = 0;
            TimerLabel.text    = "0";
        }

        void Update()
        {
            if (Showing)
                TimerLabel.text = TimerAmount.ToString();

			TimerElement.Invisible = SplicerHub.menuActive;
		}

        public static void setup(int count, string label = DEFAULT_LABEL)
		{
			Live.TimerAmount = count;
			Live.Label.text  = label;
		}

		public static void set(int count)
		{
			Live.TimerAmount = count;
		}

		public static void show()         => Live.Show();

        [Button]
        public void Show()
        {
            if (Showing) return;
            Showing = true;

            /*TimerElement.DoScale(Vector3.one * 1.25f, Vector3.one, 0.5f);
            TimerElement.DoAlphaFade(0, 1, 0.5f);
            TimerElement.DoOffset(Vector3.right * 20f, Vector3.zero, 0.5f);*/

			TimerElement.DoScale(Vector3.one * 1.15f, Vector3.one, 0.3f, Ease.OutBounce);
			TimerElement.DoAlphaFade(0, 1, 0.2f, Ease.InCubic);
			TimerElement.DoOffset(Vector3.up * 40f, Vector3.zero, 0.3f, Ease.OutBounce);
			TimerElement.DoRotation(new Vector3(0, 0, 20), new Vector3(0, 0, -9), 0.6f, Ease.OutElastic);
        }

        public static void hide() => Live.Hide();
        [Button]
        public void Hide()
        {
            if (!Showing) return;
            Showing = false;

			TimerElement.DoScale(Vector3.one, Vector3.one * 1.3f, 0.3f, Ease.InCirc);
			TimerElement.DoAlphaFade(1, 0, 0.3f, Ease.InCirc);
			TimerElement.DoOffset(Vector3.up * 500f, 0.3f, Ease.InCirc);
        }
    }
}