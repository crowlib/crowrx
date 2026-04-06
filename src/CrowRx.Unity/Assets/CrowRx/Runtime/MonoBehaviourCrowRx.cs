using System;
using UnityEngine;
using R3;
using R3.Triggers;

namespace CrowRx
{
    public abstract class MonoBehaviourCrowRx : MonoBehaviour
    {
        private GameObject _gameObject;
        private IDisposable _disposableCacheGameObject;

        private Transform _transform;
        private IDisposable _disposableCacheTransform;

        private RectTransform _rectTransform;
        private IDisposable _disposableCacheRectTransform;

        public new GameObject gameObject
        {
            get
            {
                if (_disposableCacheGameObject is null && this)
                {
                    if (base.gameObject)
                    {
                        _gameObject = base.gameObject;

                        _disposableCacheGameObject =
                            this.OnDestroyAsObservable()
                                .Subscribe(this, static (_, monoBehaviourCrowRx) =>
                                {
                                    monoBehaviourCrowRx._disposableCacheGameObject?.Dispose();
                                    monoBehaviourCrowRx._disposableCacheGameObject = null;
                                });
                    }
                    else
                    {
                        _gameObject = null;
                    }
                }

                return _gameObject;
            }
        }

        public new Transform transform
        {
            get
            {
                if (_disposableCacheTransform is null && this)
                {
                    if (base.transform)
                    {
                        _transform = base.transform;

                        _disposableCacheTransform =
                            this.OnDestroyAsObservable()
                                .Subscribe(this, static (_, monoBehaviourCrowRx) =>
                                {
                                    monoBehaviourCrowRx._disposableCacheTransform?.Dispose();
                                    monoBehaviourCrowRx._disposableCacheTransform = null;
                                });
                    }
                    else
                    {
                        _transform = null;
                    }
                }

                return _transform;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (_disposableCacheRectTransform is null && this)
                {
                    if (base.gameObject && TryGetComponent(out _rectTransform))
                    {
                        _disposableCacheRectTransform = this.OnRectTransformRemovedAsObservable()
                            .Subscribe(this, static (_, monoBehaviourCrowRx) =>
                            {
                                monoBehaviourCrowRx._disposableCacheRectTransform?.Dispose();
                                monoBehaviourCrowRx._disposableCacheRectTransform = null;
                            });
                    }
                    else
                    {
                        _rectTransform = null;
                    }
                }

                return _rectTransform;
            }
        }
    }
}