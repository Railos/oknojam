using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Launches a car when one of its 2D colliders crosses this pad.
/// Add this component to a GameObject with a Collider2D. The collider can be
/// either a trigger or a normal collider.
/// </summary>
public class JumpPad : MonoBehaviour
{
    [Header("Launch")]
    [Tooltip("World-space velocity added to the car when it hits the pad.")]
    [SerializeField] private Vector2 launchVelocity = new Vector2(3f, 12f);

    [Tooltip("Prevents the car's body and wheels from triggering the pad repeatedly.")]
    [SerializeField, Min(0f)] private float retriggerDelay = 0.25f;

    private readonly Dictionary<CarController2D, float> lastLaunchTimes =
        new Dictionary<CarController2D, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryLaunch(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryLaunch(collision.collider);
    }

    private void TryLaunch(Collider2D other)
    {
        CarController2D car = FindCar(other);
        if (car == null || !car.isActiveAndEnabled)
            return;

        float now = Time.time;
        if (lastLaunchTimes.TryGetValue(car, out float lastLaunchTime) &&
            now - lastLaunchTime < retriggerDelay)
            return;

        lastLaunchTimes[car] = now;
        car.LaunchFromJumpPad(launchVelocity);
    }

    private static CarController2D FindCar(Collider2D other)
    {
        if (other == null)
            return null;

        CarController2D car = other.GetComponentInParent<CarController2D>();
        if (car != null)
            return car;

        Rigidbody2D attachedBody = other.attachedRigidbody;
        return attachedBody != null
            ? attachedBody.transform.root.GetComponentInChildren<CarController2D>()
            : null;
    }

    private void OnDisable()
    {
        lastLaunchTimes.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + new Vector3(launchVelocity.x, launchVelocity.y, 0f) * 0.15f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.08f);
    }
}
