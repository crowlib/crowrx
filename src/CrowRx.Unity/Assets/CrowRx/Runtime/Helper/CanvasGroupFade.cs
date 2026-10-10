using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace CrowRx.Helper
{
    using Utility;

    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFade : MonoBehaviourCrowRx
    {
        [SerializeField] private UnityEvent onTaskCompleted;

        private CanvasGroup _canvasGroup;
        private CancellationTokenSource _ctsFade;
        private Action<float> _setAlpha;
        private Func<float> _getAlpha;

        public CanvasGroup CanvasGroup => !_canvasGroup && !TryGetComponent(out _canvasGroup) ? null : _canvasGroup;

        private void Awake() => CanvasGroup.alpha = 0f;
        private void OnDisable() => CancelFade();
        private void OnDestroy() => CancelFade();

        public async UniTask FadeAsync(float startAlpha, float endAlpha, float fadeDuration, bool reset, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            if (!CanvasGroup)
            {
                throw new NullReferenceException("Canvas group could not be found");
            }

            await Mathm.LerpAsync(
                _setAlpha ??= SetAlpha,
                _getAlpha ??= GetAlpha,
                startAlpha,
                endAlpha,
                fadeDuration,
                reset,
                ignoreTimeScale,
                cancellationToken
            );
        }

        /// <summary>왕복 페이드를 기다립니다. 취소와 실패는 호출자에게 전파하며 완료 이벤트는 성공한 경우에만 호출합니다.</summary>
        public async UniTask FadeOutInAsync(float startAlpha, float endAlpha, float fadeOutDuration, float fadeInDuration, float holdDuration, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            await FadeAsync(startAlpha, endAlpha, fadeOutDuration, true, ignoreTimeScale, cancellationToken);

            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), ignoreTimeScale ? DelayType.UnscaledDeltaTime : DelayType.DeltaTime, cancellationToken: cancellationToken);

            await FadeAsync(endAlpha, startAlpha, fadeInDuration, false, ignoreTimeScale, cancellationToken);

            onTaskCompleted?.Invoke();
        }

        public void Fade(float startAlpha, float endAlpha, float fadeDuration, bool reset, bool ignoreTimeScale)
        {
            CancelFade();
            _ctsFade = new CancellationTokenSource();

            CancellationTokenSource currentCts = _ctsFade;

            CompleteFadeAsync(FadeAsync(startAlpha, endAlpha, fadeDuration, reset, ignoreTimeScale, currentCts.Token), currentCts).Forget();
        }

        public void FadeInOut(float startAlpha, float endAlpha, float fadeOutDuration, float fadeInDuration, float holdDuration, bool ignoreTimeScale)
        {
            CancelFade();
            _ctsFade = new CancellationTokenSource();

            CancellationTokenSource currentCts = _ctsFade;

            CompleteFadeAsync(FadeOutInAsync(startAlpha, endAlpha, fadeOutDuration, fadeInDuration, holdDuration, ignoreTimeScale, currentCts.Token), currentCts).Forget();
        }

        private void SetAlpha(float value) => _canvasGroup.alpha = value;
        private float GetAlpha() => _canvasGroup.alpha;

        private void CancelFade()
        {
            CancellationTokenSource currentCts = _ctsFade;
            _ctsFade = null;
            currentCts?.Cancel();
        }

        private async UniTask CompleteFadeAsync(UniTask task, CancellationTokenSource currentCts)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                UnityLog.Exception(e);
            }
            finally
            {
                if (ReferenceEquals(_ctsFade, currentCts))
                {
                    _ctsFade = null;
                }

                currentCts.Dispose();
            }
        }
    }
}
