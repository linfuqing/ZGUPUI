﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CellLengths = Unity.Collections.FixedList512Bytes<Unity.Mathematics.float2>;

namespace ZG
{
    public interface IScrollRectSubmitHandler : ISubmitHandler
    {
        void OnScrollRectDrag(float value);
    }
    
    public struct ScrollRectData
    {
        public float decelerationRate;
        public float elasticity;

        //public int2 count;
        public float2 contentLength;
        public float2 viewportLength;

        public float2 length
        {
            get
            {
                return contentLength - viewportLength;
            }
        }

        /*public float2 cellLength
        {
            get
            {
                return new float2(count.x > 0 ? contentLength.x / count.x : contentLength.x, count.y > 0 ? contentLength.y / count.y : contentLength.y);
            }
        }*/

        public float2 GetNormalizedPosition(in int2 index, in int2 count, float offsetScale)
        {
            float2 cellLength = GetCellLength(count), offset = GetOffset(cellLength, offsetScale);
            return (index * cellLength + offset) / length;
        }

        public float2 GetCellLength(in int2 count)
        {
            return new float2(count.x > 0 ? contentLength.x / count.x : contentLength.x, count.y > 0 ? contentLength.y / count.y : contentLength.y);
        }

        public float2 GetOffset(in float2 cellLength, float scale)
        {
            return (cellLength - viewportLength) * scale;
        }

        public float2 GetIndex(
            in int2 count, 
            in float2 normalizedPosition, 
            in float2 length, 
            in float2 cellLength, 
            in float2 offset)
        {
            return math.clamp((normalizedPosition * length - offset) / cellLength, 0.0f, new float2(count.x > 0 ? count.x - 1 : 0, count.y > 0 ? count.y - 1 : 0));
        }

        public float2 GetIndex(
            in float2 normalizedPosition,
            in float2 length,
            in float2 offset,
            in CellLengths cellLengths)
        {
            float2 positionLength = normalizedPosition * length - offset, result = float2.zero;
            foreach(var cellLength in cellLengths)
            {
                if(positionLength.x < cellLength.x && positionLength.y < cellLength.y)
                {
                    result += math.float2(positionLength.x / cellLength.x, positionLength.y / cellLength.y);

                    break;
                }

                if (positionLength.x > cellLength.x)
                {
                    positionLength.x -= cellLength.x;

                    result.x += 1.0f;
                }

                if (positionLength.y > cellLength.y)
                {
                    positionLength.y -= cellLength.y;

                    result.y += 1.0f;
                }
            }

            return result;
        }

        public float2 GetIndex(float offsetScale, in float2 normalizedPosition, in float2 length, in int2 count)
        {
            float2 cellLength = GetCellLength(count);

            return GetIndex(count, normalizedPosition, length, cellLength, GetOffset(cellLength, offsetScale));
        }

        public float2 GetIndex(float offsetScale, in float2 normalizedPosition, in int2 count)
        {
            return GetIndex(offsetScale, normalizedPosition, length, count);
        }
    }

    public struct ScrollRectInfo
    {
        public int isVail;
        public int2 index;
    }

    public struct ScrollRectNode
    {
        public float2 velocity;
        public float2 normalizedPosition;
        public float2 index;
    }

    public struct ScrollRectEvent
    {
        [Flags]
        public enum Flag
        {
            Changed = 0x01,
            SameAsInfo = 0x02
        }

        public int version;
        public Flag flag;
        public float2 index;
    }

    public class ScrollRectComponent : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private class SubmitHandler : ISubmitHandler
        {
            public readonly GameObject GameObject;

            public SubmitHandler([NotNull]GameObject gameObject)
            {
                GameObject = gameObject;
            }

            public override bool Equals(object obj)
            {
                var component = obj as Component;
                if (component == null)
                {
                    var submitHandler = obj as SubmitHandler;
                    if (submitHandler == null)
                        return false;

                    return submitHandler.GameObject == GameObject;
                }

                return GameObject == component.gameObject;
            }

            public override int GetHashCode()
            {
                return GameObject.GetHashCode();
            }

            void ISubmitHandler.OnSubmit(BaseEventData eventData)
            {
                
            }
        }

        public event Action<float2> onChanged;

