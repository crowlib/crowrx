using System.Buffers;
using System.Collections.Generic;
using UnityEngine;


namespace CrowRx
{
    public static class TransformExtension
    {
        public static Transform FindChildDeep(this Transform self, string targetName)
        {
            Transform result = self.Find(targetName);
            if (result)
            {
                return result;
            }

            for (int i = 0, count = self.childCount; i < count; ++i)
            {
                result = self.GetChild(i).FindChildDeep(targetName);
                if (result)
                {
                    return result;
                }
            }

            return null;
        }

        public static T FindUpwardIncludeThis<T>(this Transform self)
        {
            while (true)
            {
                if (self.TryGetComponent(out T result))
                {
                    return result;
                }

                if (!self.parent)
                {
                    return default;
                }

                T findUpwardIncludeThis = self.GetComponentInParent<T>();
                if (findUpwardIncludeThis != null)
                {
                    return findUpwardIncludeThis;
                }

                self = self.parent;
            }
        }

        public static T FindUpward<T>(this Transform self)
        {
            while (true)
            {
                if (!self.parent)
                {
                    return default;
                }

                T componentInParent = self.GetComponentInParent<T>();
                if (componentInParent != null)
                {
                    return componentInParent;
                }

                self = self.parent;
            }
        }

        public static T FindDownward<T>(this Transform self) => self.TryGetComponent(out T target) ? target : self.FindDownwardInternal<T>();

        public static void RemoveAllChildren(this Transform self)
        {
            int childCount = self.childCount;
            if (childCount <= 0)
            {
                return;
            }

            GameObject[] removalObjects = ArrayPool<GameObject>.Shared.Rent(childCount);

            for (int i = 0; i < childCount; ++i)
            {
                removalObjects[i] = self.GetChild(i).gameObject;
            }

            for (int i = 0; i < childCount; ++i)
            {
                Object.Destroy(removalObjects[i]);
            }

            ArrayPool<GameObject>.Shared.Return(removalObjects);
        }

        public static void SetActiveAllChildren(this Transform self, bool value)
        {
            for (int i = 0, count = self.childCount; i < count; ++i)
            {
                self.GetChild(i).gameObject.SetActive(value);
            }
        }

        public static bool GetAllComponentsInHierarchy<T>(this Transform self, List<T> components)
        {
            GatherComponent(self, components);

            return components.Count > 0;
        }

        /// <summary>
        /// 로컬 위치, 로컬 회전값, 로컬 확대 값을 기본값으로 설정한다.
        /// 로컬 위치 =  Vector3.zero
        /// 로컬 회전 =  Vector3.zero
        /// 로컬 확대 =  Vector3.one
        /// </summary>
        public static Transform SetLocalTransformDefault(this Transform self)
        {
            self.localPosition = Vector3.zero;
            self.localEulerAngles = Vector3.zero;
            self.localScale = Vector3.one;

            return self;
        }

        private static void GatherComponent<T>(Transform root, List<T> components)
        {
            T[] rootComponents = root.GetComponents<T>();
            if (rootComponents?.Length > 0)
            {
                components.AddRange(rootComponents);
            }

            for (int index = 0, count = root.childCount; index < count; ++index)
            {
                GatherComponent(root.GetChild(index), components);
            }
        }

        private static T FindDownwardInternal<T>(this Transform self)
        {
            T target = self.GetComponentInChildren<T>();
            if (target != null)
            {
                return target;
            }

            for (int i = 0; i < self.childCount; ++i)
            {
                target = self.GetChild(i).FindDownward<T>();
                if (target != null)
                {
                    return target;
                }
            }

            return default;
        }
    }
}