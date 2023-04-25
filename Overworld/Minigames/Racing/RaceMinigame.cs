using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Audio;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Cinemachine;
using Combat.Data;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Timeline;
using Util.Math.Splines;
using Util.Odin.Attributes;

namespace Anjin.Minigames.Racing
{
	// TODO:
	// - Support for multiple courses.
	public class RaceMinigame : Minigame, ICoroutineWaitable
	{
		private const string ERR_ZERO_RACERS                  = "Cannot start a race with 0 racers.";
		private const string ERR_MODIFY_RACERS_WHILE_INACTIVE = "Cannot modify racers while the minigame state != inactive.";

		private static readonly RankComparer  _rankComparer  = new RankComparer();
		private static readonly IndexComparer _indexComparer = new IndexComparer();

		[Title("Circuit")]
		public LineShape StartLine;
		public CinemachinePathBase Path;
		[Optional]
		public GameObject RootCheckpoints;
		public bool        GenCheckpoints;

		public bool        ShowMarkers;
		public WorldMarker Prefab_Marker;

		[Optional]
		public GameObject ActiveRoot;
		[ShowIf("@GenCheckpoints"), AssetsOnly]
		public RaceCheckpoint PfbCheckpoint;
		[ShowIf("@GenCheckpoints")] public int     GenCheckpointsDensity = 1;
		[ShowIf("@GenCheckpoints")] public Vector3 GenCheckpointsOffset  = Vector3.zero;

		[Title("Intro")]
		[Optional] public TimelineAsset StartTimeline;
		public bool StartTimelineWait;
		public int  StartCountdown = 3;

		[Title("Race")]
		public List<Racer> Racers;
		[MinValue(1)] public int       Laps = 1;
		public               AudioClip Music;
		public				 AudioClip Overworld;
		public				 AudioClip Victory;
		public				 AudioClip Loss;

		// Runtime
		// ----------------------------------------
		[DebugVars]
		[NonSerialized] public Racer player;
		[NonSerialized]                 private bool   _wasSetup;
		[NonSerialized, UsedImplicitly] public  Action onStart;
		[NonSerialized, UsedImplicitly] public  Action onStartTmp;
		[NonSerialized, UsedImplicitly] public  Action onStop;
		[NonSerialized, UsedImplicitly] public  Action onStopTmp;

		private DisableBrain         _disableBrain;
		private List<RaceCheckpoint> _checkpoints;
		private List<float>          _tmpPathPositions;
		private float                _elapsed;
		private AudioZone            _music;
		private AudioZone			 _overworld;
		private AudioZone			 _victory;
		private AudioZone			 _loss;
		private bool                 _playOutro;

		private bool        _hasMarker;
		private WorldMarker _checkpointMarker;

		private MinigameResults? mostRecentResults;

		private void Awake()
		{
			_wasSetup = false;

			_checkpoints  = new List<RaceCheckpoint>();
			_disableBrain = gameObject.AddComponent<DisableBrain>();
			for (var i = 0; i < RootCheckpoints.transform.childCount; i++)
			{
				Transform child = RootCheckpoints.transform.GetChild(i);

				RaceCheckpoint rcp = child.GetComponent<RaceCheckpoint>();
				rcp.gameObject.SetActive(true);
				rcp.OnHide();
			}

			_music = AudioZone.CreateMusic(Music, priority: 2000);

			//_overworld = AudioZone.CreateMusic(Overworld);
			//_victory = AudioZone.CreateMusic(clip: Victory, loop: false);
			//_loss = AudioZone.CreateMusic(clip: Loss, loop: false);

			if (ActiveRoot != null)
				ActiveRoot.SetActive(false);
		}

		public override void Start()
		{
			base.Start();
			if (ShowMarkers && Prefab_Marker != null) {

				_checkpointMarker = Prefab_Marker.InstantiateNew();
				_checkpointMarker.transform.SetParent(GameHUD.Live.ElementsRect);
				_checkpointMarker.SetTarget();

				_hasMarker = true;
			}
		}

		private void OnDestroy()
		{
			if (_hasMarker) {
				_checkpointMarker.gameObject.Destroy();
				_hasMarker = false;
			}
		}