        private int2 __count;
        private ScrollRectData __data;
        private ScrollRectInfo __info;
        private ScrollRectNode? __node;
        private ScrollRectEvent __event;

        private ScrollRect __scrollRect;
        private ISubmitHandler __submitHandler;
        private List<ISubmitHandler> __submitHandlers;

        public int version
        {
            get;

            private set;
        }

        public virtual float offsetScale => 0.5f;

        public virtual int2 count
        {
            get
            {
                var submitHandlers = this.submitHandlers;
                if(submitHandlers == null)
                    return int2.zero;

                RectTransform.Axis axis = scrollRect.horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;
                int2 result = int2.zero;
                result[(int)axis] = submitHandlers.Count;
                
                return result;
            }
        }

        public int2 index
        {
            get;

            private set;
        } = new int2(-1, -1);

        public ScrollRectData data
        {
            get
            {
                ScrollRect scrollRect = this.scrollRect;
                if (scrollRect == null)
                    return default;

                ScrollRectData result;
                result.decelerationRate = scrollRect.decelerationRate;
                result.elasticity = scrollRect.elasticity;

                //result.count = count;

                //Canvas.ForceUpdateCanvases();

                RectTransform content = scrollRect.content;
                result.contentLength = content == null ? float2.zero : (float2)content.rect.size;

                RectTransform viewport = scrollRect.viewport;
                result.viewportLength = viewport == null ? float2.zero : (float2)viewport.rect.size;

                return result;
            }
        }

        public ScrollRect scrollRect
        {
            get
            {
                if (__scrollRect == null)
                    __scrollRect = GetComponent<ScrollRect>();

                return __scrollRect;
            }
        }

        public IReadOnlyList<ISubmitHandler> submitHandlers
        {
            
            get
            {
                ScrollRect scrollRect = this.scrollRect;
                RectTransform content = scrollRect == null ? null : scrollRect.content;
                if (content == null)
                    return null;

                if (__submitHandlers == null)
                    __submitHandlers = new List<ISubmitHandler>();

                bool isChanged = false;
                int index = 0, numChildren = content.childCount;
                ISubmitHandler submitHandler;
                GameObject gameObject;
                Transform child;
                for(int i = 0; i < numChildren; ++i)
                {
                    child = content.GetChild(i);
                    gameObject = child.gameObject;
                    if (gameObject != null && gameObject.activeSelf)
                    {
                        submitHandler = gameObject.GetComponent<ISubmitHandler>();
                        if (index < __submitHandlers.Count)
                        {
                            if (submitHandler != __submitHandlers[index])
                            {
                                if (submitHandler == null)
                                {
                                    if (__submitHandlers[index] is SubmitHandler temp && temp.GameObject == gameObject)
                                    {
                                        ++index;

                                        continue;
                                    }

                                    submitHandler = new SubmitHandler(gameObject);
                                }
                                
                                __submitHandlers[index] = submitHandler;

                                isChanged = true;
                            }
                        }
                        else
                        {
                            if (submitHandler == null)
                                submitHandler = new SubmitHandler(gameObject);
                            
                            __submitHandlers.Add(submitHandler);

                            isChanged = true;
                        }

                        ++index;
                    }
                }

                int numSubmitHandlers = __submitHandlers.Count;
                if (index < numSubmitHandlers)
                {
                    __submitHandlers.RemoveRange(index, numSubmitHandlers - index);

                    isChanged = true;
                }

                if (isChanged)
                    --version;

                return __submitHandlers;
            }
        }

