using UnityEngine;

/// <summary>
/// Smooth, cinematic follow camera for a 2D physics-driven car.
/// Add this component to the Main Camera and optionally assign a target.
/// </summary>
public class SmoothCarCamera2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool findCarAutomatically = true;

    [Header("Follow")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 1.25f, -10f);
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.18f;
    [SerializeField, Min(0f)] private float maxFollowSpeed = 50f;
    [SerializeField, Min(0f)] private float lookAheadDistance = 2.2f;
    [SerializeField, Min(0.01f)] private float lookAheadSmoothTime = 0.25f;

    [Header("Juice")]
    [SerializeField, Range(0f, 12f)] private float tiltAmount = 3f;
    [SerializeField, Min(0.01f)] private float tiltSmoothTime = 0.16f;
    [SerializeField, Min(0f)] private float speedForMaxZoom = 8f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.2f;
    [SerializeField] private float zoomOutAmount = 1.1f;

    private Camera cam;
    private Rigidbody2D targetBody;
    private Vector3 positionVelocity;
    private float lookAheadVelocity;
    private float tiltVelocity;
    private float zoomVelocity;
    private float startingOrthographicSize;
    private float currentLookAhead;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        startingOrthographicSize = cam != null ? cam.orthographicSize : 5f;
        TryResolveTarget();
    }

    private void OnEnable()
    {
        positionVelocity = Vector3.zero;
        lookAheadVelocity = 0f;
        tiltVelocity = 0f;
        zoomVelocity = 0f;
    }

    private void LateUpdate()
    {
        if (target == null && findCarAutomatically)
            TryResolveTarget();

        if (target == null)
            return;

        if (targetBody == null)
        {
            targetBody = target.GetComponentInParent<Rigidbody2D>();
            targetBody ??= target.GetComponentInChildren<Rigidbody2D>();
        }

        float horizontalVelocity = targetBody != null ? targetBody.linearVelocity.x : 0f;
        float targetLookAhead = Mathf.Clamp(horizontalVelocity * lookAheadDistance / Mathf.Max(speedForMaxZoom, 0.01f), -lookAheadDistance, lookAheadDistance);
        currentLookAhead = Mathf.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);

        Vector3 desiredPosition = target.position + baseOffset;
        desiredPosition.x += currentLookAhead;
        desiredPosition.z = baseOffset.z;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime,
            maxFollowSpeed,
            Time.unscaledDeltaTime);

        // Positive velocity tilts the camera clockwise in Unity's 2D view.
        float desiredTilt = Mathf.Clamp(horizontalVelocity / Mathf.Max(speedForMaxZoom, 0.01f), -1f, 1f) * tiltAmount;
        float currentTilt = Mathf.SmoothDampAngle(
            transform.eulerAngles.z,
            desiredTilt,
            ref tiltVelocity,
            tiltSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);

        if (cam != null && cam.orthographic)
        {
            float targetZoom = startingOrthographicSize + Mathf.Abs(horizontalVelocity) / Mathf.Max(speedForMaxZoom, 0.01f) * zoomOutAmount;
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        targetBody ??= target != null ? target.GetComponentInParent<Rigidbody2D>() : null;
        targetBody ??= target != null ? target.GetComponentInChildren<Rigidbody2D>() : null;
    }

    private void TryResolveTarget()
    {
        CarController2D car = FindFirstObjectByType<CarController2D>();
        if (car != null)
            SetTarget(car.transform);
    }
}
