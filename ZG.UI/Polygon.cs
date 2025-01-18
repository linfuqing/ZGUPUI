using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZG.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class Polygon : Graphic
    {
        private static readonly List<UIVertex> __vertexes = new List<UIVertex>();
        private static readonly List<int> __indices = new List<int>();

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            color = Color.magenta;
            raycastTarget = false;
        }
#endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            __vertexes.Clear();
            __indices.Clear();

            _Draw();

            vh.Clear();
            vh.AddUIVertexStream(__vertexes, __indices);
        }

        protected abstract void _Draw();

        public void DrawFan(Color color, Color centerColor, Vector2 centerPosition, IEnumerable<Vector2> points)
        {
            int index = __vertexes.Count;

            __vertexes.Add(new UIVertex { position = centerPosition, color = centerColor });

            foreach(var point in points)
                __vertexes.Add(new UIVertex { position = point, color = color });

            int length = __vertexes.Count - index - 1;
            for(int i = 1; i < length; ++i)
            {
                __indices.Add(index);
                __indices.Add(index + i);
                __indices.Add(index + i + 1);
            }

            __indices.Add(index);
            __indices.Add(__vertexes.Count - 1);
            __indices.Add(index + 1);
        }

        public void DrawFan(Color color, Color centerColor, Vector2 centerPosition, params Vector2[] points)
        {
            DrawFan(color, centerColor, centerPosition, points);
        }

        public void DrawLine(
            Vector2 startPosition,
            Vector2 endPosition,
            Color startColor,
            Color endColor,
            float width)
        {
            var direction = (endPosition - startPosition).normalized;
            var normal = Vector3.Cross(transform.forward, direction).normalized;
            var offsetV = new Vector2(normal.x, normal.y) * width / 2;
            var offsetH = direction * width / 2; // 如果不考虑横向偏移，当线比较宽时转折点就会出现缺口
            var leftBottom = startPosition - offsetV - offsetH;
            var leftTop = startPosition + offsetV - offsetH;
            var rightTop = endPosition + offsetV + offsetH;
            var rightBottom = endPosition - offsetV + offsetH;

            int index = __vertexes.Count;

            __vertexes.Add(new UIVertex() { position = leftBottom, color = startColor });
            __vertexes.Add(new UIVertex() { position = leftTop, color = startColor });
            __vertexes.Add(new UIVertex() { position = rightTop, color = endColor });
            __vertexes.Add(new UIVertex() { position = rightBottom, color = endColor });

            __indices.Add(index + 0);
            __indices.Add(index + 1);
            __indices.Add(index + 2);

            __indices.Add(index + 0);
            __indices.Add(index + 2);
            __indices.Add(index + 3);
        }
    }
}