		/// <summary>
		/// Remove all racers.
		/// </summary>
		public void ClearRacers()
		{
			if (state != MinigameState.Off)
			{
				this.LogError(ERR_MODIFY_RACERS_WHILE_INACTIVE);
				return;
			}

			Racers.Clear();
		}

		/// <summary>
		/// Add a player actor to the race.
		/// </summary>
		/// <param name="actor"></param>
		/// <returns></returns>
		[CanBeNull]
		public Racer AddPlayer(Actor actor)
		{
			if (state != MinigameState.Off)
			{
				this.LogError(ERR_MODIFY_RACERS_WHILE_INACTIVE);
				return null;
			}

			var racer = new Racer {Reference = actor, Type = RacerType.Player};
			Racers.Add(racer);
			return racer;
		}

		/// <summary>
		/// Add a playback actor to the race.
		/// </summary>
		/// <param name="actor"></param>
		/// <param name="playbackData"></param>
		/// <returns></returns>
		[CanBeNull]
		public Racer AddPlayback(Actor actor, ActorPlaybackData playbackData)
		{
			if (state != MinigameState.Off)
			{
				this.LogError(ERR_MODIFY_RACERS_WHILE_INACTIVE);
				return null;
			}

			var racer = new Racer {Reference = actor, Type = RacerType.Playback, PlaybackData = playbackData};
			Racers.Add(racer);
			return racer;
		}


