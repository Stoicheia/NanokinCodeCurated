using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Overworld.Controllers;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Util.Components
{
	[AddComponentMenu("Anjin: System/Occluder")]
	public class Occluder : MonoBehaviour {

		public bool Static = true;

		/// <summary>
		/// Distance to stay active in.
		/// At runtime, OnChanged must be called after modifying this.
		/// </summary>
		[SerializeField] public float Distance = 20;

		/// <summary>
		/// Radius of the bounding sphere.
		/// At runtime, OnChanged must be called after modifying this.
		/// </summary>
		[SerializeField] public float Radius = 1;

		/// <summary>
		/// Disable this object when it goes out of range.
		/// </summary>
		[FormerlySerializedAs("SelfObject"), Space]
		[SerializeField] public bool SelfObject = true;

		/// <summary>
		/// Disable all our components when going out of range.
		/// </summary>
		[FormerlySerializedAs("SelfComponents"), SerializeField]
		public bool AllComponents;

		/// <summary>
		/// Disable these objects when going out of range.
		/// </summary>
		[FormerlySerializedAs("Objects"), SerializeField, Optional]
		public List<GameObject> TargetObjects;

		/// <summary>
		/// Disable these objects when going out of range.
		/// </summary>
		[FormerlySerializedAs("Components"), SerializeField, Optional]
		public List<MonoBehaviour> TargetComponents;

		[NonSerialized]
		public bool visible;

		[CanBeNull]
		private List<MonoBehaviour> _selfComponents;

		[NonSerialized]
		public float totalDistanceSqr;

		[NonSerialized]
		public Vector3 StaticPosition;

		private void Awake()
		{
			if (AllComponents)
			{
				_selfComponents = new List<MonoBehaviour>();
				GetComponents(_selfComponents);
			}

			StaticPosition = transform.position;

			OccluderSystem.Add(this);
			Enabler.Register(gameObject);

			OnChanged();

			visible = true;
		}

		public void OnChanged() { totalDistanceSqr = Distance * Distance + Radius * Radius; }

		private void OnDestroy()
		{
			if (AllComponents)
			{
				_selfComponents = new List<MonoBehaviour>();
				GetComponents(_selfComponents);
			}

			OccluderSystem.Remove(this);
			Enabler.Deregister(gameObject);
		}

		// private void LateUpdate()
		// {
		// }

		public void SetState(bool state)
		{
			if (SelfObject)
			{
				Enabler.Set(gameObject, state, SystemID.Occluder);
				// ObjectEnabler.Set(gameObject, state);
			}

			if (AllComponents)
			{
				foreach (MonoBehaviour sc in _selfComponents)
				{
					sc.enabled = state;
				}
			}

			foreach (GameObject go in TargetObjects)
			{
				Enabler.Set(go, state, SystemID.Occluder); // BUG this could cause bugs if more than 1 occluder target the same object. unlikely, but must be noted
				// go.SetActive(state);
			}

			foreach (MonoBehaviour comp in TargetComponents)
			{
				comp.enabled = state;
			}
		}

		private void OnDrawGizmosSelected()
		{
			// Draw2.DrawTwoToneSphere(transform.position, Distance, Color.green, Color.white);
		}
	}
}