using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the current run values from GameState in UGUI Text components.
/// </summary>
public class GameStateTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text hypeText;
    [SerializeField] private TMP_Text techDebtText;
    [SerializeField] private TMP_Text daysText;

    [Header("Display formats")]
    [SerializeField] private string hypeFormat = "{0}";
    [SerializeField] private string techDebtFormat = "{0}";
    [SerializeField] private string daysFormat = "{0} / {1}";

    private void OnEnable()
    {
        GameState.Changed += Refresh;
        Refresh(GameState.Instance);
    }

    private void OnDisable()
    {
        GameState.Changed -= Refresh;
    }

    private void Refresh(GameState state)
    {
        if (state == null)
            return;

        if (hypeText != null)
            hypeText.text = string.Format(hypeFormat, state.Hype);

        if (techDebtText != null)
            techDebtText.text = string.Format(techDebtFormat, state.TechDebt);

        if (daysText != null)
            daysText.text = string.Format(daysFormat, state.Days, state.Deadline);
    }
}
