using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Util.Assets;

namespace Assets.Nanokins
{
	public class NanokinLimbCatalogue : AssetCatalogue<NanokinLimbAsset>
	{
		public static NanokinLimbCatalogue Instance => Singleton<NanokinLimbCatalogue>.Instance;

		public override string AddressPrefix    => "Limbs/";
		public override string AddressExcludes  => ".png";
		public override string AddressableLabel => Addresses.LimbLabel;

		public static IEnumerable<NanokinLimbAsset> SearchByName(IEnumerable<NanokinLimbAsset> limbsToSearch, string searchToken, bool with_case_sensitivity = true)
		{
			if (with_case_sensitivity)
				searchToken = searchToken.ToLower();

			return limbsToSearch.Where(asset =>
			{
				string stringToSearch = asset.DisplayName;

				if (with_case_sensitivity)
					stringToSearch = stringToSearch.ToLower();

				return stringToSearch.Contains(searchToken);
			});
		}

		protected override void OnAssetLoaded(NanokinLimbAsset asset)
		{
		}
	}
}