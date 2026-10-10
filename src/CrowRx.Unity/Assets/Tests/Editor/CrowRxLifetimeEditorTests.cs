using System.Collections;
using System.Reflection;
using CrowRx;
using CrowRx.Helper;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CrowRxLifetimeEditorTests
{
    [UnityTest]
    public IEnumerator CacheIsClearedWhenNeverActivatedObjectIsDestroyedOutsidePlayMode()
    {
        var root = new GameObject("editor cache lifetime test");
        try
        {
            root.SetActive(false);
            var probe = root.AddComponent<TriggerEventListener>();
            Assert.That(probe.gameObject, Is.SameAs(root));
            Object.DestroyImmediate(root);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var ownerCache = typeof(MonoBehaviourCrowRx).GetField("_gameObject", flags);
            // EditMode의 테스트 진행과 EditorApplication.update는 같은 순서로 실행되지 않을 수 있습니다.
            for (int frame = 0; frame < 20 && ownerCache.GetValue(probe) is not null; frame++) yield return null;
            Assert.That(ownerCache.GetValue(probe), Is.Null);
            Assert.That(typeof(MonoBehaviourCrowRx).GetField("_transform", flags).GetValue(probe), Is.Null);
            Assert.That(typeof(MonoBehaviourCrowRx).GetField("_activationMonitor", flags).GetValue(probe), Is.Null);
        }
        finally { if (root) Object.DestroyImmediate(root); }
    }
}
