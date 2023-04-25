using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;


namespace Anjin.Nanokin.Map
{
	[RequireComponent(typeof(Rigidbody))]
	public class PhysicsInteractible : MonoBehaviour, IHitHandler<SwordHit>
	{
		public event Action<IHitInfo> OnReceiveHit;
		public bool ReactToForce = true;

		private Rigidbody _rb;
		[SerializeField] [Range(0, 100)] [ShowIf("$ReactToForce")]private float _hitForce = 5;
		private void Awake()
		{
			_rb = GetComponent<Rigidbody>();
		}

		public void OnHit(SwordHit hit)
		{
			if(ReactToForce)
				_rb.AddForce(hit.direction.normalized * _hitForce, ForceMode.Impulse);
			OnReceiveHit?.Invoke(hit);
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}
