using UnityEngine;
using UnityEngine.UI;

namespace ZG.UI
{
    public class ImageProgressbar : Progressbar
    {
        public bool isInvert;

        public float smoothTime;
        public Image image;

        private float __velocity;
        private float __value;

        public override float value
        {
            get
            {
                return __value;
            }

            set
            {
                base.value = value;

                if (!isActiveAndEnabled)
                {
                    __value = value;

                    image.fillAmount = isInvert ? 1.0f - value : value;
                }
            }
        }

        public override void Reset(float value)
        {
            image.fillAmount = isInvert ? 1.0f - value : value;

            __value = value;

            __velocity = 0.0f;

            base.Reset(value);
        }

        void Update()
        {
            if (smoothTime > 0.0f)
                __value = Mathf.SmoothDamp(__value, base.value, ref __velocity, smoothTime);
            else
                __value = base.value;

            image.fillAmount = isInvert ? 1.0f - __value : __value;
        }
    }
}