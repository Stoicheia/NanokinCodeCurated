using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

// TODO Merge into a single Warp component with WarpVolume
public class WarpReceiver : SerializedMonoBehaviour
{
	[NonSerialized] public static List<WarpReceiver> All;

	public const int NULL_WARP  = -1;
	public const int SPAWN_MENU = -10;

	[WarpRef]
	public int ID = NULL_WARP;

	public Vector3[]        StartingPositionOffset;
	public TransitionVolume AttachedTransition;
	public Cutscene         Cutscene;

	private void Awake()
	{
		AttachedTransition = GetComponent<TransitionVolume>();

		if(Cutscene == null)
			Cutscene = GetComponent<Cutscene>();
	}

	private void OnEnable()
	{
		All = All ?? new List<WarpReceiver>();
		All.AddIfNotExists(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	public static WarpReceiver FindReceiver(int id)
	{
		//Find Receiver
		for (int i = 0; i < All.Count; i++)
		{
			if (All[i].ID == id)
			{
				return All[i];
			}
		}

		return null;
	}

	public Vector3 GetStartingPosition()
	{
		Vector3 centerPos = transform.position;
		if (AttachedTransition != null)
			centerPos = AttachedTransition.GetSideB(-0.8f);

		DebugDraw.DrawMarker(centerPos, 1, Color.green, 4, false);

		return centerPos;
	}


	public static Dictionary<int, string> ID_Names = new Dictionary<int, string>
	{
		{ NULL_WARP, "NONE" },

		//	LEVEL WARPS: Range 1 - 100
		//-------------------------------
		{ 1,    "WARP_LVL_1" },
		{ 2,    "WARP_LVL_2" },
		{ 3,    "WARP_LVL_3" },
		{ 4,    "WARP_LVL_4" },
		{ 5,    "WARP_LVL_5" },
		{ 6,    "WARP_LVL_6" },
		{ 7,    "WARP_LVL_7" },
		{ 8,    "WARP_LVL_8" },
		{ 9,    "WARP_LVL_9" },
		{ 10,   "WARP_LVL_10" },
		{ 11,   "WARP_LVL_11" },
		{ 12,   "WARP_LVL_12" },
		{ 13,   "WARP_LVL_13" },
		{ 14,   "WARP_LVL_14" },
		{ 15,   "WARP_LVL_15" },

		//	INTERIOR WARPS: Range 100 - 200
		//-------------------------------
		{ 101,   "INTERIOR_1" },
		{ 102,   "INTERIOR_2" },
		{ 103,   "INTERIOR_3" },
		{ 104,   "INTERIOR_4" },
		{ 105,   "INTERIOR_5" },
		{ 106,   "INTERIOR_6" },
		{ 107,   "INTERIOR_7" },
		{ 108,   "INTERIOR_8" },
		{ 109,   "INTERIOR_9" },
		{ 110,   "INTERIOR_10" },
		{ 111,   "INTERIOR_11" },
		{ 112,   "INTERIOR_12" },
		{ 113,   "INTERIOR_13" },
		{ 114,   "INTERIOR_14" },
		{ 115,   "INTERIOR_15" },


		//	CUTSCENE WARPS: Range 200 - 300
		//-------------------------------
		{ 201,   "CUTSCENE_1" },
		{ 202,   "CUTSCENE_2" },
		{ 203,   "CUTSCENE_3" },
		{ 204,   "CUTSCENE_4" },
		{ 205,   "CUTSCENE_5" },
		{ 206,   "CUTSCENE_6" },
		{ 207,   "CUTSCENE_7" },
		{ 208,   "CUTSCENE_8" },
		{ 209,   "CUTSCENE_9" },
		{ 210,   "CUTSCENE_10" },
		{ 211,   "CUTSCENE_11" },
		{ 212,   "CUTSCENE_12" },
		{ 213,   "CUTSCENE_13" },
		{ 214,   "CUTSCENE_14" },
		{ 215,   "CUTSCENE_15" },

		//	DEBUG LEVEL HUB: Range 800 - 816
		//-------------------------------
		{ 800,   "WARP_HUB_NORTH" },
		{ 801,   "WARP_HUB_NORTH_EAST" },
		{ 802,   "WARP_HUB_EAST" },
		{ 803,   "WARP_HUB_SOUTH_EAST" },
		{ 804,   "WARP_HUB_SOUTH" },
		{ 805,   "WARP_HUB_SOUTH_WEST" },
		{ 806,   "WARP_HUB_WEST" },
		{ 807,   "WARP_HUB_NORTH_WEST" },
	};
}