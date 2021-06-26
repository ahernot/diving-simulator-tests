using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwimmingMovement : MonoBehaviour
{
    [SerializeField] private float swimmingForce = 200f;
    [SerializeField] private float resistanceForce;
    [SerializeField] private float deadZone;
    [SerializeField] private Transform trackingSpace;

    private new Rigidbody rigidbody;
    private Vector3 currentDirection;

    public GameObject handLeft;
    public GameObject handRight;
    public Camera viewCamera;

    Vector3 handLeftPosition;
    Vector3 handRightPosition;

    public float asymmetryDeadZone = 0.25f;
    public float maxAngularVelocity = 5f;
    public float angularForceMultiplier = 25f;
    public float angularDragMultiplier = 5f;
    Vector3 angularAcceleration;
    Vector3 angularVelocity;

    private void Awake ()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void Start ()
    {
        this.angularAcceleration = new Vector3();
        this.angularVelocity = new Vector3();
        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;
    }

    private void FixedUpdate ()
    {
        Vector3 leftHandDirection = ((this.handLeft).transform.position - this.handLeftPosition);// / Time.deltaTime;
        Vector3 rightHandDirection = ((this.handRight).transform.position - this.handRightPosition);// / Time.deltaTime;

        Vector3 localVelocity = leftHandDirection + rightHandDirection;


        

        if (localVelocity.magnitude > 0f)
        {
            float movementBalance = rightHandDirection.magnitude / (leftHandDirection.magnitude + rightHandDirection.magnitude) - 0.5f;

            if (Mathf.Abs(movementBalance) > asymmetryDeadZone)
            {
                this.angularAcceleration = -1f * movementBalance * this.angularForceMultiplier * this.viewCamera.transform.up;
                this.angularAcceleration.x = 0f;
                this.angularAcceleration.z = 0f;
            }
        }


        if (Vector3.Dot(localVelocity, this.viewCamera.transform.forward) <= 0f)
        {
            if (localVelocity.sqrMagnitude > deadZone * deadZone)
            {
                AddSwimmingForce(this.viewCamera.transform.forward);
            }
        }

        ApplyResistanceForce();


        // Apply angular drag torque
        this.angularAcceleration += this.angularDragMultiplier * (-1f * this.angularVelocity);

        // Align to camera heading during sideways movement
        // this.angularAcceleration.y += localVelocity.magnitude * Vector3.Angle (transform.forward, this.viewCamera.transform.forward);


        // Integrate angle
        // rigidbody.AddTorque (this.angularAcceleration, ForceMode.Acceleration);
        this.angularVelocity += this.angularAcceleration * Time.deltaTime;
        if (this.angularVelocity.magnitude > this.maxAngularVelocity) {
            this.angularVelocity = this.angularVelocity / this.angularVelocity.magnitude * this.maxAngularVelocity;
        }
        
        transform.rotation = Quaternion.Euler (transform.rotation.eulerAngles + (this.angularVelocity * Time.deltaTime));

        this.handLeftPosition = (this.handLeft).transform.position;
        this.handRightPosition = (this.handRight).transform.position;

    }

    private void ApplyResistanceForce()
    {
        if (rigidbody.velocity.sqrMagnitude > 0.01f && currentDirection != Vector3.zero)
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
        // Vector3 worldSpaceVelocity = trackingSpace.TransformDirection(localVelocity);
        rigidbody.AddForce(localVelocity * swimmingForce, ForceMode.Acceleration);
        currentDirection = localVelocity.normalized;
    }
}
