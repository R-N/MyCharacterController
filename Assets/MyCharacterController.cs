using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MyCharacterController : MonoBehaviour {
	public static float onePerFixedDt = 1.0f/0.02f;
	public static float sqrOnePerFixedDt = 1.0f / 0.0004f;
	public Collider[] cols = null;

	public Rigidbody rigidbody = null;
	public CapsuleCollider collider = null;
	public int colliderInstanceId = -1;

	public LayerMask collisionMask = 1;

	Transform trans = null;
	Vector3 curPos = Vector3.zero;

	public float skinThickness = 0.03f;

	public bool grounded = false;
	bool _grounded = false;
	bool updatingGround = false;


	public float maxSlopeAngle = 30;
	float maxSlopeCos;



	public float groundStickiness = 0;
	public Transform ground = null;
	Vector3 prevGroundPos = Vector3.zero;
	float groundCos = -1;
	Vector3 groundNormal = Vector3.zero;

	float groundFrictionCoef = 0;

	bool didCollisionAfterPhysicsUpdate = false;

	Vector3 up = Vector3.up;

	// Use this for initialization
	void Awake () {
		onePerFixedDt = 1.0f / Time.fixedDeltaTime;
		sqrOnePerFixedDt = onePerFixedDt * onePerFixedDt;
		cols = new Collider[32];
		groundStickiness = Mathf.Clamp (groundStickiness, 0, 1);
		//Debug.Log ("Collision mask : " + (int)collisionMask);
		maxSlopeCos = Mathf.Cos(Mathf.Deg2Rad * maxSlopeAngle);
		trans = transform;
		curPos = trans.position;

		if (rigidbody == null)
			rigidbody = GetComponent<Rigidbody> ();
		if (rigidbody == null)
			rigidbody = gameObject.AddComponent<Rigidbody> ();

		rigidbody.isKinematic = false;
		rigidbody.freezeRotation = true;
		rigidbody.constraints = RigidbodyConstraints.FreezeAll;

		if (collider == null)
			collider = GetComponent<CapsuleCollider> ();
		if (collider == null)
			collider = rigidbody.GetComponent<CapsuleCollider>();
		if (collider == null)
			collider = gameObject.AddComponent<CapsuleCollider> ();
		if (collider != null)
		colliderInstanceId = collider.GetInstanceID ();
	}


	Vector3 Reject(Vector3 vector, Vector3 onNormal){
		return vector - Vector3.Project (vector, onNormal);
	}

	bool CheckGroundMovement(){
		if (grounded && groundStickiness > 0 && ground) {
			Transform g = ground;
			Vector3 deltaPos = ground.position - prevGroundPos;
			if (deltaPos != Vector3.zero) {
				Move (Reject (Reject (deltaPos, groundNormal), Physics.gravity) * groundStickiness);
				return true;
			}
		}
		return false;
	}

	void Update(){
		//CheckGroundMovement ();
		
		Vector3 mv = new Vector3 (Input.GetAxis ("Horizontal"), Input.GetAxis ("Vertical"), 0).normalized;
		Move (mv * 1 * Time.deltaTime);
	}

	void LateUpdate(){

		if (!CheckGroundMovement ())
		CheckCollision ();
	}

	void FixedUpdate(){
		/*if (!didCollisionAfterPhysicsUpdate) {
			if (!CheckGroundMovement ())
				CheckCollision ();
		}*/
		//CheckGroundMovement ();
		didCollisionAfterPhysicsUpdate = false;
	}

	void OnCollisionEnter(Collision col){
		if (!didCollisionAfterPhysicsUpdate) {
			if (!CheckGroundMovement ())
				CheckCollision ();
			didCollisionAfterPhysicsUpdate = true;
		}
	}

	void OnCollisionStay(Collision col){
		if (!didCollisionAfterPhysicsUpdate) {
			if (!CheckGroundMovement ())
				CheckCollision ();
			didCollisionAfterPhysicsUpdate = true;
		}
	}
	void OnCollisionExit(Collision col){
		if (!didCollisionAfterPhysicsUpdate || (grounded && ground != null && col.collider.transform == ground)) {
			if (!CheckGroundMovement ())
				CheckCollision ();
			didCollisionAfterPhysicsUpdate = true;
		}
	}



	public void CheckGround(Vector3 worldNormal, Transform t, Vector3 p){
		if (updatingGround){
			float cos = Vector3.Dot (worldNormal, up);
			if (cos >= maxSlopeCos) {
				_grounded = true;
				grounded = true;
				if (groundStickiness > 0){
					if (cos > groundCos) {
						groundCos = cos;
						ground = t;
						prevGroundPos = p;
						groundNormal = worldNormal;
					}
				}else
					updatingGround = false;
			}
		}
			
	}

	public void Move(Vector3 worldSpaceMovement){
		if (worldSpaceMovement == Vector3.zero)
			return;
		Vector3 pos = trans.position;
		RaycastHit hit;
		float mag = worldSpaceMovement.magnitude;
		Vector3 norm = worldSpaceMovement / mag;
		up = trans.up;
		Vector3 diff = up * collider.height * 0.5f;
		if (Physics.CapsuleCast(pos + diff, pos - diff, collider.radius, norm, out hit, mag, collisionMask, QueryTriggerInteraction.Ignore)){
			float newMag = Mathf.Clamp (hit.distance + skinThickness, 0, mag);
			if (newMag < mag){
				Rigidbody rb = hit.collider.attachedRigidbody;
				if (rb != null && !rb.isKinematic) {
					rb.AddForceAtPosition (norm * (mag - newMag) * rigidbody.mass* GetBounciness (collider.sharedMaterial, hit.collider.sharedMaterial), hit.point);
				}
			}
			worldSpaceMovement = norm * newMag;

		}
		Vector3 newPos = pos + worldSpaceMovement;
		rigidbody.position = newPos;
		trans.position = newPos;
		CheckCollision ();
	}

	public float Abs(float x){
		if (x < 0)
			return -x;
		return x;
	}

	public float Max(float a, float b){
		if (a > b)
			return a;
		return b;
	}
	public float Min(float a, float b){
		if (a < b)
			return a;
		return b;
	}
	public float GetBounciness(PhysicMaterial a, PhysicMaterial b){
		if (a == null) {
			if (b == null) {
				return 1;
			} else {
				return b.bounciness;
			}
		} else if (b == null) {
			return a.bounciness;
		} else {
			switch (b.bounceCombine) {
			case PhysicMaterialCombine.Average:
				{
					return (a.bounciness + b.bounciness) * 0.5f;
					break;
				}
			case PhysicMaterialCombine.Maximum:
				{
					return Max (a.bounciness, b.bounciness);
					break;
				}
			case PhysicMaterialCombine.Minimum:
				{
					return Min (a.bounciness, b.bounciness);
					break;
				}
			case PhysicMaterialCombine.Multiply:
				{
					return a.bounciness * b.bounciness;
					break;
				}
			}
		}
		return 1;
	}

	public void CheckCollision(){
		if (collider.enabled && !collider.isTrigger) {
			Vector3 pos = trans.position;
			Quaternion rot = trans.rotation;
			up = rot * Vector3.up;
			Vector3 diff = up * collider.height * 0.5f;

			int count = Physics.OverlapCapsuleNonAlloc (pos + diff, 
				pos - diff, 
				collider.radius,
				cols,
				collisionMask,
				QueryTriggerInteraction.Ignore);
			if (count > 1) {
				Vector3 sumDepenetration = Vector3.zero;
				Vector3 absSumDepenetration = Vector3.zero;
				updatingGround = true;
				_grounded = false;
				groundCos = -1;
				//int index = System.Array.IndexOf (cols, collider);
				
				for (int i = 0; i < count; i++){
					//if (i != index) {
						Collider c = cols [i];
					if (c.GetInstanceID() != colliderInstanceId){
						Vector3 dir;
						float dist;
						Transform t = c.transform;
						Vector3 p = t.position;
						if (Physics.ComputePenetration (collider, pos, rot, c, p, t.rotation, out dir, out dist)) {
							CheckGround (dir, t, p);

							float newDiff = dist - Mathf.Clamp (dist, 0, skinThickness);
							if (newDiff > 0) {
								dir *= newDiff;

								Rigidbody rb = c.attachedRigidbody;
								if (rb != null && !rb.isKinematic && rb.constraints != RigidbodyConstraints.FreezeAll) {
									rb.AddForce (dir * -1 * rigidbody.mass * GetBounciness (collider.sharedMaterial, c.sharedMaterial));
								} else {
									sumDepenetration = new Vector3 (dir.x == 0 ? 0 : (Abs (sumDepenetration.x) > Abs (dir.x) ? sumDepenetration.x : dir.x),
										dir.y == 0 ? 0 : (Abs (sumDepenetration.y) > Abs (dir.y) ? sumDepenetration.y : dir.y),
										dir.z == 0 ? 0 : (Abs (sumDepenetration.z) > Abs (dir.z) ? sumDepenetration.z : dir.z));
								}
							}
						}
					}
				}
				grounded = _grounded;
				if (!grounded && ground != null) {
					ground = null;
					groundNormal = Vector3.zero;
				}
				updatingGround = false;
				if (sumDepenetration != Vector3.zero) {
					Vector3 newPos = pos + sumDepenetration;
					rigidbody.position = newPos;
					trans.position = newPos;
				}
				//trans.Translate (sumDepenetration, Space.World);
			} 
		}
	}
}
