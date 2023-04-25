// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AI;
//
// [ExecuteInEditMode]
// [RequireComponent(typeof(NavMeshModifierVolume))]
// public class SurfaceLinkRig : MonoBehaviour
// {
// 	public Vector3 Size   = new Vector3(4.0f, 3.0f, 4.0f);
// 	public Vector3 Center = new Vector3(0,    1.0f, 0);
//
// 	//public List<Vector3> linkPositions;
//
// 	public float linkDensity = 1;
// 	public float linkLength = 1;
//
// 	public  NavMeshModifierVolume modifier;
// 	private BoxCollider           collider;
// 	public  List<NavMeshLink>     links;
//
// 	private void Start()
// 	{
// 		if (Application.isPlaying)
// 		{
// 			UpdateLinks();
// 		}
// 	}
//
// 	private void Update()
// 	{
// 		if (modifier != null)
// 		{
// 			modifier.size   = Size;
// 			modifier.center = Center;
// 			modifier.area = NavMesh.GetAreaFromName("SurfaceLink");
// 		}
// 		else modifier = GetComponent<NavMeshModifierVolume>();
//
// 		if (collider != null)
// 		{
// 			collider.size   = Size;
// 			collider.center = Center;
// 		} else
// 		{
// 			if (Application.isPlaying)
// 				collider = gameObject.AddComponent<BoxCollider>();
// 		}
// 	}
//
// 	private void UpdateLinks()
// 	{
// 		var worldCenter = transform.position + Center;
// 		var worldTop = worldCenter + Vector3.up * Size.y;
// 		var rotatedDir = Quaternion.AngleAxis(transform.rotation.eulerAngles.y,Vector3.up) * Vector3.right;
// 		var forwardDir = Quaternion.AngleAxis(transform.rotation.eulerAngles.y,Vector3.up) * Vector3.forward;
//
// 		//Debug.Log(rotatedDir);
//
// 		points = new List<Vector3>();
// 		if(links == null) links = new List<NavMeshLink>();
// 		else
// 		{
// 			for (int i = 0; i < links.Count; i++)
// 			{
// 				DestroyImmediate(links[i].gameObject);
// 			}
// 			links.Clear();
// 		}
//
// 		int numOnEachSide = Mathf.FloorToInt( ( ( Size.x-0.1f ) * linkDensity ) / 2 ) + 1;
//
// 		Vector3 castStartPoint;
// 		RaycastHit centerHit = new RaycastHit();
// 		GameObject go;
// 		NavMeshLink link;
//
// 		for (int i = 0; i < numOnEachSide; i++)
// 		{
// 			castStartPoint = worldTop + rotatedDir * ( i / linkDensity );
//
// 			if (Physics.Raycast(castStartPoint, Vector3.down, out centerHit, Size.y+1, 1 << LayerMask.NameToLayer("Walkable")))
// 			{
// 				//points.Add(centerHit.point);
// 				go = new GameObject("Link");
// 				link = go.AddComponent<NavMeshLink>();
// 				go.transform.SetParent(transform);
//
// 				links.Add(link);
//
// 				link.transform.position = centerHit.point;
// 				link.startPoint = forwardDir  * linkLength / 2;
// 				link.endPoint   = -forwardDir * linkLength / 2;
// 				link.area = NavMesh.GetAreaFromName("SurfaceLink");
// 			}
//
// 			if(i != 0)
// 			{
// 				castStartPoint = worldTop + rotatedDir * ( i / linkDensity ) * -1;
//
// 				if (Physics.Raycast(castStartPoint, Vector3.down, out centerHit, Size.y + 1,
// 					1 << LayerMask.NameToLayer("Walkable")))
// 				{
// 					//points.Add(centerHit.point);
// 					go   = new GameObject("Link");
// 					link = go.AddComponent<NavMeshLink>();
// 					go.transform.SetParent(transform);
//
// 					links.Add(link);
//
// 					link.transform.position = centerHit.point;
// 					link.startPoint         = forwardDir  * linkLength / 2;
// 					link.endPoint           = -forwardDir * linkLength / 2;
// 					link.area               = NavMesh.GetAreaFromName("SurfaceLink");
// 				}
// 			}
//
// 		}
//
//
// 	}
//
// 	public List<Vector3> points;
//
// 	private void OnDrawGizmos()
// 	{
// 		if (points != null)
// 		{
// 			for (int i = 0; i < points.Count; i++)
// 			{
// 				Gizmos.DrawWireSphere(points[i],0.1f);
// 			}
// 		}
// 	}
// }