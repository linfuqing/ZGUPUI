using UnityEngine;

namespace ZG.UI
{
    public class RectProgressbar : Progressbar
    {
        public enum Layout
        {
            LeftToRight,
            RightToLeft,
            UpToDown,
            DownToUp,
        }

        public bool isClamp = true;
        public Layout layout;
        public float smoothTime;
        public float maxSpeed;
        private float __velocity;
        private float __value;

        public override float value
        {
            get
            {
                return base.value;
            }

            set
            {
                if (!isActiveAndEnabled)
                    __value = value;

                base.value = value;
            }
        }

        public override void Reset(float value)
        {
            __value = value;

            __velocity = 0.0f;

            base.Reset(value);
        }

        void Update()
        {
            RectTransform transform = this.transform as RectTransform;
            if (transform == null)
                return;

            __value = Mathf.SmoothDamp(__value, base.value, ref __velocity, smoothTime, maxSpeed);
            Vector2 max = transform.anchorMax, min = transform.anchorMin;
            switch (layout)
            {
                case Layout.LeftToRight:
                    if (isClamp)
                        min.x = 0.0f;
                    else
                        min.x = __value - 1.0f;

                    max.x = __value;
                    break;
                case Layout.RightToLeft:
                    min.x = 1.0f - __value;

                    if (isClamp)
                        max.x = 1.0f;
                    else
                        max.x = 2.0f - __value;
                    break;
                case Layout.UpToDown:
                    min.y = 1.0f - __value;

                    if (isClamp)
                        max.y = 1.0f;
                    else
                        max.y = 2.0f - __value;
                    break;
                case Layout.DownToUp:
                    if (isClamp)
                        min.y = 0.0f;
                    else
                        min.y = __value - 1.0f;

                    max.y = __value;
                    break;
            }

            transform.anchorMax = max;
            transform.anchorMin = min;
        }
    }
}