using PrimeTween;
using TMPro;
using UnityEngine;

namespace Game.View.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LevelEndText : MonoBehaviour
    {
        private const float MaxAnimationDeltaTime = 1f / 30f;

        [Header("Text Revealing")]
        [SerializeField] private TextMeshProUGUI tmp;
        [SerializeField, Min(0f)] private float timeBetweenSecond = 0.1f;

        [Header("Character Scale")]
        [SerializeField] private Ease scaleEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float maxScale = 1.25f;
        [SerializeField, Min(0f)] private float normalScale = 1f;
        [SerializeField, Min(0f)] private float toMaxTime = 0.15f;
        [SerializeField, Min(0f)] private float toNormalTime = 0.15f;

        private Vector3[][] baseVertices;
        private Color32[][] baseColors;
        private bool[] visibleCharacters;
        private int[] visibleCharacterOrder;
        private int visibleCharacterCount;
        private float elapsedTime;
        private float totalDuration;
        private bool isPlaying;
        private int effectVersion;

        private void Awake()
        {
            if (tmp == null)
                tmp = GetComponent<TextMeshProUGUI>();

            if (tmp != null)
                tmp.maxVisibleCharacters = 0;
        }

        private async void OnEnable()
        {
            if (tmp == null)
                return;

            await TypingEffect();
        }

        private void OnDisable()
        {
            isPlaying = false;
            effectVersion++;
        }

        private void Update()
        {
            if (!isPlaying || tmp == null)
                return;

            // A long first frame (for example, after an editor recompile or
            // regaining focus) must not skip multiple character reveals.
            elapsedTime += Mathf.Min(
                Time.unscaledDeltaTime,
                MaxAnimationDeltaTime
            );
            ApplyAnimationFrame();

            if (elapsedTime >= totalDuration)
            {
                elapsedTime = totalDuration;
                ApplyAnimationFrame();
                isPlaying = false;
            }
        }

        public void SetDisplayText(string text)
        {
            tmp.text = text;
        }

        public async Awaitable TypingEffect()
        {
            if (tmp == null)
                return;

            if (!isActiveAndEnabled)
                return;

            int currentEffectVersion = ++effectVersion;
            InitializeAnimation();

            while (currentEffectVersion == effectVersion && isPlaying)
                await Awaitable.EndOfFrameAsync();
        }

        private void InitializeAnimation()
        {
            // TMP writes zeroed vertices when maxVisibleCharacters is zero.
            // Keep all glyphs generated and hide unrevealed characters by alpha instead.
            tmp.maxVisibleCharacters = int.MaxValue;
            tmp.ForceMeshUpdate();

            CacheMeshData(tmp.textInfo);

            elapsedTime = 0f;
            totalDuration = CalculateTotalDuration();
            isPlaying = visibleCharacters != null && visibleCharacters.Length > 0;

            ApplyAnimationFrame();

            if (totalDuration <= 0f)
                isPlaying = false;
        }

        private void ApplyAnimationFrame()
        {
            if (baseVertices == null || baseColors == null || visibleCharacters == null)
                return;

            TMP_TextInfo textInfo = tmp.textInfo;
            float normalScaleValue = GetNormalScale();
            float maxScaleValue = GetMaxScale(normalScaleValue);
            float revealInterval = Mathf.Max(0f, timeBetweenSecond);
            float growDuration = Mathf.Max(0f, toMaxTime);
            float shrinkDuration = Mathf.Max(0f, toNormalTime);

            for (int i = 0; i < visibleCharacters.Length; i++)
            {
                if (!visibleCharacters[i])
                    continue;

                int visibleOrder = visibleCharacterOrder[i];
                float characterTime = elapsedTime - (visibleOrder + 1) * revealInterval;
                bool isRevealed = characterTime >= 0f;
                float scale = CalculateScale(
                    characterTime,
                    normalScaleValue,
                    maxScaleValue,
                    growDuration,
                    shrinkDuration
                );

                ApplyCharacter(textInfo, i, scale, isRevealed);
            }

            tmp.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices |
                TMP_VertexDataUpdateFlags.Colors32
            );
        }

        private float CalculateScale(
            float characterTime,
            float normalScaleValue,
            float maxScaleValue,
            float growDuration,
            float shrinkDuration)
        {
            if (characterTime <= 0f || growDuration <= 0f)
            {
                if (characterTime >= growDuration && shrinkDuration > 0f)
                {
                    float shrinkProgress = Mathf.Clamp01(
                        (characterTime - growDuration) / shrinkDuration
                    );
                    return Mathf.LerpUnclamped(
                        maxScaleValue,
                        normalScaleValue,
                        EvaluateEase(shrinkProgress)
                    );
                }

                return normalScaleValue;
            }

            if (characterTime < growDuration)
            {
                return Mathf.LerpUnclamped(
                    normalScaleValue,
                    maxScaleValue,
                    EvaluateEase(characterTime / growDuration)
                );
            }

            if (shrinkDuration <= 0f || characterTime >= growDuration + shrinkDuration)
                return normalScaleValue;

            float shrinkProgressAfterGrowth =
                (characterTime - growDuration) / shrinkDuration;
            return Mathf.LerpUnclamped(
                maxScaleValue,
                normalScaleValue,
                EvaluateEase(shrinkProgressAfterGrowth)
            );
        }

        private void ApplyCharacter(
            TMP_TextInfo textInfo,
            int characterIndex,
            float scale,
            bool isRevealed)
        {
            if ((uint)characterIndex >= (uint)textInfo.characterCount)
                return;

            TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
            int materialIndex = characterInfo.materialReferenceIndex;
            int vertexIndex = characterInfo.vertexIndex;

            if ((uint)materialIndex >= (uint)baseVertices.Length ||
                (uint)materialIndex >= (uint)baseColors.Length ||
                (uint)materialIndex >= (uint)textInfo.meshInfo.Length)
            {
                return;
            }

            Vector3[] sourceVertices = baseVertices[materialIndex];
            Color32[] sourceColors = baseColors[materialIndex];
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            if (sourceVertices == null || sourceColors == null ||
                vertices == null || colors == null ||
                vertexIndex < 0 ||
                vertexIndex + 3 >= sourceVertices.Length ||
                vertexIndex + 3 >= sourceColors.Length ||
                vertexIndex + 3 >= vertices.Length ||
                vertexIndex + 3 >= colors.Length)
            {
                return;
            }

            Vector3 center =
                (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) / 2f;

            for (int vertexOffset = 0; vertexOffset < 4; vertexOffset++)
            {
                int currentVertex = vertexIndex + vertexOffset;
                vertices[currentVertex] = center +
                    (sourceVertices[currentVertex] - center) * scale;

                Color32 color = sourceColors[currentVertex];
                color.a = isRevealed ? color.a : (byte)0;
                colors[currentVertex] = color;
            }
        }

        private void CacheMeshData(TMP_TextInfo textInfo)
        {
            TMP_MeshInfo[] meshInfo = textInfo.meshInfo;
            baseVertices = new Vector3[meshInfo.Length][];
            baseColors = new Color32[meshInfo.Length][];
            visibleCharacters = new bool[textInfo.characterCount];
            visibleCharacterOrder = new int[textInfo.characterCount];
            visibleCharacterCount = 0;

            for (int i = 0; i < meshInfo.Length; i++)
            {
                baseVertices[i] = meshInfo[i].vertices == null
                    ? null
                    : (Vector3[])meshInfo[i].vertices.Clone();
                baseColors[i] = meshInfo[i].colors32 == null
                    ? null
                    : (Color32[])meshInfo[i].colors32.Clone();
            }

            for (int i = 0; i < visibleCharacters.Length; i++)
            {
                visibleCharacters[i] = textInfo.characterInfo[i].isVisible;
                visibleCharacterOrder[i] = visibleCharacters[i]
                    ? visibleCharacterCount++
                    : -1;
            }
        }

        private float CalculateTotalDuration()
        {
            if (visibleCharacters == null)
                return 0f;

            if (visibleCharacterCount == 0)
                return 0f;

            return visibleCharacterCount * Mathf.Max(0f, timeBetweenSecond) +
                   Mathf.Max(0f, toMaxTime) +
                   Mathf.Max(0f, toNormalTime);
        }

        private float EvaluateEase(float progress)
        {
            if (scaleEase == Ease.Custom)
                return Mathf.Clamp01(progress);

            return Mathf.Clamp01(Easing.Evaluate(Mathf.Clamp01(progress), scaleEase));
        }

        private float GetNormalScale()
        {
            return normalScale > 0f ? normalScale : 1f;
        }

        private float GetMaxScale(float normalScaleValue)
        {
            return maxScale > 0f ? maxScale : normalScaleValue * 1.1f;
        }
    }
}
