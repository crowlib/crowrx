using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;


namespace CrowRx.Helper
{
    using Tasks;
    using Utility;


    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFade : MonoBehaviourCrowRx
    {
        [SerializeField] private UnityEvent onTaskCompleted;


        private CanvasGroup _canvasGroup;
        private CancellationTokenSource _ctsFade;


        public CanvasGroup CanvasGroup => !_canvasGroup && !TryGetComponent(out _canvasGroup) ? null : _canvasGroup;


        private void Awake() => CanvasGroup.alpha = 0f;

        private void OnDisable()
        {
            _ctsFade.Release();
            _ctsFade = null;
        }

        public void Fade(float startAlpha, float endAlpha, float fadeDuration, bool reset, bool ignoreTimeScale)
        {
            _ctsFade?.Cancel();
            _ctsFade = new CancellationTokenSource();

            CancellationTokenSource currentCts = _ctsFade;

            FadeAsync(startAlpha, endAlpha, fadeDuration, reset, ignoreTimeScale, currentCts.Token)
                .ContinueWithAnyway(() =>
                {
                    currentCts.Release();
                    currentCts = null;
                })
                .Forget();
        }

        public void FadeInOut(float startAlpha, float endAlpha, float fadeOutDuration, float fadeInDuration, float holdDuration, bool ignoreTimeScale)
        {
            _ctsFade?.Cancel();
            _ctsFade = new CancellationTokenSource();

            CancellationTokenSource currentCts = _ctsFade;

            FadeOutInAsync(startAlpha, endAlpha, fadeOutDuration, fadeInDuration, holdDuration, ignoreTimeScale, currentCts.Token)
                .ContinueWithAnyway(() =>
                {
                    currentCts.Release();
                    currentCts = null;
                })
                .Forget();
        }

        public async UniTask FadeOutInAsync(float startAlpha, float endAlpha, float fadeOutDuration, float fadeInDuration, float holdDuration, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            try
            {
                await FadeAsync(startAlpha, endAlpha, fadeOutDuration, true, ignoreTimeScale, cancellationToken);

                await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime, cancellationToken: cancellationToken);

                await FadeAsync(endAlpha, startAlpha, fadeInDuration, false, ignoreTimeScale, cancellationToken);

                onTaskCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Log.Exception(e);
                throw;
            }
        }

        public async UniTask FadeAsync(float startAlpha, float endAlpha, float fadeDuration, bool reset, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            if (!CanvasGroup)
            {
                throw new NullReferenceException("Canvas group could not be found");
            }

            await Mathm.LerpAsync(
                alpha => _canvasGroup.alpha = alpha,
                () => _canvasGroup.alpha,
                startAlpha,
                endAlpha,
                fadeDuration,
                reset,
                ignoreTimeScale,
                cancellationToken
            );
        }
    }
}