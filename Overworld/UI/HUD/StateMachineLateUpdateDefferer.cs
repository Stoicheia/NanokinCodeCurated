using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anjin.UI {
	public class StateMachineLateUpdateDefferer : MonoBehaviour {

		public List<BaseStateMachineBehaviour> Behaviours = new List<BaseStateMachineBehaviour>();

		private void LateUpdate()
		{
			foreach (BaseStateMachineBehaviour behaviour in Behaviours) {
				if(behaviour)
					behaviour.OnLateUpdate();
			}
		}

	}
}