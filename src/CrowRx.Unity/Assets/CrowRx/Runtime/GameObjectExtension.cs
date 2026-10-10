using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;

namespace CrowRx
{
    using Helper;

    public static class GameObjectExtension
    {
        public static bool ExecuteEventToParent<T>(this GameObject target, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            for (Transform current = target ? target.transform : null; current; current = current.parent)
            {
                if (ExecuteEvents.CanHandleEvent<T>(current.gameObject))
                    return ExecuteEvents.Execute(current.gameObject, eventData, functor);
            }
            return false;
        }

        /// <summary>
        /// 포인터 아래에서 자신을 제외한 첫 유효 raycast 대상과 그 부모에 이벤트를 전달합니다.
        /// 해당 대상이 처리하지 못해도 다음 raycast 대상으로 넘어가지 않습니다.
        /// </summary>
        /// <param name="eventData">raycast와 이벤트 전달에 그대로 사용하는 PointerEventData입니다. 다른 종류의 데이터는 false를 반환합니다.</param>
        /// <returns>대상 또는 부모의 이벤트 핸들러를 실행했으면 true입니다. EventSystem이 없으면 false입니다.</returns>
        public static bool ExecuteEventExceptThis<T>(this GameObject target, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            if (eventData is not PointerEventData pointerEventData)
            {
                return false;
            }

            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem)
            {
                return false;
            }

            using PooledObject<List<RaycastResult>> pooled = ListPool<RaycastResult>.Get(out List<RaycastResult> raycastResult);
            eventSystem.RaycastAll(pointerEventData, raycastResult);

            int resultCount = raycastResult.Count;

            for (int i = 0; i < resultCount; ++i)
            {
                GameObject hovered = raycastResult[i].gameObject;

                if (!hovered || hovered == target)
                {
                    continue;
                }

                return hovered.ExecuteEventToParent(eventData, functor);
            }

            return false;
        }

        public static TComponent GetOrAddComponent<TComponent>(this GameObject obj) where TComponent : Component => obj.TryGetComponent(out TComponent component) ? component : obj.AddComponent<TComponent>();
        public static Component GetOrAddComponent(this GameObject obj, Type componentType) => obj.TryGetComponent(componentType, out Component component) ? component : obj.AddComponent(componentType);

        public static bool TryGetSingleComponent<T>(this GameObject self, out T component)
        {
            using PooledObject<List<T>> disposable = ListPool<T>.Get(out List<T> capabilities);
            self.GetComponents(capabilities);

            if (capabilities.Count == 1)
            {
                component = capabilities[0];
                return true;
            }

            if (capabilities.Count > 1)
            {
                UnityLog.Exception(new InvalidOperationException($"<{self.name}> requires at most one [{typeof(T).Name}] component."), self);
            }

            component = default;
            return false;
        }

        [Obsolete("FindObjectHelper를 통한 이름 기반 캐시 검색은 사용을 중단합니다. 직렬화된 참조나 생성 시 보관한 직접 참조를 사용하세요.", false)]
        public static GameObject FindTargetObject(this GameObject obj, string objectName) => obj.GetOrAddComponent<FindObjectHelper>().FindTargetObject(objectName);

        public static void RemoveAllChildren(this GameObject parent) => parent.transform.RemoveAllChildren();

        public static GameObject SetParent(this GameObject self, Transform transform)
        {
            if (self && self.transform && !ReferenceEquals(self.transform, transform))
            {
                self.transform.SetParent(transform);
            }

            return self;
        }

        public static GameObject SetParent(this GameObject self, GameObject parentObject) => parentObject ? self.SetParent(parentObject.transform) : self;
        public static GameObject SetParent(this GameObject self, Component component) => component ? self.SetParent(component.transform) : self;

        public static GameObject SetPosition(this GameObject self, Vector3 position)
        {
            self.transform.position = position;

            return self;
        }

        public static GameObject SetPosition(this GameObject self, Transform transform) => transform ? self.SetPosition(transform.position) : self;

