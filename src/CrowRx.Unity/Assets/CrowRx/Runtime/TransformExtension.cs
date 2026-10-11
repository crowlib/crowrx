using System.Buffers;
using UnityEngine;

namespace CrowRx
{
    public static class TransformExtension
    {
        /// <summary>노드별 Find를 깊이 우선으로 시도합니다. 중복 이름은 첫 결과를 선택하므로 고유한 참조가 필요한 경우 사용하지 않습니다.</summary>
        public static Transform FindChildDeep(this Transform self, string targetName)
        {
            for (Transform current = self; current; current = NextDescendant(current, self))
            {
                Transform result = current.Find(targetName);
                if (result)
                {
                    return result;
                }
            }

            return null;
        }

        public static T FindUpwardIncludeThis<T>(this Transform self) => self.TryGetComponent(out T result) ? result : FindUpwardInternal<T>(self);

        public static T FindUpward<T>(this Transform self)
        {
            T result = self.GetComponentInParent<T>();
            if (result is not null &&
                (!self.TryGetComponent(out T own) || !ReferenceEquals(own, result)))
            {
                return result;
            }

            // 자기 자신을 제외한 기존 활성 노드 선택 규칙을 보존합니다.
            // 부모에서 전체 상위 검색을 한 번만 하면 조상별 재검색은 필요 없습니다.
            return self.parent ? self.parent.GetComponentInParent<T>() : default;
        }

        private static T FindUpwardInternal<T>(Transform start)
        {
            if (!start)
            {
                return default;
            }

            T result = start.GetComponentInParent<T>();
            if (result != null)
            {
                return result;
            }

            // 엔진의 전체 상위 검색은 한 번만 실행하고, 실패 시 각 조상을 한 번씩 검사합니다.
            for (Transform current = start.parent; current; current = current.parent)
            {
                if (current.TryGetComponent(out result))
                {
                    return result;
                }
            }

            return default;
        }

        public static T FindDownward<T>(this Transform self)
        {
            if (self.TryGetComponent(out T target))
            {
                return target;
            }

            target = self.GetComponentInChildren<T>();
            return target != null ? target : self.FindDownwardInternal<T>();
        }

        public static void RemoveAllChildren(this Transform self)
        {
            int childCount = self.childCount;
            if (childCount <= 0)
            {
                return;
            }

            GameObject[] removalObjects = ArrayPool<GameObject>.Shared.Rent(childCount);

            try
            {
                for (int i = 0; i < childCount; ++i)
                {
                    removalObjects[i] = self.GetChild(i).gameObject;
                }

                for (int i = 0; i < childCount; ++i)
                {
                    Object.Destroy(removalObjects[i]);
                }
            }
            finally
            {
                ArrayPool<GameObject>.Shared.Return(removalObjects, clearArray: true);
            }
        }

        public static void SetActiveAllChildren(this Transform self, bool value)
        {
            for (int i = 0, count = self.childCount; i < count; ++i)
            {
                self.GetChild(i).gameObject.SetActive(value);
            }
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

        private static T FindDownwardInternal<T>(this Transform self)
        {
            // 활성 자손 검색이 실패한 경우에만 비활성 자손을 포함해 전위 순회합니다.
            // Transform의 부모/형제 링크를 사용하므로 재귀 스택이나 임시 컬렉션이 필요 없습니다.
            for (Transform current = NextDescendant(self, self); current; current = NextDescendant(current, self))
            {
                if (current.TryGetComponent(out T target))
                {
                    return target;
                }
            }

            return default;
        }

        internal static Transform NextDescendant(Transform current, Transform root, bool includeChildren = true)
        {
            if (includeChildren && current.childCount > 0)
            {
                return current.GetChild(0);
            }

            while (current != root)
            {
                Transform parent = current.parent;
                int nextSibling = current.GetSiblingIndex() + 1;
                if (nextSibling < parent.childCount)
                {
                    return parent.GetChild(nextSibling);
                }

                current = parent;
            }

            return null;
        }
    }
}