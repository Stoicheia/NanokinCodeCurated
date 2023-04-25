using System;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
#endif

namespace Util.UnityEditor.Odin
{
	public class PlaceholderAttribute : Attribute
	{
		public string Label { get; }

		public PlaceholderAttribute(string label)
		{
			Label = label;
		}
	}

#if UNITY_EDITOR
	[AllowGUIEnabledForReadonly]
	[DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
	[UsedImplicitly]
	public sealed class PlaceholderAttributeDrawer : OdinAttributeDrawer<PlaceholderAttribute, string>
	{
		private ValueResolver<string> _labelResolver;

		protected override void Initialize()
		{
			_labelResolver = ValueResolver.GetForString(Property, Attribute.Label);
		}

		/// <summary>Draws the property.</summary>
		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (_labelResolver.HasError)
				SirenixEditorGUI.ErrorMessageBox(_labelResolver.ErrorMessage);

			CallNextDrawer(label);

			if (string.IsNullOrEmpty(Property.ValueEntry.WeakSmartValue as string))
			{
				GUIHelper.PushGUIEnabled(false);
				GUI.Label(GUILayoutUtility.GetLastRect().HorizontalPadding(4, 0), _labelResolver.GetValue(), SirenixGUIStyles.LeftAlignedCenteredLabel);
				GUIHelper.PopGUIEnabled();
			}
		}
	}
#endif
}