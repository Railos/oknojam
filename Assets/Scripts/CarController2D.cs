using UnityEngine;
using UnityEngine.InputSystem;

public class CarController2D : MonoBehaviour
{
    private enum Gear
    {
        Reverse = -1,
        Park = 0,
        Forward = 1
    }

    [Header("Drive")]
    [SerializeField] private WheelJoint2D wheelLeft;
    [SerializeField] private WheelJoint2D wheelRight;
    [SerializeField] private float driveForce = 25f;
    [SerializeField] private float maxHorizontalSpeed = 8f;
    [SerializeField] private Gear currentGear = Gear.Park;
    [SerializeField] private float brakeForce = 12f;
    [SerializeField] private float gearChangeSpeedThreshold = 0.25f;
    [SerializeField] private float bodyAngularDamping = 1.5f;

    [Header("Jump")]
    [SerializeField] private float jumpImpulse = 5f;
    [SerializeField] private float groundCheckDistance = 0.65f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Wheel visuals")]
    [SerializeField] private float wheelRadius = 0.5f;
    [SerializeField] private float wheelSpinAcceleration = 3600f;

    private Rigidbody2D body;
    private Rigidbody2D leftWheelBody;
    private Rigidbody2D rightWheelBody;
    private Gear pendingGear = Gear.Park;
    private bool jumpRequested;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.angularDamping = bodyAngularDamping;

        wheelLeft ??= transform.root.Find("WheelLeft")?.GetComponent<WheelJoint2D>();
        wheelRight ??= transform.root.Find("WheelRight")?.GetComponent<WheelJoint2D>();

        leftWheelBody = wheelLeft != null ? wheelLeft.GetComponent<Rigidbody2D>() : null;
        rightWheelBody = wheelRight != null ? wheelRight.GetComponent<Rigidbody2D>() : null;

        if (wheelRadius <= 0f)
        {
            CircleCollider2D wheelCollider = wheelLeft != null
                ? wheelLeft.GetComponent<CircleCollider2D>()
                : null;
            if (wheelCollider != null)
                wheelRadius = wheelCollider.radius * Mathf.Abs(wheelCollider.transform.lossyScale.x);
        }
    }

    private void Update()
    {
        UpdateGear();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        float throttle = ReadThrottle();
        bool braking = IsBraking();

        body.AddForce(Vector2.right * throttle * driveForce, ForceMode2D.Force);
        if (braking)
            ApplyBrake();

        FinishGearChangeWhenStopped();
        ClampHorizontalSpeed();
        SpinWheel(leftWheelBody);
        SpinWheel(rightWheelBody);

        if (jumpRequested)
        {
            jumpRequested = false;
            if (IsGrounded())
                body.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
        }
    }

    private void UpdateGear()
    {
        if (Keyboard.current == null)
            return;

        bool forwardPressed = Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                              Keyboard.current.dKey.wasPressedThisFrame;
        bool reversePressed = Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                              Keyboard.current.aKey.wasPressedThisFrame;

        if (forwardPressed == reversePressed)
            return;

        Gear requestedGear = forwardPressed ? Gear.Forward : Gear.Reverse;

        if (currentGear == Gear.Park)
        {
            currentGear = requestedGear;
            pendingGear = Gear.Park;
            return;
        }

        if (currentGear != requestedGear)
            pendingGear = requestedGear;
    }

    private float ReadThrottle()
    {
        if (Keyboard.current == null || currentGear == Gear.Park)
            return 0f;

        bool forwardHeld = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
        bool reverseHeld = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;

        if (currentGear == Gear.Forward && forwardHeld)
            return 1f;
        if (currentGear == Gear.Reverse && reverseHeld)
            return -1f;

        return 0f;
    }

    private void ClampHorizontalSpeed()
    {
        Vector2 velocity = body.linearVelocity;

        velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);
        body.linearVelocity = velocity;
    }

    private bool IsBraking()
    {
        if (currentGear == Gear.Park || pendingGear != Gear.Park)
            return true;

        if (Keyboard.current == null)
            return false;

        bool forwardHeld = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
        bool reverseHeld = Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;

        return currentGear == Gear.Forward ? reverseHeld : forwardHeld;
    }

    private void ApplyBrake()
    {
        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, 0f, brakeForce * Time.fixedDeltaTime);
        body.linearVelocity = velocity;
    }

    private void FinishGearChangeWhenStopped()
    {
        if (pendingGear == Gear.Park || Mathf.Abs(body.linearVelocity.x) > gearChangeSpeedThreshold)
            return;

        Vector2 velocity = body.linearVelocity;
        velocity.x = 0f;
        body.linearVelocity = velocity;
        currentGear = IsGearHeld(pendingGear) ? pendingGear : Gear.Park;
        pendingGear = Gear.Park;
    }

    private bool IsGearHeld(Gear gear)
    {
        if (Keyboard.current == null)
            return false;

        if (gear == Gear.Forward)
            return Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed;
        if (gear == Gear.Reverse)
            return Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed;

        return false;
    }

    private void SpinWheel(Rigidbody2D wheel)
    {
        if (wheel == null || wheelRadius <= 0f)
            return;

        // v = r * angularVelocity. Unity reports angular velocity in degrees/second.
        float targetSpeed = -(body.linearVelocity.x / wheelRadius) * Mathf.Rad2Deg;
        wheel.angularVelocity = Mathf.MoveTowards(
            wheel.angularVelocity,
            targetSpeed,
            wheelSpinAcceleration * Time.fixedDeltaTime);
    }

    private bool IsGrounded()
    {
        return WheelTouchesGround(wheelLeft) || WheelTouchesGround(wheelRight);
    }

    private bool WheelTouchesGround(WheelJoint2D wheel)
    {
        if (wheel == null)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            wheel.transform.position,
            Vector2.down,
            groundCheckDistance,
            groundMask);

        return hit.collider != null &&
               hit.collider.transform != wheel.transform &&
               hit.rigidbody != body;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        DrawGroundRay(wheelLeft);
        DrawGroundRay(wheelRight);
    }

    private void DrawGroundRay(WheelJoint2D wheel)
    {
        if (wheel != null)
            Gizmos.DrawLine(wheel.transform.position, wheel.transform.position + Vector3.down * groundCheckDistance);
    }
}
