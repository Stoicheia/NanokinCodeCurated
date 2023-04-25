using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Util.Odin.Attributes;
using Object = UnityEngine.Object;

namespace Anjin.Regions
{
	[HideMonoScript]
	public class RegionMetadataWindow : SerializedScriptableObject
	{
		public static void InspectMetadata(RegionMetadata data, Object asset, string name)
		{
			Live.data = data;
			Live.asset = asset;
			Live.name = name;
			Selection.activeObject = Live;
		}

		public static void StopInspecting()
		{
			Live.data = null;
			Live.name = "";

			if(Selection.activeObject == Live)
				Selection.activeObject = null;
		}

		static RegionMetadataWindow _live;
		public static RegionMetadataWindow Live
		{
			get
			{
				if (_live == null)
				{
					_live = CreateInstance<RegionMetadataWindow>();
					_live.hideFlags = HideFlags.DontSave;
				}
				return _live;
			}
		}

		[HideInInspector]
		public Object asset;

		[Inline, HideReferenceObjectPicker]
		public RegionMetadata data;
	}

	[CustomEditor(typeof(RegionMetadataWindow))]
	public class RegionMetadataWindowInspector : OdinEditor
	{
		public override bool RequiresConstantRepaint() => true;

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck())
			{
				UnityEditor.EditorUtility.SetDirty((target as RegionMetadataWindow).asset);
			}
		}
	}
}