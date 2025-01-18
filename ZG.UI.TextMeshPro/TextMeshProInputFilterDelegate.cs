using System.Text;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace ZG
{
    [RequireComponent(typeof(TMP_InputField))]
    public class TextMeshProInputFilterDelegate : MonoBehaviour
    {
        public char replaceChar = '*';
        private TMP_InputField __inputField;

        [SerializeField]
        internal TextMeshProInputFilter _filter = null;

        void Awake()
        {
            __inputField = GetComponent<TMP_InputField>();
            //__inputField.onValidateInput = __Validate;
            __inputField.onValueChanged.AddListener(__OnValueChanged);
        }

        /*private char __Validate(string text, int pos, char ch)
        {
            string tempText = text + ch;
            //tempText = System.Text.RegularExpressions.Regex.Replace(PinYinConverter.Get(tempText), @"\W+", string.Empty).ToLower();

            int startIndex = Mathf.Clamp(pos, 0, tempText.Length - 1), index = startIndex + 1, tempIndex;
            foreach (string value in _filter.values)
            {
                tempIndex = tempText.LastIndexOf(value, startIndex);
                if (tempIndex != -1)
                    index = Mathf.Min(index, tempIndex);
            }

            if (index <= pos)
            {
                if (startIndex - 1 > index)
                    __inputField.text = __inputField.text.Remove(index, startIndex - index);

                __inputField.stringPosition = index - 1;

                ch = (char)0;
            }
            
            return ch;
        }*/

        private void __OnValueChanged(string text)
        {
            int length = text.Length, i;
            string pingyin;
            List<int> offsets;
            Dictionary<string, List<int>> offsetMap = new Dictionary<string, List<int>>();
            for(i = 0; i < length; ++i)
            {
                pingyin = PinYinConverter.Get(text[i]).ToLower();
                if (!offsetMap.TryGetValue(pingyin, out offsets))
                {
                    offsets = new List<int>();

                    offsetMap[pingyin] = offsets;
                }

                offsets.Add(i);
            }

            bool isContains;
            int minOffset;
            StringBuilder stringBuilder = new StringBuilder(text);
            List<int> indices = new List<int>();
            foreach (string value in _filter.values)
            {
                if (value.Length > 1)
                {
                    minOffset = 0;
                    foreach (char c in value)
                    {
                        isContains = false;
                        pingyin = PinYinConverter.Get(c).ToLower();
                        if (offsetMap.TryGetValue(pingyin, out offsets))
                        {
                            foreach (var offset in offsets)
                            {
                                if (offset < minOffset)
                                    continue;

                                indices.Add(offset);

                                minOffset = offset + 1;

                                isContains = true;

                                break;
                            }
                        }

                        if(!isContains)
                        {
                            indices.Clear();

                            break;
                        }
                    }

                    if (indices.Count > 0)
                    {
                        foreach (var index in indices)
                            stringBuilder[index] = replaceChar;

                        indices.Clear();
                    }
                }
                else
                {
                    char c = value[0];
                    for (i = 0; i < length; ++i)
                    {
                        if (stringBuilder[i] == c)
                            stringBuilder[i] = replaceChar;
                    }
                }
            }

            int position = __inputField.stringPosition;
            __inputField.SetTextWithoutNotify(stringBuilder.ToString());
            __inputField.stringPosition = position;
        }
    }
}