using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;

namespace Anjin.UI
{
	public class CharacterBustManager : MonoBehaviour, IActivatableWithTransitions
	{
		public enum States {
			Off, Transition, On,
		}

		public struct Options {

			public float duration;
			public float transition_in;
			public float transition_out;

			public static Options Default = new Options {
				duration = 3,
				transition_in = 0.16f,
				transition_out = 0.16f,
			};
		}

		public struct BustOptions {
			public string mode;
			public string state;
			public float  x;
			public float  y;
			public bool   flip_x;
			public bool   flip_y;
			public float? talk_duration;
			public bool   talk_all;

			public static BustOptions Default = new BustOptions {
				mode   = "",
				x      = 0.5f,
				y      = 0.0f,
				flip_x = false,
				flip_y = false,
				talk_duration = null,
				talk_all = false,
			};

			public static BustOptions FromTable(Table tbl)
			{
				if (tbl == null)
					return Default;

				BustOptions opt = Default;

				tbl.TryGet(Coplayer.CoplayerProxy.IDS_bust_mode,          out opt.mode,	 "");
				tbl.TryGet(Coplayer.CoplayerProxy.IDS_bust_state,         out opt.state, "");
				tbl.TryGet("x",             out opt.x,				opt.x);
				tbl.TryGet("y",             out opt.y,				opt.y);
				tbl.TryGet("flip_x",        out opt.flip_x,			opt.flip_x);
				tbl.TryGet("flip_y",        out opt.flip_y,			opt.flip_y);
				tbl.TryGet("talk_duration", out opt.talk_duration,	opt.talk_duration);
				tbl.TryGet("talk",          out opt.talk_all,		opt.talk_all);

				return opt;

			}
		}

		public class ActiveBust {
			public CharacterBust Bust;
			public string        Mode  = "";
			public string        State = "";
			public Vector2       Position;
			public bool          FlipX;
			public bool          FlipY;

			public float? Duration;
			public float? TalkDuration;
			public bool   TalkAll;
		}

		public RectTransform Root;

		[CanBeNull]
		public HUDElement Element;

		[NonSerialized, ShowInPlay] public List<ActiveBust> ActiveBusts;
		[NonSerialized, ShowInPlay] public ValTimer         Timer;

		[ShowInPlay]
		private Options? _options;
		[ShowInPlay]
		public Options CurrentOptions => _options ?? Options.Default;

		[NonSerialized, ShowInPlay]
		public States State;

		public bool IsActive => State != States.Off;

		private void Awake()
		{
			ActiveBusts = new List<ActiveBust>();
			_options     = null;
			State       = States.Off;
		}


		private void Update()
		{
			switch (State) {
				case States.Transition:

					break;

				case States.On:         break;
			}

			if (State == States.On) {
				if (Timer.Tick()) {
					Hide();
				}
			}
		}

		[ShowInPlay]
		public void Test()
		{
			AddBust(Character.Nas, new BustOptions {
				x = 0.2f,
				talk_duration = 2,
			});


			AddBust(Character.Serio, new BustOptions {
				x             = 0.8f,
				flip_x        = true,
				talk_duration = 3,
			});

			Show(new Options {
				duration = 4,
			});
		}

		[ShowInPlay]
		public void TestNewBustAnimator()
		{
			AddBust(Character.Nas, new BustOptions {
				x             = 0.2f,
				talk_duration = 5,
			});

			Show(new Options {
				duration = 10,
			});
		}

		[ShowInPlay]
		public void TestEverybody()
		{
			float duration = 5;
			int   count = CutsceneHUD.Live.BustPools.Count;
			int   i     = 0;

			foreach (KeyValuePair<Character, ComponentPool<CharacterBust>> kvp in CutsceneHUD.Live.BustPools) {
				AddBust(kvp.Key, new BustOptions {
					x             = Mathf.Lerp(0, 1, i / (float) (count - 1)),
					flip_x        = RNG.Chance(0.5f),
					talk_duration = Mathf.Lerp(duration * 0.6f, duration, RNG.Range(1)),
				});
				i++;
			}

			Show(new Options {
				duration = duration,
			});
		}

