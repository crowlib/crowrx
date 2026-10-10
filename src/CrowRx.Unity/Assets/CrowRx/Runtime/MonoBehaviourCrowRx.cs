using System.Threading;
using UnityEngine;
using R3;

namespace CrowRx
{
    public abstract class MonoBehaviourCrowRx : MonoBehaviour
    {
        private GameObject _gameObject;
        private Transform _transform;
        private bool _cacheInitialized;
        private bool _cacheDestroyed;
        private CancellationTokenRegistration _cacheDestroyRegistration;
        private ActivationMonitor _activationMonitor;

        /// <summary>처음 조회할 때 보관한 GameObject입니다. 파괴 시 비우며 Awake 전 파괴는 다음 Update에서 정리합니다.</summary>
        public new GameObject gameObject
        {
            get
            {
                InitializeCache();

                return _gameObject;
            }
        }

        /// <summary>캐시한 Transform입니다. 종류를 변경한 경우 InvalidateTransformCache로 갱신을 요청합니다.</summary>
        public new Transform transform
        {
            get
            {
                InitializeCache();

                if (_transform is null && !_cacheDestroyed && this)
                {
                    _transform = base.transform;
                }

                return _transform;
            }
        }

        /// <summary>캐시한 Transform을 RectTransform으로 반환합니다. 일반 Transform이면 null입니다.</summary>
        public RectTransform rectTransform => transform as RectTransform;

        /// <summary>Transform 종류를 변경한 뒤 호출하면 다음 transform/rectTransform 조회에서 다시 가져옵니다.</summary>
        public void InvalidateTransformCache() => _transform = null;

        private void InitializeCache()
        {
            if (_cacheInitialized || !this)
            {
                return;
            }

            _cacheInitialized = true;

            _gameObject = base.gameObject;
            _transform = base.transform;

            _cacheDestroyRegistration = destroyCancellationToken.Register(static state => ((MonoBehaviourCrowRx)state).ClearCache(), this);

            // Awake 이전에 파괴되면 destroyCancellationToken이 취소되지 않으므로 활성화 전까지만 감시합니다.
            if (!didAwake)
            {
                _activationMonitor = new ActivationMonitor(this);
                _activationMonitor.Start();
            }
        }

        private void ClearCache()
        {
            _cacheDestroyed = true;

            _gameObject = null;
            _transform = null;

            _cacheDestroyRegistration.Dispose();
            _cacheDestroyRegistration = default;

            _activationMonitor?.Stop();
            _activationMonitor = null;
        }

        private sealed class ActivationMonitor : IFrameRunnerWorkItem
        {
            private MonoBehaviourCrowRx _owner;

            public ActivationMonitor(MonoBehaviourCrowRx owner) => _owner = owner;

            public void Start()
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorApplication.update += EditorUpdate;
                    return;
                }
#endif
                UnityFrameProvider.Update.Register(this);
            }

            public void Stop()
            {
                _owner = null;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
            }

#if UNITY_EDITOR
            private void EditorUpdate()
            {
                if (!MoveNext(0)) Stop();
            }
#endif

            public bool MoveNext(long frameCount)
            {
                MonoBehaviourCrowRx owner = _owner;
                if (owner is null)
                {
                    return false;
                }

                if (!owner)
                {
                    owner.ClearCache();
                    return false;
                }

                if (owner.didAwake)
                {
                    owner._activationMonitor = null;
                    Stop();

                    return false;
                }

                return true;
            }
        }
    }
}
