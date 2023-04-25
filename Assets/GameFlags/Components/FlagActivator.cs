using System.Collections.Generic;
using Sirenix.OdinInspector;

// TODO: More than just bool!
namespace Anjin.Core.Flags.Components
{
	public class FlagActivator : SerializedMonoBehaviour
	{
		public string Flag;
		public bool   ActiveValue;

		private void Start()
		{
			if (Flags.Find(Flag) is BoolFlag flag)
			{
				flag.AddListener(name, OnChanged);
				gameObject.SetActive(flag.Value == ActiveValue);
			}
		}

		public void OnChanged(bool value, bool prev)
		{
			gameObject.SetActive(value == ActiveValue);
		}
	}
}