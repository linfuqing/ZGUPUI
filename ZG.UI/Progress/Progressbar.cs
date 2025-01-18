using UnityEngine;
using UnityEngine.Events;

namespace ZG.UI
{
    public abstract class Progressbar : MonoBehaviour
    {
        [System.Serializable]
        public struct Event
        {
            public float min;
            public float max;

            public FloatEvent onCallback;
        }

        public UnityEvent onIncrement;
        public UnityEvent onDecrement;

        public Event[] events;

        protected float _value;

        private int __eventIndex = -1;

        public virtual Transform root
        {
            get
            {
                Transform transform = this.transform;
                return transform == null ? null : transform.parent;
            }
        }

        public virtual float value
        {
            get
            {
                return _value;
            }

            set
            {
                int numEvents = events == null ? 0 : events.Length;
                for (int i = 0; i < numEvents; ++i)
                {
                    ref var @event = ref events[i];
                    if (@event.min <= value && @event.max >= value)
                    {
                        if (__eventIndex != i)
                        {
                            if (@event.onCallback != null)
                                @event.onCallback.Invoke(value);

                            __eventIndex = i;
                        }
                        break;
                    }
                }

                if (!Mathf.Approximately(value, _value))
                {
                    if (value > _value)
                    {
                        if (onIncrement != null)
                            onIncrement.Invoke();
                    }
                    else
                    {
                        if (onDecrement != null)
                            onDecrement.Invoke();
                    }

                    _value = value;
                }
            }
        }

        public virtual void Reset(float value)
        {
            int numEvents = events == null ? 0 : events.Length;
            for (int i = 0; i < numEvents; ++i)
            {
                ref var @event = ref events[i];
                if (@event.min <= value && @event.max >= value)
                {
                    if (__eventIndex != i)
                    {
                        if (@event.onCallback != null)
                            @event.onCallback.Invoke(value);

                        __eventIndex = i;
                    }
                    break;
                }
            }

            _value = value;
        }

        public void Parse(string value)
        {
            this.value = float.Parse(value);
        }
    }
}