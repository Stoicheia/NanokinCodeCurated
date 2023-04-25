namespace Combat.Components
{
	public struct SelectUIObject
	{
		public bool  state;
		public float brightness;

		public static readonly SelectUIObject Initial = new SelectUIObject {brightness = 1};
	}
}