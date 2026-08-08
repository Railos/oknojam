using System;
using UnityEngine;

/// <summary>
/// Persistent global game information and game-wide processing entry point.
/// Access it from gameplay systems with GameState.Instance.
/// </summary>
public sealed class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Current Run")]
    [SerializeField] private int hype;
    [SerializeField] private int techDebt;
    [SerializeField] private int days;
    [SerializeField] private int deadline = 60;
    [SerializeField] private int currentDecisionIndex;
    [SerializeField] private int currentSegment;

    public int Hype => hype;
    public int TechDebt => techDebt;
    public int Days => days;
    public int Deadline => deadline;
    public int CurrentDecisionIndex => currentDecisionIndex;
    public int CurrentSegment => currentSegment;

    public static event Action<GameState> Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateGlobalState()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject gameStateObject = new GameObject(nameof(GameState));
        gameStateObject.AddComponent<GameState>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        hype = 0;
        techDebt = 0;
        days = 0;
        deadline = 60;
        currentDecisionIndex = 0;
        currentSegment = 0;
        NotifyChanged();
    }

    public void AddHype(int amount)
    {
        hype += amount;
        NotifyChanged();
    }

    public void AddTechDebt(int amount)
    {
        techDebt += amount;
        NotifyChanged();
    }

    public void ApplyCard(Card card)
    {
        if (card == null)
            return;

        hype += card.HypeDelta;
        techDebt += card.TechDelta;
        days += card.DayDelta;
        NotifyChanged();
    }

    public void AdvanceDays(int amount)
    {
        days += amount;
        NotifyChanged();
    }

    public void SetDecision(int decisionIndex)
    {
        currentDecisionIndex = decisionIndex;
        NotifyChanged();
    }

    public void SetSegment(int segment)
    {
        currentSegment = segment;
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