		// API
		//===============================================================================
		public void Reset()
		{
			foreach (var active in ActiveBusts) {
				CutsceneHUD.ReturnBust(active.Bust);
			}

			ActiveBusts.Clear();
			Timer.Reset();
		}

		[ShowInPlay]
		public ActiveBust AddBust(Character character, BustOptions? options = null)
		{
			BustOptions opt = options ?? BustOptions.Default;

			if (CutsceneHUD.TryRentBust(character, out CharacterBust bust)) {
				bust.transform.SetParent(Root);
				bust.transform.localScale = Vector3.one;
				bust.gameObject.SetActive(false);

				var active = new ActiveBust { Bust = bust };
				ActiveBusts.Add(active);
				active.TalkDuration = opt.talk_duration;

				active.Mode    = opt.mode;
				active.State    = opt.state;
				active.TalkAll = opt.talk_all;

				var rt = bust.GetComponent<RectTransform>();

				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.zero;

				float xpos = opt.x;
				float ypos = opt.y;

				rt.localPosition = new Vector3(
					Mathf.Lerp(0, Root.rect.width,  xpos),
					Mathf.Lerp(0, Root.rect.height, ypos),
					rt.localPosition.z
				);

				Vector3 scale           = rt.localScale;
				if (opt.flip_x) scale.x = -scale.x;
				if (opt.flip_y) scale.y = -scale.y;
				rt.localScale = scale;

				return active;
			}

			return null;
		}

		// Show
		[ShowInPlay]
		public void Show(Options? options = null)
		{
			State = States.Transition;
			_show().ForgetWithErrors();
		}

		async UniTask _show(Options? options = null)
		{
			_options = options;

			Timer.Set(CurrentOptions.duration);

			foreach (ActiveBust bust in ActiveBusts) {

				bust.Bust.gameObject.SetActive(true);
				bust.Bust.Animator.Mode  = bust.Mode;
				bust.Bust.Animator.State = bust.State;
				if(bust.TalkDuration.HasValue)
					bust.Bust.DoTalk(bust.TalkDuration.Value);
				else if(bust.TalkAll)
					bust.Bust.DoTalk(CurrentOptions.duration);
			}

			if (Element) {
				Element.DoAlphaFade(0, 1, CurrentOptions.transition_in).Tween.ToUniTask();
				await Element.DoOffset(new Vector3(0, -30, 0), Vector3.zero, CurrentOptions.transition_in).Tween.ToUniTask();
			}

			State = States.On;
		}

		public void ShowInstant(Options? options = null)
		{
			State    = States.On;

			_options = options;

			Timer.Set(CurrentOptions.duration);

			foreach (ActiveBust bust in ActiveBusts) {

				bust.Bust.gameObject.SetActive(true);
				bust.Bust.Animator.Mode = bust.Mode;
				if(bust.TalkDuration.HasValue)
					bust.Bust.DoTalk(bust.TalkDuration.Value);
				else if(bust.TalkAll)
					bust.Bust.DoTalk(CurrentOptions.duration);
			}
		}

		// Hide

		[ShowInPlay]
		public void Hide()
		{
			State = States.Transition;
			_hide().ForgetWithErrors();
		}

		async UniTask _hide()
		{
			if (Element) {
				Element.DoAlphaFade(1, 0, CurrentOptions.transition_out).Tween.ToUniTask();
				await Element.DoOffset(Vector3.zero, new Vector3(0, -30, 0), CurrentOptions.transition_out).Tween.ToUniTask();
			}

			Reset();
			State = States.Off;
		}


		[ShowInPlay]
		public void HideInstant()
		{
			State = States.Off;
			Reset();
		}

	}
}