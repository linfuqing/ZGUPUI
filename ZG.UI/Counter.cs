using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZG.UI
{
    public class Counter : Toggle
    {
        private int __count;

        public void Add()
        {
            isOn = ++__count > 0;
        }

        public void Subtract()
        {
            isOn = --__count > 0;

#if DEBUG
            if(__count < 0)
                UnityEngine.Debug.LogWarning(name + ": Count < 0");
#endif
        }

        public void AddOrSubtract(bool value)
        {
            if (value)
                Add();
            else
                Subtract();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            //base.OnPointerClick(eventData);
        }
    }
}