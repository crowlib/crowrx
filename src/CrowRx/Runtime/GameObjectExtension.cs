using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace CrowRx
{
    using Helper;


    public static class GameObjectExtension
    {
        public static bool ExecuteEventToParent<T>(this GameObject target, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor) where T : IEventSystemHandler
        {
            if (ExecuteEvents.CanHandleEvent<T>(target))
                return ExecuteEvents.Execute(target, eventData, functor);

            var parent = target.transform.parent;

            return parent && ExecuteEventToParent(parent.gameObject, eventData, functor);
        }

        public static bool ExecuteEventExceptThis<T>(this GameObject target, BaseEventData eventData, ExecuteEvents.EventFunction<T> functor)
            where T : IEventSystemHandler
        {
            PointerEventData rayEventData = new(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> raycastResult = new();
            EventSystem.current.RaycastAll(rayEventData, raycastResult);

            int resultCount = raycastResult.Count;
            bool result = resultCount > 1;

            for (int i = 0; i < resultCount; ++i)
            {
                GameObject hovered = raycastResult[i].gameObject;

                if (hovered == target)
                    continue;

                result |= hovered.ExecuteEventToParent(eventData, functor);

                break;
            }

            return result;
        }

        public static TComponent GetOrAddComponent<TComponent>(this GameObject obj)
            where TComponent : Component
            => obj.TryGetComponent(out TComponent component) ? component : obj.AddComponent<TComponent>();

        public static Component GetOrAddComponent(this GameObject obj, Type componentType) =>
            obj.TryGetComponent(componentType, out var component) ? component : obj.AddComponent(componentType);

        public static bool GetAllComponentsInHierarchy<TComponent>(this GameObject obj, List<TComponent> components) => obj.transform.GetAllComponentsInHierarchy(components);

        public static GameObject FindTargetObject(this GameObject obj, string objectName) => obj.GetOrAddComponent<FindObjectHelper>().FindTargetObject(objectName);

        public static void RemoveAllChildren(this GameObject parent) => parent.transform.RemoveAllChildren();

        public static GameObject SetParent(this GameObject self, Transform transform)
        {
            if (self && self.transform && ReferenceEquals(self.transform, transform) == false)
                self.transform.SetParent(transform);

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
                self.layer = LayerMask.NameToLayer(layerName);

            return self;
        }

        public static void SetLayerRecursively(this GameObject self, int layer)
        {
            self.layer = layer;

            Transform transform = self.transform;

            for (int i = 0, max = transform.childCount; i < max; ++i)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        public static void SetLayerRecursively(this GameObject self, string layerName) => self.SetLayerRecursively(LayerMask.NameToLayer(layerName));

        /// <summary>
        /// 자신과 자식들 순회 하면서
        /// gameObject 를 이벤트로 콜백 해준다.
        /// 콜백 return 으로 false 를 리턴 할경우 그 오브잭트의 자식들은 순회 하지 않는다.
        /// </summary>
        public static void ChildCirculateCallback(this GameObject self, Func<GameObject, bool> callback)
        {
            if (!(callback?.Invoke(self) ?? false))
                return;

            for (int i = 0, count = self.transform.childCount; i < count; ++i)
                ChildCirculateCallback(self.transform.GetChild(i).gameObject, callback);
        }

        /// <summary>
        /// root 에서부터 자신 까지 경로
        /// </summary>
        public static string GetHierarchyPath(this GameObject self, GameObject root)
        {
            string result = string.Empty;

            Transform rootTransform = root ? root.transform : null;

            if (self.GetComponentsInChildren<Transform>().Any(transform => transform == root.transform))
                return result;

            result = self.name;
            Transform selfTransform = self.transform;

            while (selfTransform.parent != rootTransform)
            {
                selfTransform = selfTransform.parent;
                result = $"{selfTransform.name}/{result}";
            }

            return result;
        }

        public static string GetHierarchyPath(this GameObject self) => self.GetHierarchyPath(null);

        public static Component CreateGameObject(this GameObject self, Type type, string name)
        {
            GameObject gameObject = new(string.IsNullOrEmpty(name) ? type.Name : name);

            if (self)
                gameObject.transform.SetParent(self.transform); // 부모 설정

            // 컴포넌트 추가
            return gameObject.AddComponent(type);
        }

        public static Component CreateGameObject(this GameObject self, Type type) => self.CreateGameObject(type, null);

        public static TComponent CreateGameObject<TComponent>(this GameObject self, string name)
            where TComponent : Component
            => CreateGameObject(self, typeof(TComponent), name) as TComponent;

        public static TComponent CreateGameObject<TComponent>(this GameObject self)
            where TComponent : Component
            => self.CreateGameObject<TComponent>(null);
    }
}