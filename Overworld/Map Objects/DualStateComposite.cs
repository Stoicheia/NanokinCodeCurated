using System.Collections.Generic;
using Animancer;
using Anjin.Actors;
using Anjin.Core.Flags;
using Anjin.Editor;
using Anjin.Minigames;
using Anjin.Scripting;
using Anjin.Util;
using Overworld.Tags;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Playables;
using Util.Extensions;
using Util.Odin.Attributes;
using Flags = Anjin.Core.Flags.Flags;

namespace Anjin.Nanokin.Map {

	[LuaUserdata]
	public class DualStateComposite : SerializedMonoBehaviour, IHitHandler<SwordHit>, ITriggerable, IMinigameResettable {

		public enum States {
			A,
			B
		}

		public enum TriggerBehaviours {
			None,
			ToOppositeStateFromInitial,
			ToggleState
		}

		public enum TransitionType {
			None,
			AnimationClip,
			Timeline,
		}

		[Title("Config")]
		[ToggleButton(active_h: 0.3f, active_s: 1)]
		public bool RegisterInLevelTable = false;

		[EnumToggleButtons]
		[LabelText("State")]
		[HideInPlayMode]
		public States InitialState;

		[Optional]
		public string Flag;
		public bool   FlagUpdate;

		[ShowInPlay]
		[EnumToggleButtons]
		public States State {
			get => _state;
			set {
				//if (value == _state) return;
				ChangeState(value);
			}
		}

		public TriggerBehaviours TriggerBehaviour = TriggerBehaviours.ToOppositeStateFromInitial;
		public bool              TriggerOnSwordHit;
		public bool              ParentMinigameCanReset;


		[Title("Transition")]
		// Animation
		[HideLabel, EnumToggleButtons]
		public TransitionType Transition = TransitionType.AnimationClip;
		public ClipTransition FullClip;

		// References
		[Title("References")]
		[Optional] public AnimancerComponent Animator;
		[Optional] public PlayableDirector   Director;

		[Title("Actives")]
		[Optional] public List<GameObject> ActiveA;
		[Optional] public List<GameObject> InactiveA;
		[Optional] public List<GameObject> ActiveB;
		[Optional] public List<GameObject> InactiveB;


		[ShowInPlay]
		private bool _transitioning;

		[ShowInPlay] private bool           _hasAnimancer;
		[ShowInPlay] private bool           _hasDirector;
		[ShowInPlay] private bool           _hasFlag;
		[ShowInPlay] private BoolFlag       _flag;

		[ShowInPlay] private AnimancerState _clipState;
		[ShowInPlay] private TimeMarkerSystem _markerSystem;
		[ShowInPlay] private States         _initialState;
		private              States         _state;

		//private List<IActivable> _activatables_activeA;

		[ShowInPlay]
		public PlayState DirectorState => Director ? Director.state : PlayState.Paused;

		private void Awake()
		{
			if(Director == null) {
				Director = GetComponent<PlayableDirector>();
			}

			_initialState = InitialState;

			_hasAnimancer = Animator != null;
			_hasDirector = Director != null;

			_hasFlag = !Flag.IsNullOrWhitespace() && Flags.Find(Flag, out _flag);

			if (_hasFlag) {
				_flag.AddListener(name, (v, prev) => ChangeState(v, false));
			}

			if (_hasDirector) {
				_markerSystem = Director.GetOrAddComponent<TimeMarkerSystem>();
			}

			if (_hasAnimancer && Transition == TransitionType.AnimationClip) {
				if (FullClip != null) {

					_clipState                = Animator.Play(FullClip);
					_clipState.IsPlaying      = false;
					_clipState.NormalizedTime = 0;

					_clipState.Events.Add(1, () => {
						_clipState.IsPlaying      = false;
						_clipState.NormalizedTime = 1;
						_transitioning            = false;
					});
				}

			} else if (Transition == TransitionType.Timeline && _hasDirector && Director.playableAsset != null) {

				/*Director.stopped += director => {
					Debug.Log("DIRECTOR STOPPED");
				};*/

				Director.SetNormalizedTime(0);
				Director.extrapolationMode =  DirectorWrapMode.None;
			}

			State = InitialState;
		}

