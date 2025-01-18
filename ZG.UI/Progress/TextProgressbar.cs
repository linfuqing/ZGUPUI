using UnityEngine;
using UnityEngine.UI;

namespace ZG.UI
{
    public class TextProgressbar : Progressbar
    {
        public Text text;
        
        public override float value
        {
            get
            {
                return base.value;
            }
            
            set
            {
                if (text != null)
                    text.text = Mathf.RoundToInt(value * 100.0f).ToString() + '%';

                base.value = value;
            }
        }

        public override Transform root => transform;

        public override void Reset(float value)
        {
            if(text != null)
                text.text = string.Empty;

            base.Reset(value);
        }
    }
}