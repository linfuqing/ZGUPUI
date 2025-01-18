using UnityEngine;
using UnityEngine.UI;

namespace ZG
{
    [ExecuteAlways]
    public class GridLayoutGroupEx : LayoutGroup
    {
        /// <summary>
        /// The grid axis we are looking at.
        /// </summary>
        /// <remarks>
        /// As the storage is a [][] we make access easier by passing a axis.
        /// </remarks>
        public enum Axis
        {
            /// <summary>
            /// Horizontal axis
            /// </summary>
            Horizontal = 0,
            /// <summary>
            /// Vertical axis.
            /// </summary>
            Vertical = 1
        }
        
        [SerializeField] internal Axis _startAxis = Axis.Horizontal;

        /// <summary>
        /// Which axis should cells be placed along first
        /// </summary>
        /// <remarks>
        /// When startAxis is set to horizontal, an entire row will be filled out before proceeding to the next row. When set to vertical, an entire column will be filled out before proceeding to the next column.
        /// </remarks>
        public Axis startAxis { get { return _startAxis; } set { SetProperty(ref _startAxis, value); } }

        [SerializeField] internal int _constraintCount = 0;

        /// <summary>
        /// How many cells there should be along the constrained axis.
        /// </summary>
        public int constraintCount { get { return _constraintCount == 0 ? rectChildren.Count : _constraintCount; } set { SetProperty(ref _constraintCount, value); } }

        [SerializeField] internal Vector2 _spacing = Vector2.zero;

        /// <summary>
        /// The spacing to use between layout elements in the grid on both axises.
        /// </summary>
        public Vector2 spacing { get { return _spacing; } set { SetProperty(ref _spacing, value); } }
        
        [SerializeField] internal bool _childForceExpandWidth = true;

        /// <summary>
        /// Whether to force the children to expand to fill additional available horizontal space.
        /// </summary>
        public bool childForceExpandWidth { get { return _childForceExpandWidth; } set { SetProperty(ref _childForceExpandWidth, value); } }

        [SerializeField] internal bool _childForceExpandHeight = true;

        /// <summary>
        /// Whether to force the children to expand to fill additional available vertical space.
        /// </summary>
        public bool childForceExpandHeight { get { return _childForceExpandHeight; } set { SetProperty(ref _childForceExpandHeight, value); } }

        [SerializeField] internal bool _childControlWidth = true;

        /// <summary>
        /// Returns true if the Layout Group controls the widths of its children. Returns false if children control their own widths.
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the widths untouched. The widths of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the widths of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible widths. This is useful if the widths of the children should change depending on how much space is available.In this case the width of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible width for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlWidth { get { return _childControlWidth; } set { SetProperty(ref _childControlWidth, value); } }

        [SerializeField] internal bool _childControlHeight = true;

        /// <summary>
        /// Returns true if the Layout Group controls the heights of its children. Returns false if children control their own heights.
        /// </summary>
        /// <remarks>
        /// If set to false, the layout group will only affect the positions of the children while leaving the heights untouched. The heights of the children can be set via the respective RectTransforms in this case.
        ///
        /// If set to true, the heights of the children are automatically driven by the layout group according to their respective minimum, preferred, and flexible heights. This is useful if the heights of the children should change depending on how much space is available.In this case the height of each child cannot be set manually in the RectTransform, but the minimum, preferred and flexible height for each child can be controlled by adding a LayoutElement component to it.
        /// </remarks>
        public bool childControlHeight { get { return _childControlHeight; } set { SetProperty(ref _childControlHeight, value); } }

        [SerializeField] internal bool _childScaleWidth = false;

        /// <summary>
        /// Whether children widths are scaled by their x scale.
        /// </summary>
        public bool childScaleWidth { get { return _childScaleWidth; } set { SetProperty(ref _childScaleWidth, value); } }

        [SerializeField] internal bool _childScaleHeight = false;

        /// <summary>
        /// Whether children heights are scaled by their y scale.
        /// </summary>
        public bool childScaleHeight { get { return _childScaleHeight; } set { SetProperty(ref _childScaleHeight, value); } }
        
        /// <summary>
        /// Called by the layout system to calculate the horizontal layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            __SetLayoutAlongForAxis(startAxis == Axis.Vertical, 0);
        }

