using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    public interface IComponentWrapper
    {
        UnityEngine.Object As(string key);
    }

    public class ComponentManager<T> : MonoBehaviour where T : Component
    {
        [Serializable]
        public class Instances : Map<UnityEngine.Object>
        {

        }

        [SerializeField]
        internal T[] _values;

        [SerializeField, Map]
        internal Instances _instances;

        public static Action<string, T> onChanged;

        public static void Change(string key)
        {
            if (onChanged != null)
                onChanged(key, Find(key));
        }

        public static T Find(string key)
        {
            return ComponentManager._Values.TryGetValue(key, out var value) ? __As(key, value) : default;
        }

        protected void OnEnable()
        {
            if ((_values == null || _values.Length < 1) && (_instances == null || _instances.Count < 1))
                _values = GetComponents<T>();

            if (_values != null && _values.Length > 0)
            {
                string key;
                foreach (var value in _values)
                {
                    key = value.name;

                    ComponentManager._Values[key] = value;

                    if (onChanged != null)
                        onChanged(key, value);
                }
            }

            if(_instances != null && _instances.Count > 0)
            {
                string key;
                UnityEngine.Object value;
                foreach (var pair in _instances)
                {
                    key = pair.Key;
                    value = pair.Value;

                    ComponentManager._Values[key] = value;

                    if (onChanged != null)
                        onChanged(key, __As(key, value));
                }
            }
        }

        protected void OnDisable()
        {
            UnityEngine.Object target;
            if (_values != null)
            {
                string name;
                foreach (var value in _values)
                {
                    name = value.name;
                    if (ComponentManager._Values.TryGetValue(name, out target) && 
                        target == value && 
                        ComponentManager._Values.Remove(name))
                    {
                        if(onChanged != null)
                            onChanged(value.name, default);
                    }
                }
            }

            if (_instances != null)
            {
                string key;
                foreach (var pair in _instances)
                {
                    key = pair.Key;
                    if (ComponentManager._Values.TryGetValue(key, out target) &&
                        target == pair.Value && 
                        ComponentManager._Values.Remove(key))
                    {
                        if (onChanged != null)
                            onChanged(key, default);
                    }
                }
            }
        }

        private static T __As(string key, UnityEngine.Object target)
        {
            var wrapper = target as IComponentWrapper;
            return (wrapper == null ? target : wrapper.As(key)) as T;
        }
    }

    public class ComponentManager : ComponentManager<Component>
    {
        internal static readonly Dictionary<string, UnityEngine.Object> _Values = new Dictionary<string, UnityEngine.Object>();
    }
}