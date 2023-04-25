using System;
using Anjin.Utils;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Combat.Skills.Generic
{
	public class FxInfo : MonoBehaviour
	{
		[InfoBox("The origin point where the FX should appear when in relation to another object.")]
		public HitEffectOrigin Origin;

		[InfoBox("The transform to invert the X scale for horizontal flip effects. Leave empty to use this transform.")]
		[Optional]
		public Transform FlipRoot;

		[InfoBox("Dictates whether or not the flag that tells the particle system to play on awake should be set to false to prevent automatic playback.")]
		public bool KeepParticlesAwake = false;

		[InfoBox("Pushes the FX towards the camera so it appears in front of an object it is centered on.")]
		public bool BillboardSpawn = true;

		[InfoBox("Destroy this FX when the associated object dies.")]
		public bool DestroyOnAssociatedDeath = true;

		[InfoBox("Destroy this FX when combat ends. Almost always should be true.")]
		public bool DestroyOnCombatEnd = true; // TODO DEBT this is a bad idea, we have to handle things from the source

		private MotionBehaviour _motion;

		private void OnEnable()
		{
			_motion = GetComponent<MotionBehaviour>();
			if (_motion == null && DestroyOnAssociatedDeath)
			{
				Debug.LogWarning("DestroyOnAssociatedDeath requires a MotionBehaviour component. This flag will be ignored.");
			}
		}

		private void Update()
		{
			// TODO DEBT this should not be handled here, this is spaghetti
			if (_motion != null && _motion.Owner != null && _motion.Owner.fighter.dead && DestroyOnAssociatedDeath)
			{
				Destroy(gameObject); // TODO: change to some version of CoplayerProxy.Despawn(..)
			}
		}

		private void TrySelfDestroy()
		{
			if (DestroyOnCombatEnd) Destroy(gameObject);
		}
	}
}