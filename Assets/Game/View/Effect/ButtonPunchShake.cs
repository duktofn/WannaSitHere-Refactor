using UnityEngine;
using Cysharp.Threading.Tasks;
using PrimeTween;

namespace Game.View.Effect
{
    public class ButtonPunchShake : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private Vector3 punchStrength;

        public void OnButtonPressed()
        {
            PunchShake().Forget();
        }

        public async UniTask PunchShake()
        {
            await Tween.PunchScale(transform, punchStrength, duration);
        }
    }
}