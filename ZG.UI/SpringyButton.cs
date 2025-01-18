using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ZG
{
    public class SpringyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent onEnable;
        public UnityEvent onDisable;

        private Selectable __selectable = null;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!__selectable.interactable)
                return;

            if(onEnable != null)
                onEnable.Invoke();
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (!__selectable.interactable)
                return;

            if (onDisable != null)
                onDisable.Invoke();
        }
    }
}
