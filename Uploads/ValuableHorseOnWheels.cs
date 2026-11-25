using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableHorseOnWheels : Trap
{
	public enum State
	{
		Inactive,
		Idle,
		MoveForward
	}

	[Header("Animation")]
	public Transform wheel1;

	public Transform wheel2;

	public Transform wheel3;

	public Transform wheel4;

	private float wheelSpeed = 720f;

	[Header("Movement")]
	public Transform centerTransform;

	private float targetSpeed = 4f;

	private float sidewaysGrip = 0.2f;

	private Vector3 boxSize = new Vector3(0.16f, 0.1f, 0.42f);

	[Header("Colliders")]
	public List<Collider> horseColliders;

	public PhysicsMaterial defaultPhysicMaterial;

	public PhysicsMaterial movingPhysicMaterial;

	private PhysGrabObjectImpactDetector impactDetector;

	private bool stuck;

	private float stuckTime;

	private Vector3 previousPosition;

	private State currentState;

	private bool stateImpulse;

	private float stateTimer;

	private float playerNearTimer;

	public Sound soundMove;

	protected override void Start()
	{
		base.Start();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		currentState = State.Inactive;
	}

	protected override void Update()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		base.Update();
		soundMove.PlayLoop(currentState == State.MoveForward, 0.8f, 0.8f);
		if (currentState == State.MoveForward)
		{
			Vector3 val = new Vector3(wheelSpeed, 0f, 0f) * Time.deltaTime;
			wheel1.Rotate(val, (Space)1);
			wheel2.Rotate(val, (Space)1);
			wheel3.Rotate(val, (Space)1);
			wheel4.Rotate(val, (Space)1);
		}
		StateMachine(_fixedUpdate: false);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			if (currentState == State.Inactive)
			{
				return;
			}
			if (impactDetector.inCart || physGrabObject.playerGrabbing.Count > 0)
			{
				UpdateState(State.Idle);
				return;
			}
			bool flag = false;
			Vector3 position = centerTransform.position;
			if (currentState != State.MoveForward && currentState != State.Inactive)
			{
				UpdateState(State.MoveForward);
			}
			Collider[] array = Physics.OverlapBox(position, boxSize / 2f, ((Component)this).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()));
			for (int i = 0; i < array.Length; i++)
			{
				if (!Object.op_Implicit((Object)(object)((Component)array[i]).GetComponentInParent<ValuableHorseOnWheels>()))
				{
					flag = true;
					if (currentState != State.MoveForward && currentState != State.Inactive)
					{
						UpdateState(State.MoveForward);
					}
					break;
				}
			}
			if (!flag)
			{
				UpdateState(State.Idle);
			}
			else
			{
				StuckCheck();
			}
		}
		if (stuck)
		{
			stuckTime += Time.deltaTime;
			if (stuckTime >= 15f)
			{
				UpdateState(State.Inactive);
			}
		}
	}

	public void FixedUpdate()
	{
		StateMachine(_fixedUpdate: true);
	}

	private void StateInactive(bool _fixedUpdate)
	{
		if (!_fixedUpdate && physGrabObject.grabbed)
		{
			UpdateState(State.Idle);
		}
	}

	private void StateIdle(bool _fixedUpdate)
	{
		if (_fixedUpdate || !stateImpulse)
		{
			return;
		}
		foreach (Collider horseCollider in horseColliders)
		{
			if (!((Object)(object)horseCollider == (Object)null))
			{
				horseCollider.material = defaultPhysicMaterial;
			}
		}
		stateImpulse = false;
	}

	private void StateMoveForward(bool _fixedUpdate)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		if (!_fixedUpdate)
		{
			return;
		}
		if (stateImpulse)
		{
			stateTimer = 2f;
			foreach (Collider horseCollider in horseColliders)
			{
				if (!((Object)(object)horseCollider == (Object)null))
				{
					horseCollider.material = movingPhysicMaterial;
				}
			}
			stateImpulse = false;
		}
		stateTimer -= Time.fixedDeltaTime;
		if (stateTimer <= 0f)
		{
			stateTimer = 2.5f;
			if (Random.Range(0f, 1f) > 0.5f)
			{
				TurnHorse();
			}
		}
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float num = Vector3.Dot(linearVelocity, forward);
		float num2 = targetSpeed - num;
		physGrabObject.rb.AddForce(forward * num2 * Time.fixedDeltaTime, (ForceMode)1);
		Vector3 val = linearVelocity - Vector3.Project(linearVelocity, forward);
		val.y = 0f;
		physGrabObject.rb.AddForce(-val * sidewaysGrip, (ForceMode)2);
		Vector3 val2 = new Vector3(0f, Mathf.Sin(Time.time * 10f) * 0.006f, 0f);
		physGrabObject.rb.AddTorque(val2, (ForceMode)1);
		if (playerNearTimer >= 0f)
		{
			playerNearTimer -= Time.deltaTime;
			return;
		}
		playerNearTimer = 1f;
		bool flag = false;
		foreach (PlayerAvatar item in SemiFunc.PlayerGetList())
		{
			if (Vector3.Distance(((Component)item).transform.position, ((Component)this).transform.position) < 25f)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			UpdateState(State.Inactive);
		}
	}

	private void UpdateState(State _state)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && _state != currentState)
		{
			stuck = false;
			stuckTime = 0f;
			currentState = _state;
			stateImpulse = true;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("UpdateStateRPC", (RpcTarget)0, new object[1] { currentState });
			}
			else
			{
				UpdateStateRPC(currentState);
			}
		}
	}

	[PunRPC]
	private void UpdateStateRPC(State _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
		}
	}

	private void StateMachine(bool _fixedUpdate)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			switch (currentState)
			{
			case State.Inactive:
				StateInactive(_fixedUpdate);
				break;
			case State.Idle:
				StateIdle(_fixedUpdate);
				break;
			case State.MoveForward:
				StateMoveForward(_fixedUpdate);
				break;
			}
		}
	}

	private void TurnHorse()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = new Vector3(0f, Random.Range(-1f, 1f) * 0.1f, 0f);
		physGrabObject.rb.AddTorque(val, (ForceMode)1);
	}

	private void StuckCheck()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Inactive && currentState != State.Idle)
		{
			Transform val = (Object.op_Implicit((Object)(object)wheel1) ? wheel1 : ((Component)this).transform);
			Vector3 val2 = val.position - previousPosition;
			float magnitude = val2.magnitude;
			previousPosition = val.position;
			if (magnitude >= 0.2f)
			{
				stuck = false;
				stuckTime = 0f;
			}
			else
			{
				stuck = true;
			}
		}
	}

	public ValuableHorseOnWheels()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
	}
}