        public static Vector2 GetSize(RectTransform rectTransform, bool isHorizontal, bool isVertical)
        {
            /*if (rectTransform == null)
                return Vector2.zero;

            Vector2 min = rectTransform.anchorMin, max = rectTransform.anchorMax;
            if (Mathf.Approximately(min.x, max.x))
            {
                LayoutGroup layoutGroup = rectTransform.GetComponent<LayoutGroup>();
                if (layoutGroup != null)
                    layoutGroup.SetLayoutHorizontal();

                ContentSizeFitter contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                    contentSizeFitter.SetLayoutHorizontal();

                //return rectTransform.sizeDelta;
            }

            if (Mathf.Approximately(min.y, max.y))
            {
                LayoutGroup layoutGroup = rectTransform.GetComponent<LayoutGroup>();
                if (layoutGroup != null)
                    layoutGroup.SetLayoutVertical();

                ContentSizeFitter contentSizeFitter = rectTransform.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                    contentSizeFitter.SetLayoutVertical();

                //return rectTransform.sizeDelta;
            }

            Vector2 size = GetSize(rectTransform.parent as RectTransform);

            return size * max + rectTransform.offsetMax - size * min - rectTransform.offsetMin;*/
            if (rectTransform == null)
                return Vector2.zero;

            LayoutGroup layoutGroup = rectTransform.GetComponentInParent<LayoutGroup>();
            if (layoutGroup != null)
            {
                if (isHorizontal)
                    layoutGroup.SetLayoutHorizontal();

                if (isVertical)
                    layoutGroup.SetLayoutVertical();
            }

            ContentSizeFitter contentSizeFitter = rectTransform.GetComponentInChildren<ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                if (isHorizontal)
                    contentSizeFitter.SetLayoutHorizontal();

                if (isVertical)
                    contentSizeFitter.SetLayoutVertical();
            }

            rectTransform.ForceUpdateRectTransforms();

            Canvas.ForceUpdateCanvases();