		/// <summary>
		/// Setup the race, placing actors on the start line and taking control away from them.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[Title("Control")]
		[Button]
		[EnableIf("@state == MinigameState.Off")]
		public override async UniTask<bool> Setup([CanBeNull] IMinigameSettings settings = null)
		{
			if (Racers.Count == 0)
			{
				this.LogError(ERR_ZERO_RACERS);
				return false;
			}

			//state        = State.Before;

			if (ActiveRoot != null)
				ActiveRoot.SetActive(true);

			// Get checkpoints
			// ----------------------------------------
			// Get pre-defined checkpoints
			if (RootCheckpoints != null)
			{
				RootCheckpoints.GetComponentsInChildren(true, _checkpoints);
			}

			// Generate checkpoints along the path
			if (GenCheckpoints)
			{
				// TODO cut the actual path in distance units
				// for (var i = 0; i < Path.PathLength.Length - 1; i++)
				// for (var j = 1; j <= GenCheckpointsDensity; j++)
				// {
				// 	if (i == 0 && j == 0) // No checkpoint at the very start of the path
				// 		continue;
				//
				// 	float t = i + (1 / (float) GenCheckpointsDensity) * j;
				//
				// 	Vector3    pos = Path.EvaluatePositionAtUnit(t, CinemachinePathBase.PositionUnits.PathUnits);
				// 	Quaternion rot = Path.EvaluateOrientationAtUnit(t, CinemachinePathBase.PositionUnits.PathUnits);
				//
				// 	pos += GenCheckpointsOffset;
				//
				// 	RaceCheckpoint cp = Instantiate(PfbCheckpoint, pos, rot);
				// 	cp.generated = true;
				// 	cp.OnHide();
				//
				// 	_checkpoints.Add(cp);
				// }
			}

			// TODO sort the checkpoints by progress along the path (might be unnecessary?)

			for (var i = 0; i < _checkpoints.Count; i++)
			{
				RaceCheckpoint cp = _checkpoints[i];
				cp.minigame = this;
				cp.index    = i;

				cp.OnHide();
			}


			// Spawn racers & place on start line
			// ----------------------------------------
			_tmpPathPositions = new List<float>(Racers.Count);
			// Plot[] plots = StartLine.GetPlots(Racers.Count);

			for (var i = 0; i < Racers.Count; i++)
			{
				Racer racer = Racers[i];
				racer.Reset();

				racer.savedType = racer.Type;
				racer.index     = i;

				if (racer.Type == RacerType.Dummy)
					continue;

				if (racer.Reference == null && racer.Prefab)
				{
					// Spawn the racer
					racer.spawned   = true;
					racer.Reference = Instantiate(racer.Prefab);
				}

				if (racer.Reference == null && racer.Type == RacerType.Player)
				{
					racer.Reference = ActorController.playerActor;
				}

				if (racer.Reference == null)
				{
					this.LogError($"Invalid racer (index={i}), couldn't get an actor reference.");
					racer.Type = RacerType.Dummy;
				}
				else
				{
					// Actor setup ----------------------------------------
					Actor actor = racer.Reference;
					Plot  plot  = StartLine.Get(i, Racers.Count);

					actor.Teleport(plot.position);
					actor.Reorient(plot.facing);
					actor.PushOutsideBrain(_disableBrain);

					if(actor is ActorKCC kcc)
						kcc.inputs.NoLook();

					// Racer setup ----------------------------------------
					racer.checkpoint = -1;

					switch (racer.Type)
					{
						case RacerType.Player:
							player = racer;
							ActorController.playerCamera.ReorientForwardInstant();
							break;

						case RacerType.Playback:
							racer.playback      = actor.gameObject.AddComponent<ActorPlayback>();
							racer.playback.Data = racer.PlaybackData;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			Activation.Encounters(false);
			Activation.Collectables(false);
			Activation.NPCs(false);
			Activation.Guests(false);

			await Script_Setup();

			_wasSetup = true;

			return true;
		}

		/// <summary>
		/// Start the race with the intro animations
		/// </summary>
		/*public void StartRace()
		{
			if (Racers.Count == 0)
			{
				this.LogError(ERR_ZERO_RACERS);
				return;
			}

			StartRaceAsync().Forget();
		}*/
		[Button]
		[EnableIf("@state == MinigameState.Off")]
		public override async UniTask<bool> Begin(MinigamePlayOptions options = MinigamePlayOptions.Default)
		{
			if (state != MinigameState.Off || ControlsGame && !GameController.Live.BeginMinigame(this))
				return false;

			if (Racers.Count == 0)
			{
				this.LogError(ERR_ZERO_RACERS);
				return false;
			}

			_playOptions = options;

			if (!_wasSetup)
				await Setup();

			Boot();
			state = MinigameState.Intro;

			//var mute_zone = AudioController.AddMute(AudioLayer.Music, 0.5f, 1000);

			await Script_OnStart();

			// Trying something here...
			//AudioController.RemoveZone(mute_zone);
			//AudioManager.Stop();
			AudioManager.AddZone(_music);
			//AudioManager.Play();

			if ((_playOptions & MinigamePlayOptions.PlayIntro) != 0) await Script_Intro();
			else
			{
				await TextFXHUD.DefaultCountdown();
			}

			TimerHUD.setup(0, "Elapsed");
			TimerHUD.show();

			RaceHUD.setup(0);
			RaceHUD.show();

			await Script_OnRun();

			foreach (Racer racer in Racers)
			{
				if (racer.Type != RacerType.Dummy) {
					if(racer.Type == RacerType.Playback)
						racer.Reference.PopOutsideBrain(_disableBrain);
					else if(racer.Type == RacerType.Player)
						racer.Reference.PushOutsideBrain(ActorController.playerBrain);
				}

				racer.OnStart();

				UpdateCheckpointVisibility(racer);
			}


			onStart?.Invoke();
			onStartTmp?.Invoke();

			state = MinigameState.Running;

			return true;
		}

		/// <summary>
		/// Start the race with the intro animations.
		/// </summary>
		/*public async UniTask StartRaceAsync()
		{
			if (Racers.Count == 0)
			{
				this.LogError(ERR_ZERO_RACERS);
				return;
			}

			if (state < State.Before)
				Setup();

			state = State.Racing;

			// TODO play start timeline
			// TODO use StartIntroAwait to decide if we should wait for the timeline to finish before countdown

			await SceneLoader.GetOrLoadAsync("UI_Countdown");
			await TextFXHUD.DefaultCountdown();

			StartRaceNow();
		}*/

		/// <summary>
		/// Start the race instantly, without animation.
		/// </summary>
		/*public async void StartRaceNow()
		{
		}*/

		/// <summary>
		/// Stop the race.
		/// </summary>
		/*[Button]
		[EnableIf("@state == State.Racing")]
		public void StopRace()
		{
		}*/
		[Button]
		[EnableIf("@state == MinigameState.Running")]
		public override async UniTask Finish(MinigameFinish finish = MinigameFinish.Normal)
		{
			Debug.Log("FINISHING RACE...");

			if (state != MinigameState.Running)
				return;

			Script._state.table["was_quit"] = finish == MinigameFinish.UserQuit;

			state = MinigameState.Outro;

			RefreshRanks();

			bool won = false;

			if (finish == MinigameFinish.DebugWin) {
				won               = true;
				mostRecentResults = new MinigameResults { place = 1 };
			} else if(finish == MinigameFinish.DebugLose) {
				mostRecentResults = new MinigameResults { place = 2 };
			} else {
				mostRecentResults = new MinigameResults { place = player.rank,  was_quit = finish == MinigameFinish.UserQuit};
				won               = player.rank == 1;
			}

			_checkpointMarker.SetTarget();

			await Script_OnFinish();

			foreach (RaceCheckpoint cp in _checkpoints)
			{
				cp.OnHide();
			}

			foreach (Racer racer in Racers)
			{
				racer.Type = racer.savedType;
				switch (racer.Type)
				{
					case RacerType.Player:
						racer.Reference.PopOutsideBrain(ActorController.playerBrain);
						racer.Reference.PopOutsideBrain(_disableBrain);
						break;

					case RacerType.Playback:
						racer.playback.Stop();
						Destroy(racer.playback);
						racer.playback = null;
						break;
				}
			}

			//AudioManager.Stop();
			AudioManager.RemoveZone(_music);
			//AudioManager.Play();

			/*bool won = (!mostRecentResults.Value.was_quit && (mostRecentResults.Value.place == 1));
			PlayResultsMusic(won).Forget();*/

			if (ActiveRoot != null)
				ActiveRoot.SetActive(false);

			onStop?.Invoke();
			onStopTmp?.Invoke();

			TimerHUD.hide();
			RaceHUD.hide();

			UniTask results_music = PlayResultsMusic(won);

			if ((_playOptions & MinigamePlayOptions.PlayOutro) != 0)
				await Script_Outro();

			await results_music;

			await Script_OnEnd();

			CleanupRace();
			AfterFinish();

			_wasSetup = false;

			/*AudioManager.Stop();
			AudioManager.AddZone(_overworld);
			AudioManager.Play();*/
		}

		private async UniTask PlayResultsMusic(bool won)
		{
			if (won) {
				GameSFX.PlayGlobal(Victory);

				/*AudioManager.Stop();
				AudioManager.AddZone(_victory);
				AudioManager.Play();*/

				await UniTask2.Seconds(Victory.length + 0.25f);
			}
			else
			{
				GameSFX.PlayGlobal(Loss);
				/*AudioManager.Stop();
				AudioManager.AddZone(_loss);
				AudioManager.Play();*/

				await UniTask2.Seconds(Loss.length + 0.25f);
			}

		}

		/// <summary>
		/// Cleanup the race.
		/// </summary>
		[Button]
		[EnableIf("@state == MinigameState.Running")]
		public void CleanupRace()
		{
			Racers.Sort(_indexComparer);
			foreach (Racer racer in Racers)
			{
				if (racer.spawned)
				{
					Destroy(racer.Reference.gameObject);

					racer.spawned   = false;
					racer.Reference = null;
				}
			}

			foreach (RaceCheckpoint cp in _checkpoints)
			{
				if (cp.generated)
					Destroy(cp.gameObject);
			}

			_elapsed          = 0;
			player            = null;
			_tmpPathPositions = null;
			onStopTmp         = null;
			onStartTmp        = null;

			// state = MinigameState.Off;

			Activation.Encounters();
			Activation.Collectables();
			Activation.NPCs();
			Activation.Guests();
		}

		[CanBeNull] public override IMinigameResults GetResults() => mostRecentResults;

		public void OnCheckpointReached(RaceCheckpoint checkpoint, GameObject go)
		{
			foreach (Racer racer in Racers)
			{
				if (racer.Type == RacerType.Dummy) continue;
				if (racer.Reference.gameObject == go)
				{
					OnCheckpointReached(checkpoint, racer);
					return;
				}
			}
		}

		public void OnCheckpointReached([NotNull] RaceCheckpoint checkpoint, [NotNull] Racer racer)
		{
			int distance = Mathf.Abs(checkpoint.index - racer.checkpoint);
			if (distance != 1) return;

			Debug.Log($"Racer #{racer.index} reached checkpoint {checkpoint.index + 1} / {_checkpoints.Count}");

			racer.checkpoint = checkpoint.index;

			RefreshRanks();

			if (racer.checkpoint == _checkpoints.Count - 1)
			{
				// Reached finish line
				racer.lap++;
				// TODO update lap UI

				if (racer.lap == Laps)
					OnFinish(racer);
				else
					OnLap(racer);
			}


			UpdateCheckpointVisibility(racer);
		}

		public override void Quit()
		{
			Debug.Log("QUITTING RACE...");

			for (int i = 0; i < Racers.Count; i++)
			{
				Racer racer = Racers[i];
				racer.OnFinish();

				if (racer.Type == RacerType.Player)
				{
					racer.rank = 2;
				}
				else
				{
					racer.rank = 1;
				}
			}

			Finish(MinigameFinish.UserQuit).ForgetWithErrors();
		}

		private void OnLap([NotNull] Racer racer)
		{
			Debug.Log($"Racer #{racer.index} lap {racer.lap + 1} / {Laps}");
			racer.checkpoint = -1;
		}

		private void OnFinish([NotNull] Racer racer)
		{
			if (racer.finished) return;

			racer.OnFinish();

			// NOTE:
			// rank is numbered from 1, so -1 is actually comparing for second to last.
			// Meaning, we don't let the player in last place finish the race as the
			// final outcome is already decided.
			if (racer.rank >= Racers.Count - 1)
			{
				Finish();
			}

			Debug.Log($"Racer #{racer.index} finished rank {racer.rank}!");
		}

		private void UpdateCheckpointVisibility([NotNull] Racer racer)
		{
			if (racer.Type == RacerType.Player)
			{
				if (!racer.finished)
				{
					int idx = racer.checkpoint;

					RaceCheckpoint nextCheckpoint = null;

					if (racer.checkpoint == -1) // Finish line
					{
						_checkpoints.WrapGet(1).OnInactive();

						nextCheckpoint = _checkpoints.WrapGet(0);
						nextCheckpoint.OnActive();

						_checkpoints.WrapGet(_checkpoints.Count - 1).OnInactive();
					}
					else
					{
						_checkpoints.WrapGet(idx - 1).OnInactive();
						_checkpoints.WrapGet(idx).OnInactive();

						nextCheckpoint = _checkpoints.WrapGet(idx + 1);
						nextCheckpoint.OnActive();
					}

					if (_hasMarker) {
						_checkpointMarker.SetTarget(nextCheckpoint.MarkerTarget);
					}

				}
				else
				{
					if (_hasMarker) {
						_checkpointMarker.SetTarget();
					}

					foreach (RaceCheckpoint cp in _checkpoints)
						cp.OnHide();
				}
			}
		}

		private void Update()
		{
			if (state != MinigameState.Running || GameController.IsWorldPaused)
				return;

			// Update path positions
			// ----------------------------------------
			bool pathUpdated = false;
			int  numFinished = 0;

			_tmpPathPositions.Clear();
			for (var i = 0; i < Racers.Count; i++)
			{
				Racer racer = Racers[i];
				Actor actor = racer.Reference;

				if (racer.Type == RacerType.Dummy)
					continue;

				switch (racer.Type)
				{
					case RacerType.Dummy:
						continue;

					case RacerType.Player:
						break;

					case RacerType.Playback:
						if (!racer.finished && !racer.playback.Playing)
						{
							OnFinish(racer);
						}

						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				if (racer.finished)
					numFinished++;

				// Rank update
				float distSqr = racer.pathLastCheck.SqrDistance(actor.Position);
				if (distSqr > 1)
				{
					racer.pathLastCheck = actor.Position;

					float pathUnit  = Path.FindClosestPoint(actor.Position, racer.pathseg, -1, 5);
					float nativePos = Path.ToNativePathUnits(pathUnit, CinemachinePathBase.PositionUnits.PathUnits);
					float normPos   = Path.FromPathNativeUnits(nativePos, CinemachinePathBase.PositionUnits.Normalized);

					racer.pathseg = Mathf.FloorToInt(pathUnit);
					racer.pathpos = normPos;

					pathUpdated = true;
				}
			}

			// update ranks
			if (pathUpdated)
			{
				RefreshRanks();
			}

			_elapsed += Time.deltaTime;
			TimerHUD.set(Mathf.FloorToInt(_elapsed));
			RaceHUD.set(player.rank);

			// TODO update rank label
		}

		private void RefreshRanks()
		{
			Racers.Sort(_rankComparer);
			for (var i = 0; i < Racers.Count; i++)
			{
				Racers[i].rank = i + 1;
			}
		}

		public enum RacerType
		{
			Dummy,
			Player,
			Playback,
		}

		[Serializable]
		[LuaUserdata]
		public class Racer
		{
			public RacerType Type;
			public Actor     Reference;
			[AssetsOnly]
			public Actor Prefab;
			[ShowIf("@Type == RacerType.Playback")]
			public ActorPlaybackData PlaybackData;

			[NonSerialized]
			public Action<Racer> onFinished;

			[NonSerialized, UsedImplicitly]
			public bool finished;

			[NonSerialized, UsedImplicitly]
			public Closure on_finished;

			[DebugVars]
			internal RacerType savedType;
			internal int           index;
			internal bool          spawned;
			internal ActorPlayback playback;

			internal int     checkpoint; // -1 means no checkpoint (start/finish line)
			internal int     rank;       // numbered from 1
			internal int     lap;
			internal float   pathpos;
			internal int     pathseg;
			internal Vector3 pathLastCheck;

			public void Reset()
			{
				spawned       = false;
				finished      = false;
				rank          = 0;
				checkpoint    = -1;
				lap           = 0;
				pathseg       = 0;
				pathpos       = 0;
				pathLastCheck = Vector3.zero;
			}

			public void OnStart()
			{
				switch (Type)
				{
					case RacerType.Player:
						break;

					case RacerType.Playback:
						playback.Play();
						break;
				}
			}

			public void OnFinish()
			{
				if (finished)
					return;

				finished = true;

				onFinished?.Invoke(this);
				Lua.InvokeSafe(on_finished, LuaUtil.Args(this));

				switch (Type)
				{
					case RacerType.Player:
						break;

					case RacerType.Playback:
						playback.Stop();
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public class IndexComparer : IComparer<Racer>
		{
			public int Compare(Racer x, Racer y)
			{
				if (ReferenceEquals(x, y)) return 0;
				if (ReferenceEquals(null, y)) return 1;
				if (ReferenceEquals(null, x)) return -1;

				return x.index.CompareTo(y.index);
			}
		}

		public class RankComparer : IComparer<Racer>
		{
			private const int LAP_SCORE    = 10000;
			private const int FREEZE_SCORE = 1000000;

			public int Compare(Racer x, Racer y)
			{
				if (ReferenceEquals(x, y)) return 0;
				if (ReferenceEquals(null, y)) return 1;
				if (ReferenceEquals(null, x)) return -1;

				float xx = x.pathpos + x.lap * LAP_SCORE;
				float yy = y.pathpos + y.lap * LAP_SCORE;

				if (x.finished) return x.rank * FREEZE_SCORE;
				if (y.finished) return y.rank * FREEZE_SCORE;

				return -xx.CompareTo(yy);
			}
		}

		public bool CanContinue(bool justYielded, bool isCatchup)
		{
			return state == MinigameState.Off;
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class RaceMinigameProxy : MinigameLuaProxy<RaceMinigame>
		{
			public int player_rank => proxy.player.rank;

			public void clear_racers()
			{
				proxy.ClearRacers();
			}

			public void add_player(Actor actor)
			{
				proxy.AddPlayer(actor);
			}

			public void add_playback(Actor actor, ActorPlaybackData data)
			{
				proxy.AddPlayback(actor, data);
			}

			/*public void setup()
			{
				proxy.();
			}*/

			/*public void start()
			{
				proxy.StartRace();
			}*/

			/*public void stop()
			{
				proxy.StopRace();
			}*/

			/*public void start_now()
			{
				proxy.StartRaceNow();
			}*/

			public void cleanup()
			{
				proxy.CleanupRace();
			}
		}
	}
}