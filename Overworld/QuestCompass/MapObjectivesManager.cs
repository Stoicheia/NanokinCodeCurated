using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEditor.UIElements;
using UnityEngine;

namespace Overworld.QuestCompass
{

	public class MapObjectivesManager : MonoBehaviour
	{
		public static event Action OnReevaluation;
		private List<MapObjective> _objectives;
		public List<MapObjective> Objectives => _objectives;

		private void Awake()
		{
			MapObjective.Manager = this;
			_objectives = new List<MapObjective>();
		}

		public void Reevaluate()
		{
			Clear();
			OnReevaluation?.Invoke();
		}

		private void Clear()
		{
			_objectives.Clear();
		}

		public void Register(MapObjective obj)
		{
			//DebugLogger.Log("Registered: " + obj.name, LogContext.Overworld, LogPriority.Low);
			_objectives.Add(obj);
		}

		public void Unregister(MapObjective obj)
		{
			_objectives.Remove(obj);
		}
	}
}