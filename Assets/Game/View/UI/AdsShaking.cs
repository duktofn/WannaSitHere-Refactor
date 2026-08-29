using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;
using System.Threading;

namespace Game.View.UI
{
    public class AdsShaking : MonoBehaviour
    {
        [SerializeField] private Image img;
        [SerializeField] private float scaleUpFactor;
        [SerializeField] private float scaleUpTime;
        [SerializeField] private Ease scaleEase;
        [SerializeField] private float scaleDownTime;
        [SerializeField] private Vector3 shakeStrength;
        [SerializeField] private float shakeDuration;
        [SerializeField] private int idleDelay;

        private CancellationTokenSource _cts;

        [ContextMenu("Start Shaking")]
        public void StartShaking()
        {
            _cts = new CancellationTokenSource();
            Shaking(_cts.Token).Forget();
        }

        [ContextMenu("Stop Shaking")]
        public void StopShaking()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTask Shaking(CancellationToken token)
        {
            while(!token.IsCancellationRequested) {
                await Tween.Scale(transform, scaleUpFactor, scaleUpTime, scaleEase);
                await Tween.ShakeLocalRotation(transform, shakeStrength, shakeDuration);
                await Tween.Scale(transform, 1f, scaleDownTime, scaleEase);
                await UniTask.Delay(idleDelay);
            }
        }
    }
}
