using UnityEditor;
using UnityEngine;

namespace Util.Bullshit
{
	/// <summary>
	/// There is currently a major and enigmatic bug where in some rare cases
	/// all lua scripts in the resources folder will not be found.
	/// The solution then is to reimport all resources.
	///
	/// It's not clear yet but I'm actually pretty sure it depends on the color of the cereals you ate earlier this morning
	/// </summary>
	[InitializeOnLoad]
	public static class ReimportAllResourcesWhenScriptsCantBeFound
	{
		static ReimportAllResourcesWhenScriptsCantBeFound()
		{
			if (Resources.Load("Scripts/main") == null)
			{
				// Everything is fucked and I hate society
				AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ForceUpdate);
			}
		}
	}
}



