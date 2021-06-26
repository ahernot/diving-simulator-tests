/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 DiverMovement v1.5.1
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiverMovement : MonoBehaviour
{
    [Header("Player")]
    public CharacterController controller;
    public Camera viewCamera;

    public GameObject handLeft;
    public GameObject handRight;

    public float deadZoneAsymmmetry = 0.25f;
    public float deadZoneVal = 0.00001f;

    Vector3 handLeftPosition;
    Vector3 handRightPosition;

    //----------//

    [Header("Water Parameters")]
    [Tooltip("Water current direction vector (not normalized) — requires force generation on update")]
    [Range(0, 360)]
    public float waterCurrentDirection = 0f;
    [Tooltip("Water current velocity — requires force generation on update")]
    public float waterCurrentVelocity = 0.15f;
    Vector3 waterVelocity = new Vector3();

    //----------//

    [Header("Force Parameters")]
    public float movementForceMultiplier = 20f;
    public float movementTorqueMultiplier = 25f;
    public float dragForceMultiplier = 4f;
    public float dragTorqueMultiplier = 5f;
    [Tooltip("Vertical force multiplier (gravity + buoyancy) — requires force generation on update")]
    public float verticalForceMultiplier = -0.8f;

    public float maxVelocity = 10f;
    public float maxAngularVelocity = 5f;

    //----------//

    [Header("Instantaneous Movement")]
    public Vector3 acceleration = new Vector3();
    public Vector3 velocity = new Vector3();
    public Vector3 angularAcceleration = new Vector3();
    public Vector3 angularVelocity = new Vector3();

    // Forces
    Vector3 movementForce = new Vector3();
    Vector3 movementTorque = new Vector3();
    Vector3 dragForce = new Vector3();
    Vector3 dragTorque = new Vector3();
    Vector3 verticalForce = new Vector3();


    // ----- START ----- //
    void Start ()
    {
        // Calculate vertical force
        this.CalculateVerticalForce();
        
        // Calculate water velocity
        this.CalculateWaterVelocity();

        // Initialise hand position memory
        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;
    }

    // ----- UPDATE ----- //
    void Update ()
    {
        this.UpdateMovement();
        this.TriggerResetMovement();
    }


    // ----- UPDATE MOVEMENT ----- //
    void UpdateMovement ()
    {
        // Calculate forces
        this.CalculateMovementForces();
        this.CalculateDragForces();

        // Integrate motion
        this.acceleration = this.dragForce + this.verticalForce + this.movementForce;
        this.velocity += this.acceleration * Time.deltaTime;
        if (this.velocity.magnitude > this.maxVelocity) { this.velocity = this.maxVelocity * Vector3.Normalize (this.velocity); }
        this.angularAcceleration = this.dragTorque + this.movementTorque;
        this.angularVelocity += this.angularAcceleration * Time.deltaTime;
        if (this.angularVelocity.magnitude > this.maxAngularVelocity) { this.angularVelocity = this.maxAngularVelocity * Vector3.Normalize (this.angularVelocity); }

        Vector3 positionDelta = this.velocity * Time.deltaTime;
        Vector3 rotationDelta = this.angularVelocity * Time.deltaTime;

        // Move player
        controller.Move (positionDelta);
        transform.rotation = Quaternion.Euler (transform.rotation.eulerAngles + rotationDelta);
    }


    // ----- CALCULATE FORCES ----- //
    void CalculateMovementForces ()
    {
        // Calculate hand position deltas
        Vector3 handLeftPositionDelta = (this.handLeft).transform.position - this.handLeftPosition;
        Vector3 handRightPositionDelta = (this.handRight).transform.position - this.handRightPosition;

        // ?
        Vector3 globalPositionDelta = handLeftPositionDelta + handRightPositionDelta;
        float cumulativePositionDelta = handLeftPositionDelta.magnitude + handRightPositionDelta.magnitude;

        // Movement torque
        if (cumulativePositionDelta.magnitude > 0f)
        {
            float movementBalance = rightHandDirection.magnitude / cumulativePositionDelta - 0.5f;

            if (Mathf.Abs(movementBalance) > deadZoneAsymmmetry)
            {
                this.movementTorque = new Vector3 (
                    0f,
                    -1f * movementBalance * this.movementTorqueMultiplier * this.viewCamera.transform.up,
                    0f
                );
            }
        }

        // Movement force (forwards)
        float globalForwardsMotion = Vector3.Dot(globalPositionDelta, this.viewCamera.transform.forward);

        if (globalForwardsMotion <= 0f)
        {
            if (globalPositionDelta.sqrMagnitude > deadZone * deadZone)
            {
                // Add swimming force
                this.movementForce = this.movementForceMultiplier * this.viewCamera.transform.forward;
            }
        }

        // Refresh hand position memory
        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;
    }

    void CalculateDragForces ()
    {
        // Drag force
        this.dragForce = -1 * this.dragForceMultiplier * (this.velocity - this.waterVelocity);

        // Drag torque
        this.dragTorque = -1 * this.dragTorqueMultiplier * this.angularVelocity;
    }

    void CalculateVerticalForce ()
    {
        this.verticalForce = new Vector3 (
            0f,
            this.verticalForceMultiplier,
            0f
        );
    }

    void CalculateWaterVelocity ()
    {
        float waterCurrentDirectionRadians = this.waterCurrentDirection / 180 * Mathf.PI;
        
        Vector3 waterDirectionVector = new Vector3 (
            Mathf.Cos (waterCurrentDirectionRadians),
            0f,
            Mathf.Sin (waterCurrentDirectionRadians)
        );

        this.waterVelocity = this.waterCurrentVelocity * waterDirectionVector;
    }


    // ----- RESET MOVEMENT ----- //
    public void ResetMovement ()
    {
        this.acceleration = Vector3.zero;
        this.velocity = Vector3.zero;
    }

    void TriggerResetMovement ()
    {
        // Get Backspace input
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            this.ResetMovement();
        }
    }
}
