using Anjin.Util;
using Sirenix.OdinInspector;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Anjin.Actors
{
	public class CharacterPathfinder : SerializedMonoBehaviour
	{
		/*public List<NavMeshPath> Paths;
		public LayerMask navMeshLayer;*/


		public enum State
		{
			Idle,
			Finding,
			Done
		}


		public State state;

		public bool success;

		public CharacterPath Path;
		CharacterPath        UnsimplifiedPath;

		public PathQueryData currentQueryData;
		public NavMeshQuery  query;

		public int agentTypeID;
		public int areaMask;

		public bool SimplifyWithRaytrace;

		private void Start()
		{
			Path             = new CharacterPath();
			UnsimplifiedPath = new CharacterPath();
		}

		private void Update()
		{
			if (state == State.Finding)
			{
				var status = query.UpdateFindPath(512, out int interationsPerformed);

				if (status != PathQueryStatus.InProgress)
				{
					state = State.Idle;

					if (status == PathQueryStatus.Success)
					{
						var endStatus = query.EndFindPath(out int polySize);

						//Debug.Log($"Pathing ended with status {endStatus}. Now attempting to find path.");

						if (endStatus == PathQueryStatus.Success)
						{
							// // TODO update once the pathfinding jobs have been fixed
							//
							// var polygons = new NativeArray<PolygonId>(polySize, Allocator.Temp);
							// query.GetPathResult(polygons);
							// var straightPathFlags = new NativeArray<StraightPathFlags>(1024, Allocator.Temp);
							// var vertexSide        = new NativeArray<float>(1024, Allocator.Temp);
							// var cornerCount       = 0;
							// var results           = new NativeArray<NavMeshLocation>(1024, Allocator.Temp);
							// var pathStatus = PathUtils.FindStraightPath(
							// 	query,
							// 	currentQueryData.from,
							// 	currentQueryData.to,
							// 	polygons,
							// 	polySize,
							// 	ref results,
							// 	ref straightPathFlags,
							// 	ref vertexSide,
							// 	ref cornerCount,
							// 	1024
							// );
							//
							// if (pathStatus == PathQueryStatus.Success)
							// {
							// 	Path.Points.Clear();
							// 	UnsimplifiedPath.Points.Clear();
							//
							// 	int        lastSimplifiy = 0;
							// 	NavMeshHit raycastHit;
							//
							// 	for (int i = 0; i < cornerCount; i++)
							// 	{
							// 		var point = results[i];
							//
							// 		var type = query.GetPolygonType(point.polygon);
							//
							// 		var  newPoint = new CharacterPathPoint(CharacterPathPoint.Type.Walk, results[i].polygon, results[i].position);
							// 		bool addPoint = true;
							//
							// 		if (i < cornerCount - 1)
							// 		{
							// 			//Figure out the type of point
							// 			if (type == NavMeshPolyTypes.Ground)
							// 			{
							// 				if (SimplifyWithRaytrace && i != 0 && i < cornerCount - 1)
							// 				{
							// 					if (Mathf.Abs(results[i + 1].position.y - results[lastSimplifiy].position.y) > 0.2f
							// 						|| NavMesh.Raycast(
							// 							results[lastSimplifiy].position,
							// 							results[i + 1].position,
							// 							out raycastHit,
							// 							NavMesh.AllAreas))
							// 					{
							// 						lastSimplifiy = i;
							// 					}
							// 					else
							// 					{
							// 						addPoint = false;
							// 					}
							// 				}
							// 			}
							// 			//Handle off mesh connections
							// 			else if (type == NavMeshPolyTypes.OffMeshConnection)
							// 			{
							// 				var nextPoint = results[i + 1];
							// 				var diff      = nextPoint.position.y - point.position.y;
							//
							// 				if (NavMesh.SamplePosition(
							// 					point.position,
							// 					out NavMeshHit hit,
							// 					0.5f,
							// 					1 << NavMesh.GetAreaFromName("SurfaceLink")))
							// 				{
							// 					//Just add the point if it's a surface link
							// 					addPoint = false;
							// 					i++;
							// 				}
							// 				else
							// 				{
							// 					if (diff >= -0.3f)
							// 					{
							// 						//We need to add a command to actually walk to the point before jumping link actions, otherwise it'll do it early.
							// 						Path.Points.Add(newPoint);
							// 						if (diff > 0.3f)
							// 							newPoint.type = CharacterPathPoint.Type.JumpUp;
							// 						else if (Mathf.Abs(diff) < 0.3f)
							// 							newPoint.type = CharacterPathPoint.Type.JumpAcross;
							// 					}
							// 					else
							// 						newPoint.type = CharacterPathPoint.Type.FallDown;
							// 				}
							// 			}
							// 		}
							//
							// 		if (addPoint) Path.Points.Add(newPoint);
							// 		UnsimplifiedPath.Points.Add(newPoint);
							// 	}
							// }
							//
							// //Debug.Log($"Path found with {cornerCount} corners.");
							// success = true;
							// state   = State.Done;
							//
							// straightPathFlags.Dispose();
							// vertexSide.Dispose();
							// results.Dispose();
							// polygons.Dispose();
							// query.Dispose();
						}
					}
					else
					{
						//Debug.Log($"Pathing ended unsucsessfully with status {status}.");
						success = false;
						state   = State.Done;
						query.Dispose();
					}
				}
			}
		}

		public void FindPath(Vector3 desiredLocation)
		{
			if (state != State.Finding)
			{
				query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 2048);

				var extents = Vector3.one * 5;

				var start = query.MapLocation(transform.position, extents, agentTypeID, areaMask);
				var end   = query.MapLocation(desiredLocation, extents, agentTypeID, areaMask);

				var status = query.BeginFindPath(start, end, areaMask);

				if (status == PathQueryStatus.InProgress || status == PathQueryStatus.Success)
				{
					success = false;
					state   = State.Finding;
					currentQueryData = new PathQueryData
					{
						areaMask = areaMask,
						from     = start.position,
						to       = end.position
					};
				}
				else
				{
					//Todo: Figure out error handling for this
					Debug.Log($"Failed to find a path. Status: {status}");
					query.Dispose();
				}
			}
		}


		public struct PathQueryData
		{
			public Vector3 from;
			public Vector3 to;
			public int     areaMask;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			if (Path != null)
			{
				for (int i = 0; i < Path.Points.Count; i++)
				{
					Gizmos.color = Color.red;
					var point = Path.Points[i];
					Gizmos.DrawWireSphere(point.Position, 0.1f);

					if (i != 0)
					{
						var prev = Path.Points[i - 1];
						if (prev.type == CharacterPathPoint.Type.Walk)
							Gizmos.color = Color.blue;
						else if (prev.type == CharacterPathPoint.Type.JumpUp)
							Gizmos.color = Color.magenta;
						else if (prev.type == CharacterPathPoint.Type.JumpAcross)
							Gizmos.color = Color.yellow;
						else if (prev.type == CharacterPathPoint.Type.FallDown)
							Gizmos.color = Color.red;
						Gizmos.DrawLine(prev.Position, Path.Points[i].Position);

						//Handles.Label(prev.Position, i.ToString());
					}
				}
			}

			if (UnsimplifiedPath != null)
			{
				for (int i = 0; i < UnsimplifiedPath.Points.Count; i++)
				{
					Gizmos.color = ColorUtil.MakeColorHSVA(0, 0.7f, 1, 0.8f);
					var point = UnsimplifiedPath.Points[i];
					Gizmos.DrawWireSphere(point.Position, 0.1f);
					var c = Color.HSVToRGB(0.18f, 1, 1);
					if (i != 0)
					{
						var prev = UnsimplifiedPath.Points[i - 1];
						if (prev.type == CharacterPathPoint.Type.Walk)
							Gizmos.color = ColorUtil.MakeColorHSVA(0.55f, 0.7f, 1, 0.8f);
						else if (prev.type == CharacterPathPoint.Type.JumpUp)
							Gizmos.color = ColorUtil.MakeColorHSVA(0.9f, 0.7f, 1, 0.8f);
						else if (prev.type == CharacterPathPoint.Type.JumpAcross)
							Gizmos.color = ColorUtil.MakeColorHSVA(0.18f, 0.7f, 1, 0.8f);
						else if (prev.type == CharacterPathPoint.Type.FallDown)
							Gizmos.color = ColorUtil.MakeColorHSVA(0.0f, 0.7f, 1, 0.8f);
						Gizmos.DrawLine(prev.Position, UnsimplifiedPath.Points[i].Position);

#if UNITY_EDITOR
						Handles.color = Color.red;
						Handles.Label(prev.Position, i.ToString());
#endif
					}
				}
			}
		}
#endif
	}
}