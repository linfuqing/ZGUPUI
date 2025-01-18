using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZG
{
    public class TextEx : Text, IPointerClickHandler
    {
        [Serializable]
        public class HrefClickEvent : UnityEvent<string> { }

        /// <summary>
        /// 超链接信息类
        /// </summary>
        private class HrefInfo
        {
            public int index;

            public int count;

            public string name;

            public readonly List<Rect> boxes = new List<Rect>();

            public HrefInfo(int index, int count, string name)
            {
                this.index = index;
                this.count = count;
                this.name = name;
            }
        }
        
        private bool __isUpdating;

        /// <summary>
        /// 解析完最终的文本
        /// </summary>
        private string __text;

        private Vector2 __size;

        /// <summary>
        /// 图片池
        /// </summary>
        private readonly List<Image> __imagesPool = new List<Image>();

        /// <summary>
        /// 图片的最后一个顶点的索引
        /// </summary>
        private readonly List<int> __imagesVertexIndex = new List<int>();

        /// <summary>
        /// 超链接信息列表
        /// </summary>
        private readonly List<HrefInfo> __hrefInfos = new List<HrefInfo>();

        /// <summary>
        /// 文本构造器
        /// </summary>
        private static readonly StringBuilder __textBuilder = new StringBuilder();
        
        public HrefClickEvent onHrefClick;

        /// <summary>
        /// 加载精灵图片方法
        /// </summary>
        public Action<string, string, Action<Sprite>> load;

        public override float preferredWidth
        {
            get
            {
                return base.preferredWidth + __size.x;
            }
        }

        public override float preferredHeight
        {
            get
            {
                return base.preferredHeight + __size.y;
            }
        }

        public override string text
        {
            get => __text;

            set
            {
                __text = value;

                __isUpdating = true;
                base.text = GetOutputText(value);
                __isUpdating = false;
            }
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();

            if(__isUpdating)
                __UpdateQuadImage();
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            string orignText = __text;
            __text = m_Text;
            base.OnPopulateMesh(toFill);
            __text = orignText;

            if (toFill != null)
            {
                int numImagesVertexIndices = __imagesVertexIndex.Count, i;
                Vector3 position;
                UIVertex vertex = new UIVertex();
                if (numImagesVertexIndices > 0)
                {
                    bool isHorizontal;
                    int currentVertCount = toFill.currentVertCount, imageVertexIndex, j, k;
                    Vector2 result = Vector2.zero, pivot = GetTextAnchorPivot(alignment), size;
                    Vector3 offset;
                    Rect rect;
                    Image image;
                    RectTransform rectTransform, parent = transform as RectTransform;

                    rect = parent == null ? default : parent.rect;
                    for (i = 0; i < numImagesVertexIndices; i++)
                    {
                        imageVertexIndex = __imagesVertexIndex[i];
                        image = __imagesPool[i];
                        rectTransform = image == null ? null : image.rectTransform;
                        if (rectTransform != null)
                        {
                            if (imageVertexIndex < toFill.currentVertCount)
                            {
                                toFill.PopulateUIVertex(ref vertex, imageVertexIndex);

                                size = rectTransform.sizeDelta;

                                offset = rectTransform.pivot - pivot;
                                rectTransform.anchoredPosition = new Vector2(vertex.position.x + offset.x * size.x, vertex.position.y + offset.y * size.y) - Rect.NormalizedToPoint(rect, (rectTransform.anchorMin + rectTransform.anchorMax) * 0.5f);

                                // 抹掉左下角的小黑点
                                toFill.PopulateUIVertex(ref vertex, imageVertexIndex - 3);
                                position = vertex.position;
                                toFill.PopulateUIVertex(ref vertex, imageVertexIndex);
                                vertex.position = position;

                                for (j = 1; j < 4; ++j)
                                    toFill.SetUIVertex(vertex, imageVertexIndex - j);

                                isHorizontal = false;
                                for (j = imageVertexIndex + 4; j < currentVertCount; j += 4)
                                {
                                    toFill.PopulateUIVertex(ref vertex, j);
                                    if (vertex.position.y < position.y)
                                        offset = new Vector3(0.0f, -size.y * pivot.y, 0.0f);
                                    else
                                    {
                                        isHorizontal = true;

                                        offset = new Vector3(size.x, 0.0f, 0.0f);
                                    }

                                    vertex.position += offset;
                                    toFill.SetUIVertex(vertex, j);

                                    for (k = 1; k < 4; ++k)
                                    {
                                        toFill.PopulateUIVertex(ref vertex, j - k);
                                        vertex.position += offset;
                                        toFill.SetUIVertex(vertex, j - k);
                                    }
                                }

                                for (j = imageVertexIndex - 8; j > -2; j -= 4)
                                {
                                    toFill.PopulateUIVertex(ref vertex, j + 4);
                                    if (vertex.position.y > position.y)
                                        offset = new Vector3(0.0f, size.y * (1.0f - pivot.y), 0.0f);
                                    else
                                    {
                                        isHorizontal = true;

                                        offset = new Vector3(-size.x * pivot.x, 0.0f, 0.0f);
                                    }

                                    vertex.position += offset;
                                    toFill.SetUIVertex(vertex, j + 4);

                                    for (k = 3; k > 0; --k)
                                    {
                                        toFill.PopulateUIVertex(ref vertex, j + k);
                                        vertex.position += offset;
                                        toFill.SetUIVertex(vertex, j + k);
                                    }
                                }

                                if (isHorizontal)
                                    result.x += size.x - fontSize;

                                result.y += size.y - lineSpacing;
                            }
                        }
                    }

                    if (!Mathf.Approximately(__size.x, result.x) || !Mathf.Approximately(__size.y, result.y))
                    {
                        __size = result;

                        SetLayoutDirty();
                    }
                }

                //__imagesVertexIndex.Clear();

                int count = toFill.currentVertCount, index;
                Bounds bounds;
                // 处理超链接包围框
                foreach (HrefInfo hrefInfo in __hrefInfos)
                {
                    hrefInfo.boxes.Clear();

                    index = hrefInfo.index << 2;
                    if (index >= count)
                        break;

                    // 将超链接里面的文本顶点索引坐标加入到包围框
                    toFill.PopulateUIVertex(ref vertex, index);
                    bounds = new Bounds(vertex.position, Vector3.zero);
                    for (i = 0; i < hrefInfo.count; ++i)
                    {
                        position = vertex.position;
                        if (position.y < bounds.min.y) // 换行重新添加包围框
                        {
                            hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                            bounds = new Bounds(position, Vector3.zero);
                        }
                        else
                            bounds.Encapsulate(position); // 扩展包围框

                        toFill.PopulateUIVertex(ref vertex, ++index);
                        bounds.Encapsulate(vertex.position);

                        toFill.PopulateUIVertex(ref vertex, ++index);
                        bounds.Encapsulate(vertex.position);

                        toFill.PopulateUIVertex(ref vertex, ++index);
                        bounds.Encapsulate(vertex.position);

                        ++index;
                        if (index >= count)
                            break;

                        toFill.PopulateUIVertex(ref vertex, index);
                    }

                    hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                }
            }
        }

        /// <summary>
        /// 获取超链接解析后的最后输出文本
        /// </summary>
        /// <returns></returns>
        protected virtual string GetOutputText(string outputText)
        {
            __textBuilder.Length = 0;
            __hrefInfos.Clear();

            int indexText = 0;
            MatchCollection matchCollection = string.IsNullOrEmpty(outputText) ? null : Regex.Matches(outputText, @"<a\s+href\s*=\s*([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);
            if (matchCollection != null)
            {
                string text;
                foreach (Match match in matchCollection)
                {
                    if (match == null)
                        continue;

                    __textBuilder.Append(outputText.Substring(indexText, match.Index - indexText));

                    text = match.Result("$2");
                    __hrefInfos.Add(new HrefInfo(__textBuilder.Length, text == null ? 0 : text.Length, match.Result("$1")));

                    __textBuilder.Append(text);
                    indexText = match.Index + match.Length;
                }
            }

            __textBuilder.Append(outputText.Substring(indexText, outputText.Length - indexText));
            return __textBuilder.ToString();
        }

        private void __UpdateQuadImage()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.GetPrefabAssetType(this) != UnityEditor.PrefabAssetType.NotAPrefab)
                return;
#endif

            Image image;

            //__text = GetOutputText(text);
            __imagesVertexIndex.Clear();
            MatchCollection matchCollection = Regex.Matches(m_Text,
                @"<quad(?:\s+\w+\s*=\s*\S+)*?(?:\s+name\s*=\s*(\S+))(?:\s+\w+\s*=\s*\S+)*?(?:(?:\s+label\s*=\s*(\S+))|(?:\s+width\s*=\s*(\d*\.?\d+))|(?:\s+height\s*=\s*(\d*\.?\d+)))*(?:\s+\w+\s*=\s*\S+)*/>",
                RegexOptions.Singleline);
            if (matchCollection != null)
            {
                __imagesPool.RemoveAll(x => x == null);
                if (__imagesPool.Count < 1)
                    GetComponentsInChildren(__imagesPool);

                int fontSize = base.fontSize;
                GameObject gameObject;
                Transform transform, parent = base.transform;
                RectTransform rectTransform;
                string width, height;
                foreach (Match match in matchCollection)
                {
                    if (match == null)
                        continue;

                    __imagesVertexIndex.Add(match.Index * 4 + 3);

                    if (__imagesVertexIndex.Count > __imagesPool.Count)
                    {
                        gameObject = DefaultControls.CreateImage(new DefaultControls.Resources());
                        if (gameObject != null)
                        {
                            gameObject.layer = gameObject.layer;
                            transform = gameObject.transform;
                            if (transform != null)
                            {
                                transform.SetParent(parent);
                                transform.localPosition = Vector3.zero;
                                transform.localRotation = Quaternion.identity;
                                transform.localScale = Vector3.one;
                            }

                            __imagesPool.Add(gameObject.GetComponent<Image>());
                        }
                    }

                    image = __imagesPool[__imagesVertexIndex.Count - 1];
                    if (image != null)
                    {
                        if(load != null)
                        {
                            Image temp = image;
                            load(match.Result("$1"), match.Result("$2"), x =>
                            {
                                if (temp != null)
                                    temp.sprite = x;
                            });
                        }

                        //image.sprite = load == null ? Resources.Load<Sprite>(match.Result("$1")) : load(match.Result("$1"), match.Result("$2"));

                        rectTransform = image.rectTransform;
                        if (rectTransform != null)
                        {
                            width = match.Result("$3");
                            height = match.Result("$4");

                            rectTransform.sizeDelta = new Vector2(string.IsNullOrEmpty(width) ? 1.0f : float.Parse(width), string.IsNullOrEmpty(height) ? 1.0f : float.Parse(height));/*new Vector2(
                                (string.IsNullOrEmpty(width) ? 1.0f : float.Parse(width)) / (string.IsNullOrEmpty(height) ? 1.0f : float.Parse(height)) * fontSize,
                                fontSize);*/
                        }

                        image.enabled = true;
                    }
                }
            }

            for (var i = __imagesVertexIndex.Count; i < __imagesPool.Count; i++)
            {
                image = __imagesPool[i];
                if (image != null)
                    image.enabled = false;
            }
        }

        /// <summary>
        /// 点击事件检测是否点击到超链接文本
        /// </summary>
        /// <param name="eventData"></param>
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
                return;

            foreach (HrefInfo hrefInfo in __hrefInfos)
            {
                foreach (Rect box in hrefInfo.boxes)
                {
                    if (box.Contains(localPoint))
                    {
                        onHrefClick.Invoke(hrefInfo.name);

                        return;
                    }
                }
            }
        }
    }
}