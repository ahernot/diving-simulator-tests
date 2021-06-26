using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwimmingMovement : MonoBehaviour
{
    [SerializeField] private float swimmingForce;
    [SerializeField] private float resistanceForce;
    [SerializeField] private float deadZone;
    [SerializeField] private Transform trackingSpace;

    public GameObject handLeft;
    public GameObject handRight;

    private new Rigidbody rigidbody;
    private Vector3 currentDirection;

    // Previous positions
    Vector3 handLeftPosition;
    Vector3 handRightPosition;


    Vector3 angularAcceleration;
    Vector3 angularVelocity;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }


    void UpdateHands ()
    {
        

    }

    private void FixedUpdate()
    {   
        // Get hand speeds
        Vector3 leftHandDirection = ((this.handLeft).transform.position - this.handLeftPosition) / Time.deltaTime;
        Vector3 rightHandDirection = ((this.handRight).transform.position - this.handRightPosition) / Time.deltaTime;

        // Get movement in relation to forwards vector
        Vector3 u = viewCamera.transform.forward;
        float handLeftMovement = Vector3.Dot (handLeftPositionDelta, u);
        float handRightMovement = Vector3.Dot (handRightPositionDelta, u);

        // Get movement percentage
        float movementNorm = handLeftMovement.magnitude + handRightMovement.magnitude;
        float handLeftMovementPercentage = handLeftMovement / movementNorm;
        float handRightMovementPercentage = handRightMovement / movementNorm;

        // 
        Vector3 angularForce = handRightMovementPercentage * angularForceMultiplier * viewCamera.transform.up;
        rigidbody.AddTorque (angularForce);


        
        Vector3 localVelocity = leftHandDirection + rightHandDirection;
        Vector3 cameraDirection = Camera.main.transform.forward;

        localVelocity *= -1f;
        if (localVelocity.sqrMagnitude > deadZone * deadZone)
        {
            AddSwimmingForce(cameraDirection);
        }
        ApplyResistanceForce();

        // Update previous hand position values
        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;
    }

    private void ApplyResistanceForce()
    {
        if (rigidbody.velocity.sqrMagnitude > 0f && currentDirection != Vector3.zero)
        {
            rigidbody.AddForce(-rigidbody.velocity * resistanceForce, ForceMode.Acceleration);
        }
        else
        {
            currentDirection = Vector3.zero;
        }
    }

    private void AddSwimmingForce(Vector3 localVelocity)
    {
        Vector3 worldSpaceVelocity = trackingSpace.TransformDirection(localVelocity);
        rigidbody.AddForce(worldSpaceVelocity * swimmingForce, ForceMode.Acceleration);
        currentDirection = worldSpaceVelocity.normalized;
    }

}
