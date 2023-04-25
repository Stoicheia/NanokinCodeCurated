using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Utils
{
	/// <summary>
	/// Base class for a time scale volume.
	/// Must have either a SphereCollider or a BoxCollider.
	/// </summary>
	public abstract class TimeScaleVolumeBase : SerializedMonoBehaviour
	{
		public LayerMask LayerMask = int.MaxValue;

		[SerializeField]
		public bool Global;

		[NonSerialized]
		public Action onChanged;

		// We don't care about the order of scalables so a hashset is more efficient.
		protected readonly HashSet<TimeScalable> managedScalables = new HashSet<TimeScalable>();

		public static readonly List<TimeScaleVolumeBase> globals = new List<TimeScaleVolumeBase>();

		private List<TimeScalable> _getComponents = new List<TimeScalable>();

		private float    _previousValue;
		private Collider _collider;

		private static Collider[] _tmpColliders = new Collider[32];

		public abstract float Scaling { get; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			globals.Clear();
		}


		private void Awake()
		{
			Rigidbody rb = gameObject.AddComponent<Rigidbody>();
			rb.isKinematic = true;

			_collider = GetComponentInChildren<Collider>();

			switch (_collider)
			{
				// Really? No way to check for overlap with an arbitrary collider..?
				case SphereCollider sphere:
				{
					int size = Physics.OverlapSphereNonAlloc(sphere.transform.position, sphere.radius, _tmpColliders);
					for (var i = 0; i < size; i++)
					{
						// BUG does not check for layer mask
						OnTriggerEnter(_tmpColliders[i]);
					}

					break;
				}

				case BoxCollider _:
				{
					int size = Physics.OverlapBoxNonAlloc(_collider.bounds.center, _collider.bounds.extents / 2f, _tmpColliders, _collider.transform.rotation);
					for (var i = 0; i < size; i++)
					{
						// BUG does not check for layer mask
						OnTriggerEnter(_tmpColliders[i]);
					}

					break;
				}
			}
		}

		public void SetGlobal()
		{
			if (Global) return;
			if (_collider)
			{
				this.LogError("Cannot set global because this time scale volume is already collider-based.");
				return;
			}

			Global = true;
			globals.Add(this);

			foreach (TimeScalable scale in TimeScalable.all)
			{
				Add(scale);
			}
		}

		public void Add([NotNull] TimeScalable scalable)
		{
			managedScalables.Add(scalable);
			scalable.Add(this);
			scalable.Refresh();
		}

		public void Remove([NotNull] TimeScalable scalable)
		{
			managedScalables.Remove(scalable);
			scalable.Remove(this);
			scalable.Refresh();
		}

		protected virtual void Update()
		{
			if (Mathf.Abs(Scaling - _previousValue) > Mathf.Epsilon)
			{
				UpdateAffectedObjects();
			}

			_previousValue = Scaling;
		}

		private void UpdateAffectedObjects()
		{
			foreach (TimeScalable scalable in managedScalables)
			{
				scalable.Refresh();
			}
		}

		private void OnTriggerEnter([NotNull] Collider other)
		{
			other.GetComponentsInChildren(_getComponents);

			foreach (TimeScalable scalable in _getComponents)
			{
				Add(scalable);
			}
		}

		private void OnTriggerExit([NotNull] Collider other)
		{
			other.GetComponentsInChildren(_getComponents);

			foreach (TimeScalable scalable in _getComponents)
			{
				Remove(scalable);
			}
		}

		private void OnDestroy()
		{
			foreach (TimeScalable managedScalable in managedScalables)
			{
				managedScalable.Remove(this);
			}

			if (Global)
			{
				globals.Remove(this);
			}
		}
	}
}