using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Anjin.Scripting.Waitables;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UniTween.Core;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.UI
{

	public struct DialogueOptions
	{
		public bool controllable;

		public bool always_show_advance_button;

		public bool auto;
		public float auto_delay;
		public bool soft_auto;

		public float transition_in;
		public float transition_out;

		public bool prefix_name;

		[CanBeNull] public string font_override;
		[CanBeNull] public string font_material_override;

		public bool no_typing;

		// Note(C.L.): Temp
		//public bool  special_muppet;

		public static DialogueOptions Default = new DialogueOptions
		{
			controllable               = true,
			always_show_advance_button = false,
			auto                       = false,
			soft_auto                  = false,
			auto_delay                 = 2,
			transition_in              = 0.16f,
			transition_out             = 0.16f,
			prefix_name                = false,
			font_override              = null,
			font_material_override	   = null,
			no_typing                  = false,
		};

		public void FillFromTable(Table tbl)
		{
			if (tbl.TryGet(new[] { "interrupted", "interrupt_start" }, out bool interrupt) && interrupt)
			{
				Debug.Log("Interrupted");
				controllable = false;
				auto = true;
				auto_delay = 0.15f;
				transition_out = 0;
			}

			if (tbl.TryGet(new[] { "interruptee", "interrupt_end" }, out bool interruptee) && interruptee)
			{
				Debug.Log("Interruptee");
				transition_in = 0;
			}

			tbl.TryGet("controllable", out controllable, controllable);
			tbl.TryGet("always_show_advance_button", out always_show_advance_button, always_show_advance_button);

			if (tbl.TryGet("auto", out DynValue val)) {
				if (val.AsBool(out bool _auto)) auto = _auto;
				else if (val.AsFloat(out float _delay)) {
					auto       = true;
					auto_delay = _delay;
				}
			}

			if (tbl.TryGet("locked_auto", out DynValue locked_auto)) {
				if (locked_auto.AsBool(out bool _auto)) auto = _auto;
				else if (locked_auto.AsFloat(out float _delay)) {
					auto         = true;
					auto_delay   = _delay;
				}

				controllable = false;
			}

			tbl.TryGet("auto_delay",     out auto_delay,     auto_delay);
			tbl.TryGet("soft_auto",      out soft_auto,      soft_auto);
			tbl.TryGet("transition_in",  out transition_in,  transition_in);
			tbl.TryGet("transition_out", out transition_out, transition_out);

			tbl.TryGet("font", out font_override, font_override);
			tbl.TryGet("material", out font_material_override, font_material_override);
		}
	}

	public interface IDialogueTextboxInvoker
	{
		void SoftAutoActivated();
		void SoftAutoDeactivated();
		bool SoftAuto { get; }
	}

	public class DialogueTextbox : SerializedMonoBehaviour, IActivatableWithTransitions
	{

		public enum State
		{
			Off,
			Transition,
			Playing,
			Paused
		}

		public enum Type
		{
			Full,
			Ambient
		}

		public enum AdvanceMode
		{
			Manual,
			Auto,
		}

		public const string    DEFAULT_NAME      = "???";
		//public const Character DEFAULT_CHARACTER = Character.TestDummy;

		public Type type;
		//public bool NameAddedToTextBeginning = false;

		[Title("References")]
		public HUDElement Element;
		public TextDisplaySequencer Sequencer;
		public TMP_Typewriter Typer;
		public TextMeshProUGUI TMP_Dialogue;
		public TextMeshProUGUI TMP_CharName;
		public TextMeshProUGUI TMP_SoftAutoText;

		public Transform BustRoot;

		public InputButtonLabel AdvanceButtonLabel;
		public InputButtonLabel SoftAutoLabel;

		[Title("Options")]
		public DialogueOptions DefaultOptions = DialogueOptions.Default;

		[ShowInPlay]
		public DialogueOptions CurrentOptions => OverrideOptions.HasValue ? OverrideOptions.Value : DefaultOptions;

		[ShowInPlay, NonSerialized]
		public DialogueOptions? OverrideOptions;

		public bool SuppressTestBust;

		[Title("Runtime")]
		[ShowInPlay]
		public bool IsActive => state >= State.Transition;

		[ShowInPlay, NonSerialized]     public State                                state;
		[ShowInPlay, NonSerialized]     public float                                Timer;
		[ShowInPlay, NonSerialized]     public Dictionary<Character, CharacterBust> Busts;
		[ShowInPlay, NonSerialized]     public Dictionary<Character, string>        Names;
		[ShowInPlay, NonSerialized]     public CharacterBust                        TestDummyBust;
		[ShowInPlay, NonSerialized]     public GameInputs.ActionButton              AdvanceButton;
		[ShowInPlay, NonSerialized]     public GameInputs.ActionButton              SoftAutoButton;
		[ShowInPlay, NonSerialized]     public IDialogueTextboxInvoker              Invoker;

		[ShowInPlay, NonSerialized, CanBeNull] public string DialoguePrefix = null;

		private bool SoftAuto
		{
			get
			{
				if (Invoker != null)
					return Invoker.SoftAuto;
				return CurrentOptions.soft_auto;
			}
		}

		private CharacterBust _currentBust;

		private Character _charShowing;
		[ShowInPlay]
		public Character CharShowing
		{
			get => _charShowing;
			set
			{
				_charShowing = value;

				if (_currentBust) {
					CutsceneHUD.ReturnBust(_currentBust);
				}

				_currentBust = null;

				CharacterBust bust = null;

				if(BustRoot != null) {
					if (value != Character.None) {
						if (CutsceneHUD.TryRentBust(_charShowing, out CharacterBust normal)) {
							bust = normal;
						} else if (GameOptions.current.use_test_dummy_bust && !SuppressTestBust && CutsceneHUD.TryRentBust(Character.TestDummy, out var dummy)) {
							bust = dummy;
						}
					}

					if (bust != null) {
						_currentBust = bust;
						bust.transform.SetParent(BustRoot);

						var rt = bust.GetComponent<RectTransform>();
						rt.localScale = Vector3.one;

						if (type == Type.Ambient) {
							rt.pivot     = new Vector2(1, 0);
							rt.anchorMax = new Vector2(1, 0);
							rt.anchorMin = new Vector2(1, 0);
						}

						rt.anchoredPosition = Vector3.zero;
						bust.Typer          = Typer;
					}
				}
			}
		}

		private async void Start()
		{
			Element = GetComponent<HUDElement>();
			Element.Alpha = 0;
			Timer = 0;

			/*async UniTask<CharacterBust> LoadBust(string address)
			{
				GameObject prefab = (await Addressables2.LoadHandleAsync<GameObject>(address)).Result;
				if (prefab == null) return null;

				GameObject ins = prefab.Instantiate(BustRoot);

				var rt = ins.GetComponent<RectTransform>();

				if (type == Type.Ambient)
				{
					rt.pivot = new Vector2(1, 0);
					rt.anchorMax = new Vector2(1, 0);
					rt.anchorMin = new Vector2(1, 0);
				}

				rt.anchoredPosition = Vector3.zero;


				var bust = ins.GetComponent<CharacterBust>();
				bust.Typer = Typer;

				ins.gameObject.SetActive(false);
				return bust;
			}

			Busts = new Dictionary<Character, CharacterBust>();

			TestDummyBust = await LoadBust("UIBusts/TestDummy");

			Busts[Character.Nas] = await LoadBust("UIBusts/Nas");
			Busts[Character.Jatz] = await LoadBust("UIBusts/Jatz");
			Busts[Character.Serio] = await LoadBust("UIBusts/Serio");
			Busts[Character.Peggie] = await LoadBust("UIBusts/Peggie");
			Busts[Character.ChalkyLeBarron] = await LoadBust("UIBusts/Chalky");*/

			CharShowing = Character.None;

			Names = new Dictionary<Character, string> {
				{Character.Nas,     "Nas"},
				{Character.Jatz,    "Jatz"},
				{Character.Serio,   "Serio"},
				{Character.Peggie,  "Peggie"},
				{Character.David,   "David"},
				{Character.NeAi,    "NeAi"},
				{Character.Koa,     "Koa"},

				{Character.BennyBroomer,    "Benny Broomer"},
				{Character.CooperTheSwift, 	"Cooper The Swift"},
				{Character.ChalkyLeBarron,  "Chalky LeBarron"},

				{Character.Nelson,  "Nelson"},
				{Character.Marisa,  "Marisa"},

				{Character.ChesterMcLarge,      "Chester McLarge"},
				{Character.JeffryMontgomery,    "Jeffry Montgomery"},
				{Character.WittCassedy,         "Witt Cassedy"},
			};

			Sequencer.OnDoneSequencing.AddListener(OnDoneSequencing);
			Sequencer.OnDisplayNext.AddListener(OnDisplayNext);

			Typer.OnDisplayLine += OnDisplayLine;
		}


		private void Update()
		{
			var opt = CurrentOptions;

			bool can_advance = state == State.Playing && Sequencer.Displayer.DisplayProcessDone;

			if (state == State.Playing)
			{

				if (GameInputs.textBoxSoftAuto.IsPressed)
				{
					if (Invoker != null)
					{
						if (SoftAuto)
							Invoker.SoftAutoDeactivated();
						else
							Invoker.SoftAutoActivated();
					}
					else if (OverrideOptions != null)
					{
						var o = OverrideOptions.Value;
						o.soft_auto = true;
						OverrideOptions = o;
					}
				}

				if (opt.controllable)
				{

					if (Sequencer.Sequencing && GameInputs.confirm.IsPressedOrHeld())
					{
						Sequencer.Advance();
					}
				}

				if (opt.auto || SoftAuto)
				{

					if (can_advance /*&& (_currentBust == null || !_currentBust.Talking)*/)
					{
						Timer -= Time.deltaTime;

						if (Timer <= 0)
						{
							Advance();
						}
					}
				}

				if (SoftAuto)
				{
				}

				/*switch (Mode) {
					case AdvanceMode.Manual:
						break;

					case AdvanceMode.Auto:


						break;
				}*/
			}


			if (state != State.Off)
			{
				if (AdvanceButtonLabel)
				{
					if (AdvanceButton != null)
						AdvanceButtonLabel.Button = AdvanceButton;

					AdvanceButtonLabel.gameObject.SetActive(state == State.Playing && (opt.controllable && can_advance || opt.always_show_advance_button));
				}

				if (TMP_SoftAutoText)
					TMP_SoftAutoText.text = SoftAuto ? "Auto: On" : "Auto: Off";

				if (SoftAutoButton != null && SoftAutoLabel)
					SoftAutoLabel.Button = SoftAutoButton;
			}
		}

		public void Advance()
		{
			Sequencer.Advance();
			Timer = CurrentOptions.auto_delay;
		}

		private string OnDisplayLine(TMP_Typewriter typewriter, string arg)
		{
			if (DialoguePrefix != null) {
				arg = $"<b>{DialoguePrefix}</b>: {arg}";
				typewriter.characterCount = DialoguePrefix.Length;
			}

			if (!CurrentOptions.font_override.IsNullOrWhitespace()) {
				if(CurrentOptions.font_material_override.IsNullOrWhitespace())
					arg = $"<font=\"{CurrentOptions.font_override}\">{arg}</font>";
				else
					arg = $"<font=\"{CurrentOptions.font_override}\" material=\"{CurrentOptions.font_material_override}\">{arg}</font>";
			}

			if (CurrentOptions.no_typing) {
				typewriter.InstantFlag = true;
			}

			return arg;
		}

		public void OnDoneSequencing() => Hide();
		public void OnDisplayNext()
		{
			if (_currentBust)
				_currentBust.DoTalkForString(Sequencer.Lines[Sequencer.index]);
			/*if (_charShowing != Character.None) {
			}*/

			Timer = CurrentOptions.auto_delay;
		}

		// API
		//--------------------------------------------------------
		[Title("API")]
		[ShowInPlay]
		public void Show([CanBeNull] string characterName, GameText text, DialogueOptions? options = null, BustAnimConfig? bustAnim = null, IDialogueTextboxInvoker invoker = null)
		{
			if (state == State.Off) state = State.Transition;
			_show(null, new List<GameText> { text }, options, bustAnim, invoker, characterName).ForgetWithErrors();
		}

		public void Show([CanBeNull] string characterName, List<GameText> text, DialogueOptions? options = null, BustAnimConfig? bustAnim = null, IDialogueTextboxInvoker invoker = null)
		{
			if (state == State.Off) state = State.Transition;
			_show(null, text, options, bustAnim, invoker, characterName).ForgetWithErrors();
		}

		[ShowInPlay]
		public void Show(Character character, GameText text, DialogueOptions? options = null, BustAnimConfig? bustAnim = null, IDialogueTextboxInvoker invoker = null, string characterName = null)
		{
			if (state == State.Off) state = State.Transition;
			_show(character, new List<GameText> { text }, options, bustAnim, invoker, characterName).ForgetWithErrors();
		}

		public void Show(Character character, List<GameText> text, DialogueOptions? options = null, BustAnimConfig? bustAnim = null, IDialogueTextboxInvoker invoker = null, string characterName = null)
		{
			if (state == State.Off) state = State.Transition;
			_show(character, text, options, bustAnim, invoker, characterName).ForgetWithErrors();
		}

		[ShowInPlay]
		public void Hide()
		{
			if (state != State.Off) state = State.Transition;
			_hide().ForgetWithErrors();
		}

		[ShowInPlay]
		public void HideInstant() => _hideInstant();

		[ShowInPlay]
		public void Test(Character c)
		{
			Show(c, new List<GameText> {
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
				"Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
				"Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
				"Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
			});
		}

		[ShowInPlay]
		public void TestAllBusts() => _testAllBusts().ForgetWithErrors();

		public async UniTask _testAllBusts()
		{
			if (state != State.Off) return;

			var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";

			List<Character> vals = Enum.GetValues(typeof(Character)).OfType<Character>().ToList();

			DialogueOptions opt = new DialogueOptions {
				transition_in	= 0.05f,
				transition_out	= 0.05f,
				soft_auto		= true,
				auto_delay		= 0.75f,
			};

			foreach (Character character in vals) {

				CutsceneHUD.TryRentBust(character, out var bust);
				if (bust == null) continue;

				CharacterBustModeAsset mode = bust.Animator.BaseMode;
				CutsceneHUD.ReturnBust(bust);

				Show(character, $"Mode: {mode.name}\n{lorem}", options: opt);
				await UniTask.WaitUntil(() => state == State.Off);


				foreach (BustState _state in mode.States) {

					Show(character, $"Mode: {mode.name}, State: {_state.ID}\n{lorem}", opt, new BustAnimConfig{state = _state.ID});
					await UniTask.WaitUntil(() => state == State.Off);
				}

				foreach (KeyValuePair<string, CharacterBustModeAsset> modes in bust.Animator.AltModes) {

					mode = modes.Value;

					Show(character, $"Mode: {mode.name}\n{lorem}", options: opt, new BustAnimConfig{mode = modes.Key});
					await UniTask.WaitUntil(() => state == State.Off);

					foreach (BustState _state in mode.States) {

						Show(character, $"Mode: {mode.name}, State: {_state.ID}\n{lorem}", options: opt, new BustAnimConfig{mode = modes.Key,state = _state.ID});
						await UniTask.WaitUntil(() => state == State.Off);
					}

				}
			}

			foreach (KeyValuePair<Character, ComponentPool<CharacterBust>> pair in CutsceneHUD.Live.BustPools) {

			}
		}

		[ShowInPlay]
		public void Pause()
		{
			if (state != State.Playing) return;
			state = State.Paused;

			Element.Alpha.EnsureComplete();
			Element.Alpha = 0;

			Sequencer.Pause();
		}

		[ShowInPlay]
		public void Unpause()
		{
			if (state != State.Paused) return;
			state = State.Playing;

			Element.Alpha.EnsureComplete();
			Element.Alpha = 1;

			Sequencer.Unpause();
		}
		//--------------------------------------------------------

		async UniTask _show(Character? character, List<GameText> text, DialogueOptions? options = null, BustAnimConfig? bustAnim = null, IDialogueTextboxInvoker invoker = null, [CanBeNull] string nameOverride = null)
		{
			bool was_hidden = false;

			Invoker = invoker;

			if (state == State.Paused)
			{
				// TODO
			}

			if (state == State.Playing)
			{
				Sequencer.StopSequencing(false);
				await _hide(false);
				state = State.Off;
				was_hidden = true;
			}

			TMP_Dialogue.text = "";

			string shownName = DEFAULT_NAME;

			OverrideOptions = options;

			if (nameOverride != null) {
				shownName = nameOverride;
			} else if (character != null) {
				shownName = Names.GetOrDefault(character.Value, character.Value.ToString());
			}

			if (character != null) {
				CharShowing = character.Value;

				if (_currentBust)
				{
					_currentBust.DoTalkForString(text[0]);
					_currentBust.Animator.Mode               = bustAnim.HasValue && bustAnim.Value.mode  != null  ? bustAnim.Value.mode : null;
					_currentBust.Animator.State              = bustAnim.HasValue && bustAnim.Value.state != null ? bustAnim.Value.state : null;
					_currentBust.Animator.StateOverrideEyes  = bustAnim.HasValue && bustAnim.Value.state_eyes != null ? bustAnim.Value.state_eyes : null;
					_currentBust.Animator.StateOverrideMouth = bustAnim.HasValue && bustAnim.Value.state_mouth != null ? bustAnim.Value.state_mouth : null;
					_currentBust.Animator.StateOverrideBrows = bustAnim.HasValue && bustAnim.Value.state_brows != null ? bustAnim.Value.state_brows : null;
				}

				/*if (options.HasValue)
					_currentBust.Muppet = options.Value.special_muppet;*/
			} else {
				CharShowing = Character.None;
			}

			if (TMP_CharName != null)
				TMP_CharName.text = shownName;

			if (CurrentOptions.prefix_name) {
				DialoguePrefix = shownName;
			}

			if (state < State.Playing || was_hidden)
			{
				Element.DoAlphaFade(0, 1, CurrentOptions.transition_in).Tween.ToUniTask();
				if (type == Type.Full)
				{
					await Element.DoOffset(new Vector3(0, -30, 0), Vector3.zero, CurrentOptions.transition_in).Tween.ToUniTask();
				}
				else
				{
					await Element.DoOffset(new Vector3(0, -15, 0), Vector3.zero, CurrentOptions.transition_in).Tween.ToUniTask();
				}
			}

			await UniTask.WaitForEndOfFrame();
			Sequencer.StartSequence(text);
			state = State.Playing;
			Timer = CurrentOptions.auto_delay;
		}

		async UniTask _hide(bool stateChange = true)
		{
			if (CurrentOptions.transition_out <= Mathf.Epsilon)
			{
				_hideInstant(stateChange);
				return;
			}

			Element.DoAlphaFade(1, 0, CurrentOptions.transition_out).Tween.ToUniTask();

			if (type == Type.Full)
			{
				await Element.DoOffset(Vector3.zero, new Vector3(0, -30, 0), CurrentOptions.transition_out).Tween.ToUniTask();
			}
			else
			{

				await Element.DoOffset(Vector3.zero, new Vector3(0, 15, 0), CurrentOptions.transition_out).Tween.ToUniTask();
			}

			if (stateChange)
				state = State.Off;

			Reset();
		}

		void _hideInstant(bool stateChange = true)
		{
			state = State.Off;

			Element.Alpha = 0;

			if (stateChange)
				state = State.Off;

			Reset();
		}

		void Reset()
		{
			OverrideOptions = null;

			CharShowing = Character.None;

			Sequencer.StopSequencing(false);

			TMP_Dialogue.text = "";

			if (TMP_CharName != null)
				TMP_CharName.text = DEFAULT_NAME;

			Invoker = null;

			DialoguePrefix = null;
		}

	}
}