            return rectTransform.rect.size;
        }

        public virtual float __ToSubmitIndex(in float2 index)
        {
            RectTransform.Axis axis = scrollRect.horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;

            return index[(int)axis];
        }

        public virtual void UpdateData()
        {
            //__data = data;

            //this.SetComponentData(__data);

            __EnableNode();

            //任务会SB
            //__info.index = 0;// math.clamp(__info.index, 0, math.max(1, __data.count) - 1);
        }

        public virtual void MoveTo(in int2 index)
        {
            ScrollRectInfo info;
            info.isVail = 1;
            info.index = index;
            __info = info;
            //this.AddComponentData(info);
        }

        //不行
        /*public void SetTo(in int2 index)
        {
            //For Update Count
            --version;
            
            float2 normalizedPosition = data.GetNormalizedPosition(index, count, offsetScale);
            scrollRect.velocity = float2.zero;
            scrollRect.normalizedPosition = normalizedPosition;
            __EnableNode();
            __info.isVail = 1;
            __info.index = index;
        }*/

        protected void OnEnable()
        {
            __event.version = 0;
            __event.flag = 0;
            __event.index = math.int2(-1, -1);
        }

        protected void Start()
        {
            UpdateData();
        }

        protected void Update()
        {
            float2 indexFloat;
            if (__node != null)
            {
                __data = data;

                __count = count;

                if (__info.isVail != 0)
                {
                    int2 source = __info.index, 
                        destination = math.clamp(source, 0, math.max(1, __count) - 1);
                    if (!destination.Equals(source))
                    {
                        __info.index = destination;

                        --version;
                    }
                }

                var node = __node.Value;
                if (ScrollRectUtility.Execute(
                        version, 
                        Time.deltaTime, 
                        offsetScale, 
                        __count, 
                        __data, 
                        __info, 
                        ref node, 
                        ref __event))
                    _Set(__event);

                //if(!node.normalizedPosition.Equals(__node.Value.normalizedPosition) || !((float2)scrollRect.normalizedPosition).Equals(node.normalizedPosition))
                scrollRect.normalizedPosition = node.normalizedPosition;

                __node = node;

                indexFloat = node.index;
            }
            else
                indexFloat = __data.GetIndex(offsetScale, scrollRect.normalizedPosition, count);
            
            if (__submitHandlers != null)
            {
                //var temp = __data.GetIndex(offsetScale, node.normalizedPosition, count);
                float originIndex = __ToSubmitIndex(indexFloat);
                int sourceIndex = (int)math.floor(originIndex), 
                    destinationIndex = (int)math.ceil(originIndex), 
                    numSubmitHandles = __submitHandlers.Count;
                var submitHandler = sourceIndex >= 0 && sourceIndex < numSubmitHandles
                    ? __submitHandlers[sourceIndex] as IScrollRectSubmitHandler
                    : null;
                if (sourceIndex == destinationIndex)
                {
                    if(submitHandler != null)
                        submitHandler.OnScrollRectDrag(1.0f);
                }
                else
                {
                    if(submitHandler != null)
                        submitHandler.OnScrollRectDrag(destinationIndex - originIndex);

                    submitHandler = destinationIndex >= 0 && destinationIndex < numSubmitHandles
                        ? __submitHandlers[destinationIndex] as IScrollRectSubmitHandler
                        : null;
                        
                    if(submitHandler != null)
                        submitHandler.OnScrollRectDrag(originIndex - sourceIndex);
                }
            }
        }

        internal void _Set(in ScrollRectEvent result)
        {
            if (version == result.version)
                return;

            version = result.version;

            if ((result.flag & ScrollRectEvent.Flag.Changed) == ScrollRectEvent.Flag.Changed)
            {
                var index = (int2)math.round(result.index);

                __OnChanged(result.index);

                this.index = index;
            }

            if ((result.flag & ScrollRectEvent.Flag.SameAsInfo) == ScrollRectEvent.Flag.SameAsInfo)
                __info.isVail = 0;//this.RemoveComponent<ScrollRectInfo>();
        }

        private int2 __EnableNode(in float2 velocity, in float2 normalizedPosition)
        {
            __data = data;

            int version = this.version;
            __count = count;
            bool isChanged = version != this.version;

            ScrollRectNode node;
            node.velocity = velocity;
            node.normalizedPosition = normalizedPosition;// scrollRect.normalizedPosition;
            node.index = __data.GetIndex(offsetScale, normalizedPosition, __count);

            __node = node;
            //this.AddComponentData(node);

            int2 index = (int2)math.round(node.index);
            if (isChanged || math.any(index != this.index))
            {
                __OnChanged(node.index);

                this.index = index;
            }

            return index;
        }

        /*private int2 __EnableNode(in float2 normalizedPosition)
        {
            return __EnableNode(scrollRect.velocity, normalizedPosition);
        }*/

        private int2 __EnableNode()
        {
            return __EnableNode(scrollRect.velocity, scrollRect.normalizedPosition);
        }

        private void __DisableNode()
        {
            __node = null;
            //this.RemoveComponent<ScrollRectNode>();
        }

        /*private void __UpdateData()
        {
            Canvas.willRenderCanvases -= __UpdateData;

            if(this != null)
                UpdateData();
        }*/

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            __DisableNode();
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            __EnableNode();
            
            __info.isVail = 0;
            
            /*ScrollRectInfo info;
            info.isVail = true;
            info.index = __EnableNode(scrollRect.normalizedPosition);

            __info = info;*/
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            ScrollRect scrollRect = this.scrollRect;
            if (scrollRect == null)
                return;

            float2 source = __data.GetIndex(offsetScale, scrollRect.normalizedPosition, count);
            int2 destination = (int2)math.round(source);
            if (math.any(destination != this.index))
            {
                __OnChanged(source);

                this.index = destination;
            }
        }

        private void __OnChanged(in float2 index)
        {
            if (onChanged != null)
                onChanged.Invoke(index);

            if (__submitHandlers != null)
            {
                int submitHandlerIndex = (int)math.round(__ToSubmitIndex(index));
                var submitHandler = submitHandlerIndex >= 0 && submitHandlerIndex < __submitHandlers.Count ? __submitHandlers[submitHandlerIndex] : null;
                if (submitHandler != __submitHandler)
                {
                    if (submitHandler != null)
                        submitHandler.OnSubmit(new BaseEventData(EventSystem.current));

                    /*if (submitHandler is IScrollRectSubmitHandler destination)
                        destination.OnScrollRectDrag(1.0f);

                    if (__submitHandler is IScrollRectSubmitHandler source)
                        source.OnScrollRectDrag(0.0f);*/

                    __submitHandler = submitHandler;
                }
            }
        }

        /*void IEntityComponent.Init(in Entity entity, EntityComponentAssigner assigner)
        {
            ScrollRectEvent result;
            result.version = 0;
            result.flag = 0;
            result.index = math.int2(-1, -1);
            assigner.SetComponentData(entity, result);
        }*/
    }

    [BurstCompile]
    public static class ScrollRectUtility
    {
        private struct Data
        {
            public ScrollRectData instance;
            public ScrollRectInfo info;
            public ScrollRectNode node;
            public ScrollRectEvent result;

            public Data(
                in ScrollRectData instance,
                in ScrollRectInfo info,
                ref ScrollRectNode node,
                ref ScrollRectEvent result)
            {
                this.instance = instance;
                this.info = info;
                this.node = node;
                this.result = result;
            }

            public void Execute(float deltaTime, float offsetScale, in int2 count)
            {
                int2 origin = (int2)math.round(node.index);// math.clamp(info.index, 0, instance.count - 1);
                float2 length = instance.length,
                    cellLength = instance.GetCellLength(count),
                    offset = instance.GetOffset(cellLength, offsetScale), 
                    distance = node.normalizedPosition * length - (info.isVail == 0 ? origin : info.index) * cellLength + offset;
                float t = math.pow(instance.decelerationRate, deltaTime);
                
                node.velocity = math.lerp(node.velocity, distance / instance.elasticity, t);

                node.normalizedPosition -= math.select(float2.zero, node.velocity / length, length > math.FLT_MIN_NORMAL) * deltaTime;

                node.normalizedPosition = math.saturate(node.normalizedPosition);
                
                node.index = instance.GetIndex(count, node.normalizedPosition, length, cellLength, offset);
                
                int2 target = (int2)math.round(node.index);

                ScrollRectEvent.Flag flag = 0;
                if (!math.all(origin == target))
                    flag |= ScrollRectEvent.Flag.Changed;

                if (info.isVail != 0 && math.all(info.index == target))
                {
                    flag |= ScrollRectEvent.Flag.SameAsInfo;

                    node.velocity = float2.zero;
                }

                //nodes[index] = node;
                if (flag != 0)
                    ++result.version;
                
                result.flag = flag;
                result.index = node.index;
            }

            public void Execute(
                int width, 
                float offsetScale, 
                float deltaTime, 
                in CellLengths cellLengths)
            {
                int2 origin = (int2)math.round(node.index);// math.clamp(info.index, 0, instance.count - 1);
                int index = math.min(width * origin.y + origin.x + 1, cellLengths.Length);
                float2 length = instance.length,
                    offset = instance.GetOffset(cellLengths[index - 1], offsetScale), 
                         distance = node.normalizedPosition * length + offset;// - destination * cellLength + offset;

                for(int i = 0; i < index; ++i)
                    distance -= cellLengths[i];

                if (info.isVail == 0)
                    node.normalizedPosition -= math.select(float2.zero, distance / length, length > math.FLT_MIN_NORMAL);
                else
                {
                    float t = math.pow(instance.decelerationRate, deltaTime);
                    //t = t * t* (3.0f - (2.0f * t));
                    node.velocity = math.lerp(node.velocity, distance / instance.elasticity, t);

                    //velocity *= math.pow(instance.decelerationRate, deltaTime);

                    //node.velocity = velocity;

                    //velocity += distance / instance.elasticity;

                    node.normalizedPosition -= math.select(float2.zero, node.velocity / length, length > math.FLT_MIN_NORMAL) * deltaTime;
                }

                node.index = instance.GetIndex(node.normalizedPosition, length, offset, cellLengths);
                
                int2 target = (int2)math.round(node.index);

                ScrollRectEvent.Flag flag = 0;
                if (!math.all(origin == target))
                    flag |= ScrollRectEvent.Flag.Changed;

                if (info.isVail != 0 && math.all(info.index == target))
                {
                    flag |= ScrollRectEvent.Flag.SameAsInfo;

                    node.velocity = float2.zero;
                }

                //nodes[index] = node;

                if (flag != 0)
                {
                    ++result.version;
                    result.flag = flag;
                    result.index = node.index;
                }
            }
        }

        public static bool Execute(
            int version,
            float deltaTime,
            float offsetScale, 
            in int2 count, 
            in ScrollRectData instance,
            in ScrollRectInfo info,
            ref ScrollRectNode node,
            ref ScrollRectEvent result)
        {
            var data = new Data(instance, info, ref node, ref result);

            __Execute(deltaTime, offsetScale, count, ref data);

            node = data.node;

            if (version != data.result.version)
            {
                result = data.result;
                if (result.flag == 0)
                {
                    result.flag |= ScrollRectEvent.Flag.Changed;
                    result.index = node.index;
                }

                return true;
            }

            return false;
        }

        [BurstCompile]
        private static void __Execute(float deltaTime, float offsetScale, in int2 count, ref Data data)
        {
            data.Execute(deltaTime, offsetScale, count);
        }
    }
}