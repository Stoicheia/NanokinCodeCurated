using TMPro;

namespace Anjin.UI {
	public class LabelElement : HUDElement {
		public TMP_Text Label;

		protected override void Awake()
		{
			base.Awake();
			if(!Label) Label = GetComponent<TMP_Text>();
		}
	}
}