        public static GameObject SetLocalPosition(this GameObject self, Vector3 localPosition)
        {
            self.transform.localPosition = localPosition;

            return self;
        }

        public static GameObject SetLocalEulerAngles(this GameObject self, Vector3 localEulerAngles)
        {
            self.transform.localEulerAngles = localEulerAngles;

            return self;
        }

        public static GameObject SetLocalScale(this GameObject self, Vector3 localScale)
        {
            self.transform.localScale = localScale;

            return self;
        }

        public static GameObject SetLocalTransformDefault(this GameObject self)
        {
            self.transform.SetLocalTransformDefault();

            return self;
        }

        public static GameObject SetLayer(this GameObject self, string layerName)
        {
            if (self)
            {
                self.layer = LayerMask.NameToLayer(layerName);
            }

            return self;
        }

        public static void SetLayerRecursively(this GameObject self, int layer)
        {
            Transform root = self.transform;
            for (Transform current = root; current; current = TransformExtension.NextDescendant(current, root))
                current.gameObject.layer = layer;
        }

        public static void SetLayerRecursively(this GameObject self, string layerName) => self.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        /// <summary>
        /// 자신과 자식들 순회 하면서
        /// gameObject 를 이벤트로 콜백 해준다.
        /// 콜백 return 으로 false 를 리턴 할경우 그 오브잭트의 자식들은 순회 하지 않는다.
        /// 순회 중 콜백에서 계층을 변경하지 않아야 한다.
        /// </summary>
        public static void ChildCirculateCallback(this GameObject self, Func<GameObject, bool> callback)
        {
            if (!(callback?.Invoke(self) ?? false)) return;
            Transform root = self.transform;
            Transform current = TransformExtension.NextDescendant(root, root);
            while (current)
            {
                bool includeChildren = callback(current.gameObject);
                current = TransformExtension.NextDescendant(current, root, includeChildren);
            }
        }

        /// <summary>
        /// root 에서부터 자신 까지 경로
        /// </summary>
        public static string GetHierarchyPath(this GameObject self, GameObject root)
        {
            if (!self)
            {
                return string.Empty;
            }

            Transform currentTransform = self.transform;
            Transform rootTransform = root ? root.transform : null;

            if (currentTransform == rootTransform)
            {
                return string.Empty;
            }

            using PooledObject<List<string>> disposable = ListPool<string>.Get(out List<string> names);
            int length = 0;
            while (currentTransform != rootTransform)
            {
                if (!currentTransform)
                {
                    return string.Empty;
                }

                string name = currentTransform.name;
                names.Add(name);
                length += name.Length;

                currentTransform = currentTransform.parent;
            }

            if (names.Count == 1)
            {
                return names[0];
            }

            return string.Create(length + names.Count - 1, names, static (destination, path) =>
            {
                int offset = 0;
                for (int i = path.Count - 1; i >= 0; i--)
                {
                    if (i < path.Count - 1) destination[offset++] = '/';
                    path[i].AsSpan().CopyTo(destination[offset..]);
                    offset += path[i].Length;
                }
            });
        }

        public static string GetHierarchyPath(this GameObject self) => self.GetHierarchyPath(null);

        public static Component CreateGameObject(this GameObject self, Type type, string name)
        {
            GameObject gameObject = new(string.IsNullOrEmpty(name) ? type.Name : name);

            if (self)
            {
                gameObject.transform.SetParent(self.transform);
            }

            return gameObject.AddComponent(type);
        }

        public static Component CreateGameObject(this GameObject self, Type type) => self.CreateGameObject(type, null);
        public static TComponent CreateGameObject<TComponent>(this GameObject self, string name) where TComponent : Component => self.CreateGameObject(typeof(TComponent), name) as TComponent;
        public static TComponent CreateGameObject<TComponent>(this GameObject self) where TComponent : Component => self.CreateGameObject<TComponent>(null);
    }
}
