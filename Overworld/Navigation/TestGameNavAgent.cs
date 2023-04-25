// using Anjin.Cameras;
// using NavJob.Systems;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// namespace Anjin.Navigation
// {
// 	public class TestGameNavAgent : SerializedMonoBehaviour, IGameNavRequester
// 	{
// 		public bool 	    waitingForPath;
//
// 		public GameNavPath? currentPath;
//
// 		void Start()
// 		{
// 			waitingForPath = false;
// 			currentPath    = null;
// 		}
//
// 		void OnDestroy()
// 		{
// 			currentPath?.Corners.Dispose();
// 		}
//
// 		void Update()
// 		{
// 			if (!waitingForPath)
// 			{
// 				if (Input.GetMouseButtonDown(0))
// 				{
// 					var (hit, success) = CameraController.RaycastFromCamera(Input.mousePosition.xy(), Layers.Walkable.Mask);
//
// 					if (success)
// 					{
// 						waitingForPath = true;
// 						GameNav.Live.RequestPath(new GameNav.GameNavRequest(transform.position, hit.point, this));
//
// 						if (currentPath.HasValue)
// 						{
// 							currentPath.Value.Corners.Dispose();
// 							currentPath = null;
// 						}
// 					}
// 				}
// 			}
//
// 		}
//
// 		public void OnPathResolved(GameNavPath path)
// 		{
// 			waitingForPath = false;
// 			currentPath    = path;
//
// 			Debug.Log("Resolved " + name);
// 		}
//
// 		public void OnPathFailed(PathfindingFailedReason reason)
// 		{
// 			waitingForPath = false;
// 			currentPath = null;
//
// 			Debug.Log("Failed " + name);
// 		}
//
// 		void OnDrawGizmos()
// 		{
//
// #if UNITY_EDITOR
//
// 			Handles.color = (!waitingForPath) ? Color.white : Color.green;
//
// 			Handles.DrawWireDisc(transform.position, Vector3.up, 0.4f);
//
// 			if (currentPath.HasValue)
// 			{
// 				Handles.color = Color.red;
// 				for (int i = 0; i < currentPath?.Corners.Length; i++)
// 				{
// 					Handles.SphereHandleCap(0, currentPath.Value.Corners[i], Quaternion.identity, 0.4f, EventType.Repaint);
//
// 					Handles.color = Color.white;
//
// 					if(i != 0)
// 						Handles.DrawLine(currentPath.Value.Corners[i-1], currentPath.Value.Corners[i]);
//
// 				}
// 			}
//
// #endif
//
// 			/*Gizmos.color = (!waitingForPath) ? Color.white : Color.green;
//
// 			Gizmos.DrawWireSphere(transform.position, 0.5f);
//
// 			if (currentPath.HasValue)
// 			{
// 				Gizmos.color = Color.red;
// 				for (int i = 0; i < currentPath?.Corners.Length; i++)
// 				{
// 					Gizmos.DrawWireSphere(currentPath.Value.Corners[i], 0.2f);
// 				}
// 			}*/
// 		}
// 	}
// }