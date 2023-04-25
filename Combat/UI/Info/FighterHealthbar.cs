using System;
using System.Collections.Generic;
using Combat;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Components.UI;
using Util.RenderingElements.PointBars;

public class FighterHealthbar : SerializedMonoBehaviour
{
	[SerializeField] private CanvasGroup          CanvasGroup;
	[SerializeField] public  PointBar             Bar;
	[SerializeField] public  WorldToCanvasRaycast Raycast;

	[NonSerialized] public HashSet<string> enables;
	[NonSerialized] public Fighter         fighter;
	[NonSerialized] public float           displayTime;

	private Fighter _fighter;

	private void Awake()
	{
		enables     = new HashSet<string>();
		displayTime = 0;
	}

	public void SetVisible(bool b)
	{
		CanvasGroup.alpha = b ? 1 : 0;
	}

	public void SyncFighterValues()
	{
		Bar.Set(fighter.hp, fighter.max_hp);
	}

	public void SyncFighterPos()
	{
		if (fighter.actor != null)
		{
			Raycast.SetWorldPos(fighter.actor);
			Raycast.WorldTransformOffset = Vector3.up * fighter.actor.height;
		}
		else
		{
			Raycast.SetWorldPos(Vector3.zero);
		}
	}
}