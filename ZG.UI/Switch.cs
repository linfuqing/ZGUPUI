using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ZG
{
    public class Switch : Toggle
    {
        public enum Mode
        {
            Normal,
            DeselectOff
        }

        public Mode mode;

        private bool __isOn;

        public void Cancel()
        {
            if (!__isOn)
                return;

            __isOn = false;
            
            isOn = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (mode == Mode.Normal)
            {
                if (!interactable)
                    return;

                isOn = true;

                __isOn = true;

                base.OnPointerDown(eventData);

                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            else
                base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (mode == Mode.Normal)
            {
                if (__isOn)
                {
                    __isOn = false;

                    var gameObject = eventData.pointerCurrentRaycast.gameObject;
                    var button = gameObject == null ? null : gameObject.GetComponentInParent<Button>();
                    if (button != null && button.transform.ContainsInParent(graphic.transform))
                        button.OnPointerClick(eventData);

                    isOn = false;

                    EventSystem eventSystem = EventSystem.current;
                    if (eventSystem.currentSelectedGameObject == gameObject)
                        eventSystem.SetSelectedGameObject(null);
                }
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (mode == Mode.DeselectOff)
            {
                base.OnPointerClick(eventData);

                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            Cancel();

            if (mode == Mode.DeselectOff)
                isOn = false;

            base.OnDeselect(eventData);
        }
    }
}