using System;
using System.Collections.Generic;
using Animancer;
using Anjin.Scripting;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Util.Odin.Attributes;

namespace Overworld.Anim {

	[Serializable]
	public struct State {
		public string         Key;
		public ClipTransition Clip;
	}

	[RequireComponent(typeof(AnimancerComponent))]
	[LuaUserdata]
	public class SimpleAnimStates : SerializedMonoBehaviour {

		[DrawWithUnity, SerializeField]
		public ClipTransition DefaultState = new ClipTransition();

		[DrawWithUnity]
		public State[] States;

		[NonSerialized]
		public AnimancerComponent _Animancer;

		[NonSerialized, ShowInPlay]
		public Dictionary<string, ClipTransition> RuntimeStates = new Dictionary<string, ClipTransition>();

		private string _state;

		[ShowInPlay]
		[CanBeNull]
		public string State {
			get => _state;
			set {
				if (value == _state)
					return;

				_state = value;

				if (_state != null && RuntimeStates.TryGetValue(_state, out ClipTransition transition)) {
					_Animancer.Play(transition);
				} else if(DefaultState.IsValid) {
					_Animancer.Play(DefaultState);
				}
			}
		}


		private void Awake()
		{
			_Animancer = GetComponent<AnimancerComponent>();

			RuntimeStates = new Dictionary<string, ClipTransition>();

			foreach (State state in States) {
				if(!state.Key.IsNullOrWhitespace() && !RuntimeStates.ContainsKey(state.Key))
					RuntimeStates[state.Key] = state.Clip;
			}

			if(DefaultState.IsValid)
				_Animancer.Play(DefaultState);
		}


		public void set_state(string key)
		{
			State = key;
		}


	}
}