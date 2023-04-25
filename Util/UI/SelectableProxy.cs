using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableProxy : UIBehaviour
{
	public ColorBlock Colors;

	public void DoStateTransition(int state, bool instant)
	{
		// TODO 

		// 0 = Normal
		// 1 = Highlighted
		// 2 = Pressed
		// 3 = Selected
		// 4 = Disabled

		switch (state)
		{
			case 0:
				break;
		}

	}

	private void Update()
	{ }
}