using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Offers three different cards after a segment, applies the selected card,
/// and spawns the next segment.
/// </summary>
public class ChoiceFlow : MonoBehaviour
{
    [Serializable]
    public class CardChoiceEvent : UnityEvent<Card[]> { }

    [Header("Cards")]
    [SerializeField] private Card[] cardPool;
    [SerializeField, Min(1)] private int choicesPerSegment = 3;

    [Header("Segments")]
    [SerializeField] private Segment initialSegmentPrefab;
    [SerializeField] private Transform segmentParent;
    [SerializeField] private bool startWithFirstSegment = true;

    [Header("Player")]
    [SerializeField] private CarController2D playerCar;

    [Header("Presentation")]
    [Tooltip("Called whenever a new set of cards is ready. Connect this to your card UI.")]
    [SerializeField] private CardChoiceEvent onChoicesPresented;
    [SerializeField] private UnityEvent onChoiceConfirmed;

    private readonly List<Card> currentChoices = new List<Card>();
    private Segment currentSegment;
    private bool waitingForChoice;

    public IReadOnlyList<Card> CurrentChoices => currentChoices;
    public bool WaitingForChoice => waitingForChoice;
    public event Action<Card[]> ChoicesPresented;

    private void OnEnable()
    {
        EventManager.OnSegmentCompleted += HandleSegmentCompleted;

        if (startWithFirstSegment && currentSegment == null)
            SpawnSegment(initialSegmentPrefab, 0);
    }

    private void OnDisable()
    {
        EventManager.OnSegmentCompleted -= HandleSegmentCompleted;
    }

    private void HandleSegmentCompleted(Segment completedSegment)
    {
        if (completedSegment != currentSegment || waitingForChoice)
            return;

        PresentChoices();
    }

    public void PresentChoices()
    {
        StopPlayer();
        currentChoices.Clear();

        if (cardPool == null || cardPool.Length == 0)
        {
            Debug.LogError("ChoiceFlow needs at least one card in Card Pool.", this);
            return;
        }

        List<Card> availableCards = new List<Card>(cardPool);
        availableCards.RemoveAll(card => card == null);
        int choiceCount = Mathf.Min(choicesPerSegment, availableCards.Count);

        for (int i = 0; i < choiceCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
            currentChoices.Add(availableCards[randomIndex]);
            availableCards.RemoveAt(randomIndex);
        }

        waitingForChoice = true;
        Card[] choices = currentChoices.ToArray();
        onChoicesPresented?.Invoke(choices);
        ChoicesPresented?.Invoke(choices);
    }

    /// <summary>
    /// Connect each card UI button to this method with its displayed card index.
    /// </summary>
    public void ChooseCard(int choiceIndex)
    {
        if (!waitingForChoice || choiceIndex < 0 || choiceIndex >= currentChoices.Count)
            return;

        Card selectedCard = currentChoices[choiceIndex];

        if (selectedCard == null || selectedCard.SegmentPrefab == null)
        {
            Debug.LogError("The selected card must have a corresponding segment prefab.", this);
            return;
        }

        waitingForChoice = false;
        currentChoices.Clear();

        if (GameState.Instance == null)
        {
            Debug.LogError("ChoiceFlow could not find GameState.Instance.", this);
            return;
        }

        GameState.Instance.ApplyCard(selectedCard);
        GameState.Instance.SetDecision(choiceIndex);
        onChoiceConfirmed?.Invoke();
        SpawnSegment(selectedCard.SegmentPrefab, GameState.Instance.CurrentSegment + 1);
    }

    private void SpawnSegment(Segment segmentPrefab, int segmentIndex)
    {
        if (segmentPrefab == null)
        {
            Debug.LogError("The selected card has no corresponding segment prefab.", this);
            return;
        }

        Segment previousSegment = currentSegment;
        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (previousSegment != null && previousSegment.NextSegmentSpawnPoint != null)
        {
            Transform spawnPoint = previousSegment.NextSegmentSpawnPoint;
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }

        currentSegment = Instantiate(segmentPrefab, spawnPosition, spawnRotation, segmentParent);
        currentSegment.isCurrentSegment = true;

        if (previousSegment != null)
            previousSegment.isCurrentSegment = false;

        GameState.Instance.SetSegment(segmentIndex);
        ResumePlayer();
    }

    private void StopPlayer()
    {
        if (playerCar == null)
            playerCar = FindFirstObjectByType<CarController2D>();

        if (playerCar == null)
            return;

        playerCar.ResetPhysics();
        playerCar.enabled = false;
    }

    private void ResumePlayer()
    {
        if (playerCar != null)
            playerCar.enabled = true;
    }
}
