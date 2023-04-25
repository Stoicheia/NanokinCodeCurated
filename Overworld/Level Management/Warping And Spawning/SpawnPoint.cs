using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Util;
using Drawing;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Components;

namespace Anjin.Nanokin
{
	[LuaUserdata]
	public class SpawnPoint : AnjinBehaviour, IComparable<SpawnPoint>
	{
		public int    Priority = 0;
		public string ScriptFunction;
		public bool   AddToActive = true;

		public bool HideInTeleportMenu;

		public static List<SpawnPoint> allActive = new List<SpawnPoint>();

		public virtual string SpawnPointName => name;

		public List<LevelSpawnPoint> MemberSpawnPoints = new List<LevelSpawnPoint>()
		{
			new LevelSpawnPoint(Vector3.zero, Vector3.forward),
			new LevelSpawnPoint(Vector3.zero + Vector3.right, Vector3.forward),
			new LevelSpawnPoint(Vector3.zero - Vector3.right, Vector3.forward),
		};

		private void Awake()
		{
			transform.position = transform.position.DropToGround();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			allActive.Clear();
		}

		public virtual void OnEnable()
		{
			if (AddToActive)
			{
				allActive.Add(this);
				allActive.Sort();
			}
		}

		public virtual void OnDisable()
		{
			if (AddToActive)
				allActive.Remove(this);
		}

		public Vector3 GetSpawnPointPosition(int index)
		{
			var rot = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
			var p   = MemberSpawnPoints.WrapGet(index).Offset;

			return rot.MultiplyPoint3x4(p);
		}

		public Vector3 GetSpawnPointFacing(int index)
		{
			Vector3 facing = MemberSpawnPoints.WrapGet(index).Facing;
			if (facing.magnitude < Mathf.Epsilon)
				facing = Vector3.forward;

			return (transform.rotation * facing).normalized;
		}

		public virtual void OnSpawn() { }

		public int CompareTo(SpawnPoint other)
		{
			if (ReferenceEquals(this, other))
				return 0;
			if (ReferenceEquals(null, other))
				return 1;
			return -Priority.CompareTo(other.Priority);
		}


		[Button]
		private void TeleportHere() { }

		public override void DrawGizmos()
		{
			for (int i = 0; i < MemberSpawnPoints.Count; i++)
			{
				Vector3 pos  = GetSpawnPointPosition(i);
				Vector3 face = GetSpawnPointFacing(i);

				using (Draw.WithColor(Color.magenta))
					Draw.CircleXZ(pos, 0.4f);

				using (Draw.WithColor(Color.red))
					Draw.ArrowheadArc(pos, face, 0.4f);
			}
		}

		[Serializable]
		public struct LevelSpawnPoint
		{
			public Vector3 Offset;
			[FormerlySerializedAs("FacingDirection")]
			public Vector3 Facing;

			public LevelSpawnPoint(Vector3 offset, Vector3 facing)
			{
				Offset = offset;
				Facing = facing;
			}
		}
	}
}