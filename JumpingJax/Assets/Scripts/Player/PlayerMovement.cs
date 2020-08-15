﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))] // Collider is necessary for custom collision detection
[RequireComponent(typeof(Rigidbody))] // Rigidbody is necessary to ignore certain colliders for portals
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("Red line is current velocity, blue is the new direction")]
    public bool showDebugGizmos = false;
    public LayerMask layersToIgnore;
    public BoxCollider myCollider;
    public CameraMove cameraMove;

    //The velocity applied at the end of every physics frame
    public Vector3 newVelocity;

    [SerializeField]
    private float airAcceleration = PlayerConstants.AirAcceleration;

    [SerializeField]
    private bool grounded;
    [SerializeField]
    private bool crouching;

    private float jumpTimer = 0;

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        cameraMove = GetComponent<CameraMove>();
    }

    private void FixedUpdate()
    {
        CheckCrouch();
        ApplyGravity();
        CheckGrounded();
        CheckAbove();

        CheckJump();

        var inputVector = GetWorldSpaceInputVector();
        var wishDir = inputVector.normalized;
        var wishSpeed = inputVector.magnitude;

        if (grounded)
        {
            if (IsPlayerWalkingBackwards())
            {
                wishSpeed *= PlayerConstants.BackWardsMoveSpeedScale;
            }
            ApplyGroundAcceleration(wishDir, wishSpeed, PlayerConstants.NormalSurfaceFriction);
            ClampVelocity(PlayerConstants.MoveSpeed);
            ApplyFriction();
        }
        else
        {
            ApplyAirAcceleration(wishDir, wishSpeed);
        }

        ClampVelocity(PlayerConstants.MaxVelocity);
        ContinuousCollisionDetection();

        // Perform a second ground check after moving to prevent bugs at the beginning of the next frame
        CheckGrounded();
        CheckAbove();
        ResolveCollisions();
    }

    private void CheckCrouch()
    {
        if (InputManager.GetKey(PlayerConstants.Crouch))
        {
            crouching = true;
            float velocity = 0;
            float height = Mathf.SmoothDamp(myCollider.size.y, PlayerConstants.CrouchingPlayerHeight, ref velocity, Time.deltaTime);
            myCollider.size = new Vector3(myCollider.size.x, height, myCollider.size.z);
        }
        else
        {
            crouching = false;
            float velocity = 0;
            float height = Mathf.SmoothDamp(myCollider.size.y, PlayerConstants.StandingPlayerHeight, ref velocity, Time.deltaTime);
            myCollider.size = new Vector3(myCollider.size.x, height, myCollider.size.z);
        }
    }

    private void ApplyGravity()
    {
        if (!grounded)
        {
            float gravityScale = GameManager.GetCurrentLevel().gravityMultiplier;
            newVelocity.y -= gravityScale * PlayerConstants.Gravity * Time.fixedDeltaTime;
        }
    }

    private void CheckAbove()
    {
        Ray[] boxTests = GetRays(Vector3.up);

        bool willBeCeilinged = false;

        foreach (Ray ray in boxTests)
        {
            if (showDebugGizmos)
            {
                Debug.DrawRay(ray.origin, ray.direction, Color.blue, 3);
            }
            if (Physics.Raycast(
                ray: ray,
                hitInfo: out RaycastHit hit,
                maxDistance: myCollider.bounds.extents.y + 0.1f, // add a small offset to allow the player to find the ground is ResolveCollision() sets us too far away
                layerMask: layersToIgnore,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y > transform.position.y) 
                {
                    willBeCeilinged = true;
                }
            }
        }

        if (willBeCeilinged && newVelocity.y > 0)
        {
            newVelocity.y = 0;
        }
    }

    // Performs 5 raycasts to check if there is a spot on the BoxCollider which is below the player and sets the grounded status
    private void CheckGrounded()
    {
        Ray[] boxTests = GetRays(Vector3.down);

        bool willBeGrounded = false;

        foreach(Ray ray in boxTests)
        {
            if (showDebugGizmos)
            {
                Debug.DrawRay(ray.origin, ray.direction, Color.blue, 3);
            }
            if(Physics.Raycast(
                ray: ray,
                hitInfo: out RaycastHit hit,
                maxDistance: myCollider.bounds.extents.y + 0.02f, // add a small offset to allow the player to find the ground is ResolveCollision() sets us too far away
                layerMask: layersToIgnore,
                QueryTriggerInteraction.Ignore))
            {
                if(hit.point.y < transform.position.y && 
                    !Physics.GetIgnoreCollision(myCollider, hit.collider) && // don't check for the ground on an object that is ignored (for example a wall with a portal on it)
                    hit.normal.y > 0.7f) // don't check for the ground on a sloped surface above 45 degrees
                {
                    willBeGrounded = true;
                }
            }
        }

        grounded = willBeGrounded;

        if (grounded && newVelocity.y < 0)
        {
            newVelocity.y = 0;
        }
    }

    private Ray[] GetRays(Vector3 direction)
    {
        Vector3 center = myCollider.bounds.center;
        Vector3 frontLeft = myCollider.bounds.center;
        frontLeft.x -= myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        frontLeft.z += myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 backLeft = myCollider.bounds.center;
        backLeft.x -= myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        backLeft.z -= myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 frontRight = myCollider.bounds.center;
        frontRight.x += myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        frontRight.z -= myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 backRight = myCollider.bounds.center;
        backRight.x += myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        backRight.z += myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;

        Ray ray0 = new Ray(center, direction);
        Ray ray1 = new Ray(frontLeft, direction);
        Ray ray2 = new Ray(backLeft, direction);
        Ray ray3 = new Ray(frontRight, direction);
        Ray ray4 = new Ray(backRight, direction);

        return new Ray[] { ray0, ray1, ray2, ray3, ray4 };
    }

    private void CheckJump()
    {
        // Prevent the player from finding a way to jump each frame, this would allow them to perform a super jump that is game breaking
        jumpTimer += Time.fixedDeltaTime;

        if (grounded && InputManager.GetKey(PlayerConstants.Jump) && jumpTimer > PlayerConstants.TimeBetweenJumps)
        {
            newVelocity.y += crouching ? PlayerConstants.CrouchingJumpPower : PlayerConstants.JumpPower;
            grounded = false;
            jumpTimer = 0;
        }
    }

    private Vector3 GetWorldSpaceInputVector()
    {
        float moveSpeed = crouching ? PlayerConstants.CrouchingMoveSpeed : PlayerConstants.MoveSpeed;

        var inputVelocity = GetInputVelocity(moveSpeed);
        if (inputVelocity.magnitude > moveSpeed)
        {
            inputVelocity *= moveSpeed / inputVelocity.magnitude;
        }

        //Get the velocity vector in world space coordinates, by rotating around the camera's y-axis
        return Quaternion.AngleAxis(cameraMove.playerCamera.transform.rotation.eulerAngles.y, Vector3.up) * inputVelocity;
    }

    private Vector3 GetInputVelocity(float moveSpeed)
    {
        float horizontalSpeed = 0;
        float verticalSpeed = 0;

        if (InputManager.GetKey(PlayerConstants.Left))
        {
            horizontalSpeed = -moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Right))
        {
            horizontalSpeed = moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Back))
        {
            verticalSpeed = -moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Forward))
        {
            verticalSpeed = moveSpeed;
        }

        return new Vector3(horizontalSpeed, 0, verticalSpeed);
    }

    private bool IsPlayerWalkingBackwards()
    {
        Vector3 inputDirection = GetInputVelocity(PlayerConstants.MoveSpeed);

        return inputDirection.z < 0;
    }

    //wishDir: the direction the player wishes to go in the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyGroundAcceleration(Vector3 wishDir, float wishSpeed, float surfaceFriction)
    {
        var currentSpeed = Vector3.Dot(newVelocity, wishDir); //Vector projection of the current velocity onto the new direction
        var speedToAdd = wishSpeed - currentSpeed;

        var acceleration = PlayerConstants.GroundAcceleration * Time.fixedDeltaTime; //acceleration to apply in the newest direction

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(acceleration * wishSpeed * surfaceFriction, speedToAdd);
        newVelocity += accelspeed * wishDir; //add acceleration in the new direction
    }

    //wishDir: the direction the player  wishes to goin the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyAirAcceleration(Vector3 wishDir, float wishSpeed)
    {
        var wishSpd = Mathf.Min(wishSpeed, PlayerConstants.AirAccelerationCap);
        var currentSpeed = Vector3.Dot(newVelocity, wishDir);
        var speedToAdd = wishSpd - currentSpeed;
        
        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(speedToAdd, airAcceleration * wishSpeed * Time.fixedDeltaTime);
        var velocityTransformation = accelspeed * wishDir;

        if (showDebugGizmos)
        {
            Debug.DrawRay(transform.position, newVelocity + velocityTransformation, Color.red, 1);
            Debug.DrawRay(transform.position, wishDir, Color.blue, 1);
            Debug.DrawRay(transform.position, velocityTransformation, Color.green, 1);
        }

        newVelocity += velocityTransformation;
    }

    private void ApplyFriction()
    {
        var speed = newVelocity.magnitude;

        // Don't apply friction if the player isn't moving
        // Clear speed if it's too low to prevent accidental movement
        // Also makes the player's friction feel more snappy
        if (speed < PlayerConstants.MinimumSpeedCutoff)
        {
            newVelocity = Vector3.zero;
            return;
        }

        var control = (speed < PlayerConstants.StopSpeed) ? PlayerConstants.StopSpeed : speed;
        var lossInSpeed = control * PlayerConstants.Friction * Time.fixedDeltaTime;
        var newSpeed = Mathf.Max(speed - lossInSpeed, 0);

        if (newSpeed != speed)
        {
            newVelocity *= newSpeed / speed; //Scale velocity based on friction
        }
    }

    // This function keeps the player from exceeding a maximum velocity
    private void ClampVelocity(float range)
    {
        newVelocity = Vector3.ClampMagnitude(newVelocity, PlayerConstants.MaxVelocity);
    }

    // This function is what keeps the player from walking through walls
    // We calculate how far we are inside of an object from moving this frame
    // and move the player just barely outside of the colliding object

    private void ContinuousCollisionDetection()
    {
        // - boxcast forwards based on speed, find the point in time where i hit it, and stop me there
        float castDistance = newVelocity.magnitude * Time.fixedDeltaTime;
        RaycastHit[] hits = Physics.BoxCastAll(
            center: myCollider.bounds.center,
            halfExtents: myCollider.bounds.extents,
            direction: newVelocity.normalized,
            Quaternion.identity,
            maxDistance: castDistance,
            layerMask: layersToIgnore);

        List<RaycastHit> validHits = hits
            .ToList()
            .OrderBy(hit => hit.distance)
            .Where(hit => !hit.collider.isTrigger)
            .Where(hit => !Physics.GetIgnoreCollision(hit.collider, myCollider))
            .Where(hit => hit.point != Vector3.zero)
            .Where(hit => hit.normal.y < 1)
            .ToList();

        // If we are going to hit a wall, set ourselves just outside of the wall and translate momentum along the wall
        if (validHits.Count() > 0 && newVelocity.magnitude > 10)
        {
            // find the time at which we would have hit the wall between this and the next frame
            float timeToImpact = validHits.First().distance / newVelocity.magnitude;
            // slide along the wall and prevent a complete loss of momentum
            ClipVelocity(validHits.First().normal);
            // set our position to just outside of the wall
            transform.position += newVelocity * timeToImpact;
        }
        else
        {
            transform.position += newVelocity * Time.fixedDeltaTime;
        }
    }

    //Slide off of the impacting surface
    private void ClipVelocity(Vector3 normal)
    {
        // Determine how far along plane to slide based on incoming direction.
        var backoff = Vector3.Dot(newVelocity, normal);

        var change = normal * backoff;
        change.y = 0; // only affect horizontal velocity
        newVelocity -= change;
    }

    private void ResolveCollisions()
    {
        var overlaps = Physics.OverlapBox(myCollider.bounds.center, myCollider.bounds.extents, Quaternion.identity);

        foreach (var other in overlaps)
        {
            // If the collider is my own, check the next one
            if (other == myCollider || other.isTrigger)
            {
                continue;
            }

            if (Physics.ComputePenetration(myCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                if (Vector3.Dot(dir, newVelocity.normalized) > 0 ||
                    Physics.GetIgnoreCollision(myCollider, other))
                {
                    continue;
                }

                Vector3 depenetrationVector = dir * dist; // The vector needed to get outside of the collision

                if (showDebugGizmos)
                {
                    Debug.Log($"Object Depenetration Vector: {depenetrationVector.ToString("F8")} \n Collided with: {other.gameObject.name}");
                }

                transform.position += depenetrationVector;
            }
        }
    }

}