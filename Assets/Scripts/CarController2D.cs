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
    [SerializeField] private float driveMotorSpeed = 900f;
    [SerializeField] private float driveMotorTorque = 150f;
    [SerializeField] private float maxHorizontalSpeed = 8f;
    [SerializeField] private Gear currentGear = Gear.Park;
    [SerializeField] private float brakeForce = 12f;
    [SerializeField] private float gearChangeSpeedThreshold = 0.25f;
    [SerializeField] private float bodyAngularDamping = 1.5f;

    [Header("Jump")]
    [SerializeField] private float jumpImpulse = 5f;
    [SerializeField] private float groundCheckDistance = 0.65f;
    [SerializeField] private float groundCheckRadius = 0.45f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Air stability")]
    [SerializeField] private float airUprightTorque = 8f;
    [SerializeField] private float airAngularDamping = 4f;
    [SerializeField] private float airMaxTorque = 20f;

    [Header("Ground contact")]
    [SerializeField] private float groundStickForce = 20f;

    private Rigidbody2D body;
    private Rigidbody2D leftWheelBody;
    private Rigidbody2D rightWheelBody;
    private Transform playerRoot;
    private Vector3 bodyLocalPosition;
    private Vector3 leftWheelLocalPosition;
    private Vector3 rightWheelLocalPosition;
    private float bodyLocalRotation;
    private float leftWheelLocalRotation;
    private float rightWheelLocalRotation;
    private Gear pendingGear = Gear.Park;
    private bool jumpRequested;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.angularDamping = bodyAngularDamping;
        playerRoot = transform.root;

        wheelLeft ??= transform.root.Find("WheelLeft")?.GetComponent<WheelJoint2D>();
        wheelRight ??= transform.root.Find("WheelRight")?.GetComponent<WheelJoint2D>();

        leftWheelBody = wheelLeft != null ? wheelLeft.GetComponent<Rigidbody2D>() : null;
        rightWheelBody = wheelRight != null ? wheelRight.GetComponent<Rigidbody2D>() : null;

        bodyLocalPosition = body.transform.localPosition;
        leftWheelLocalPosition = leftWheelBody != null ? leftWheelBody.transform.localPosition : Vector3.zero;
        rightWheelLocalPosition = rightWheelBody != null ? rightWheelBody.transform.localPosition : Vector3.zero;
        bodyLocalRotation = body.transform.localEulerAngles.z;
        leftWheelLocalRotation = leftWheelBody != null ? leftWheelBody.transform.localEulerAngles.z : 0f;
        rightWheelLocalRotation = rightWheelBody != null ? rightWheelBody.transform.localEulerAngles.z : 0f;

        ApplyWheelGrip(wheelLeft);
        ApplyWheelGrip(wheelRight);
    }

    public void Respawn(Transform respawnPoint)
    {
        if (respawnPoint == null)
            return;

        playerRoot.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        SetRigidbodyPose(body, respawnPoint, bodyLocalPosition, bodyLocalRotation);
        SetRigidbodyPose(leftWheelBody, respawnPoint, leftWheelLocalPosition, leftWheelLocalRotation);
        SetRigidbodyPose(rightWheelBody, respawnPoint, rightWheelLocalPosition, rightWheelLocalRotation);
        Physics2D.SyncTransforms();
        ResetPhysics();
    }

    public void ResetPhysics()
    {
        currentGear = Gear.Park;
        pendingGear = Gear.Park;
        jumpRequested = false;

        ResetRigidbody(body);
        ResetRigidbody(leftWheelBody);
        ResetRigidbody(rightWheelBody);

        SetWheelMotor(wheelLeft, 0f, false);
        SetWheelMotor(wheelRight, 0f, false);
    }

    private static void ResetRigidbody(Rigidbody2D rigidbody)
    {
        if (rigidbody == null)
            return;

        rigidbody.linearVelocity = Vector2.zero;
        rigidbody.angularVelocity = 0f;
        rigidbody.Sleep();
    }

    private static void SetRigidbodyPose(
        Rigidbody2D rigidbody,
        Transform poseRoot,
        Vector3 localPosition,
        float localRotation)
    {
        if (rigidbody == null)
            return;

        rigidbody.position = poseRoot.TransformPoint(localPosition);
        rigidbody.rotation = poseRoot.eulerAngles.z + localRotation;
    }

    private void Update()
    {
        UpdateGear();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) 
        {
            GameState.Instance?.AdvanceDays(1);
            EventManager.OnSegmentRestart?.Invoke(this);
        }
    }

    private void FixedUpdate()
    {
        float throttle = ReadThrottle();
        bool braking = IsBraking();

        DriveWheels(throttle);
        if (braking)
            ApplyBrake();

        FinishGearChangeWhenStopped();
        ClampHorizontalSpeed();

        bool grounded = IsGrounded();
        if (grounded)
            body.AddForce(Vector2.down * groundStickForce, ForceMode2D.Force);
        else
            StabilizeInAir();

        if (jumpRequested)
        {
            jumpRequested = false;
            if (grounded)
            {
                Vector2 velocity = body.linearVelocity;
                velocity.y = jumpImpulse;
                body.linearVelocity = velocity;
                body.WakeUp();
            }
        }
    }

    private void UpdateGear()
    {
        if (Keyboard.current == null)
            return;

        bool forwardPressed = ForwardPressedThisFrame();
        bool reversePressed = ReversePressedThisFrame();

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

        if (currentGear == Gear.Forward && IsForwardHeld())
            return 1f;
        if (currentGear == Gear.Reverse && IsReverseHeld())
            return -1f;

        return 0f;
    }

    private void ClampHorizontalSpeed()
    {
        Vector2 velocity = body.linearVelocity;

        velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);
        body.linearVelocity = velocity;
    }

    private void DriveWheels(float throttle)
    {
        float motorSpeed = throttle * driveMotorSpeed;
        bool enabled = Mathf.Abs(throttle) > 0.01f;
        SetWheelMotor(wheelLeft, motorSpeed, enabled);
        SetWheelMotor(wheelRight, motorSpeed, enabled);
    }

    private void SetWheelMotor(WheelJoint2D wheel, float speed, bool enabled)
    {
        if (wheel == null)
            return;

        JointMotor2D motor = wheel.motor;
        motor.motorSpeed = speed;
        motor.maxMotorTorque = driveMotorTorque;
        wheel.motor = motor;
        wheel.useMotor = enabled;
    }

    private void ApplyWheelGrip(WheelJoint2D wheel)
    {
        if (wheel == null)
            return;

        CircleCollider2D collider = wheel.GetComponent<CircleCollider2D>();
        if (collider == null)
            return;

        PhysicsMaterial2D grip = new PhysicsMaterial2D("Car Wheel Grip")
        {
            friction = 1.5f,
            bounciness = 0f
        };
        collider.sharedMaterial = grip;
    }

    private bool IsBraking()
    {
        if (currentGear == Gear.Park || pendingGear != Gear.Park)
            return true;

        if (Keyboard.current == null)
            return false;

        return currentGear == Gear.Forward ? IsReverseHeld() : IsForwardHeld();
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

        return gear == Gear.Forward ? IsForwardHeld() : IsReverseHeld();
    }

    private static bool IsForwardHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed);
    }

    private static bool IsReverseHeld()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed);
    }

    private static bool ForwardPressedThisFrame()
    {
        return Keyboard.current != null &&
               (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                Keyboard.current.dKey.wasPressedThisFrame);
    }

    private static bool ReversePressedThisFrame()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame);
    }

    private void StabilizeInAir()
    {
        float angleError = Mathf.DeltaAngle(body.rotation, 0f);
        float torque = angleError * airUprightTorque - body.angularVelocity * airAngularDamping;
        body.AddTorque(Mathf.Clamp(torque, -airMaxTorque, airMaxTorque), ForceMode2D.Force);
    }

    private bool IsGrounded()
    {
        return WheelTouchesGround(wheelLeft) || WheelTouchesGround(wheelRight);
    }

    private bool WheelTouchesGround(WheelJoint2D wheel)
    {
        if (wheel == null)
            return false;

        Collider2D wheelCollider = wheel.GetComponent<Collider2D>();
        if (wheelCollider != null && wheelCollider.IsTouchingLayers(groundMask))
            return true;

        RaycastHit2D hit = Physics2D.CircleCast(
            wheel.transform.position,
            groundCheckRadius,
            Vector2.down,
            groundCheckDistance,
            groundMask);

        return hit.collider != null &&
               hit.collider.transform != wheel.transform &&
               hit.rigidbody != body &&
               hit.rigidbody != wheel.GetComponent<Rigidbody2D>();
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

    private void OnTriggerEnter2D(Collider2D other){
        if (other.GetComponent<FailTrigger>() != null)
        {
            GameState.Instance?.AdvanceDays(2);
            EventManager.OnSegmentRestart?.Invoke(this);
        }
    }
}
