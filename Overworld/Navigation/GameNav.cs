// using System.Collections.Generic;
// using NavJob.Systems;
// using UnityEngine;
//
// namespace Anjin.Navigation
// {
// 	public class GameNav : StaticBoyOdin<GameNav>
// 	{
// 		public struct GameNavRequest
// 		{
// 			public Vector3           from;
// 			public Vector3           to;
// 			public IGameNavRequester requester;
//
// 			public GameNavRequest(Vector3 @from, Vector3 to, IGameNavRequester requester)
// 			{
// 				this.@from     = @from;
// 				this.to        = to;
// 				this.requester = requester;
// 			}
// 		}
//
//
// 		protected override void OnAwake()
// 		{
// 			rand            = new System.Random();
// 			PendingRequests = new Dictionary<int, GameNavRequest>();
//
// 			NavMeshQuerySystem.RegisterPathResolvedCallbackStatic(NMQS_OnPathResolved);
// 			NavMeshQuerySystem.RegisterPathFailedCallbackStatic(NMQS_OnPathFailed);
// 		}
//
// 		Dictionary<int, GameNavRequest> PendingRequests;
// 		System.Random                   rand;
//
// 		public (int id, bool success) RequestPath(GameNavRequest request)
// 		{
// 			//Generate ID
// 			var id = rand.Next();
// 			if(!PendingRequests.ContainsKey(id))
// 			{
// 				PendingRequests[id] = request;
// 				NavMeshQuerySystem.RequestPathStatic(id, request.from, request.to);
//
// 				return ( id, true );
// 			}
//
// 			return ( -1, false );
// 		}
//
// 		void NMQS_OnPathResolved(int id, Vector3[] corners)
// 		{
// 			if (!PendingRequests.TryGetValue(id, out var request)) return;
//
// 			PendingRequests.Remove(id);
//
// 			if (request.requester != null)
// 			{
// 				GameNavPath path = new GameNavPath(corners);
// 				request.requester.OnPathResolved(path);
// 			}
// 			else Debug.LogError("GameNav: Requester is null. (ID:" + id + ")");
// 		}
//
// 		void NMQS_OnPathFailed(int id, PathfindingFailedReason reason)
// 		{
// 			if (!PendingRequests.TryGetValue(id, out var request)) return;
//
// 			PendingRequests.Remove(id);
//
// 			if (request.requester != null)
// 				request.requester.OnPathFailed(reason);
// 			else
// 				Debug.LogError("GameNav: Requester is null. (ID:" + id + ")");
// 		}
// 	}
//
// 	public interface IGameNavRequester
// 	{
// 		void OnPathResolved(GameNavPath           path);
// 		void OnPathFailed(PathfindingFailedReason reason);
// 	}
// }