using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Defines an area that could be part of a room.
/// </summary>
public class LevelRoomArea : MonoBehaviour
{
	public enum AreaType { Box, Polygon }
	public AreaType areaType;

	public LevelRoom room;

	[Title("Collider")]
	public bool GenerateCollider = true;
	public Collider collider;

	//All
	[Title("All")]
	public Vector3 Offset;

	//Box/Ellipsis
	[Title("Box")]
	public Bounds BoxBounds;

	//Polygon
	[Title("Polygon")]
	public List<Vector2> Points;
	public float Depth;

	[Title("Misc")]
	public Color EditorDrawColor = Color.blue;

	private void Start()
	{
		if (GenerateCollider)
		{
			if (collider == null)
			{
				if (areaType == AreaType.Box)
				{
					GameObject col = new GameObject();
					col.name = $"Box Collider ({transform.childCount})";
					col.AddComponent<LevelRoomAreaCollider>().area = this;
					col.layer = LayerMask.NameToLayer("LevelRoomArea");
					col.transform.parent = transform;
					col.transform.localPosition = Vector3.zero;

					var box = col.AddComponent<BoxCollider>();
					box.isTrigger = true;
					box.size = BoxBounds.size;
					box.center = Offset + BoxBounds.center;
					collider = box;
				}
			}
		}
	}

	public bool PointInsideArea(Vector3 point)
	{
		switch (areaType)
		{

		case AreaType.Box:
			return BoxBounds.Contains(point);
			break;
		case AreaType.Polygon:

			break;
		}

		return false;
	}

	public bool BoundsInsideArea(Bounds bounds)
	{
		switch (areaType)
		{

		case AreaType.Box:
			return BoxBounds.Intersects(bounds);
			break;
		case AreaType.Polygon:

			break;
		}

		return false;
	}


	private void OnDrawGizmos()
	{
		var c = EditorDrawColor;

		switch (areaType)
		{

		case AreaType.Box:
			c.a = 0.2f;
			Gizmos.color = c;
			Gizmos.DrawCube(transform.position +Offset +BoxBounds.center, BoxBounds.size);

			c.a = 1f;
			Gizmos.color = c;
			Gizmos.DrawWireCube(transform.position+Offset+BoxBounds.center, BoxBounds.size);
			break;
		case AreaType.Polygon:
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
