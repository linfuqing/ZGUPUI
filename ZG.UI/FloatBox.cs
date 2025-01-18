using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ZG
{
    public class FloatBox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum StyleType
        {
            Normal,
            Left = 0x01,
            Right = 0x02,
            Top = 0x04,
            Bottom = 0x08,
            LeftTop = Left | Top,
            RightTop = Right | Top,
            LeftBottom = Left | Bottom,
            RightBottom = Right | Bottom
        }

        [System.Serializable]
        public struct Style
        {
            public StyleType type;

            public UnityEvent callback;
        }

        public UnityEvent onFocus;
        public UnityEvent onDefocus;

        public RectTransform viewport;
        public Graphic graphic;
        public Style[] styles;

        private Vector3[] __corners = new Vector3[4];
        private StyleType __styleType;
        private int __count;
        private bool __isPointerEnter;

        public Vector2 position
        {
            set
            {
                var graphic = this.graphic;
                var canvas = graphic == null ? null : graphic.canvas;
                if (canvas == null)
                    canvas = GetComponentInParent<Canvas>();
                
                if (canvas == null)
                    return;

                var renderMode = canvas.renderMode;
                var camera = renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                if (renderMode == RenderMode.WorldSpace)
                {
                    if(!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        ((RectTransform)transform.parent),
                        value,
                        camera,
                        out var worldPosition))
                        return;

                    transform.position = worldPosition;
                }
                else
                {
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        ((RectTransform)transform.parent),
                        value,
                        camera,
                        out var localPosition))
                        return;

                    ((RectTransform)transform).anchoredPosition = localPosition;
                }

                var rectTransform = graphic == null ? transform as RectTransform : graphic.rectTransform;
                rectTransform.GetWorldCorners(__corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue), point;
                for (int i = 0; i < 4; ++i)
                {
                    point = camera == null ? __corners[i] : RectTransformUtility.WorldToScreenPoint(camera, __corners[i]);
                    if(RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewport, point, camera, out point))
                    {
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }
                }

                Rect graphicRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y), viewport = this.viewport.rect;

                StyleType styleType = StyleType.Normal;
                if (graphicRect.xMin < viewport.xMin)
                    styleType |= StyleType.Left;
                else if (graphicRect.xMax > viewport.xMax)
                    styleType |= StyleType.Right;

                if (graphicRect.yMin < viewport.yMin)
                    styleType |= StyleType.Bottom;
                else if (graphicRect.yMax > viewport.yMax)
                    styleType |= StyleType.Top;

                if (styleType != __styleType)
                {
                    bool isNormal = styleType != StyleType.Normal;
                    if (styles != null)
                    {
                        foreach (var style in styles)
                        {
                            if (style.type == styleType)
                            {
                                if (style.callback != null)
                                    style.callback.Invoke();

                                isNormal = false;

                                break;
                            }
                        }
                    }

                    if (isNormal)
                    {
                        styleType = StyleType.Normal;

                        foreach (var style in styles)
                        {
                            if (style.type == styleType)
                            {
                                if (style.callback != null)
                                    style.callback.Invoke();

                                break;
                            }
                        }
                    }

                    __styleType = styleType;
                }
            }
        }

        public void Release()
        {
            if (--__count == 0)
            {
                if (onDefocus != null)
                    onDefocus.Invoke();
            }
        }

        public void Retain()
        {
            if (++__count == 1)
            {
                if (onFocus != null)
                    onFocus.Invoke();
            }
        }

/*#if UNITY_EDITOR
        void Update()
        {
            position = transform.position;
        }
#endif*/

        void OnDisable()
        {
            if(__isPointerEnter)
            {
                __isPointerEnter = false;

                Release();
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            __isPointerEnter = true;

            Retain();
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (__isPointerEnter)
            {
                __isPointerEnter = false;

                Release();
            }
        }
    }
}