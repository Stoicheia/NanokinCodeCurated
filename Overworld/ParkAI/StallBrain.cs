using System;
using System.Collections.Generic;
using Anjin.Regions;
using UnityEngine;
using Util;

namespace Anjin.Nanokin.ParkAI {

	public interface IAgentBrain {
		void Update(float dt);

		void Control(PeepAgent agent);
		//void ReleaseControl(PeepAgent agent);

		void Init();
	}

	// Something that can control agents on the graph
	public abstract class AgentBrain<STATE> : IAgentBrain
		where STATE : struct, IControlledAgentState
	{
		[NonSerialized]
		public List<ControlledAgent> Controlling;

		public void Init()
		{
			Controlling = new List<ControlledAgent>();
		}

		public virtual void Update(float dt)
		{
			for (int i = 0; i < Controlling.Count; i++) {
				ControlledAgent agent = Controlling[i];

				agent.actions.UpdateForAgent(agent.agent, dt, SimLevel.InMap, PeepLOD.LOD0);

				UpdateAgent(ref agent, dt);

				Controlling[i] = agent;
			}

			// Release any agents after the main loop.
			for (int i = 0; i < Controlling.Count; i++) {
				ControlledAgent controlled = Controlling[i];
				if (controlled.releaseFlag) {
					controlled.agent.IsBrainControlled = false;
					controlled.agent.Brain              = null;
					Controlling.RemoveAt(i);
					i--;
				}
			}
		}

		public abstract void UpdateAgent(ref ControlledAgent agent, float dt);

		public abstract STATE GetStateForAgent(PeepAgent agent);

		public void Control(PeepAgent agent)
		{
			if (agent.IsBrainControlled) return;

			agent.IsBrainControlled = true;
			agent.Brain             = this;

			ControlledAgent controlled = new ControlledAgent {
				agent 	= agent,
				state 	= GetStateForAgent(agent),
				actions = new PeepAgent.ActionQueue(),
			};

			OnGainControl(agent, ref controlled);

			Controlling.Add(controlled);
		}

		public virtual void OnGainControl(PeepAgent agent, ref ControlledAgent controlled) {  }

		/*public void ReleaseControl(PeepAgent agent)
		{
			agent.IsBrainControlled = false;
			agent.Brain             = null;

			for (int i = 0; i < Controlling.Count; i++) {
				if (Controlling[i].agent == agent) {
					Controlling.RemoveAt(i);
					break;
				}
			}
		}*/

		public void ReleaseIfNotActing(ref ControlledAgent agent)
		{
			if(agent.actions.state == PeepAgent.ActionQueue.State.Idle) {
				//ReleaseControl(agent.agent);
				agent.releaseFlag = true;
			}
		}

		public struct ControlledAgent
		{
			public PeepAgent             agent;
			public STATE                 state;
			public PeepAgent.ActionQueue actions; // TODO: Pool?
			public bool                  releaseFlag;
		}

		public virtual AgentBrain<STATE> Instantiate() => (AgentBrain<STATE>)MemberwiseClone();
	}

	public interface IControlledAgentState { }

	public class StallBrain : AgentBrain<StallBrain.State> {

		public FloatRange TimeRange;

		public void Init(ParkAIStall stall) => base.Init();

		public override void OnGainControl(PeepAgent agent, ref ControlledAgent controlled)
		{
			float time = TimeRange.RandomInclusive;
			Debug.Log($"Stall: gain control of {agent.ID}. Time: {time}");

			controlled.actions.Stand(time);
			controlled.actions.Start();
		}

		public override void  UpdateAgent(ref ControlledAgent agent, float dt) => ReleaseIfNotActing(ref agent);
		public override State GetStateForAgent(PeepAgent      agent) => new State();

		public struct State : IControlledAgentState {
			public float timer;
		}
	}
}