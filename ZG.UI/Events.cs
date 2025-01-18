using UnityEngine;
using UnityEngine.Events;

namespace ZG
{
    [System.Serializable]
    public class ActiveEvent : UnityEvent<bool>
    {

    }

    [System.Serializable]
    public class StringEvent : UnityEvent<string>
    {

    }

    [System.Serializable]
    public class FloatEvent : UnityEvent<float>
    {

    }

    [System.Serializable]
    public class PointerEvent : UnityEvent<Vector2>
    {

    }

    [System.Serializable]
    public class IntEvent : UnityEvent<int>
    {

    }

    [System.Serializable]
    public class SpriteEvent : UnityEvent<Sprite>
    {

    }
}