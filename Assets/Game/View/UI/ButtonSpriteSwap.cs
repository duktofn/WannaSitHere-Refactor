using UnityEngine;
using UnityEngine.UI;

namespace Game.View.UI
{
    public class ButtonSpriteSwap : MonoBehaviour
    {
        [SerializeField] private Sprite enableSprite;
        [SerializeField] private Sprite disableSprite;
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;

        public Sprite EnableSprite => enableSprite;
        public Sprite DisableSprite => disableSprite;
        public Button Button => button;
        public bool IsEnabled => button != null ? button.interactable : true;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        public void SetState(bool isEnable)
        {
            EnsureComponents();

            if (button != null)
            {
                button.interactable = isEnable;
            }

            if (targetImage != null)
            {
                Sprite targetSprite = isEnable ? enableSprite : disableSprite;
                if (targetSprite != null)
                {
                    targetImage.sprite = targetSprite;
                }
            }
        }
    }
}