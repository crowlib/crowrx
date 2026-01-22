using System.Collections.Generic;
using UnityEngine;


namespace CrowRx.Helper
{
    public class FindObjectHelper : MonoBehaviourCrowRx
    {
        private readonly Dictionary<string, GameObject> _objectCache = new();


        private void OnTransformChildrenChanged() => _objectCache.Clear();

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