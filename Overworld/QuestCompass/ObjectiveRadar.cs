using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin.Map;
using Cysharp.Threading.Tasks;
using Pathfinding;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.QuestCompass
{
	//Hardcoded to refer to active player/player camera
	public class ObjectiveRadar : SerializedMonoBehaviour
	{
		public const int MAX_TRACKED_OBJECTS = 128;

		private Transform _posTracker;
		private Transform _rotTracker;
		private Transform _compassRotTracker;
		[SerializeField] private MapObjectivesManager _objectivesManager;
		[SerializeField] private float _maxRadius;
		[SerializeField] private bool _fixRotation;
		[SerializeField] [ShowIf("_fixRotation")] private float _rotation;
		[SerializeField] [ShowIf("_fixRotation")] private float _compassRotation;
		private List<RadarObjectInfo> _objectInfo;
		private List<RadarObjectInfo> _drawInfo;

		[SerializeField] [ShowIf("@!_fixRotation")] private float _rotationOffset;

		public float RotationOffset
		{
			get => _rotationOffset;
			set => _rotationOffset = value;
		}


		[EnumFlag]
		public MapObjective.ObjectiveType UserEnabledTypes;

		[EnumFlag]
		public MapObjective.ObjectiveType ImportantTypes;

		public float _displayImportantRadius = 0.9f;
		public float _displayImportantTransparency = 0.6f;
		public List<MapObjective> Tracking => _objectivesManager.Objectives;
		public float TrackerRotation
		{
			get
			{
				if(!_fixRotation)
					return (_rotTracker ? _rotTracker.rotation.eulerAngles.y : 0) + RotationOffset;
				return (_rotTracker ? _rotTracker.rotation.eulerAngles.y : 0) - _rotation;
			}
		}

		public Vector2 TrackerPosition => _posTracker ? new Vector2(_posTracker.position.x, _posTracker.position.z) : Vector2.zero;
		public float CompassRotation => (_compassRotTracker && !_fixRotation ? _compassRotTracker.rotation.eulerAngles.y + RotationOffset : _compassRotation);
		public List<RadarObjectInfo> DrawInfo => _drawInfo;
		public float RelativePlayerRotation => -TrackerRotation; //counterintuitive but correct somehow



		private void Awake()
		{
			_objectInfo = new List<RadarObjectInfo>(MAX_TRACKED_OBJECTS);
			_drawInfo = new List<RadarObjectInfo>(MAX_TRACKED_OBJECTS);
		}

		private void Start()
		{
			if (_objectivesManager == null)
				_objectivesManager = MapObjective.Manager;
		}

		private void Update()
		{
			if (ActorController.playerActor == null || ActorController.playerCamera == null) return;
			_posTracker = ActorController.playerActor.transform;
			_compassRotTracker = ActorController.playerCamera.activeCam ? ActorController.playerCamera.activeCam.transform : null;
			_rotTracker = _posTracker;
			ReadObjects(_objectInfo);
			DrawObjects(_objectInfo, _drawInfo, _rotation);
		}

		private void ReadObjects(List<RadarObjectInfo> obj)
		{
			obj.Clear();
			for (int i = 0; i < Tracking.Count && obj.Count < MAX_TRACKED_OBJECTS; i++)
			{
				MapObjective.ObjectiveType type = Tracking[i].Type;
				float relativeDistance = (Tracking[i].Position - TrackerPosition).magnitude;

				if (Tracking[i].Active && (type & UserEnabledTypes) != 0x0 && (relativeDistance <= _maxRadius || (type & ImportantTypes) != 0x0))
				{
					obj.Add(new RadarObjectInfo(type, Tracking[i].Position));
				}
			}
		}

		private void DrawObjects(List<RadarObjectInfo> from, List<RadarObjectInfo> to, float rot)
		{
			to.Clear();
			foreach (var obj in from)
			{
				to.Add(RadarConvert(obj, rot));
			}
		}

		private RadarObjectInfo RadarConvert(RadarObjectInfo info, float rot)
		{
			Vector2 offsetVector = info.Position - TrackerPosition;
			Vector2 drawVector = new Vector2
			(
				Mathf.Cos((rot - RotationOffset) * Mathf.Deg2Rad) * offsetVector.x - Mathf.Sin((rot - RotationOffset) * Mathf.Deg2Rad) * offsetVector.y,
				Mathf.Sin((rot - RotationOffset) * Mathf.Deg2Rad) * offsetVector.x + Mathf.Cos((rot - RotationOffset) * Mathf.Deg2Rad) * offsetVector.y
			);
			Vector2 atVector = drawVector / _maxRadius;

			return new RadarObjectInfo(info.Type, (info.Type & ImportantTypes) == 0x0 ?
				drawVector / _maxRadius :
				Vector2.ClampMagnitude(drawVector / _maxRadius, _displayImportantRadius));
		}

		public bool IsImportant(RadarObjectInfo info)
		{
			return (info.Type & ImportantTypes) != 0x0;
		}

		public bool IsImportant(MapObjective.ObjectiveType type)
		{
			return (type & ImportantTypes) != 0x0;
		}

}

	public struct RadarObjectInfo
	{
		public MapObjective.ObjectiveType Type;
		public Vector2 Position;

		public RadarObjectInfo(MapObjective.ObjectiveType t, Vector2 p)
		{
			Type = t;
			Position = p;
		}
	}
}