        /// <summary>
        /// Called by the layout system to calculate the vertical layout size.
        /// Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            __SetLayoutAlongForAxis(startAxis == Axis.Vertical, 1);
        }
        
        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            //SetCellsAlongAxis(0);
            
            __SetChildrenAlongAxis(0, startAxis == Axis.Vertical);
        }

        /// <summary>
        /// Called by the layout system
        /// Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical()
        {
            //SetCellsAlongAxis(1);

            __SetChildrenAlongAxis(1, startAxis == Axis.Vertical);
        }
        
        /// <summary>
        /// Calculate the layout element properties for this layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        private void __CalcAlongAxis(
            bool isVertical,
            int axis,
            int startRectChildernIndex,
            int rectChildrenCount,
            out float totalMin,
            out float totalPreferred,
            out float totalFlexible)
        {
            float /*combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical), */spacing = this.spacing[axis];
            bool controlSize = (axis == 0 ? _childControlWidth : _childControlHeight), 
                useScale = (axis == 0 ? _childScaleWidth : _childScaleHeight), 
                childForceExpandSize = (axis == 0 ? _childForceExpandWidth : _childForceExpandHeight);

            totalMin = 0.0f;// combinedPadding;
            totalPreferred = 0.0f;// combinedPadding;
            totalFlexible = 0;

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            for (int i = 0; i < rectChildrenCount; i++)
            {
                RectTransform child = rectChildren[i + startRectChildernIndex];
                float min, preferred, flexible;
                __GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

                if (useScale)
                {
                    float scaleFactor = child.localScale[axis];
                    min *= scaleFactor;
                    preferred *= scaleFactor;
                    flexible *= scaleFactor;
                }

                if (alongOtherAxis)
                {
                    totalMin = Mathf.Max(min/* + combinedPadding*/, totalMin);
                    totalPreferred = Mathf.Max(preferred/* + combinedPadding*/, totalPreferred);
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
        }

        private void __SetLayoutAlongForAxis(bool isVertical, int axis)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GridLayoutGroupEx.SetLayoutAlongForAxis");

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            int rectChildrenCount = rectChildren.Count;
            float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical), 
                spacing = this.spacing[axis], 
                totalMin = combinedPadding, 
                totalPreferred = combinedPadding, 
                totalFlexble = 0.0f, 
                min, preferred, flexible;
            for (int i = 0; i < rectChildrenCount; i += constraintCount)
            {
                __CalcAlongAxis(isVertical, axis, i, Mathf.Min(rectChildrenCount - i, constraintCount), out min, out preferred, out flexible);

                if (alongOtherAxis)
                {
                    totalMin += min + spacing;
                    totalPreferred += preferred + spacing;
                    totalFlexble += flexible;
                }
                else
                { 
                    totalMin = Mathf.Max(min + combinedPadding, totalMin);
                    totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                    totalFlexble = Mathf.Max(flexible, totalFlexble);
                }
            }

            if (alongOtherAxis && rectChildrenCount > 0)
            {
                totalMin -= spacing;
                totalPreferred -= spacing;
            }

            totalPreferred = Mathf.Max(totalMin, totalPreferred);

            SetLayoutInputForAxis(
                totalMin,
                totalPreferred,
                totalFlexble,
                axis);
            
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        private void __SetChildrenAlongAxis(int axis, bool isVertical)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GridLayoutGroupEx.SetChildrenAlongAxis");

            float size = rectTransform.rect.size[axis],
                combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical),
                spacing = this.spacing[axis], 
                alignmentOnAxis = GetAlignmentOnAxis(axis);
            bool controlSize = (axis == 0 ? _childControlWidth : _childControlHeight);
            bool useScale = (axis == 0 ? _childScaleWidth : _childScaleHeight);
            bool childForceExpandSize = (axis == 0 ? _childForceExpandWidth : _childForceExpandHeight);

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            if (alongOtherAxis)
            {
                float startOffset = 0;

                float itemFlexibleMultiplier = 0.0f, totalMin = GetTotalMinSize(axis), totalPreferred = GetTotalPreferredSize(axis), totalFlexible = GetTotalFlexibleSize(axis);

                float surplusSpace = size - totalPreferred;
                if (surplusSpace > 0.0f)
                {
                    if (totalFlexible > 0.0f)
                        itemFlexibleMultiplier = surplusSpace / totalFlexible;
                    else if (totalFlexible == 0.0f)
                        startOffset = GetStartOffset(axis, totalPreferred - (axis == 0 ? padding.horizontal : padding.vertical)) - (axis == 0 ? padding.left : padding.top);
                }

                float minMaxLerp = 0.0f;
                if (totalMin != totalPreferred)
                    minMaxLerp = Mathf.Clamp01((size - totalMin) / (totalPreferred - totalMin));

                float innerSize = size - combinedPadding, maxSpace = 0.0f;
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    __GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;
                    //float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    //space = requiredSpace * scaleFactor;
                    float requiredSpace = Mathf.Lerp(min, preferred, minMaxLerp);
                    requiredSpace += flexible * itemFlexibleMultiplier;
                    float space = requiredSpace * scaleFactor;
                    if (i % constraintCount == 0)
                    {
                        if (i != 0)
                            startOffset += maxSpace + spacing;

                        __CalcAlongAxis(
                            isVertical,
                            axis,
                            i + 1,
                            Mathf.Min(rectChildren.Count - i, constraintCount) - 1,
                            out totalMin,
                            out totalPreferred,
                            out totalFlexible);

                        maxSpace = Mathf.Max(space, Mathf.Lerp(totalMin, totalPreferred, minMaxLerp) + totalFlexible * itemFlexibleMultiplier);
                    }

                    float offset = startOffset + GetStartOffset(axis, innerSize - maxSpace + space);
                    //float startOffset = GetStartOffset(axis, innerSize - maxSpace + space);
                    //maxOffset = Mathf.Max(maxOffset, startOffset + requiredSpace);
                    //maxSpace = Mathf.Max(maxSpace, requiredSpace * scaleFactor);
                    if (controlSize)
                    {
                        SetChildAlongAxisWithScale(child, axis, offset, requiredSpace, scaleFactor);
                    }
                    else
                    {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScale(child, axis, offset + offsetInCell, scaleFactor);
                    }
                }
            }
            else
            {
                float startPos = (axis == 0 ? padding.left : padding.top), pos = startPos, itemFlexibleMultiplier = 0.0f, minMaxLerp = 0.0f;
                
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    __GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    if (i % constraintCount == 0)
                    {
                        pos = startPos;

                        __CalcAlongAxis(isVertical, 
                            axis, 
                            i, 
                            Mathf.Min(rectChildren.Count - i, constraintCount), 
                            out float totalMin, 
                            out float totalPreferred, 
                            out float totalFlexible);

                        totalMin += combinedPadding;
                        totalPreferred += combinedPadding;

                        itemFlexibleMultiplier = 0.0f;

                        float surplusSpace = size - totalPreferred;
                        if (surplusSpace > 0.0f)
                        {
                            if (totalFlexible > 0.0f)
                                itemFlexibleMultiplier = surplusSpace / totalFlexible;
                            else if(totalFlexible == 0.0f)
                                pos = GetStartOffset(axis, totalPreferred - (axis == 0 ? padding.horizontal : padding.vertical));
                        }

                        minMaxLerp = 0.0f;
                        if (totalMin != totalPreferred)
                            minMaxLerp = Mathf.Clamp01((size - totalMin) / (totalPreferred - totalMin));
                    }

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
            
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void __GetChildSizes(
            RectTransform child, 
            int axis, 
            bool controlSize, 
            bool childForceExpand,
            out float min, 
            out float preferred, 
            out float flexible)
        {
            if (!controlSize)
            {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
            }
            else
            {
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetFlexibleSize(child, axis);
            }

            if (childForceExpand)
                flexible = Mathf.Max(flexible, 1);
        }
        
#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();

            // For new added components we want these to be set to false,
            // so that the user's sizes won't be overwritten before they
            // have a chance to turn these settings off.
            // However, for existing components that were added before this
            // feature was introduced, we want it to be on be default for
            // backwardds compatibility.
            // Hence their default value is on, but we set to off in reset.
            _childControlWidth = false;
            _childControlHeight = false;
        }

        private int __capacity = 8;
        private Vector2[] __sizes = new Vector2[8];

        protected virtual void Update()
        {
            if (Application.isPlaying)
                return;

            int count = transform.childCount;

            if (count > __capacity)
            {
                if (count > __capacity * 2)
                    __capacity = count;
                else
                    __capacity *= 2;

                __sizes = new Vector2[__capacity];
            }

            // If children size change in editor, update layout (case 945680 - Child GameObjects in a Horizontal/Vertical Layout Group don't display their correct position in the Editor)
            bool dirty = false;
            for (int i = 0; i < count; i++)
            {
                RectTransform t = transform.GetChild(i) as RectTransform;
                if (t != null && t.sizeDelta != __sizes[0])
                {
                    dirty = true;
                    __sizes[i] = t.sizeDelta;
                }
            }

            if (dirty)
                LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }

#endif
    }
}