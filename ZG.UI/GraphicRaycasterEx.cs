using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZG
{
    public class GraphicRaycasterEx : GraphicRaycaster
    {
        /*private struct ForegroundGraphics
        {
            public int sortingOrder;
            public int depth;
        }*/

        private Canvas __canvas;
        private Dictionary<int, int> __counts;

        private static HashSet<GraphicRaycasterEx> __instances;
        /*private static Dictionary<Transform, ForegroundGraphics> __foregroundGraphics;

        public Canvas canvas
        {
            get
            {
                if (__canvas == null)
                    __canvas = GetComponent<Canvas>();

                return __canvas;
            }
        }

        public int displayIndex
        {
            get
            {
                var currentCanvas = canvas;
                var currentEventCamera = eventCamera; // Property can call Camera.main, so cache the reference

                if (currentCanvas.renderMode == RenderMode.ScreenSpaceOverlay || currentEventCamera == null)
                    return currentCanvas.targetDisplay;

                return currentEventCamera.targetDisplay;
            }
        }*/

        public static int MakePointerIdForTouch(int deviceID, int touchID)
        {
            unchecked
            {
                return (deviceID << 24) + touchID;
            }
        }

        public static bool IsHit(int pointerID, in Vector2 screenPosition, GameObject gameObject =null)
        {
            var eventSystem = EventSystem.current;
            var pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.pointerId = pointerID;
            pointerEventData.position = screenPosition;

            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerEventData, raycastResults);
            if (raycastResults.Count > 0)
            {
                foreach (var result in raycastResults)
                {
                    if (result.gameObject == gameObject ||
                        !(result.gameObject.transform is RectTransform))
                        continue;

                    return true;
                }
            }

            return false;
        }

        public static bool IsHit(int deviceID, int touchID)
        {
            return IsHit((deviceID << 24) + touchID);
        }
        
        public static bool IsHit(int pointerId)
        {
            if (__instances == null)
                return false;

            int count;
            foreach (var instance in __instances)
            {
                if (instance != null && instance.__counts != null && instance.__counts.TryGetValue(pointerId, out count) && count > 0)
                    return true;
            }

            return false;
        }
        
        /*public static void AddForeground(Transform transform, int sortingOrder = 0, int depth = 0)
        {
            ForegroundGraphics foregroundGraphics;
            foregroundGraphics.sortingOrder = sortingOrder;
            foregroundGraphics.depth = depth;

            if (__foregroundGraphics == null)
                __foregroundGraphics = new Dictionary<Transform, ForegroundGraphics>();

            __foregroundGraphics[transform] = foregroundGraphics;
        }

        public static bool RemoveForeground(Transform transform)
        {
            if (__foregroundGraphics != null)
                return __foregroundGraphics.Remove(transform);

            return false;
        }*/

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            int count = resultAppendList.Count;

            base.Raycast(eventData, resultAppendList);

            /*if (resultAppendList.Count == count)
            {
                var canvas = this.canvas;
                var currentEventCamera = eventCamera;
                Transform transform;
                Vector3 eventPosition = eventData.position, forward;
                Ray ray = currentEventCamera == null ? default : currentEventCamera.ScreenPointToRay(eventPosition);
                float distance;
                int sortingLayerID = canvas.sortingLayerID, sortingOrder = canvas.sortingOrder;
                bool isScreenSpaceOverlay = currentEventCamera == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay;
                if (__foregroundGraphics != null)
                {
                    foreach (var pair in __foregroundGraphics)
                    {
                        if (pair.Value.sortingOrder >= sortingOrder)
                            continue;

                        transform = pair.Key;
                        if (transform == null)
                            continue;

                        forward = transform.forward;
                        if (isScreenSpaceOverlay)
                            distance = 0.0f;
                        else
                        {
                            // http://geomalgorithms.com/a06-_intersect-2.html
                            distance = (Vector3.Dot(forward, transform.position - ray.origin) / Vector3.Dot(forward, ray.direction));

                            // Check to see if the go is behind the camera.
                            /*if (distance < 0.0f)
                                continue;
                        }

                        resultAppendList.Add(
                            new RaycastResult()
                            {
                                module = this,
                                gameObject = transform.gameObject,
                                screenPosition = eventPosition,
                                distance = distance,
                                displayIndex = displayIndex,
                                index = resultAppendList.Count,
                                depth = pair.Value.depth,
                                sortingLayer = sortingLayerID,
                                sortingOrder = sortingOrder,
                                worldPosition = ray.origin + ray.direction * distance,
                                worldNormal = -forward
                            });
                    }
                }
            }*/

            count = resultAppendList.Count - count;

            if (__counts == null)
                __counts = new Dictionary<int, int>();

            __counts[eventData == null ? 0 : eventData.pointerId] = count;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (__instances == null)
                __instances = new HashSet<GraphicRaycasterEx>();

            __instances.Add(this);
        }

        protected override void OnDisable()
        {
            if (__instances != null)
                __instances.Remove(this);

            base.OnDisable();
        }
    }
}