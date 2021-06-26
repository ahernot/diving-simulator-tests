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
    [Tooltip("Main FPV camera")]
    public Camera viewCamera;

    public GameObject handLeft;
    public GameObject handRight;

    public float deadZoneVal = 0.00001f;

    Vector3 handLeftPosition;
    Vector3 handRightPosition;

    //----------//

    [Header("Water Parameters")]
    [Tooltip("Water current direction vector (not normalized) — requires force generation on update")]
    public Vector3 waterCurrentDirection = new Vector3 (1, 0, 0);
    [Tooltip("Water current velocity — requires force generation on update")]
    public float waterCurrentVelocity = 0.15f;
    Vector3 waterVelocity = new Vector3();

    //----------//

    [Header("Force Parameters")]
    public float movementForceMultiplier = 20f;
    public float movementTorqueMultiplier = 5f;
    public float dragForceMultiplier = 4f;
    public float dragTorqueMultiplier = 5f;
    [Tooltip("Vertical force multiplier (gravity + buoyancy) — requires force generation on update")]
    public float verticalForceMultiplier = -0.8f;

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


    void Start ()
    {
        // Initialise motion variables
        this.acceleration = new Vector3();
        this.velocity = new Vector3();

        // Generate forces
        this.RegenerateForces();

        // Initialise hand position memory
        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;
    }

    void Update ()
    {
        this.UpdateMovement();
    }

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

            if (Mathf.Abs(movementBalance) > asymmetryDeadZone)
            {
                this.angularAcceleration = new Vector3 (
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



    void UpdateResetMovement ()
    {
        // Get Backspace input
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            this.ResetMovement();
        }
    }

    void UpdateMovement ()
    {   
        // Update inputs
        this.UpdateResetMovement();

        // Calculate forces
        this.CalculateMovementForces();
        this.CalculateDragForces();

        // Integrate motion
        this.acceleration = this.dragForce + this.verticalForce + this.movementForce;
        this.velocity += this.acceleration * Time.deltaTime;
        this.angularAcceleration = this.dragTorque + this.movementTorque;
        this.angularVelocity += this.angularAcceleration * Time.deltaTime;

        // Move player
        controller.Move (this.velocity * Time.deltaTime);
    }


    public void RegenerateForces ()
    {
        // Compute vertical force vector
        this.verticalForce = new Vector3 (0f, this.verticalForceMultiplier, 0f);

        // Compute water velocity vector
        this.waterVelocity = this.waterCurrentVelocity * Vector3.Normalize (this.waterCurrentDirection);
    }


    public void ResetMovement ()
    {
        this.velocity = Vector3.zero;
    }
}