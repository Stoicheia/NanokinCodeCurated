using System.Collections.Generic;
using System.Text;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Util;
using Drawing;
using Pathfinding;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overworld.Controllers {
	public class PartyAITool : DebugCameraTool, IPartyLeader {

		// Set goal positions for each member.
		// Set leader center for each member (would require separating formation calculations from PartyLeader)


		public List<PartyMemberBrain>                                Members;
		public Dictionary<PartyMemberBrain, PartyMemberInstructions> MemberReg;

		public int MemberControlling = 0;

		public PartyAITool()
		{
			Members   = new List<PartyMemberBrain>();
			MemberReg = new Dictionary<PartyMemberBrain, PartyMemberInstructions>();
		}

		public override void OnUpdate()
		{
			if (!ActorController.isSpawned) {
				Exit();
				return;
			}

			// Change member controlling using 1-6
			if (Keyboard.current.digit1Key.wasPressedThisFrame && Members.Count >= 1) MemberControlling = 0;
			if (Keyboard.current.digit2Key.wasPressedThisFrame && Members.Count >= 2) MemberControlling = 1;
			if (Keyboard.current.digit3Key.wasPressedThisFrame && Members.Count >= 3) MemberControlling = 2;
			if (Keyboard.current.digit4Key.wasPressedThisFrame && Members.Count >= 4) MemberControlling = 3;
			if (Keyboard.current.digit5Key.wasPressedThisFrame && Members.Count >= 5) MemberControlling = 4;
			if (Keyboard.current.digit6Key.wasPressedThisFrame && Members.Count >= 6) MemberControlling = 5;


			if (MemberControlling < 0 || MemberControlling >= Members.Count) {
				return;
			}


			PartyMemberBrain member = Members[MemberControlling];

			PartyMemberInstructions instructions = MemberReg[member];

			// Modify instructions

			bool inp_setGoal     = GameInputs.InputsEnabled && Mouse.current.leftButton.wasPressedThisFrame;
			bool inp_releaseGoal = GameInputs.InputsEnabled && Mouse.current.rightButton.wasPressedThisFrame;

			(RaycastHit hit, bool ok) = GameCams.RaycastFromCamera(new Vector2(Screen.width, Screen.height) * 0.5f, Layers.Walkable.mask);
            if (ok)
            {
            	Draw.ingame.WireSphere(hit.point, 0.2f, ColorsXNA.White);

				if (inp_setGoal) {
					instructions.GoalPosition = hit.point;

					(NNInfo info, bool infoOk) = MotionPlanning.GetPosOnNavmesh(hit.point);

					if (infoOk && !hit.point.AnyNAN())
						instructions.GoalPositionLocked = info.position;
				}
			}

			foreach (var ins in MemberReg.Values) {
				if(ins.GoalPosition.HasValue)
					Draw.WireSphere(ins.GoalPosition.Value, 0.1f);

				if(ins.GoalPositionLocked.HasValue)
					Draw.WireBox(ins.GoalPositionLocked.Value, 0.1f);

			}

			if (inp_releaseGoal) {
				instructions.GoalPosition = null;
			}

			MemberReg[member] = instructions;
		}


		public PartyMode Mode => PartyMode.Ground;

		public bool PollInstructions(PartyMemberBrain brain, out PartyMemberInstructions instructions) => MemberReg.TryGetValue(brain, out instructions);

		public override void OnActivate()
		{
			foreach (Actor actor in ActorController.partyActors) {
				if(actor.activeBrain is PartyMemberBrain member) {
					member.SetSecondaryLeader(this);
					Members.Add(member);
					MemberReg[member] = PartyMemberInstructions.Default;
				}
			}
		}

		public override void OnDeactivate()
		{
			foreach (var member in Members) {
				member.SetSecondaryLeader(null);
			}

			Members.Clear();
			MemberReg.Clear();
		}

		public override void OnToolbarString(StringBuilder builder)
		{
			builder.Append("Members: [");
			for (var i = 0; i < Members.Count; i++) {
				PartyMemberBrain member = Members[i];

				if(i == MemberControlling) {
					builder.Append("[");
					builder.Append((i + 1).ToString());
					builder.Append("]");
				} else {
					//builder.Append(" ");
					builder.Append((i + 1).ToString());
					//builder.Append("");
				}

				builder.Append(": ");

				builder.Append(member.actor.Character.ToString());

				if(i < Members.Count - 1)
					builder.Append(", ");
			}

			builder.Append("]");
		}

		public override string Name                                                                               => "PartyAI";
		public override string ShortcutString                                                                     => "L";
		public override bool   ShouldActivate()                                                                   => Keyboard.current.lKey.wasPressedThisFrame;


	}
}