using UnityEngine;

/// <summary>
/// Data for a card that can be offered to the player at the end of a segment.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Oknojam/Card")]
public class Card : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private int hypeDelta;
    [SerializeField] private int techDelta;
    [SerializeField] private int dayDelta;
    [SerializeField] private string preferredSegmentType;
    [SerializeField] private Segment segmentPrefab;

    public string CardName => cardName;
    public int HypeDelta => hypeDelta;
    public int TechDelta => techDelta;
    public int DayDelta => dayDelta;
    public string PreferredSegmentType => preferredSegmentType;
    public Segment SegmentPrefab => segmentPrefab;
}
