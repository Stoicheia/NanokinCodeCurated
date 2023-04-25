using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Combat.UI.TurnOrder
{
	public class ViewStateStyleCollection : SerializedScriptableObject
	{
		[SerializeField, Required, Space] private ViewStyle _hidden;
		[SerializeField, Required]        private ViewStyle _minor;
		[SerializeField, Required]        private ViewStyle _normal;
		[SerializeField, Required]        private ViewStyle _major;

		[SerializeField, Required]        private ViewStyle _stacked;

		public int    width;
		public Sprite frameSprite;
		public Sprite contentMask;

		public float stackMultiplier;

		public                               bool   enableCondensed;
		[EnableIf("enableCondensed")] public int    widthCondensed;
		[EnableIf("enableCondensed")] public Sprite frameSpriteCondensed;
		[EnableIf("enableCondensed")] public Sprite contentMaskCondensed;

		public ViewStyle Get(ViewStates value)
		{
			switch (value)
			{
				case ViewStates.Inactive: return _hidden;
				case ViewStates.Hidden:   return _hidden;
				case ViewStates.Minor:    return _minor;
				case ViewStates.Normal:   return _normal;
				case ViewStates.Major:    return _major;
				case ViewStates.Stacked:  return _stacked;
				default:
					throw new ArgumentOutOfRangeException(nameof(value), value, null);
			}
		}
	}
}