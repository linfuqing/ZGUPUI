namespace ZG.UI
{
    public class MixProgressbar : Progressbar
    {
        public Progressbar[] instances;

        public override float value
        {
            get
            {
                return base.value;
            }

            set
            {
                if(instances != null)
                {
                    foreach(Progressbar instance in instances)
                    {
                        if (instance != null)
                            instance.value = value;
                    }
                }

                base.value = value;
            }
        }

        public override void Reset(float value)
        {
            if (instances != null)
            {
                foreach (Progressbar instance in instances)
                {
                    if (instance != null)
                        instance.Reset(value);
                }
            }

            base.Reset(value);
        }
    }
}