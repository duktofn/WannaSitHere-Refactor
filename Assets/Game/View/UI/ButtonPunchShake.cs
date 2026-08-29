using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace Game.View.UI
{
    public class ButtonPunchShake : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private Vector3 punchStrength;

        public void OnButtonPressed()
        {
            PunchShake().Forget();
        }

        private async UniTask PunchShake()
        {
            await Tween.PunchScale(transform, punchStrength, duration);
        }
    }
}