namespace ZG.UI
{
    public class MixProgressbar : Progressbar
    {
        public Progressbar[] instances;

        public Progressbar[] invertInstances;

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
                
                if(invertInstances != null)
                {
                    foreach(Progressbar instance in invertInstances)
                    {
                        if (instance != null)
                            instance.value = 1.0f - value;
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
            
            if (instances != null)
            {
                foreach (Progressbar instance in invertInstances)
                {
                    if (instance != null)
                        instance.Reset(1.0f - value);
                }
            }
            
            base.Reset(value);
        }
    }
}