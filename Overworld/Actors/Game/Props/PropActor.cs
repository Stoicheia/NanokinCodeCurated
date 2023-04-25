using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Anjin.Actors
{
	public class PropActor : Actor
	{
		public string StartingState = "";
		[HideInEditorMode, NonSerialized, ShowInInspector] 	public string CurrentState;
		[HideInEditorMode, ShowInInspector] string PrevState;

		[HideInEditorMode, NonSerialized, ShowInInspector]
		public PropState[] ChildStates;

		[HideInEditorMode, NonSerialized, ShowInInspector]
		public Dictionary<string, PropState> StatesRegistry;

		void Awake()
		{
			ChildStates = GetComponentsInChildren<PropState>(true);
			StatesRegistry = new Dictionary<string, PropState>();

			for (int i = 0; i < ChildStates.Length; i++) {
				StatesRegistry[ChildStates[i].name] = ChildStates[i];
			}

			CurrentState = StartingState;
			UpdateState();
		}

		void Update()
		{
			if(PrevState != CurrentState)
				UpdateState();
		}

		public void UpdateState()
		{
			PrevState = CurrentState;

			if (ChildStates == null) return;

			for (int i = 0; i < ChildStates.Length; i++) {
				ChildStates[i].gameObject.SetActive(false);
			}

			if (StatesRegistry.TryGetValue(CurrentState, out var state)) {
				state.gameObject.SetActive(true);
			}
		}

	}
}