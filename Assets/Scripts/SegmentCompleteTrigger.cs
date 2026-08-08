using UnityEngine;

/// <summary>
/// Put this on a trigger collider at the end of a segment.
/// </summary>
public class SegmentCompleteTrigger : MonoBehaviour
{
    [SerializeField] private Segment segment;
    [SerializeField] private Collider2D completionCollider;
    private bool completed;

    private void Awake()
    {
        segment ??= GetComponentInParent<Segment>();
        completionCollider ??= GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (completed || other.GetComponentInParent<CarController2D>() == null)
            return;

        completed = true;
        if (completionCollider != null)
            completionCollider.enabled = false;

        segment?.Complete();
    }
}
