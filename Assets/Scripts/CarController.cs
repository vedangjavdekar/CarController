using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public Transform forceApply;
    public Transform centerOfMass;

    public bool useGlobalSettings;
    public float suspensionLength;
    public float suspensionSpringConstant;
    public float suspensionSpringDamping;

    public float sideTractionMultiplier;
    public float dragMultiplier;

    public float moveSpeed;
    public float turnSpeed;

    public Suspension[] suspensions;
    public float skinWidth;
    private Rigidbody carBody;
    private BoxCollider boxCollider;

    private int wheelsGrounded;

    // Start is called before the first frame update
    void Start()
    {
        carBody = GetComponent<Rigidbody>();
        carBody.centerOfMass = centerOfMass.position;
    }

    // Update is called once per frame
    void Update()
    {
        CalculateRaycastPositions();
    }

    private void FixedUpdate()
    {
        CalculateSpringCompression();
        if (wheelsGrounded > 2)
        {
            carBody.AddForceAtPosition(moveSpeed * Input.GetAxisRaw("Vertical") * transform.forward, forceApply.position);
        }
        carBody.AddTorque(turnSpeed * Input.GetAxisRaw("Horizontal") * Mathf.Abs(Input.GetAxisRaw("Vertical")) * transform.up);
        CalculateTraction();


        if(Input.GetKeyDown(KeyCode.Space))
        {
            carBody.AddForceAtPosition(10*Vector3.up, carBody.position + Random.onUnitSphere, ForceMode.Impulse);
        }
    }

    void CalculateRaycastPositions()
    {
        if (!boxCollider) boxCollider = GetComponent<BoxCollider>();
        Vector3 center = boxCollider.center;
        Vector3 size = boxCollider.size;
        Debug.Log(suspensions.Length);
        if(suspensions.Length == 4)
        {
            suspensions[0].rayCastPosition = transform.TransformPoint(center + (skinWidth - 0.5f * size.y) * Vector3.up - 0.5f * size.x * Vector3.right + 0.5f * size.z * Vector3.forward);
            suspensions[1].rayCastPosition = transform.TransformPoint(center + (skinWidth - 0.5f * size.y) * Vector3.up + 0.5f * size.x * Vector3.right + 0.5f * size.z * Vector3.forward);
            suspensions[2].rayCastPosition = transform.TransformPoint(center + (skinWidth - 0.5f * size.y) * Vector3.up - 0.5f * size.x * Vector3.right - 0.5f * size.z * Vector3.forward);
            suspensions[3].rayCastPosition = transform.TransformPoint(center + (skinWidth - 0.5f * size.y) * Vector3.up + 0.5f * size.x * Vector3.right - 0.5f * size.z * Vector3.forward);
        }
        else
        {
            Debug.LogError("the system supports only 4 suspensions on the controller.");
        }
    }

    void CalculateTraction()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(carBody.velocity);
        float localVelocityX = -sideTractionMultiplier*localVelocity.x;
        float localVelocityZ = -dragMultiplier*localVelocity.z;
        carBody.AddRelativeForce(localVelocityX * Vector3.right,ForceMode.Acceleration);
        Vector3 dippedVector = transform.forward;
        Vector3 crossGroundNormal = Vector3.Cross(dippedVector, Vector3.up);
        Vector3 crossRightGround = Vector3.Cross(Vector3.up, crossGroundNormal);
        carBody.AddForceAtPosition(localVelocityZ * crossRightGround,forceApply.position, ForceMode.Acceleration);
    }

    void CalculateSpringCompression()
    {
        wheelsGrounded = 0;
        foreach (var sus in suspensions)
        {
            if(Physics.Raycast(new Ray(sus.rayCastPosition,-transform.up),out RaycastHit hit,sus.springLength))
            {
                float force = sus.CalculateCompressionForce(hit,carBody.GetPointVelocity(sus.rayCastPosition));
                carBody.AddForceAtPosition(-force * transform.up, sus.rayCastPosition,ForceMode.Acceleration);
                wheelsGrounded++;
            }
        }
    }


    private void OnDrawGizmos()
    {
        CalculateRaycastPositions();

        Gizmos.color = Color.red;
        for(int i=0;i<suspensions.Length;i++)
        {
            Gizmos.DrawRay(suspensions[i].rayCastPosition, -suspensions[i].springLength * transform.up);
        }
    }

    private void OnValidate()
    {
        if(useGlobalSettings)
        {
            for(int i=0;i<suspensions.Length;i++)
            {
                suspensions[i].SetParams(suspensionSpringConstant, suspensionSpringDamping, suspensionLength);
            }
        }
    }
}

[System.Serializable]
public struct Suspension
{
    public float springLength;
    public float springConstant;
    public float springDamping;

    public float compression { get; private set; }
    public Vector3 rayCastPosition { get; set; }

    public float CalculateCompressionForce(RaycastHit hitPoint,Vector3 velocityAtPoint)
    {
        compression = springLength - hitPoint.distance;
        float force = -springConstant * compression - springDamping * velocityAtPoint.y;
        return force;
    }

    public void SetParams(float springConstant, float springDamping,float springLength)
    {
        this.springConstant = springConstant;
        this.springDamping = springDamping;
        this.springLength= springLength;
    }
}
