using System;
using Combat.StandardResources;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	public interface IEncounterActor {
		bool IsAggroed { get; }
	}

	public class EncounterMonster : MonoBehaviour
	{
		[NonSerialized] public Vector3                                       home;
		[NonSerialized] public float                                         homeRadius;
		[NonSerialized] public Action<EncounterMonster, EncounterAdvantages> onTrigger;
		[NonSerialized] public Action                                        onSpawn;
		[NonSerialized] public bool                                          spawned;
		[NonSerialized] public Action										 onDestroy;
		[NonSerialized] public EncounterBounds								 bounds;

		private void Awake()
		{
			home = transform.position;
		}

		[Button]
		public void Trigger(EncounterAdvantages advantage)
		{
			onTrigger(this, advantage);
		}

		private void OnDestroy()
		{
			onDestroy?.Invoke();
		}


		public void Despawn()
		{
			Enabler.Disable(gameObject, SystemID.Spawned);
			spawned = false;
			gameObject.SetActive(false);
		}
	}

	public static class EncounterAdvantagesExtensions
	{
		public static PlayerAlignments ToAlignment(this EncounterAdvantages advantage)
		{
			switch (advantage)
			{
				case EncounterAdvantages.Player: return PlayerAlignments.Ally;
				case EncounterAdvantages.Enemy:  return PlayerAlignments.Enemy;
			}

			return PlayerAlignments.Neutral;
		}
	}

	public enum EncounterAdvantages
	{
		Neutral,
		Player,
		Enemy
	}
}