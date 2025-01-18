using UnityEngine;

namespace ZG.UI
{
    public class TextEventProgressbar : Progressbar
    {
        public StringEvent onText;

        public override float value
        {
            get
            {
                return base.value;
            }

            set
            {
                if (onText != null)
                    onText.Invoke(Mathf.RoundToInt(value * 100.0f).ToString() + '%');

                base.value = value;
            }
        }

        public override void Reset(float value)
        {
            if (onText != null)
                onText.Invoke(string.Empty);

            base.Reset(value);
        }
    }
}