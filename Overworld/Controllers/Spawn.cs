using System;
using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Nanokin
{
	public struct Spawn
	{
		public Vector3        	position;
		public Vector3 			facing;
		public Actor 	prefab;
	}

	[Serializable]
	public struct OverworldDeathConfig {

		public enum Mode {
			LastValidGrounding             = 0,
			LastCheckpointOrValidGrounding = 1,
			LastCheckpoint                 = 2,
			SpecificSpawnPoint             = 3,
		}

		public enum PlayerBehaviour {
			None = 0,
			Knockback = 1,
		}

		[EnumToggleButtons, HideLabel]
		public Mode mode;

		public Option<float> time;
		public Option<float> transitionTime;

		public PlayerBehaviour playerBehaviour;

		public float KnockbackForce;

		[ShowIf("@mode==OverworldDeathConfig.Mode.SpecificSpawnPoint")]
		public SpawnPoint spawn;

	}

	public interface IOverworldDeathHandler {
		void ModifyDeathConfig(ref OverworldDeathConfig config);
	}
}