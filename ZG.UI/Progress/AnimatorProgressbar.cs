using UnityEngine;

namespace ZG.UI
{
    public class AnimatorProgressbar : Progressbar
    {
        public enum Type
        {
            Float,
            TriggerPositive,
            TriggerNegative
        }

        public Type type;
        public float smoothTime;
        public float maxSpeed;
        public string paramterName;
        public Animator animator;
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
                var animator = _GetAnimator();
                if (animator != null)
                {
                    switch (type)
                    {
                        case Type.TriggerPositive:
                            if (value > __value)
                                animator.SetTrigger(paramterName);
                            break;
                        case Type.TriggerNegative:
                            if (value < __value)
                                animator.SetTrigger(paramterName);
                            break;
                    }
                }

                base.value = value;

                if (!isActiveAndEnabled)
                    __value = value;
            }
        }

        public override Transform root
        {
            get
            {
                var animator = _GetAnimator();
                return animator == null ? transform : animator.transform;
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
            if (smoothTime > 0.0f && maxSpeed > 0.0f)
                __value = Mathf.SmoothDamp(__value, base.value, ref __velocity, smoothTime, maxSpeed);
            else
                __value = base.value;

            if (type == Type.Float)
            {
                var animator = _GetAnimator();
                if(animator != null && animator.isActiveAndEnabled)
                    animator.SetFloat(paramterName, __value);
            }
        }

        private Animator _GetAnimator()
        {
            if (animator == null)
                animator = ComponentManager<Animator>.Find(name);

            return animator;
        }
    }
}