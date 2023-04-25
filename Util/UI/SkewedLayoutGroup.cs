using UnityEngine;
using UnityEngine.UI;

namespace Anjin.EditorUtility {
	public class SkewedLayoutGroup : LayoutGroup {

		public AxisDirection Direction;

		public float Skew;
		public bool  ShrinkWithSkew = false;

		public ScrollRect Scroll;

		private bool _scrollChanged;

		// Copied from HorizontalOrVerticalLayoutGroup
		#region Copied From Unity
		[SerializeField]
		protected float m_Spacing = 0;
		public float spacing { get => m_Spacing; set => SetProperty(ref m_Spacing, value); }

        [SerializeField]
		protected 	bool m_ChildForceExpandWidth = true;
		public 		bool childForceExpandWidth 	{ get => m_ChildForceExpandWidth; set => SetProperty(ref m_ChildForceExpandWidth, value); }

        [SerializeField]
		protected 	bool m_ChildForceExpandHeight = true;
        public 		bool childForceExpandHeight { get => m_ChildForceExpandHeight; set => SetProperty(ref m_ChildForceExpandHeight, value); }

        [SerializeField]
        protected 	bool m_ChildControlWidth = true;
        public 		bool childControlWidth 		{ get => m_ChildControlWidth; set => SetProperty(ref m_ChildControlWidth, value); }

        [SerializeField]
        protected 	bool m_ChildControlHeight = true;
        public 		bool childControlHeight 	{ get => m_ChildControlHeight; set => SetProperty(ref m_ChildControlHeight, value); }

        [SerializeField]
        protected 	bool m_ChildScaleWidth = false;
        public 		bool childScaleWidth 		{ get => m_ChildScaleWidth; set => SetProperty(ref m_ChildScaleWidth, value); }

        [SerializeField]
        protected 	bool m_ChildScaleHeight = false;
        public 		bool childScaleHeight 		{ get => m_ChildScaleHeight; set => SetProperty(ref m_ChildScaleHeight, value); }

		[SerializeField]
		protected 	bool m_ReverseArrangement = false;
        public 		bool reverseArrangement 	{ get => m_ReverseArrangement; set => SetProperty(ref m_ReverseArrangement, value); }
		#endregion

		protected override void Start()
		{
			base.Start();
			if(Scroll != null)
				Scroll.onValueChanged.AddListener(v => SetDirty());
		}

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcAlongAxis(0, Direction == AxisDirection.Vertical);
		}

		public override void CalculateLayoutInputVertical() => CalcAlongAxis(1, Direction        == AxisDirection.Vertical);
		public override void SetLayoutHorizontal()          => SetChildrenAlongAxis(0, Direction == AxisDirection.Vertical);
		public override void SetLayoutVertical()            => SetChildrenAlongAxis(1, Direction == AxisDirection.Vertical);

		protected void CalcAlongAxis(int axis, bool isVertical)
        {
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            bool alongOtherAxis = (isVertical ^ (axis == 1));

            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible;
                GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                if (useScale)
                {
                    float scaleFactor = child.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                if (alongOtherAxis)
                {
                    totalMin = Mathf.Max(min + combinedPadding, totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                    totalFlexible = Mathf.Max(flexible, totalFlexible);
                }
                else
                {
                    totalMin += min + spacing;
                    totalPreferred += preferred + spacing;

                    // Increment flexible size with element's flexible size.
                    totalFlexible += flexible;
                }
            }

            if (!alongOtherAxis && rectChildren.Count > 0)
            {
                totalMin -= spacing;
                totalPreferred -= spacing;
            }
            totalPreferred = Mathf.Max(totalMin, totalPreferred);
            SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
        }

		protected void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float size = rectTransform.rect.size[axis];
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);


            bool alongOtherAxis = (isVertical ^ (axis == 1));
            int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
            int endIndex = m_ReverseArrangement ? 0 : rectChildren.Count;
            int increment = m_ReverseArrangement ? -1 : 1;


			float offsetFromScrollrect = 0;
			if(Scroll != null) {
				float skewOffset = Skew * (rectChildren.Count);

				if (axis == 0) {
					offsetFromScrollrect = -(1 - Scroll.verticalNormalizedPosition) * (skewOffset - rectTransform.rect.width / 2);
				} else {
					offsetFromScrollrect = -(1 - Scroll.horizontalNormalizedPosition) * (skewOffset - rectTransform.rect.height / 2);
				}
			}

            if (alongOtherAxis)
            {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);

                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

					float skew = Skew * i;

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred) - (ShrinkWithSkew ? skew : 0);
                    float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);

                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, startOffset + skew + offsetFromScrollrect, requiredSpace, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell + skew + offsetFromScrollrect, scaleFactor);
                    }
                }
            }
            else
            {
                float pos = (axis == 0 ? padding.left : padding.top);
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - GetTotalPreferredSize(axis);

                if (surplusSpace > 0)
                {
                    if (GetTotalFlexibleSize(axis) == 0)
                        pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    else if (GetTotalFlexibleSize(axis) > 0)
                        itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
                }

                float minMaxLerp = 0;
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;

                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    childSize += flexible * itemFlexibleMultiplier;
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                    }
                    pos += childSize * scaleFactor + spacing;
                }
            }
        }

		private void GetChildSizes(RectTransform child, int       axis,      bool      controlSize, bool childForceExpand,
								   out float     min,   out float preferred, out float flexible)
		{
			if (!controlSize)
			{
				min       = child.sizeDelta[axis];
				preferred = min;
				flexible  = 0;
			}
			else
			{
				min       = LayoutUtility.GetMinSize(child, axis);
				preferred = LayoutUtility.GetPreferredSize(child, axis);
				flexible  = LayoutUtility.GetFlexibleSize(child, axis);
			}

			if (childForceExpand)
				flexible = Mathf.Max(flexible, 1);
		}
	}


}