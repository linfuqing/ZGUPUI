using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ZG
{
    public class ToggleEx : Toggle, ISerializationCallbackReceiver
    {
        private enum Status
        {
            None, 
            Deserializing, 
            Awake
        }
        /*private struct Group
        {
            public HashSet<ToggleEx> onToggles;
            public HashSet<ToggleEx> offToggles;
        }

        private static Dictionary<ToggleGroup, Group> __groups;

        private bool __isOn;
        private ToggleGroup __group;*/

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/ZG/Replace All Toggle To Ex", false, 10)]
        public static void ReplaceAll()
        {
            List<Toggle> toggles = null;
            List<UnityEngine.GameObject> gameObjects = null;
            UnityEngine.GameObject[] rootGameObjects;
            Toggle[] children;
            UnityEngine.SceneManagement.Scene scene;
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; ++i)
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                rootGameObjects = scene.GetRootGameObjects();
                if (rootGameObjects != null && rootGameObjects.Length > 0)
                {
                    foreach (var rootGameObject in rootGameObjects)
                    {
                        children = rootGameObject == null ? null : rootGameObject.GetComponentsInChildren<Toggle>(true);
                        if (children != null && children.Length > 0)
                        {
                            if (toggles == null)
                                toggles = new List<Toggle>();

                            toggles.AddRange(children);
                        }
                    }

                    if (gameObjects == null)
                        gameObjects = new List<UnityEngine.GameObject>();

                    gameObjects.AddRange(rootGameObjects);
                }
            }

            int numToggles = toggles == null ? 0 : toggles.Count;
            if (numToggles < 0)
                return;

            bool isDirty = false;
            Toggle source;
            ToggleEx destination;
            UnityEngine.GameObject gameObject;
            HashSet<object> targets;
            for(int i = 0; i < numToggles; ++i)
            {
                source = toggles[i];

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Update Toggles", source == null ? null : source.name, i * 1.0f / numToggles))
                    break;

                gameObject = source == null ? null : source.gameObject;
                if (gameObject == null || gameObject.GetComponent<ToggleEx>() != null)
                    continue;

                DestroyImmediate(source);

                destination = gameObject.AddComponent<ToggleEx>();
                if (destination == null)
                    continue;

                source.CopyTo(destination);

                if (gameObjects != null)
                {
                    targets = null;

                    foreach (UnityEngine.GameObject rootGameObject in gameObjects)
                        ChangeDependencies(rootGameObject, source, destination, ref targets);
                }

                isDirty = true;
            }

            if (isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            UnityEditor.EditorUtility.ClearProgressBar();
        }

        private static void ChangeDependencies(UnityEngine.GameObject gameObject, Toggle source, Toggle destination, ref HashSet<object> targets)
        {
            gameObject.ChangeDependencies(source, destination, ref targets);

            var transform = gameObject == null ? null : gameObject.transform;
            if (transform != null)
            {
                foreach (UnityEngine.Transform child in transform)
                    ChangeDependencies(child == null ? null : child.gameObject, source, destination, ref targets);
            }
        }
#endif

        [SerializeField]
        internal bool _isInstance;

        private bool __isInstance;

        private bool __oldStatus;

        private Status __status;

        private ToggleEvent __onValueChanged;

        private volatile ToggleEx __next;

        private static volatile ToggleEx __head;

        private static FieldInfo __isOn = typeof(Toggle).GetField("m_IsOn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo __group = typeof(Toggle).GetField("m_Group", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool __isInit;

        private static bool __AwakeNextToggle(ToggleGroup group)
        {
            ToggleEx toggle;
            do
            {
                toggle = __head;
            } while (((object)toggle) != null && Interlocked.CompareExchange(ref __head, toggle.__next, toggle) != toggle);
            
            if (toggle == null)
                return (object)toggle == null ? false : __AwakeNextToggle(group);

            toggle.__next = null;

            if (toggle.__status != Status.Deserializing)
                return __AwakeNextToggle(group);

            var scene = toggle.gameObject.scene;
            UnityEngine.Assertions.Assert.IsTrue(scene.isSubScene || scene.IsValid());

            /*if(toggle.name.Contains("World"))
            {
                Debug.LogError(toggle.__oldStatus);
            }*/

            //toggle.isOn = toggle.__oldStatus;
            toggle.__Awake();

            if (toggle.group == group)
                toggle.isOn = toggle.__oldStatus;
            else
                __isOn.SetValue(toggle, toggle.__oldStatus);

            return true;
        }

        private static void __OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            while (__AwakeNextToggle(null)) ;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void __Init()
        {
            SceneManager.sceneLoaded += __OnSceneLoaded;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= __OnPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += __OnPlayModeStateChanged;

            __isInit = UnityEditor.EditorApplication.isPlaying;
#else
            __isInit = true;
#endif
        }

#if UNITY_EDITOR
        private static void __OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playModeStateChange)
        {
            switch(playModeStateChange)
            {
                case UnityEditor.PlayModeStateChange.EnteredEditMode:
                case UnityEditor.PlayModeStateChange.ExitingPlayMode:
                    __isInit = false;
                    break;
                case UnityEditor.PlayModeStateChange.EnteredPlayMode:
                case UnityEditor.PlayModeStateChange.ExitingEditMode:
                    __isInit = true;
                    break;
            }
        }
#endif

        public override void OnSubmit(BaseEventData eventData)
        {
            base.OnSubmit(eventData);
        }

        public override bool IsActive()
        {
            return __status == Status.Awake;// && base.IsActive();
        }

        protected override void Awake()
        {
            base.Awake();

            __Awake();
        }

        protected override void OnEnable()
        {
            /*ToggleGroup group = base.group;
            bool allowSwitchOff = group == null || group.allowSwitchOff;
            if (!allowSwitchOff)
                group.allowSwitchOff = true;

            base.OnEnable();

            if (!allowSwitchOff)
                group.allowSwitchOff = false;*/

            var group = base.group;
            if (group != null)
                __group.SetValue(this, null);

            base.OnEnable();

            if (group != null)
                __group.SetValue(this, group);
        }

        protected override void OnDisable()
        {
            /*ToggleGroup group = base.group;
            bool allowSwitchOff = group == null || group.allowSwitchOff;
            if (!allowSwitchOff)
                group.allowSwitchOff = true;

            base.OnDisable();

            if (!allowSwitchOff)
                group.allowSwitchOff = false;*/


            var group = base.group;
            if (group != null)
                __group.SetValue(this, null);

            base.OnDisable();

            if (group != null)
                __group.SetValue(this, group);
        }

        protected override void OnDestroy()
        {
            var group = base.group;
            if (group != null)
            {
                group.UnregisterToggle(this);

                __group.SetValue(this, null);
            }

            base.OnDestroy();
        }

        private void __Awake()
        {
            if (__status == Status.Awake)
                return;

            ToggleGroup group = base.group;
            if (group != null)
            {
                bool allowSwitchOff = group == null || group.allowSwitchOff;
                if (!allowSwitchOff)
                    group.allowSwitchOff = true;

                group.RegisterToggle(this);

                if (!allowSwitchOff)
                    group.allowSwitchOff = false;
            }

            __status = Status.Awake;
        }

        private void __OnChanged(bool value)
        {
            //UnityEngine.Debug.Log($"Toggle {name} Change {value}", this);

            UnityEngine.Assertions.Assert.AreNotEqual(__onValueChanged, onValueChanged);

            ToggleGroup group = base.group;

            if (value)
            {
                if (group != null && (!group.isActiveAndEnabled || !IsActive()))
                {
                    __Awake();

                    var onValueChanged = base.onValueChanged;
                    base.onValueChanged = null;
                    group.NotifyToggleOn(this);
                    base.onValueChanged = onValueChanged;
                }
            }

            if (__onValueChanged != null)
                __onValueChanged.Invoke(value);

            //while (__AwakeNextToggle(group)) ;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if(!__isInit)
            {
                _isInstance = false;

                return;
            }

            if (__status != Status.None || 
                onValueChanged != null && onValueChanged == __onValueChanged)
                return;
            
            __oldStatus = isOn;

            if (__oldStatus)
                __isOn.SetValue(this, false);

            __onValueChanged = onValueChanged;
            
            onValueChanged = new ToggleEvent();

            onValueChanged.AddListener(__OnChanged);

            if (_isInstance)
            {
                __isInstance = true;

                __Awake();
            }
            else
            {
                __status = Status.Deserializing;

                UnityEngine.Assertions.Assert.IsNull(__next);
                do
                {
                    __next = __head;
                } while (Interlocked.CompareExchange(ref __head, this, __next) != __next);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _isInstance = __isInit;

            if (__isInit)
            {
                if (__onValueChanged != null)
                    onValueChanged = __onValueChanged;
            }
        }

        /*protected override void OnEnable()
        {
            if (!__isOn && __group != null)
            {
                Group group;
                if (__groups != null && __groups.TryGetValue(__group, out group) && group.onToggles != null)
                {
                    Transform transform = base.transform, parent = transform == null ? null : transform.parent;
                    foreach (ToggleEx toggle in group.onToggles)
                    {
                        transform = toggle == null ? null : toggle.transform;
                        if(transform != null && transform.parent == parent)
                            __group.RegisterToggle(toggle);
                    }
                }
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (__isOn && __group != null)
            {
                Group group;
                if (__groups != null && __groups.TryGetValue(__group, out group) && group.offToggles != null)
                {
                    Transform transform = base.transform, parent = transform == null ? null : transform.parent;
                    foreach (ToggleEx toggle in group.offToggles)
                    {
                        transform = toggle == null ? null : toggle.transform;
                        if(transform != null && transform.parent == parent)
                            __group.UnregisterToggle(toggle);
                    }
                }
            }

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            __Remove();

            base.OnDestroy();
        }

        private void __Add()
        {
            if (__group == null)
                return;

            if (__groups == null)
                __groups = new Dictionary<ToggleGroup, Group>();

            Group group;
            __groups.TryGetValue(__group, out group);

            if (__isOn)
            {
                if (group.onToggles == null)
                    group.onToggles = new HashSet<ToggleEx>();

                group.onToggles.Add(this);

                __groups[__group] = group;
            }
            else
            {
                if (group.offToggles == null)
                    group.offToggles = new HashSet<ToggleEx>();

                group.offToggles.Add(this);

                __groups[__group] = group;
            }
        }

        private void __Remove()
        {
            if (__group == null)
                return;

            if (__groups == null)
                __groups = new Dictionary<ToggleGroup, Group>();

            Group group;
            __groups.TryGetValue(__group, out group);

            if (__isOn)
            {
                if (group.onToggles != null)
                    group.onToggles.Remove(this);

                __groups[__group] = group;
            }
            else
            {
                if (group.offToggles != null)
                    group.offToggles.Remove(this);

                __groups[__group] = group;
            }
        }

        private void __Update(bool value)
        {
            __Remove();

            __isOn = value;

            __group = group;

            __Add();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            __isOn = isOn;

            ToggleGroup group = base.group;
            if (group != null)
            {
                __group = group;

                __Add();
            }

            if (onValueChanged == null)
                onValueChanged = new ToggleEvent();

            onValueChanged.AddListener(__Update);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {

        }*/
    }
}