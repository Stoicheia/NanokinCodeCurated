namespace UnityEditor
{
	public static class LimbAssetFixer
	{
		[MenuItem("Anjin/Utilities/Fix Limb Assets")]
		public static void FixLimbs()
		{
// 			string[] files = Directory.GetFiles("Assets/Data/Content/Limbs/", "*.asset", SearchOption.AllDirectories).
// 				Select(x=>x.Replace("\\", "/")).
// 				ToArray();
//
// 			List<NanokinLimbAsset> loaded = new List<NanokinLimbAsset>();
//
// 			for (int i = 0; i < files.Length; i++) {
//
// 				var asset = AssetDatabase.LoadAssetAtPath<NanokinLimbAsset>(files[i]);
// 				if(asset == null) continue;
//
// 				loaded.Add(asset);
//
// 				EditorUtility.DisplayProgressBar("Load Assets", $"{i}/{files.Length}: {asset.name}", i / (float)files.Length);
// 			}
//
// 			int num = 100;
//
// 			for (int i = 0; i < loaded.Count; i++) {
// 				var limb  = loaded[i];
// 				var sheet = limb.Spritesheet.Spritesheet;
//
// 				Debug.Log($"Fixing {limb.name}!");
// 				//SpritesheetUtilities.Slice(sheet, false);
// 				SpritesheetUtilities.ReadSprites(sheet);
// 				EditorUtility.SetDirty(limb);
//
// 				/*for (int j = 0; j < sheet.TypedCells.Length; j++) {
// 					if (sheet.TypedCells.Any(x => x.Sprite == null)) {
//
// 						if (num > 0) {
// 							num--;
// 						} else {
// 							Debug.Log($"Limb {limb.name} contains null cells!");
// 						}
//
// 						break;
// 					}
// 				}*/
//
// 				EditorUtility.DisplayProgressBar("Check Assets", $"{i}/{loaded.Count}: {limb.name}", i / (float)loaded.Count);
// 			}
//
// 			EditorUtility.ClearProgressBar();
// 			AssetDatabase.SaveAssets();
		}

	}
}