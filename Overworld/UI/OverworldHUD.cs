using System;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.UI;
using Cysharp.Threading.Tasks;
using Overworld.QuestCompass.UI;
using Overworld.Shopping;
using Sirenix.OdinInspector;
using Util.Components.Timers;
using Util.Odin.Attributes;


namespace Anjin.Nanokin.Map
{
	public class OverworldHUD : StaticBoy<OverworldHUD>
	{
		[LuaEnum("OverworldHUD_Modes")]
		public enum Modes {
			Off,
			Radar,
			Full,

			// Only used for specific segments
			Stats,
			StatsAndCredits
		}

		public SprintUI					SprintUI;
		public CollectGainUI			CollectGains;

		public OverworldStatsDisplay   StatsDisplay;
		public OverworldCreditsDisplay CreditsDisplay;

		public ObjectiveRadarUI Radar; //refactor

		//public static bool StatsShowing   => Live.StatsDisplay.state   == OverworldStatsDisplay.State.On;

		// TODO
		public static bool StatsShowing   => false;
		public static bool CreditsShowing => false;
		public static bool RadarShowing   => false;

		public static bool AnyShowing     => StatsShowing || CreditsShowing;

		[ShowInPlay] private bool _creditsEnabled;
		[ShowInPlay] private bool _statsEnabled;
		[ShowInPlay] private bool _radarEnabled;

		[NonSerialized, ShowInPlay]
		public Modes Mode;

		public Modes? OverrideMode {
			get;
			set;
		}

		private int _state = 0;

		public ValTimer StatsTimer;
		public ValTimer CreditsTimer;

		protected override void OnAwake()
		{
			base.OnAwake();

			// TODO(C.L.): Hook the defaults for these into the settings menu
			_statsEnabled   = true;
			_creditsEnabled = true;
			_radarEnabled   = true;

			Mode = Modes.Radar;
		}

		private void Update()
		{
			// Update activation for each element
			bool enabled = GameController.OverworldHUDShowable || OverrideMode.HasValue;

			Modes _mode = OverrideMode ?? Mode;

			bool mode_stats   = (_mode == Modes.Full || _mode == Modes.Stats || _mode == Modes.StatsAndCredits) && !SplicerHub.menuActive && !ShopMenu.menuActive;
			bool mode_credits = (((_mode == Modes.Full || _mode == Modes.StatsAndCredits) && !SplicerHub.menuActive) || SplicerHub.ShouldShowCredits || ShopMenu.menuActive);
			bool mode_radar   = (_mode == Modes.Radar || _mode == Modes.Full) && !SplicerHub.menuActive && !ShopMenu.menuActive;

			if(StatsDisplay.Ready) {
				if (enabled && (_statsEnabled && mode_stats || !StatsTimer.Tick())) {
					if (StatsDisplay.State == TransitionStates.Off)
						StatsDisplay.Transitioner.Show();
				} else {
					if (StatsDisplay.State == TransitionStates.On)
						StatsDisplay.Transitioner.Hide(enabled);
				}
			}

			// Credits

			if (enabled && (_creditsEnabled && mode_credits || !CreditsTimer.Tick())) {
				if (CreditsDisplay.State == TransitionStates.Off)
					CreditsDisplay.Transitioner.Show();
			} else {
				if (CreditsDisplay.State == TransitionStates.On)
					CreditsDisplay.Transitioner.Hide(enabled);
			}

			// Radar
			if (enabled && _radarEnabled && mode_radar) {
				if (Radar.State == TransitionStates.Off)
					Radar.Transitioner.Show();
			} else {
				if (Radar.State == TransitionStates.On)
					Radar.Transitioner.Hide(enabled);
			}
		}

		public void NextMode()
		{
			switch (Mode) {
				case Modes.Off:   Mode = Modes.Radar; break;
				case Modes.Radar: Mode = Modes.Full;  break;
				case Modes.Full:  Mode = Modes.Off;   break;
			}
		}

		public void OnSpawnPlayer()
		{
			Mode = Modes.Radar;
		}


		[Button] public static void ShowStatsTimed(float duration) => Live.StatsTimer.Set(duration);
		[Button] public static void ShowCreditsTimed(float duration) => Live.CreditsTimer.Set(duration);


		// Lua
		//-----------------------------------------------------------------
		[LuaGlobalFunc] public static void hud_override_mode(Modes mode) => Live.OverrideMode = mode;
		[LuaGlobalFunc] public static void hud_release_override()        => Live.OverrideMode = null;

	}
}