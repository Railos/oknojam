using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Basic UGUI view for ChoiceFlow. Use three Buttons and three Text labels.
/// </summary>
public class ChoicePanel : MonoBehaviour
{
    [SerializeField] private ChoiceFlow choiceFlow;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] choiceLabels = new TextMeshProUGUI[3];

    private void Start()
    {
        choiceFlow.ChoicesPresented += ShowChoices;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int choiceIndex = i;
            if (choiceButtons[i] != null)
                choiceButtons[i].onClick.AddListener(() => Choose(choiceIndex));
        }

        Hide();

        if (choiceFlow.WaitingForChoice)
            ShowChoices(new List<Card>(choiceFlow.CurrentChoices).ToArray());
    }

    private void OnDestroy()
    {
        if (choiceFlow != null)
            choiceFlow.ChoicesPresented -= ShowChoices;
    }

    private void Choose(int choiceIndex)
    {
        choiceFlow.ChooseCard(choiceIndex);

        if (!choiceFlow.WaitingForChoice)
            Hide();
    }

    private void ShowChoices(Card[] choices)
    {
        if (choices == null)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool available = i < choices.Length && choices[i] != null;
            if (choiceButtons[i] != null)
                choiceButtons[i].gameObject.SetActive(available);

            if (available && i < choiceLabels.Length && choiceLabels[i] != null)
            {
                Card card = choices[i];
                choiceLabels[i].text = $"{card.CardName}\nHype {card.HypeDelta:+#;-#;0}  " +
                                       $"Tech {card.TechDelta:+#;-#;0}  Days {card.DayDelta:+#;-#;0}";
            }
        }
    }

    private void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
