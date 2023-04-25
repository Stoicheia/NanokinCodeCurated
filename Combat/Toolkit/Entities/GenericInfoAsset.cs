using Sirenix.OdinInspector;

namespace Combat.Entities
{
	public class GenericInfoAsset : SerializedScriptableObject
	{
		[InlineProperty]
		[HideReferenceObjectPicker]
		[HideLabel]
		public GenericInfo Info = new GenericInfo();
	}
}