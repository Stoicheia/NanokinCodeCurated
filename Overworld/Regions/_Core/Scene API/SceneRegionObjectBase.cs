using System;
using System.Collections.Generic;
using Drawing;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Components;
using Util.Odin.Attributes;

namespace Anjin.Regions
{
	public abstract class SceneRegionObjectBase : AnjinBehaviour
	{
		public abstract bool AddObjectToList(List<RegionObject> Objects);

		public Vector3 LinkOffset;

		[ShowInInspector, ShowInPlay, ReadOnly]
		public static List<SceneRegionObjectBase> All = new List<SceneRegionObjectBase>();

		private void OnEnable()
		{
			RegionController.trackedSceneObjects.Add(this);
		}

		private void OnDisable()
		{
			RegionController.trackedSceneObjects.Remove(this);
		}
	}
}