using System.Globalization;
using Anjin.Actors;
using Anjin.UI;
using Anjin.Util;
using TMPro;
using UnityEngine;

namespace Overworld.UI {
	public class WorldMarker : HUDElement {

		public TMP_Text TMP_Distance;

		private bool	_hasTarget;
		private bool	_hasDistance;

		protected override void Awake()
		{
			base.Awake();
			PositionMode = ElemPositionMode.AnchoredToWorldPoint;

			_hasDistance = TMP_Distance != null;
		}

		private void Update()
		{
			if (_hasTarget && ActorController.playerActive) {
				Alpha = 1;

				float distance = Vector3.Distance(WorldAnchor.Get(), ActorController.playerActor.transform.position);

				if (_hasDistance) {
					TMP_Distance.text = distance.ToString("F1", CultureInfo.InvariantCulture);
				}
			} else {
				Alpha = 0;
			}
		}

		public void SetTarget(Transform target = null)
		{
			if (target == null) {
				_hasTarget = false;
				return;
			}

			WorldAnchor = new WorldPoint(target);
			_hasTarget  = true;
		}

	}
}