		private async void Start()
		{
			//await Lua.initTask;
			await GameController.TillIntialized();
			UniTask2.Frames(1); // Note(C.L. 6-20-22): Find a better way.
			if(RegisterInLevelTable) {
				Lua.RegisterToLevelTable(this);
			}
		}

		private void Update()
		{
			if (_transitioning && Transition == TransitionType.Timeline && _hasDirector) {
				if (_markerSystem.reachedEnd)
					_transitioning = false;
			}
		}

		[ShowInPlay]
		public void ToA() => ChangeState(States.A);

		[ShowInPlay]
		public void ToB() => ChangeState(States.B);

		[ShowInPlay]
		public void ChangeState(bool IsB, bool transition = true) => ChangeState(IsB ? States.B : States.A, transition);

		[ShowInPlay]
		public void ChangeState(States newState, bool transition = true)
		{
			if (_state == newState) return;

			_state = newState;

			bool isA = newState == States.A;
			bool isB = newState == States.B;

			if (_hasFlag && FlagUpdate) {
				_flag.SetValue(isB);
			}

			// TODO(C.L.): This may be slow, but I want to be able to preserve being able to add objects to the active lists at runtime. This will have to be cleaned up at some point.
			foreach (GameObject o in ActiveA)	{
				foreach (IActivable activable in o.GetComponents<IActivable>())
					if (isA)
						activable.OnActivate();
					else
						activable.OnDeactivate();

				o.gameObject.SetActive(isA);
			}

			foreach (GameObject o in InactiveA) {
				foreach (IActivable activable in o.GetComponents<IActivable>())
					if (!isA)
						activable.OnActivate();
					else
						activable.OnDeactivate();

				o.gameObject.SetActive(!isA);
			}

			foreach (GameObject o in ActiveB)	{

				foreach (IActivable activable in o.GetComponents<IActivable>())
					if (isB)
						activable.OnActivate();
					else
						activable.OnDeactivate();

				o.gameObject.SetActive(isB);
			}

			foreach (GameObject o in InactiveB) {

				foreach (IActivable activable in o.GetComponents<IActivable>())
					if (!isB)
						activable.OnActivate();
					else
						activable.OnDeactivate();

				o.gameObject.SetActive(!isB);
			}

			_transitioning = false;

			if (Transition == TransitionType.AnimationClip && _hasAnimancer) {
				if (!transition) {
					_clipState.IsPlaying = false;
					if (isA) {
						_clipState.NormalizedTime = 0;
					} else {
						_clipState.NormalizedTime = 1;
					}
				} else {
					if (isB) {
						_clipState.IsPlaying      = true;
						_clipState.NormalizedTime = 0;
						_transitioning            = true;
					} else {
						_clipState.IsPlaying      = false;
						_clipState.NormalizedTime = 0;
					}
				}
			} else if (Transition == TransitionType.Timeline && _hasDirector) {

				if (!transition) {
					if (isA) {
						Director.SetNormalizedTime(0);
						Director.SetSpeedTo0();
					} else {
						Director.SetNormalizedTime(1);
					}
				} else {
					if (isB) {
						_markerSystem.Reset();
						Director.SetNormalizedTime(0);
						Director.SetSpeedTo1();
						_transitioning = true;
					} else {
						Director.SetNormalizedTime(0);
						Director.SetSpeedTo0();
					}
				}
			}

		}


		public void OnHit(SwordHit      hit) => OnInternalTrigger();
		public bool IsHittable(SwordHit hit) => TriggerOnSwordHit && TriggerBehaviour == TriggerBehaviours.ToggleState || TriggerBehaviour == TriggerBehaviours.ToOppositeStateFromInitial && State == _initialState;

		public void OnInternalTrigger()
		{
			switch (TriggerBehaviour) {
				case TriggerBehaviours.ToOppositeStateFromInitial:
					if (State == _initialState) {
						ChangeState(State == States.A ? States.B : States.A);
					}
					break;

				case TriggerBehaviours.ToggleState:
					ChangeState(State == States.A ? States.B : States.A);
					break;
			}
		}

		public void OnTrigger(Trigger source, Actor actor, TriggerID triggerID = TriggerID.None)
		{
			OnInternalTrigger();
		}

		public void OnMinigameReset()
		{
			if (ParentMinigameCanReset)
			{
				ChangeState(_initialState);
			}
		}
	}
}