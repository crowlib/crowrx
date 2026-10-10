using System.Buffers;
using System.Collections;
using CrowRx;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class TransformExtensionTests
{
    private static Transform[] MakeTree()
    {
        var nodes = new Transform[5];
        nodes[0] = new GameObject("CrowRx transform regression").transform;
        for (int i = 1; i < nodes.Length; i++)
        {
            nodes[i] = new GameObject("Node " + i).transform;
            nodes[i].SetParent(nodes[i == 2 ? 1 : i == 4 ? 3 : 0]);
        }
        return nodes;
    }

    [Test]
    public void SearchPreservesSelectionAcrossActivityAndComponentPlacements()
    {
        // 5노드의 활성 여부 32가지 × 컴포넌트 배치 32가지.
        // 단순 성공뿐 아니라 비활성/활성 우선순위와 자기 자신 제외를 이전 구현과 대조합니다.
        for (int componentMask = 0; componentMask < 32; componentMask++)
        {
            Transform[] nodes = MakeTree();
            try
            {
                for (int i = 0; i < nodes.Length; i++)
                    if ((componentMask & (1 << i)) != 0) nodes[i].gameObject.AddComponent<BoxCollider>();
                for (int activeMask = 0; activeMask < 32; activeMask++)
                {
                    for (int i = 0; i < nodes.Length; i++) nodes[i].gameObject.SetActive((activeMask & (1 << i)) != 0);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        Transform node = nodes[i];
                        string scenario = $"components={componentMask}, active={activeMask}, node={i}";
                        Assert.That(node.FindUpwardIncludeThis<Collider>(), Is.SameAs(LegacyUpwardIncludeThis(node)), "include-self upward: " + scenario);
                        Assert.That(node.FindUpward<Collider>(), Is.SameAs(LegacyUpward(node)), "exclude-self upward: " + scenario);
                        Assert.That(node.FindDownward<Collider>(), Is.SameAs(LegacyDownward(node)), "downward: " + scenario);
                    }
                }
            }
            finally { Object.DestroyImmediate(nodes[0].gameObject); }
        }

        var root = new GameObject("CrowRx absent component chain");
        try
        {
            Transform leaf = root.transform;
            for (int i = 0; i < 256; i++)
            {
                var child = new GameObject("Child");
                child.transform.SetParent(leaf);
                leaf = child.transform;
            }
            Assert.That(root.transform.FindDownward<Collider>(), Is.Null);
            Assert.That(leaf.FindUpwardIncludeThis<Collider>(), Is.Null);
            Assert.That(leaf.FindUpward<Collider>(), Is.Null);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [UnityTest]
    public IEnumerator RemovalDestroysChildrenAndClearsReturnedArray()
    {
        var root = new GameObject("CrowRx removal regression");
        try
        {
            // 해당 bucket을 먼저 빌리고 반납해 same-thread 캐시를 준비합니다.
            GameObject[] prepared = ArrayPool<GameObject>.Shared.Rent(3);
            int capacity = prepared.Length;
            for (int i = 0; i < capacity; i++) prepared[i] = root;
            ArrayPool<GameObject>.Shared.Return(prepared);

            for (int i = 0; i < 3; i++) new GameObject("Removal child").transform.SetParent(root.transform);
            root.transform.RemoveAllChildren();
            GameObject[] returned = ArrayPool<GameObject>.Shared.Rent(3);
            try
            {
                Assert.That(returned, Is.SameAs(prepared), "same-thread pool buffer was returned");
                for (int i = 0; i < returned.Length; i++)
                    Assert.That(ReferenceEquals(returned[i], null), Is.True, "managed reference cleared, including unused capacity");
            }
            finally { ArrayPool<GameObject>.Shared.Return(returned, clearArray: true); }

            yield return null; // Object.Destroy는 프레임 끝에서 적용됩니다.
            Assert.That(root.transform.childCount, Is.Zero);
            root.transform.RemoveAllChildren();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Collider LegacyUpwardIncludeThis(Transform self)
    {
        while (true)
        {
            if (self.TryGetComponent(out Collider result)) return result;
            if (!self.parent) return null;
            Collider found = self.GetComponentInParent<Collider>();
            if (found != null) return found;
            self = self.parent;
        }
    }

    private static Collider LegacyUpward(Transform self)
    {
        Collider result = self.GetComponentInParent<Collider>();
        if (result is not null && (!self.TryGetComponent(out Collider own) || !ReferenceEquals(own, result))) return result;
        for (Transform current = self.parent; current; current = current.parent)
        {
            result = current.GetComponentInParent<Collider>();
            if (result is not null) return result;
        }
        return null;
    }

    private static Collider LegacyDownward(Transform self)
    {
        if (self.TryGetComponent(out Collider own)) return own;
        Collider result = self.GetComponentInChildren<Collider>();
        if (result != null) return result;
        for (int i = 0; i < self.childCount; i++)
        {
            result = LegacyDownward(self.GetChild(i));
            if (result != null) return result;
        }
        return null;
    }
}
