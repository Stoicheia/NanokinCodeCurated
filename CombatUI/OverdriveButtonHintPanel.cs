using Anjin.Nanokin;
using UnityEngine;

namespace Combat.UI
{
	public class OverdriveButtonHintPanel : MonoBehaviour
	{
		public InputButtonLabel ButtonLabel;

		private void Start()
		{
			ButtonLabel.Button = GameInputs.overdriveDown;
		}
	}
}