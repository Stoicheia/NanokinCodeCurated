using System;
using UnityEngine;

namespace Overworld.QuestCompass
{

	public class MapObjective : MonoBehaviour
	{
		public enum ObjectiveType
		{
			Shop = 0x1, Quest = 0x2, Nanocore = 0x4, Chest = 0x8, Interest = 0x10, NPC = 0x20, Exit = 0x40, NanocoreActive = 0x80, Enemy = 0x100
		}

		public static MapObjectivesManager Manager;

		[SerializeField] private ObjectiveType _type;
		[SerializeField] private bool _hideFromRadar;
		public Vector2 Position => new Vector2(transform.position.x, transform.position.z);

		private bool _isRegistered;
		private bool _spawned;

		public ObjectiveType Type
		{
			get => _type;
			set => _type = value;
		}

		private bool _active;
		public bool Active => _active && isActiveAndEnabled && !_hideFromRadar;

		private void Awake()
		{
			_active = true;
			_isRegistered = false;
			_spawned = false;
		}

		private void Start()
		{
			if (Manager == null)
			{
				DebugLogger.LogError("There are map objectives in the scene but no MapObjectivesManager!", LogContext.Overworld, LogPriority.Critical);
				return;
			}
			Register();
			_spawned = true;
		}

		private void OnEnable()
		{
			if (!_isRegistered && _spawned)
			{
				Register();
			}

			MapObjectivesManager.OnReevaluation += Register;
		}

		private void OnDisable()
		{
			if (_isRegistered)
			{
				Unregister();
			}

			MapObjectivesManager.OnReevaluation -= Register;
		}

		public void Toggle(bool b)
		{
			_active = b;
		}

		private void Register()
		{
			Manager.Register(this);
			_isRegistered = true;
		}

		private void Unregister()
		{
			Manager.Unregister(this);
			_isRegistered = false;
		}
	}
}