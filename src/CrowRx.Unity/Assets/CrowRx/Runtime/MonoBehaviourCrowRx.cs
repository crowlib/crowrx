using System;
using UnityEngine;
using R3;
using R3.Triggers;

// ReSharper disable InconsistentNaming


namespace CrowRx
{
    using Utility;


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
                if (_disposableCacheGameObject is null && this && base.gameObject)
                {
                    _gameObject = base.gameObject;

                    _disposableCacheGameObject =
                        this.OnDestroyAsObservable()
                            .Subscribe(_ =>
                            {
                                _disposableCacheGameObject?.Dispose();
                                _disposableCacheGameObject = null;
                            });
                }

                return _gameObject;
            }
        }

        public new Transform transform
        {
            get
            {
                if (_disposableCacheTransform is null && this && base.transform)
                {
                    _transform = base.transform;

                    _disposableCacheTransform =
                        this.OnDestroyAsObservable()
                            .Subscribe(_ =>
                            {
                                _disposableCacheTransform?.Dispose();
                                _disposableCacheTransform = null;
                            });
                }

                return _transform;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (_disposableCacheRectTransform is null && this && base.gameObject)
                {
                    if (TryGetComponent(out _rectTransform))
                    {
                        _disposableCacheRectTransform = this.OnRectTransformRemovedAsObservable()
                            .Subscribe(_ =>
                            {
                                _disposableCacheRectTransform?.Dispose();
                                _disposableCacheRectTransform = null;
                            });
                    }
                    else
                    {
                        Log.Error($"{name} has no RectTransform Component.");
                    }
                }

                return _rectTransform;
            }
        }
    }
}