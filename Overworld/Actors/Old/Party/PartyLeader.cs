
#define PARTY_LEADER_SAFETY_CHECKS
using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.MP;
using Anjin.Util;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Util;
using Util.Components.Timers;
using Util.ConfigAsset;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public enum PartyMode {
		Ground,
		Swimming,
	}

	public class PartyLeader : SerializedMonoBehaviour, IPartyLeader
	{


		public enum PartyFormation
		{
			// TODO (C.L.): New: No particular formation. Figure out what to do here.
			None,

			// Stand to either side in a V shape
			//	X   X
			//	 X X
			//	  P

			VShape,

			// Each party member walks directly behind the player in a single file.
			// We should switch to this when the
			SingleFile,

			//
			Caterpillar,

			Swimming,
		}


		// Settings
		//-------------------------------------------
		public SettingsAsset<Settings> SettingsAsset;
		public Settings                settings => SettingsAsset;

		public class Settings
		{

			public float   SingleFileFollowDistance     = 1.0f;
			public float   SingleFileSwimFollowDistance = 3f;

			public float[] VShapeAngles                 = {115, 180, 248};
			public float   GoalPosLerp                  = 0.3f;

			public float SwimExitGraceTime = 1;

			public int   RadarRaycasts      = 12;
			public float RadarRadius        = 4;
			public float RadarCurrentRadius = 0;

			public bool DoReversal    = true;
			public bool LockToNavmesh = true;
		}


		// Runtime
		//-------------------------------------------
		[NonSerialized, ShowInPlay] public PlayerActor            Player;

		[ShowInPlay]                public PartyMode Mode { get; set; }
		[NonSerialized, ShowInPlay] public PartyFormation  Formation;

		[NonSerialized, ShowInPlay] public List<PartyMemberBrain>                                Members;
		[NonSerialized, ShowInPlay] public Dictionary<PartyMemberBrain, PartyMemberInstructions> MemberReg;

		[NonSerialized, ShowInPlay] public Vector3? GoalPosCenter;
		[NonSerialized, ShowInPlay] public float    PlrFacingDir;
		[NonSerialized, ShowInPlay] public float    PlrLastFacingDir;
		[NonSerialized, ShowInPlay] public Vector3  InputDirection;

		[NonSerialized, ShowInPlay] public List<(Vector3 pt, float distToNext)> CaterPoints;
		[NonSerialized, ShowInPlay] public Vector3                              LastCaterPos;


		[NonSerialized, ShowInPlay] public MPPath path;

		[ShowInPlay] private ValTimer _swimExitGraceTimer;

		[NonSerialized, ShowInPlay]
		private bool _teleported;

		private void Awake()
		{
			Members     = new List<PartyMemberBrain>();
			MemberReg   = new Dictionary<PartyMemberBrain, PartyMemberInstructions>();
			CaterPoints = new List<(Vector3 pt, float distToNext)>();
		}

		void Start()
		{
			Player       = GetComponent<PlayerActor>();
			PlrFacingDir = Player.facing.xz().ToDegrees();
			Player.OnLand.AddListener(OnLand);

			GoalPosCenter = transform.position;
			LastCaterPos  = transform.position;

			Mode = PartyMode.Ground;

			for (int i = 0; i < Members.Count; i++)
			{
				Members[i].SetLeader(this);
				MemberReg[Members[i]] = PartyMemberInstructions.Default;
			}

			Player.OnTeleport += v => {
				_teleported = true;
			};
		}

		public void Update()
		{
			if (Members.Count == 0) return;

			float currentDir = Player.facing.xz().ToDegrees();
			//GoalPosCenter = transform.position;

			// If the actor is on the ground, we just use its position.
			// If in the air, we cast down and try to see if we can find ground below.
			// All positions are snapped to the navmesh.

			GoalPosCenter = null;

			PartyMode prevMode      = Mode;

			// Decide mode
			switch (Mode) {
				case PartyMode.Ground: {

					if (Player.IsSwimming) {
						Mode        = PartyMode.Swimming;
						_teleported = true;
						_swimExitGraceTimer.Set(settings.SwimExitGraceTime);
						UpdateGoalCenter(transform.position, GraphLayer.Water, 3f);
						break;
					}

					if (Player.IsMotorStable) {
						// if we're on the ground, we try to find a stable navmesh position to work off of
						UpdateGoalCenter(transform.position, GraphLayer.Main);
					} else {

						// If we're in the air, we should instead cast down to find the nearest ground
						if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10, Layers.Walkable.mask)) {
							UpdateGoalCenter(hit.point, GraphLayer.Main, 1);
						}
					}

					UpdateRadar();

					if (settings.RadarCurrentRadius > 0.7f)
						Formation = PartyFormation.VShape;
					else
						Formation = PartyFormation.SingleFile;

				} break;

				case PartyMode.Swimming: {

					// If we aren't actually swimming, we need to check if we've jumped out of the water and are likely to get back in.
					bool swimming = Player._swim;

					if (Player.IsAirState) {
						swimming = true;
					} else if (!Player._swim && !Player.IsGroundState) {

						// If we're not grounded (out of the water), then we should still try see if we're above water. If we aren't then we probably aren't swimming.
						if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 5, Layers.Walkable.mask)) {
							UpdateGoalCenter(hit.point, GraphLayer.Main);
						}
					}

					if (!swimming) {
						// Even after all of that, have a small delay
						if (_swimExitGraceTimer.Tick()) {
							// Members would do their transitions here in reaction to us.
							_teleported = true;
							Mode        = PartyMode.Ground;
							UpdateGoalCenter(transform.position, GraphLayer.Main, 3f);
							break;
						}
					} else {
						_swimExitGraceTimer.Set(settings.SwimExitGraceTime);
					}

					Formation = PartyFormation.Swimming;
					UpdateGoalCenter(transform.position, GraphLayer.Water, 3);

				} break;
			}

			if (Player.inputs.hasMove)
			{
				float diff = Quaternion.Angle(Quaternion.LookRotation(Player.inputs.move.normalized),
											  Quaternion.LookRotation(InputDirection.normalized));

				//Only do a reverse if we're in a v shape.
				if (Formation == PartyFormation.VShape) {
					if (diff > 90 && settings.DoReversal)
						Members.Reverse();
				}

				InputDirection = Player.inputs.move;
			}

			// DECIDE ON GOAL POSITIONS FOR PARTY MEMBERS
			//==============================================

			//Set all the positions
			for (int i = 0; i < Members.Count; i++) {
				PartyMemberInstructions ins;
				PartyMemberBrain        member = Members[i];

				#if PARTY_LEADER_SAFETY_CHECKS
				if (!MemberReg.TryGetValue(member, out ins))
					continue;
				#else
				ins = MemberReg[member];
				#endif

				ins.GoalPosInMotion = false;
				ins.IsMoving        = false;

				if(prevMode != Mode) {
					GraphLayer layer = Mode == PartyMode.Swimming ? GraphLayer.Water : GraphLayer.Main;

					Vector3 center = GoalPosCenter.GetValueOrDefault(transform.position);
					Vector3 goal   = center + RNG.InCircle * 2f;
					ins.GoalPosition = goal;

					TryCastLockGoal(ref ins, center, goal, layer, 2f);
					member.InitiateModeTransition(Mode, ins.GoalPositionLocked.GetValueOrDefault(goal));

				} else if (GoalPosCenter.HasValue) {
					CalcGoalPositionForMember(i, member, ref ins, GoalPosCenter.Value, _teleported);
				}

				MemberReg[member] = ins;
			}

			PlrLastFacingDir = currentDir;
			_teleported      = false;
		}

		void CalcGoalPositionForMember(int i,
									   PartyMemberBrain               member,
									   ref PartyMemberInstructions    ins,
									   Vector3                        leaderCenter,
									   bool                           teleport)
		{
			Vector3 base_dir = GetBaseDir();

			if (!ins.GoalPosition.HasValue)
				ins.GoalPosition = leaderCenter;

			Vector3 current_pos  = ins.GoalPosition.Value;
			float   current_dist = Vector3.Distance(current_pos, leaderCenter);

			Vector3 new_pos = current_pos;

			var dt_constant = Time.deltaTime * 60;

			GraphLayer graphLayer = GraphLayer.Main;
			if (Mode == PartyMode.Swimming)
				graphLayer = GraphLayer.Water;

			switch (Formation)
			{
				case PartyFormation.Swimming:
				case PartyFormation.SingleFile:
				{
					var single_file_dist = settings.SingleFileFollowDistance * (i + 1);
					if (Formation == PartyFormation.Swimming) {
						single_file_dist = settings.SingleFileSwimFollowDistance * (i + 1);
					}

					if (current_dist <= single_file_dist)
					{
						new_pos = current_pos;
					}
					else
					{
						if (!teleport)
						{
							Vector3 to_goal = (leaderCenter - current_pos).normalized;
							to_goal *= (current_dist - single_file_dist) * dt_constant;
							new_pos =  current_pos + to_goal;
						}
						else
						{
							Vector3 goal_target = leaderCenter + single_file_dist * -Player.transform.forward;
							MotionPlanning.RaycastOnGraph(leaderCenter, goal_target, out var info, graphLayer);
							new_pos = info.point;
						}
					}
				}
				break;

				case PartyFormation.VShape:
				{
					Vector2 slotAngle = MathUtil.AnglePosition(transform.forward.xz(), settings.VShapeAngles[i] + 180, 1).normalized;

					if (!teleport && current_dist <= settings.SingleFileFollowDistance)
						new_pos = current_pos;
					else
					{
						if (!teleport)
						{
							// Get the direction to the goal.
							//

							// Get direction from us to the leader center
							Vector3 to_leader = (leaderCenter - current_pos).normalized;

							// Use the direction vector and get a point backed up towards us from the leader, by the follow distance.
							to_leader *= (current_dist - settings.SingleFileFollowDistance) * dt_constant;

							DebugDraw.DrawVector(current_pos + Vector3.up * 0.25f, to_leader, 1f, 0.2f, Color.green, 0, false);
							new_pos = current_pos + to_leader;

							//Vector3 target_space = ( leaderCenter - current_pos );

							Vector3 target_dir = new Vector3(slotAngle.x, 0, slotAngle.y);
							DebugDraw.DrawVector(current_pos + Vector3.up * 0.25f, target_dir, 1f, 0.2f, Color.green, 0, false);

							Vector3 target_space = Vector3.RotateTowards(to_leader.normalized, target_dir.normalized, 1f, 0) * to_leader.magnitude;
							DebugDraw.DrawVector(current_pos, target_space, 1f, 0.2f, Color.red, 0, false);

							Vector3 world_space = current_pos + target_space;
							new_pos += (world_space - new_pos);
						}
						else
						{
							Vector3 goal_target = leaderCenter + Quaternion.Euler(0, settings.VShapeAngles[i], 0) * Player.transform.forward;
							MotionPlanning.RaycastOnGraph(leaderCenter, goal_target, out var info, graphLayer);
							new_pos = info.point;
						}
					}
				}
				break;
			}

			ins.PrevGoalPosition = ins.GoalPosition;

			ins.GoalPosition       = !teleport ? Vector3.Lerp(ins.GoalPosition.Value, new_pos, settings.GoalPosLerp) : new_pos;
			ins.GoalPositionLocked = null;

			// Try to snap the goal to the navmesh.
			TryCastLockGoal(ref ins, leaderCenter, ins.GoalPosition.Value, graphLayer, 2f);
			/*MotionPlanning.RaycastOnGraph(leaderCenter, ins.GoalPosition.Value, out GraphHitInfo hit, graphLayer, searchRadius: 2f);
			if (!hit.point.AnyNAN())
				ins.GoalPositionLocked = hit.point;*/

			ins.GoalPosInMotion = Vector3.Distance(ins.PrevGoalPosition.Value, ins.GoalPosition.Value) > 0.01f;
			ins.IsMoving        = Vector3.Distance(ins.PrevGoalPosition.Value, ins.GoalPosition.Value) > 0.2f;

			ins.FacingDirection         = Vector3.Slerp(ins.FacingDirection, Player.facing, 0.3f);
			ins.DirectionAtGoalPosition = base_dir;

			ins.IsRunning = Player.IsRunning;

			var hor_speed = Player.velocity.xz();
			ins.LeaderSpeed = Mathf.Clamp(hor_speed.magnitude, 0, 10);
			ins.NoSnap      = hor_speed.magnitude >= 10f;

			//ins.Swimming = Mode == PartyLeaderMode.Swimming;
		}

		void TryCastLockGoal(ref PartyMemberInstructions instructions, Vector3 start, Vector3 end, GraphLayer layer, float searchRadius)
		{
			MotionPlanning.RaycastOnGraph(start, end, out GraphHitInfo hit, layer, searchRadius);
			if (!hit.point.AnyNAN()) {
				instructions.GoalPositionLocked = hit.point;
			}
		}

		void UpdateGoalCenter(Vector3 referncePoint, GraphLayer layer, float searchRadius = MotionPlanning.NAVMESH_SAMPLE_RADIUS)
		{
			(NNInfo info, bool ok) = MotionPlanning.GetPosOnNavmesh(referncePoint, layer, searchRadius);
			if (ok) GoalPosCenter = info.position;
		}

		void UpdateRadar()
		{
			Profiler.BeginSample("Party Leader Radar Calculation");
			{
				settings.RadarCurrentRadius = settings.RadarRadius;
				var pos     = transform.position;
				var forward = Vector3.forward * settings.RadarRadius;
				for (int i = 0; i < settings.RadarRaycasts; i++)
				{
					Quaternion spreadAngle = Quaternion.AngleAxis(i * (360f / settings.RadarRaycasts), new Vector3(0, 1, 0));

					var angledPos = spreadAngle * forward;

					var did_hit = MotionPlanning.RaycastOnGraph(pos, pos + angledPos, out var info, searchRadius: 3, snapEnd: false);

					if (did_hit && info.distance < settings.RadarCurrentRadius)
						settings.RadarCurrentRadius = info.distance;

					/*DebugDraw.DrawMarker(angledPos, 3, Color.yellow, 0, false);

					DebugDraw.DrawVector(pos, (info.point - pos).normalized, (did_hit ? info.distance : RadarRadius),
										 (did_hit ? 0.5f : 0), (did_hit ? Color.red : Color.green), 0, false);*/
				}

				//DebugDraw.DrawMarker(pos, 3, Color.magenta, 0, false);
			}
			Profiler.EndSample();
		}

		// TODO
		/*void CalculateCaterpillar()
		{
			var baseDir = GetBaseDir();

			//Calculate Caterpillar
			var dist = Vector3.Distance(transform.position, LastCaterPos);
			if (dist > 0.25)
			{
				if (CaterPoints.Count > 0)
				{
					var p = CaterPoints[CaterPoints.Count - 1];
					CaterPoints[CaterPoints.Count - 1] = (p.pt, dist);
				}

				CaterPoints.Insert(0, (transform.position, 0));
				LastCaterPos = transform.position;
			}

			while (CaterPoints.Count > 30)
			{
				CaterPoints.RemoveAt(CaterPoints.Count - 1);
			}


			if (CaterPoints.Count > 0)
			{
				float segmentLength = 2f;
				float segmentPos    = segmentLength;
				int   segment       = 0;

				Vector3 first = CaterPoints[0].pt;

				for (int i = 0; i < Members.Count; i++)
				{
					var mem = Members[i];
					var ins = mem.Instructions;


					//newPos       = TargetCenter - (baseDir * (behindDistance + behindDistance * i));

					ins.IsMoving         = true /* && lerpTime > 0#1#;
					ins.PrevGoalPosition = ins.GoalPosition;
					ins.GoalPosition     = GetCaterpillarPoint(segmentLength * (i + 1));

					/*if (LockToNavmesh)
					{
						//NavMesh.Raycast(TargetCenter, newPos, out NavMeshHit hit, -1);
						MotionPlanning.RaycastOnGraph(TargetCenter, newPos, out GraphHitInfo hit);
						//DO THIS TO KEEP THE TARGET POSITION FROM TURNING INTO (NaN,NaN,NaN)
						if (!(float.IsInfinity(hit.point.x) || float.IsInfinity(hit.point.y) || float.IsInfinity(hit.point.z)))
							newPos = hit.point;
					}#1#

					/*if (lerpPosition && ins.GoalPosition.HasValue)
						ins.GoalPosition = Vector3.Lerp(ins.GoalPosition.Value, newPos, lerpTime);
					else#1#

					if (ins.PrevGoalPosition.HasValue && ins.GoalPosition.HasValue)
					{
						ins.GoalPosInMotion = Vector3.Distance(ins.PrevGoalPosition.Value, ins.GoalPosition.Value) > 0.001f;
					}

					ins.DirectionAtGoalPosition = baseDir;

					/*ins.FacingDirection = Vector3.Slerp(ins.FacingDirection, actor.FacingDirection, 0.3f);
					ins.IsRunning       = actor.IsRunning;
					ins.LeaderSpeed     = actor.CurrentVelocity.magnitude;#1#

					mem.Instructions = ins;
				}
			}
		}

		Vector3 GetCaterpillarPoint(float length)
		{
			if (CaterPoints.Count < 2) return CaterPoints[0].pt;
			float   dist = 0;
			Vector3 p1   = CaterPoints[0].pt;
			Vector3 p2   = CaterPoints[1].pt;

			for (int i = 0; i < CaterPoints.Count - 1; i++)
			{
				var toPoint = Vector3.Distance(p1, p2);
				p1 = p2;
				p2 = CaterPoints[i + 1].pt;

				if (dist + toPoint >= length) break;
				dist += toPoint;
			}

			return p1 + (p2 - p1).normalized * -(length - dist);
		}*/

		Vector3 GetBaseDir()
		{
			Vector2 dir = (Quaternion.Euler(0, 0, PlrFacingDir) * Vector2.right).normalized;
			return new Vector3(dir.x, 0, dir.y);
		}

		public void OnLand()
		{
			//CalculateGoalPositions(true);
		}

		public void AddPartyMember(PartyMemberBrain member)
		{
			if (Members.Any(x => x == member)) return;

			Members.Add(member);
			member.SetLeader(this);
			MemberReg[member] = PartyMemberInstructions.Default;
		}

		public void RemovePartyMember(PartyMemberBrain member)
		{
			Members.RemoveAll(x => x == member);
			if (member.Leader == this) {
				member.SetLeader(null);
				MemberReg.Remove(member);
			}
		}

		public bool            PollInstructions(PartyMemberBrain member, out PartyMemberInstructions instructions) => MemberReg.TryGetValue(member, out instructions);

		#if UNITY_EDITOR

		private void OnDrawGizmos()
		{
			/*Vector3 pos;
			float degree;*/

			/*for (int i = 0; i < 16; i++)
			{
				degree = i * 22.5f;
				Vector2 dir = ( Quaternion.Euler(0, 0, degree) * Vector2.right ).normalized;
				pos = transform.position + new Vector3(dir.x,0,dir.y)*1.5f;

				Handles.color = ColorUtil.MakeColorHSVA(degree /360, 0.9f, 0.9f,0.05f);
				Handles.DrawSolidDisc(pos, Vector3.up, 0.4f);
				Handles.color = ColorUtil.MakeColorHSVA(degree /360, 0.9f, 0.9f, 1.0f);
				Handles.DrawWireDisc(pos, Vector3.up, 0.4f);
				Handles.DrawWireDisc(pos, Vector3.up, 0.3999f);
				Handles.DrawWireDisc(pos, Vector3.up, 0.3998f);
			}*/

			/*for (int i = 0; i < 12; i++)
			{
				degree = i * 30f;
				Vector2 dir = ( Quaternion.Euler(0, 0, degree) * Vector2.right ).normalized;
				pos = transform.position + new Vector3(dir.x, 0, dir.y) *1.5f;

				Handles.color = ColorUtil.MakeColorHSVA(degree /360, 0.9f, 0.9f, 0.05f);
				Handles.DrawSolidDisc(pos, Vector3.up, 1.0f);
				Handles.color = ColorUtil.MakeColorHSVA(degree /360, 0.9f, 0.9f, 1.0f);
				Handles.DrawWireDisc(pos, Vector3.up, 1.0f);
				Handles.DrawWireDisc(pos, Vector3.up, 0.9999f);
				Handles.DrawWireDisc(pos, Vector3.up, 0.9998f);
			}*/


			if (EditorApplication.isPlaying)
			{
				Vector2 dir = (Quaternion.Euler(0, 0, PlrFacingDir) * Vector2.right).normalized;

				//Debug.Log(dir);

				/*Handles.color = Color.red;
				Handles.DrawLine(transform.position, transform.position + Player.CurrentVelocity * 2);*/

				if (GoalPosCenter.HasValue)
					Gizmos.DrawWireSphere(GoalPosCenter.Value, 0.2f);

				//Gizmos.color = Color.green;
				//Gizmos.DrawWireSphere(OriginalCenter, 0.2f);
				int i = 0;
				foreach (var kvp in MemberReg) {

					var ins = kvp.Value;

					if (!ins.GoalPosition.HasValue) continue;

					var pos = ins.GoalPositionLocked.GetValueOrDefault(ins.GoalPosition.Value);

					Gizmos.color = Color.red;
					Gizmos.DrawWireCube(ins.GoalPosition.Value, new Vector3(0.2f, 0.2f, 0.2f));

					if (ins.GoalPosInMotion)
						Gizmos.color = Color.cyan;
					else
						Gizmos.color = Color.magenta;

					Gizmos.DrawWireCube(pos, new Vector3(0.2f, 0.2f, 0.2f));

					Gizmos.color = Color.green;
					Gizmos.DrawLine(pos, pos + ins.DirectionAtGoalPosition * 0.4f);

					Handles.color = Color.red;
					Handles.Label(pos + Vector3.up * 0.5f, i.ToString());

					i++;
				}


				/*if (caterpillarPoints != null) {
					#if UNITY_EDITOR
					MotionPlanning.DrawPolyLineInEditor(caterpillarPoints.Select(x=>x.pt).ToList(), Color.black, false, p =>
					{
						Handles.color = Color.red;
						Handles.DotHandleCap(0, p, Quaternion.identity, 0.04f, EventType.Repaint);
					});
					#endif
				}*/
			}
		}
#endif
	}
}