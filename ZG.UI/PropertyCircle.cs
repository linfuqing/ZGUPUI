using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ZG.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class PropertyCircle : Polygon
    {
        [Serializable]
        public struct Outline
        {
            public float value;
            public float outWidth;
            public float inWidth;
            public Color outColor;
            public Color inColor;
        }

        [Serializable]
        public struct Property
        {
            public float value;
            public UnityEvent<Vector2> onLocate;
        }

        private static readonly List<Vector2> __vertices = new List<Vector2>();

        [SerializeField]
        internal float _lineWidth = 2;

        [SerializeField]
        internal Color _lineColor = Color.white;

        [SerializeField]
        internal Color _centerColor = Color.black;

        [SerializeField]
        internal List<Property> _properties = new List<Property>
        {
            new Property()
            {
                value = 1.0f,
            },
            new Property()
            {
                value = 1.0f,
            },
            new Property()
            {
                value = 1.0f,
            }
        };

        [SerializeField]
        internal List<Outline> _outlines = null;

        public Vector2 GetPosition(int index, float value)
        {
            var rect = rectTransform.rect;
            Vector2 center = rect.center;

            int count = _properties == null ? 0 : _properties.Count;
            if (count < 2)
                return center;

            float radius = Mathf.Min(rect.width, rect.height) * 0.5f,
                angle = Mathf.PI * 2.0f / count, currentAngle = angle * index;

            return new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * value * radius + center;
        }

        public List<Property> Begin()
        {
            return _properties;
        }

        public void End() => SetVerticesDirty();

        protected override void _Draw()
        {
            int count = _properties == null ? 0 : _properties.Count;
            if (count < 2)
                return;

            Property property;
            var rect = rectTransform.rect;
            Vector2 center = rect.center, position;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f, 
                angle = Mathf.PI * 2.0f / count, currentAngle;
            int i;

            if (_outlines != null)
            {
                Vector2 origin;
                foreach (var outline in _outlines)
                {
                    origin = new Vector2(outline.value * radius, 0.0f) + center;

                    if (outline.outWidth > Mathf.Epsilon)
                    {
                        currentAngle = -angle;

                        position = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * outline.value * radius + center;

                        DrawLine(position, origin, outline.outColor, outline.outColor, outline.outWidth);
                    }

                    if (outline.inWidth > Mathf.Epsilon)
                        DrawLine(origin, center, outline.outColor, outline.inColor, outline.inWidth);

                    for (i = 1; i < count; ++i)
                    {
                        currentAngle = angle * i;

                        position = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * outline.value * radius + center;

                        if(outline.outWidth > Mathf.Epsilon)
                            DrawLine(origin, position, outline.outColor, outline.outColor, outline.outWidth);

                        if(outline.inWidth > Mathf.Epsilon)
                            DrawLine(position, center, outline.outColor, outline.inColor, outline.inWidth);

                        origin = position;
                    }

                    currentAngle = angle * (count - 1);
                }
            }

            __vertices.Clear();

            for (i = 0; i < count; ++i)
            {
                property = _properties[i];

                currentAngle = angle * i;

                position = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * property.value * radius + center;

                if (property.onLocate != null)
                    property.onLocate.Invoke(position);

                __vertices.Add(position);
            }

            DrawFan(color, _centerColor, center, __vertices);

            if(_lineWidth > Mathf.Epsilon)
            {
                for(i = 1; i < count; ++i)
                    DrawLine(__vertices[i - 1], __vertices[i], _lineColor, _lineColor, _lineWidth);

                DrawLine(__vertices[count - 1], __vertices[0], _lineColor, _lineColor, _lineWidth);
            }
        }
    }
}
