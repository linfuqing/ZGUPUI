using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZG
{
    [CreateAssetMenu(menuName = "ZG/TextMeshProInputFilter")]
    public class TextMeshProInputFilter : TMPro.TMP_InputValidator
    {
        public string[] values;

#if UNITY_EDITOR
        [MenuItem("Assets/ZG/TextMeshPro Input Filter")]
        public static void FilterChars()
        {
            var filter = Selection.activeObject as TextMeshProInputFilter;
            if (filter == null)
                return;

            string path = EditorUtility.OpenFilePanel("Filter Chars", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;

            filter.values = string.Join(",", System.IO.File.ReadAllLines(path)).Split(',');
            if ((filter.values == null ? 0 : filter.values.Length) > 0)
            {
                var values = new System.Collections.Generic.List<string>();
                
                foreach (string value in filter.values)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    values.Add(value);
                }

                filter.values = values.ToArray();

                EditorUtility.SetDirty(filter);
            }
        }
#endif
        public override char Validate(ref string text, ref int pos, char ch)
        {
            string tempText = text + ch;
            //tempText = System.Text.RegularExpressions.Regex.Replace(PinYinConverter.Get(tempText), @"\W+", string.Empty).ToLower();

            int startIndex = Mathf.Clamp(pos, 0, tempText.Length - 1), index = startIndex + 1, tempIndex;
            foreach (string value in values)
            {
                tempIndex = tempText.LastIndexOf(value, startIndex);
                if (tempIndex != -1)
                    index = Mathf.Min(index, tempIndex);
            }

            if (index > pos)
                text = tempText;
            else
            {
                if (startIndex - 1 > index)
                    text = text.Remove(index, startIndex - index);

                --index;
            }

            pos = index;

            return ch;
        }
    }
}