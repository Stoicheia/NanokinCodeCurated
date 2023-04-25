using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Overworld.UI {

	// TODO: Maybe an option to either let the goal fall back to a previous phase, or keep it in the current one?

    [LuaUserdata(StaticAuto = true)]
    public class CollectionTickerHUD : StaticBoy<CollectionTickerHUD> {

        public HUDElement TickerElement;
        public TMP_Text   TickerLabel;
		public UICircle   Circle;
		public Camera     Camera;

		public float CircleFillLerp = 0.15f;

		public bool IsMultiPhase = false;

        public int       CollectionAmount;

        public int       CollectionGoal;
		public List<int> MultiPhaseGoals;
		public int       CurrentPhase;

        public bool Showing;
        void Start()
        {
            TickerElement.Alpha = 0;
            TickerLabel.text    = "0/0";
			Circle.Arc    		= 0;

			Camera.gameObject.SetActive(false);

			if(MultiPhaseGoals == null)
				MultiPhaseGoals = new List<int>();
		}

        void Update()
        {
			TickerElement.Invisible = SplicerHub.menuActive;

            if (Showing) {

				float goal      = GetGoal();
				TickerLabel.text = CollectionAmount + "   <size=70>I</size>" + goal;

				float arcAmount = CollectionAmount;
				if (IsMultiPhase && MultiPhaseGoals.Count > 0) {

					for (int i = MultiPhaseGoals.Count - 1; i >= 0; i--)
						if (i > 0) {
							if (CollectionAmount >= MultiPhaseGoals[i - 1] && CollectionAmount < MultiPhaseGoals[i]) {
								arcAmount = CollectionAmount   - MultiPhaseGoals[i - 1];
								goal      = MultiPhaseGoals[i] - MultiPhaseGoals[i - 1];
								break;
							}
						} else {
							arcAmount = CollectionAmount;
							goal      = MultiPhaseGoals[0];
							break;
						}
				} else {
					arcAmount = CollectionAmount;
				}

				if(goal != 0) {
					Circle.Arc = Mathf.Lerp(Circle.Arc, Mathf.Clamp01(arcAmount / goal), CircleFillLerp);
				}
				Circle.SetVerticesDirty();
			}
        }

		public int GetGoal()
		{
			if (!IsMultiPhase)
				return CollectionAmount;
			else {
				if (MultiPhaseGoals.Count == 0) return 0;

				for (int i = 0; i < MultiPhaseGoals.Count; i++)
					if (CollectionAmount < MultiPhaseGoals[i])
						return MultiPhaseGoals[i];

				return MultiPhaseGoals[MultiPhaseGoals.Count - 1];
			}
		}

		public void OnChangeAmount()
		{
			if (!Showing) return;


		}

        public static void setup(int goal)
        {
            Live.CollectionAmount = 0;
            Live.CollectionGoal   = goal;
			Live.IsMultiPhase     = false;
			Live.MultiPhaseGoals.Clear();
		}

		public static void setup(int[] goals)
		{
			Live.CollectionAmount = 0;
			Live.MultiPhaseGoals  = goals.ToList();
			Live.IsMultiPhase     = true;
			Live.CurrentPhase     = 0;
			Live.OnChangeAmount();
		}

        public static void increase(int count = 1)
		{
			Live.CollectionAmount += count;
			Live.OnChangeAmount();
		}

		public static void decrease(int count = 1)
        {
            Live.CollectionAmount -= count;
            Live.CollectionAmount =  Mathf.Max(0, Live.CollectionAmount);
			Live.OnChangeAmount();
        }

        public static void show() => Live.Show();
        public static void hide() => Live.Hide();

        [Button]
        public void Show()
        {
            if (Showing) return;
            Showing = true;

			Camera.gameObject.SetActive(true);

            TickerElement.DoScale(Vector3.one * 1.15f, Vector3.one, 0.3f, Ease.OutBounce);
			TickerElement.DoAlphaFade(0, 1, 0.2f, Ease.InCubic);
            TickerElement.DoOffset(Vector3.up * 40f, Vector3.zero, 0.3f, Ease.OutBounce);
            TickerElement.DoRotation(new Vector3(0, 0, -20), Vector3.zero, 0.6f, Ease.OutElastic);
		}

        [Button]
        public void Hide()
        {
            if (!Showing) return;
            Showing = false;

            TickerElement.DoScale(Vector3.one, Vector3.one * 1.3f, 0.3f, Ease.InCirc);
            TickerElement.DoAlphaFade(1, 0, 0.3f, Ease.InCirc).Tween.onComplete += () => {
				if(!Showing)
					Camera.gameObject.SetActive(false);
			};
            TickerElement.DoOffset(Vector3.up * 500f, 0.3f, Ease.InCirc);
        }
    }
}