using Game.Domain.SaveAndLoad;
using TMPro;
using UnityEngine;

public class LevelView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveText;

    private LevelRuntimeData data;

    public void BindData(LevelRuntimeData source)
    {
        if (data != null && isActiveAndEnabled)
            data.OnMoveChanged -= UpdateMove;

        data = source;

        if (data == null)
        {
            Debug.LogWarning("Level data cannot be bound because it is null");
            return;
        }

        if (isActiveAndEnabled)
            data.OnMoveChanged += UpdateMove;

        UpdateMove(data.CurrentMove);
    }

    private void OnEnable()
    {
        if (data == null)
            return;

        data.OnMoveChanged += UpdateMove;
        UpdateMove(data.CurrentMove);
    }

    private void OnDisable()
    {
        if (data != null)
            data.OnMoveChanged -= UpdateMove;
    }

    private void UpdateMove(int value)
    {
        if (moveText != null)
            moveText.text = value.ToString();
    }
}
