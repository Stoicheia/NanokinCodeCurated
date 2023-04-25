using System;
using System.Collections.Generic;
using Anjin.Minigames;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UniTween.Core;
using UnityEngine;
using Util.UniTween.Value;
using Vexe.Runtime.Extensions;

namespace Overworld.UI {

	[LuaUserdata(StaticAuto = true)]
	public class MinigameRankHUD : StaticBoy<MinigameRankHUD> {

		public HUDElement RootElement;
		public HUDElement RankElement;
		public AnjinSDF   RankSDF;
		public TMP_Text   RankLabel;

		[NonSerialized, ShowInInspector]
		public MinigameRank Rank;

		[ShowInInspector]
		private bool Showing;

		public Sequence       RankGainSeq;
		public Sequence       RankLoseSeq;

		public Dictionary<MinigameRank, Color32> RankColors;
		public Dictionary<MinigameRank, Color32> RankColorsSpec;

		private void Start()
		{
			Showing           = false;
			RootElement.Alpha = 0;

			UpdateLabel();

			/*RankGainSeq = DOTween.Sequence()
								 .Append(RankElement.Scale.FromTo(Vector3.one, Vector3.one * 1.35f, new EaserTo(0.6f, Ease.OutElastic)))
								 .AppendInterval(0.3f)
								 .InsertCallback(0.05f, UpdateLabel)
								 .Append(RankElement.Scale.To(Vector3.one, new EaserTo(0.4f,                          Ease.OutBounce)))
								 .Pause().SetAutoKill(false);*/

			float midPoint = 0.7f;

			/*RankLoseSeq = DOTween.Sequence()
								 .Append(RankElement.SequenceOffset.FromTo(Vector3.zero, Vector3.down * 30, new EaserTo(midPoint, Ease.OutQuad)))
								 .Join(RankElement.Alpha.To(0, new EaserTo(midPoint)))
								 .InsertCallback(midPoint, UpdateLabel)
								 .Insert(midPoint + 0.1f, RankElement.SequenceOffset.FromTo(Vector3.up * 30, Vector3.zero, new EaserTo(midPoint, Ease.OutQuad)))
								 .Append(RankElement.Alpha.To(1, new EaserTo(midPoint)))
								 .Pause().SetAutoKill(false);*/
		}

		private void Update()
		{
			RootElement.Invisible = SplicerHub.menuActive;
		}

		[Button]
		public void SetRank(MinigameRank rank, bool anim = true)
		{
			if (Rank == rank) return;

			if(anim) {
				if (rank > Rank) {
					/*RankLoseSeq.Rewind();
					RankLoseSeq.Play();*/


					RankElement.SequenceOffset.EnsureComplete();

					TweenableVector3 off = RankElement.SequenceOffset;
					off.SetupForTweeningIfNecessary();

					RankSDF.DissolveStrength.SetupForTweeningIfNecessary();

					float midPoint = 0.6f;
					DOTween.Sequence()
						   .Append(DOTween.To(off.getter,                    off.setter,                      Vector3.down * 30, midPoint))
						   .Join(DOTween.To(RankSDF.DissolveStrength.getter, RankSDF.DissolveStrength.setter, 1,                 midPoint))

						   .AppendCallback(UpdateLabel)
						   .Append(DOTween.To(off.getter, off.setter, Vector3.up * 30, 0))

						   .Append(DOTween.To(off.getter,                    off.setter,                      Vector3.zero, midPoint))
						   .Join(DOTween.To(RankSDF.DissolveStrength.getter, RankSDF.DissolveStrength.setter, 0,            midPoint));

				} else {
					DOTween.Sequence()
						   .Append(RankElement.Scale.FromTo(Vector3.one, Vector3.one * 1.35f, new EaserTo(0.6f, Ease.OutElastic)))
						   .AppendInterval(0.15f)
						   .InsertCallback(0.02f, UpdateLabel)
						   .Append(RankElement.Scale.To(Vector3.one, new EaserTo(0.4f, Ease.OutBounce)));
				}
			} else {
				UpdateLabel();
			}

			Rank = rank;
		}

		[Button]
		public void UpdateLabel()
		{
			if (LuaMinigame.RankNames.TryGetValue(Rank, out string rank_name)) {
				RankLabel.text = rank_name;
			} else {
				RankLabel.text = "-";
			}

			RankLabel.gameObject.SetActive(false);
			RankSDF.SetOutlineColor(RankColors.ValueOrDefault(Rank, Color.black));
			RankSDF._matInstance.SetColor(Shader.PropertyToID("_SpecularColor"), RankColorsSpec.ValueOrDefault(Rank, Color.white));
			RankLabel.gameObject.SetActive(true);
		}

		[Button]
		public void Show()
		{
			if (Showing) return;
			Showing = true;

			RootElement.DoScale(Vector3.one * 1.15f, Vector3.one, 0.3f, Ease.OutBounce);
			RootElement.DoAlphaFade(0, 1, 0.1f, Ease.InCubic);
			RootElement.DoOffset(Vector3.up * 40f, Vector3.zero, 0.3f, Ease.OutBounce);
			RootElement.DoRotation(new Vector3(-80, 0, 0), Vector3.zero, 1.5f, Ease.OutElastic);

			RankElement.DoScale(Vector3.one * 1.35f, Vector3.one, 2.5f, Ease.OutElastic);

			UpdateLabel();
		}

		[Button]
		public void Hide()
		{
			if (!Showing) return;
			Showing = false;

			RootElement.DoScale(Vector3.one, Vector3.one * 1.3f, 0.3f, Ease.InCirc);
			RootElement.DoAlphaFade(1, 0, 0.3f, Ease.InCirc);
			RootElement.DoOffset(Vector3.up * 500f, 0.3f, Ease.InCirc);

			RankElement.DoScale(Vector3.one, Vector3.zero, 2.5f, Ease.OutQuart);
		}

		public static void show() => Live.Show();
		public static void hide() => Live.Hide();
		public static void set_rank(MinigameRank rank, bool anim = true) => Live.SetRank(rank, anim);

	}
}