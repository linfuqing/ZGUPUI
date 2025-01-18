using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

namespace ZG
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMeshProUGUILink : MonoBehaviour, IPointerClickHandler, IDeselectHandler
    {
        public event Action deselectEvent;

        public event Action<string, PointerEventData> clickEvent;

        private TextMeshProUGUI __text;
        
        public StringEvent onClick;

        public TextMeshProUGUI text
        {
            get
            {
                if(__text == null)
                    __text = GetComponent<TextMeshProUGUI>();

                return __text;
            }
            
        }
        
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
            
            if (onClick == null)
                return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.enterEventCamera);
            if (linkIndex == -1)
                return;

            // was a link clicked?
            TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

            var id = linkInfo.GetLinkID();
            // open the link id as a url, which is the metadata we added in the text field
            onClick.Invoke(id);

            if (clickEvent != null)
                clickEvent(id, eventData);
        }

        void IDeselectHandler.OnDeselect(BaseEventData baseEventData)
        {
            if (deselectEvent != null)
                deselectEvent();
        }
    }
}