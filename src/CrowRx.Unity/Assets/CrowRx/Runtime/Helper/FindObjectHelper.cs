using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrowRx.Helper
{
    [Obsolete("중복 이름과 계층 변경으로 대상이 모호해질 수 있습니다. 직렬화된 참조나 생성 시 보관한 직접 참조를 사용하세요.", false)]
    public class FindObjectHelper : MonoBehaviourCrowRx
    {
        private readonly Dictionary<string, GameObject> _objectCache = new();

        private void OnTransformChildrenChanged() => _objectCache.Clear();

        [Obsolete("이름 기반 캐시 검색 대신 직렬화된 참조나 생성 시 보관한 직접 참조를 사용하세요.", false)]
        public GameObject FindTargetObject(string objectName)
        {
            if (_objectCache.TryGetValue(objectName, out GameObject target))
            {
                return target;
            }

            Transform targetTrans = transform.FindChildDeep(objectName);
            if (!targetTrans)
            {
                return null;
            }

            target = targetTrans.gameObject;

            _objectCache.Add(objectName, target);

            return target;
        }
    }
}
