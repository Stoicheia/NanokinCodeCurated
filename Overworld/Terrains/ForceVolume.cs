using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Terrains
{
	public class Volume : MonoBehaviour
	{
		protected HashSet<Actor> _affectedCharacters;

		private void Awake()
		{
			_affectedCharacters = new HashSet<Actor>();
		}

		protected virtual void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent(out Actor character))
			{
				_affectedCharacters.Add(character);
			}
		}

		protected virtual void OnTriggerExit(Collider other)
		{
			if (other.TryGetComponent(out Actor character))
			{
				_affectedCharacters.Remove(character);
			}
		}
	}

	/// <summary>
	/// Applies a force continuously while the character is inside.
	/// </summary>
	[AddComponentMenu("Anjin: Level Building/Force Volume")]
	public class ForceVolume : Volume
	{
		[Tooltip("Direction of the force to apply"), Delayed, OnValueChanged("OnDirectionChanged")]
		public Vector3 Direction = Vector3.up;
		[Tooltip("X-axis: velocity of the character.\nY-axis: force to apply each frame.")]
		public AnimationCurve ForceByVelocity = AnimationCurve.Linear(0, 5, 15, 0);

		public float ForceScale = 1;

		private void FixedUpdate()
		{
			foreach (Actor character in _affectedCharacters)
			{
				Vector3 velocityInDirection = character.velocity * Vector3.Dot(character.velocity.normalized, Direction).Minimum(0);

				float force = ForceByVelocity.Evaluate(velocityInDirection.magnitude) * ForceScale;
				character.AddForce(Direction * force);
			}
		}

#if UNITY_EDITOR
		private void OnDirectionChanged()
		{
			Direction = Direction.normalized;
		}
#endif
	}
}