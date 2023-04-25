using System;
using Combat.Data;
using Combat.Entities;
using Combat.Toolkit;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Menu.Formation
{
	public class FormationMonster : SerializedMonoBehaviour, ITargetable
	{
		[NonSerialized] public MultiSpritePuppet puppet;
		[NonSerialized] public Vector2Int        slot;
		[NonSerialized] public CharacterEntry    entry;
		[NonSerialized] public CharacterEntry    mirror;
		[NonSerialized] public Target            target;

		public Vector3 GetTargetPosition() => transform.position;

		public Vector3 GetTargetCenter() => transform.position;

		public GameObject GetTargetObject() => transform.gameObject;

		public Color IrrelevantColor => Color.grey;
		public Color ReticleHoverColor => Color.magenta;

		public ManualVFX DimVFX;
		public ManualVFX SelectVFX;
	}
}