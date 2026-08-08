using UnityEngine;

public class Segment : MonoBehaviour
{

    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Transform nextSegmentSpawnPoint;
    public bool isCurrentSegment = false;

    public Transform NextSegmentSpawnPoint => nextSegmentSpawnPoint;

    private void Awake()
    {
        EventManager.OnSegmentRestart += OnSegmentRestart;
    }

    private void OnDestroy()
    {
        EventManager.OnSegmentRestart -= OnSegmentRestart;
    }

    /// <summary>
    /// Call this from the segment finish trigger when the player reaches the end.
    /// </summary>
    public void Complete()
    {
        EventManager.OnSegmentCompleted?.Invoke(this);
    }

    private void OnSegmentRestart(CarController2D car)
    {
        if (isCurrentSegment)
        {
            car.Respawn(respawnPoint);
        }
    }
}
