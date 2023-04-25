using UnityEngine.AddressableAssets;

namespace Util.Extensions
{
	public static class AddressablesExtensions
	{
		public static bool IsSet(this AssetReference reference)
		{
			if (reference == null) return false;
			return reference.RuntimeKeyIsValid();
		}

	}
}