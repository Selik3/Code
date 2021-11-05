using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	[SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f, maxAirAcceleration = 1f;

	[SerializeField, Range(0f, 10f)]
	float jumpHeight = 2f;

	[SerializeField, Range(0, 5)]
	int maxAirJumps = 0;

	[SerializeField, Range(0, 90)]
	float maxGroundAngle = 25f, maxStairsAngle = 50f;

	int stepsSinceLastGrounded, stepsSinceLastJump;

	[SerializeField, Range(0f, 100f)]
	float maxSnapSpeed = 100f;

	[SerializeField, Min(0f)]
	float probeDistance;

	[SerializeField]
	LayerMask probeMask = -1, stairsMask = -1;

	[SerializeField]
	Transform playerInputSpace = default;

	//my stuff
	[SerializeField]
	Transform bodyObject;
	[SerializeField, Range(0f, 1f)]
	float bodyRotationSpeed = 0.2f;
	//end my stuff

	float GetMinDot (int layer)
    {
		return (stairsMask & (1 << layer)) == 0 ? minGroundDotProduct : minStairsDotProduct;
    }

	bool SnapToGround()
    {
		if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
			return false;
        }
		float speed = velocity.magnitude;
		if (speed > maxSnapSpeed)
		{
			return false;
		}
		if (!Physics.Raycast(body.position, -upAxis, out RaycastHit hit, probeDistance, probeMask))
        {
			return false;
        }
		float upDot = Vector3.Dot(upAxis, hit.normal);
		if (upDot < GetMinDot(hit.collider.gameObject.layer))
        {
			return false;
        }
		groundContactCount = 1;
		contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
		if (dot > 0f)
        {
			velocity = (velocity - hit.normal * dot).normalized * speed;
		}
		return true;
    }

	bool CheckSteepContacts()
    {
		if (steepContactCount > 1)
        {
			steepNormal.Normalize();
			float upDot = Vector3.Dot(upAxis, steepNormal);
			if (upDot >= minGroundDotProduct)
            {
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
            }
        }
		return false;
    }

	Rigidbody body;

	Vector3 velocity, desiredVelocity;

	Vector3 contactNormal, steepNormal;

	bool desiredJump;

	[SerializeField]
	int groundContactCount, steepContactCount;

	bool OnGround => groundContactCount > 0;

	bool OnSteep => steepContactCount > 0;

	int jumpPhase;

	float minGroundDotProduct, minStairsDotProduct;

	Vector3 upAxis, rightAxis, forwardAxis;

	public float vertical;
	public float horizontal;

	public Vector3 lastDirection;

	public float X;
	public float Y;
	public float Z;

	Vector3 movement;

	void OnValidate()
	{
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
		minStairsDotProduct = Mathf.Cos(maxStairsAngle * Mathf.Deg2Rad);
	}

	void Awake()
	{
		body = GetComponent<Rigidbody>();
		OnValidate();
		body.useGravity = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update()
	{
		Vector2 playerInput;
		playerInput.x = Input.GetAxis("Horizontal");
		playerInput.y = Input.GetAxis("Vertical");
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);
		if (playerInputSpace)
		{
			rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
		}
		else
		{
			rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
		}
		desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
		desiredJump |= Input.GetButtonDown("Jump");

	}

	void FixedUpdate()
	{
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
		UpdateState();
		AdjustVelocity();

		if (desiredJump)
		{
			desiredJump = false;
			Jump(gravity);
		}

		velocity += gravity * Time.deltaTime;

		body.velocity = velocity;

		//my stuff
		//horizontal = Input.GetAxisRaw("Horizontal");
		//vertical = Input.GetAxisRaw("Vertical");
		//Vector3 direction = velocity / velocity.magnitude;
		//      Quaternion rotation = Quaternion.LookRotation(direction, transform.up);
		//Quaternion lastrotation = Quaternion.LookRotation(lastDirection, transform.up);
		//      //bodyObject.up = upAxis;
		//      if ( velocity.magnitude >= 0)
		//      {
		//	if (velocity.magnitude < 1)
		//          {
		//		//bodyObject.transform.rotation = Quaternion.Slerp(bodyObject.rotation, lastrotation, bodyRotationSpeed);
		//	}
		//          if (!Input.GetButtonDown("Jump"))
		//          {
		//		lastDirection = direction;
		//          }
		//          if (velocity.magnitude > 1)
		//          {
		//              //lastDirection = direction;
		//              bodyObject.transform.rotation = Quaternion.Slerp(bodyObject.rotation, rotation, bodyRotationSpeed);
		//          }
		//      }
		//      else if (!OnGround)
		//      {
		//	//Quaternion upDirection = Quaternion.LookRotation(upAxis, upAxis);
		//	//upDirection = upDirection * Quaternion.Euler(90f, 0f, 0);
		//	//bodyObject.transform.rotation = Quaternion.Slerp(bodyObject.rotation, upDirection, bodyRotationSpeed);
		//}
		AdjustBody();
		////end my stuff

		ClearState();
	}

	void ClearState()
	{
		groundContactCount = steepContactCount = 0;
		contactNormal = steepNormal = Vector3.zero;
	}

	void UpdateState()
	{
		stepsSinceLastGrounded += 1;
		if (stepsSinceLastJump > 1)
        {
			jumpPhase = 0;
        }
		stepsSinceLastJump += 1;
		velocity = body.velocity;
		if (OnGround || SnapToGround() || CheckSteepContacts())
		{
			stepsSinceLastGrounded = 0;
			jumpPhase = 0;
			if (groundContactCount > 1)
			{
				contactNormal.Normalize();
			}
		}
		else
		{
			contactNormal = upAxis;
		}
	}

	void AdjustBody()
    {
		float x = Input.GetAxisRaw("Horizontal");
		float z = Input.GetAxisRaw("Vertical");
		Vector3 input = new Vector3(x, 0, z);
		if (input.magnitude > 0.5f)
        {
			movement = velocity;
        }
		X = Vector3.Dot(Vector3.right, movement);
		Z = Vector3.Dot(Vector3.forward, movement);
        Y = Vector3.Dot(bodyObject.up, upAxis);

		//this.transform.up = upAxis;
		//bodyObject.transform.up = Vector3.MoveTowards(bodyObject.transform.up, this.transform.up, bodyRotationSpeed);

		

        Vector3 directionface = new Vector3(X, 0, Z);
        directionface = directionface / directionface.magnitude;

		//bodyObject.transform.up = upAxis;

        Quaternion rotationforward = Quaternion.LookRotation(directionface, Vector3.up);
		bodyObject.transform.localRotation = Quaternion.Slerp(bodyObject.rotation, rotationforward, bodyRotationSpeed);


	}

	void AdjustVelocity()
	{
		Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
		Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

		float currentX = Vector3.Dot(velocity, xAxis);
		float currentZ = Vector3.Dot(velocity, zAxis);

		float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
		float maxSpeedChange = acceleration * Time.deltaTime;

		float newX =
			Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
		float newZ =
			Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

		
	}

	void Jump(Vector3 gravity)
	{
		Vector3 jumpDirection;
		if (OnGround)
		{
			jumpDirection = contactNormal;
		}
		else if (OnSteep)
		{
			jumpDirection = steepNormal;
			jumpPhase = 0;
		}
		else if (maxAirJumps > 0 && jumpPhase <= maxAirJumps)
		{
			if (jumpPhase == 0)
            {
				jumpPhase = 1;
            }
			jumpDirection = contactNormal;
		}
		else
		{
			return;
		}
		if (OnGround || jumpPhase < maxAirJumps)
		{
			stepsSinceLastJump = 0;
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
			jumpDirection = (jumpDirection + upAxis).normalized;
			float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
			if (alignedSpeed > 0f)
			{
				jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
			}
			velocity += jumpDirection * jumpSpeed;
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		EvaluateCollision(collision);
	}

	void OnCollisionStay(Collision collision)
	{
		EvaluateCollision(collision);
	}

	void EvaluateCollision(Collision collision)
	{
		float minDot = GetMinDot(collision.gameObject.layer);
		for (int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;
			float upDot = Vector3.Dot(upAxis, normal);
			if (upDot >= minDot)
			{
				groundContactCount += 1;
				contactNormal += normal;
			}
			else if (upDot > -0.01f)
            {
				steepContactCount += 1;
				steepNormal += normal;
            }
		}
	}

	Vector3 ProjectDirectionOnPlane (Vector3 direction, Vector3 normal)
    {
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
    }
}

