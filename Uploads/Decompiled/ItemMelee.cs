using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ItemMelee : MonoBehaviour
{
	public enum State
	{
		Spawn,
		Idle,
		Swinging,
		Broken,
		Unequipped
	}

	[HideInInspector]
	public State currentState;

	private bool stateStart = true;

	private float stateTimer;

	private float stateTimerMax;

	private bool isFixedUpdate;

	[Tooltip("Enable this if the item uses swing logic but shouldn't deal damage (no hurt collider)")]
	public bool swingLogicOnly;

	public bool useMeleeGrabLead = true;

	[Tooltip("Enable if this weapon uses battery/durability logic")]
	public bool usesBatteryLogic = true;

	[Tooltip("Enable if this weapon has a broken mesh to swap to when durability runs out")]
	public bool hasBrokenMesh = true;

	public float durabilityDrainOnEnemiesAndPVP = 5f;

	public float hitFreeze = 0.2f;

	public float hitFreezeDelay;

	public float cameraImpactMultiplier = 1f;

	public float swingDetectSpeedMultiplier = 1f;

	[Tooltip("Force applied when using scroll wheel to swing")]
	[FormerlySerializedAs("scrollRotateTorque")]
	public float scrollSwingForce = 5f;

	[Tooltip("Forces the weapon to maintain a specific orientation while held")]
	public bool usesForceRotation = true;

	[Tooltip("Forward/backward tilt angle of the weapon when held")]
	public float forwardTilt = 45f;

	[Tooltip("Enable automatic weapon rotation based on swing direction")]
	public bool turnWeapon = true;

	public bool customTorqueStrength;

	public float torqueStrength = 0.4f;

	public bool symmetricalSides = true;

	[Tooltip("Y rotation angle where the hitting edge is facing forward")]
	public float hittingSideAngle;

	[Tooltip("How fast the weapon rotates to follow swing direction")]
	public float turnWeaponStrength = 5f;

	[Tooltip("Custom Y rotation when turnWeapon is disabled")]
	public Quaternion customRotation = Quaternion.identity;

	[Tooltip("Additional orientation offset (X, Y, Z) applied to the weapon")]
	public Vector3 orientationOffset = Vector3.zero;

	[Tooltip("Extra Y rotation degrees applied only to the hard-set resting pose on unequip")]
	public float unequipYRotationOffset;

	public Sound soundSwingLoop;

	public Sound soundSwing;

	public Sound soundHit;

	public Sound soundDink;

	public ParticleSystem particleSystemSwing;

	public ParticleSystem particleSystemDink;

	public UnityEvent onHit;

	private float durabilityDrain = 2.5f;

	private HurtCollider hurtCollider;

	private Transform hurtColliderTransform;

	private Transform hurtColliderRotation;

	private PhysGrabObjectImpactDetector physGrabObjectImpactDetector;

	private PhysGrabObject physGrabObject;

	private Rigidbody rb;

	private float hitBoxTimer = 0.1f;

	private Vector3 prevPosition;

	private float prevPosDistance;

	private float prevPosUpdateTimer;

	private Transform swingPoint;

	private Quaternion swingDirection;

	private PlayerAvatar playerAvatar;

	private float hitSoundDelayTimer;

	private ParticleSystem particleSystem;

	private ParticleSystem particleSystemGroundHit;

	private PhotonView photonView;

	private float swingPitch = 1f;

	private float swingPitchTarget;

	private Vector3 swingLoopPrevPos;

	private Quaternion swingLoopPrevRot;

	private float swingLoopSpeed;

	private float swingRotationSpeed;

	private float distanceCheckTimer;

	private ItemBattery itemBattery;

	private Vector3 swingStartDirection = Vector3.zero;

	private Transform forceGrabPoint;

	private float targetTurnAngle;

	private float currentTurnAngle;

	private Quaternion currentYRotation;

	private float durabilityLossCooldown;

	private Transform meshHealthy;

	private Transform meshBroken;

	private float newSwingCooldown;

	private float swingDisableTimer;

	private float forceSwingTimer;

	private bool newSwing;

	private float hitTimer;

	private ItemEquippable itemEquippable;

	private float hitCooldown;

	private float groundHitCooldown;

	private float groundHitSoundTimer;

	private float grabbedTimer;

	private float enemyOrPVPDurabilityLossCooldown;

	private float scrollRotateUpCooldown;

	private float scrollRotateDownCooldown;

	private float scrollSwingTimer;

	private int scrollSwingDirection;

	private Vector3 scrollSwingAxis = Vector3.zero;

	private bool scrollSwingPhaseTwo;

	private bool wasUnequipping;

	private PlayerAvatar playerAvatarUnequipped;

	private bool playerAvatarUnequippedGet;

	private float wasOwnerUnequipTimer;

	private bool wasGrabbed;

	private Vector3 grabberPrevPos;

	private Vector3 grabPointPrevPos;

	private float spinAccumulator;

	private Vector3 spinPrevUp;

	private Vector3 spinPrevPos;

	private Vector3 spinSwingDir;

	private float spinDirTimer;

	private float lastSwingSoundTime;

	private float swingFeedbackCooldownTimer;

	private float dinkCooldown;

	private PhysGrabObject dinkIgnoreTarget;

	private float dinkIgnoreTargetTimer;

	private Dictionary<PlayerAvatar, float> dinkIgnorePlayerTimers = new Dictionary<PlayerAvatar, float>();

	private List<PlayerAvatar> dinkIgnorePlayersBuffer = new List<PlayerAvatar>();

	private void Start()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		rb = ((Component)this).GetComponent<Rigidbody>();
		physGrabObjectImpactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		swingPoint = ((Component)this).transform.Find("Swing Point");
		swingLoopPrevPos = swingPoint.position;
		swingLoopPrevRot = swingPoint.rotation;
		spinPrevUp = swingPoint.up;
		spinPrevPos = swingPoint.position;
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		particleSystem = ((Component)((Component)this).transform.Find("Particles")).GetComponent<ParticleSystem>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		forceGrabPoint = ((Component)this).transform.Find("Force Grab Point");
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		if (usesBatteryLogic)
		{
			itemBattery = ((Component)this).GetComponent<ItemBattery>();
		}
		if (!swingLogicOnly)
		{
			hurtColliderTransform = ((Component)((Component)this).GetComponentInChildren<HurtCollider>()).transform;
			hurtCollider = ((Component)this).GetComponentInChildren<HurtCollider>();
			hurtCollider.addRuckusOnPhysHit = true;
			hurtColliderRotation = ((Component)this).transform.Find("Hurt Collider Rotation");
			((Component)hurtColliderTransform).gameObject.SetActive(false);
			particleSystemGroundHit = ((Component)((Component)this).transform.Find("Particles Ground Hit")).GetComponent<ParticleSystem>();
			if (hasBrokenMesh)
			{
				meshHealthy = ((Component)this).transform.Find("Mesh Healthy");
				meshBroken = ((Component)this).transform.Find("Mesh Broken");
			}
			if (SemiFunc.RunIsArena() || SemiFunc.RunIsShop())
			{
				HurtCollider component = ((Component)hurtColliderTransform).GetComponent<HurtCollider>();
				component.playerDamage = component.enemyDamage;
			}
		}
		rb.mass = 0.1f;
	}

	private void Update()
	{
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if (LevelGenerator.Instance.Generated)
		{
			if (swingFeedbackCooldownTimer > 0f)
			{
				swingFeedbackCooldownTimer = Mathf.Max(0f, swingFeedbackCooldownTimer - Time.deltaTime);
			}
			if (physGrabObject.grabbed && physGrabObject.playerGrabbing.Count > 0)
			{
				Vector3 position = physGrabObject.playerGrabbing[0].playerAvatar.PlayerVisionTarget.VisionTransform.position;
				Vector3 val = swingPoint.position - position;
				Vector3 val2 = swingLoopPrevPos - grabberPrevPos;
				swingLoopSpeed = Vector3.Distance(val, val2) / Mathf.Max(Time.deltaTime, 0.001f);
				grabberPrevPos = position;
			}
			else
			{
				swingLoopSpeed = Vector3.Distance(swingPoint.position, swingLoopPrevPos) / Mathf.Max(Time.deltaTime, 0.001f);
			}
			swingLoopPrevPos = swingPoint.position;
			swingRotationSpeed = Quaternion.Angle(swingLoopPrevRot, swingPoint.rotation) / Mathf.Max(Time.deltaTime, 0.001f);
			swingLoopPrevRot = swingPoint.rotation;
			if (!swingLogicOnly)
			{
				bool playing = ((Component)hurtColliderTransform).gameObject.activeSelf && swingLoopSpeed > 3f;
				float volumeMultiplier = Mathf.Lerp(0f, 0.2f, Mathf.InverseLerp(3f, 20f, swingLoopSpeed));
				soundSwingLoop.PlayLoop(playing, 3f, 5f, swingPitch, volumeMultiplier);
				SpinSoundLogic();
				HitFreezeTick();
				HurtColliderIgnoreLocalPlayerLogic();
			}
			GrabbedTimerTick();
			EquipLogic();
			DinkIgnoreTick();
			StateMachine();
			if (stateTimer <= stateTimerMax)
			{
				stateTimer += Time.deltaTime;
			}
			if (newSwingCooldown > 0f)
			{
				newSwingCooldown -= Time.deltaTime;
			}
			if (swingDisableTimer > 0f)
			{
				swingDisableTimer -= Time.deltaTime;
			}
			if (forceSwingTimer > 0f)
			{
				forceSwingTimer -= Time.deltaTime;
			}
			if (Object.op_Implicit((Object)(object)physGrabObjectImpactDetector.currentCart) && !physGrabObject.grabbed)
			{
				swingDisableTimer = 0.25f;
			}
		}
	}

	private void FixedUpdate()
	{
		if (!swingLogicOnly)
		{
			DisableHurtBoxWhenEquipping();
		}
		ScrollRotateLogic();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			LowMassAngularDampingLogic();
			ReleaseTorqueLogic();
			isFixedUpdate = true;
			StateMachine();
			isFixedUpdate = false;
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Spawn:
			StateSpawn();
			break;
		case State.Idle:
			StateIdle();
			break;
		case State.Swinging:
			StateSwinging();
			break;
		case State.Broken:
			StateBroken();
			break;
		case State.Unequipped:
			StateUnequipped();
			break;
		}
	}

	public void StateSet(State _newState)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != _newState && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { (int)_newState });
			}
			else
			{
				StateSetRPC((int)_newState);
			}
		}
	}

	[PunRPC]
	private void StateSetRPC(int _newState, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			if (currentState != (State)_newState)
			{
				currentState = (State)_newState;
				stateStart = true;
				stateTimer = 0f;
			}
		}
	}

	public bool IsSwinging()
	{
		return currentState == State.Swinging;
	}

	public bool ForceSwingCanUse()
	{
		if (!swingLogicOnly && currentState != State.Broken && Object.op_Implicit((Object)(object)forceGrabPoint) && Object.op_Implicit((Object)(object)swingPoint))
		{
			return Object.op_Implicit((Object)(object)hurtCollider);
		}
		return false;
	}

	public Transform ForceGrabPointGet()
	{
		return forceGrabPoint;
	}

	public Transform SwingPointGet()
	{
		return swingPoint;
	}

	public Vector3 SwingDirectionGet()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		return swingDirection * Vector3.forward;
	}

	public Collider HurtColliderGet()
	{
		if (!Object.op_Implicit((Object)(object)hurtCollider))
		{
			return null;
		}
		return ((Component)hurtCollider).GetComponent<Collider>();
	}

	public void ForceSwing(float _time = 0.2f)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && ForceSwingCanUse())
		{
			forceSwingTimer = Mathf.Max(forceSwingTimer, _time);
			swingDisableTimer = 0f;
			hurtCollider.ignoreLocalPlayer = false;
			StateSet(State.Swinging);
		}
	}

	public void OverrideSwingDisable(float _time = 0.1f)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			swingDisableTimer = Mathf.Max(swingDisableTimer, _time);
			forceSwingTimer = 0f;
			if (currentState == State.Swinging)
			{
				StateSet(State.Idle);
			}
		}
	}

	private void StateSpawn()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = 3f;
		}
		bool isEquipping = itemEquippable.isEquipping;
		if (!isFixedUpdate)
		{
			prevPosition = swingPoint.position;
			if (stateTimer >= stateTimerMax)
			{
				StateSet(State.Idle);
			}
		}
		if (isFixedUpdate)
		{
			RotatingPlayerLogic();
			GrabVerticalPositionOverride();
			TurnXYZLogic();
			if (!isEquipping)
			{
				PrevPositionUpdateLogic();
				GrabOverridesLogic();
				DistanceCheckLogic();
				TurnWeapon();
			}
		}
	}

	private void StateIdle()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = 999f;
			if (!swingLogicOnly)
			{
				((Component)hurtColliderTransform).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				particleSystemSwing.Stop();
			}
			prevPosition = swingPoint.position;
		}
		bool isEquipping = itemEquippable.isEquipping;
		if (!isFixedUpdate)
		{
			TimersTick();
			if (!swingLogicOnly)
			{
				DisableHurtBoxWhenEquipping();
			}
			if (!isEquipping && usesBatteryLogic)
			{
				BatteryCheckLogic();
			}
		}
		if (isFixedUpdate)
		{
			RotatingPlayerLogic();
			GrabVerticalPositionOverride();
			TurnXYZLogic();
			if (!isEquipping)
			{
				PrevPositionUpdateLogic();
				GrabOverridesLogic();
				DistanceCheckLogic();
				TurnWeapon();
				SwingDetectionLogic();
			}
		}
	}

	private void StateSwinging()
	{
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = 0.2f;
			newSwing = true;
			if (!swingLogicOnly)
			{
				if (Object.op_Implicit((Object)(object)hurtCollider))
				{
					if (SemiFunc.RunIsArena() || SemiFunc.RunIsShop())
					{
						hurtCollider.playerDamage = hurtCollider.enemyDamage;
					}
					else
					{
						hurtCollider.playerDamage = 0;
					}
				}
				ActivateHitbox();
			}
		}
		bool isEquipping = itemEquippable.isEquipping;
		if (!isFixedUpdate)
		{
			TimersTick();
			if (!swingLogicOnly)
			{
				DisableHurtBoxWhenEquipping();
			}
			if (!isEquipping)
			{
				if (SemiFunc.IsMultiplayer() && !SemiFunc.IsMasterClient())
				{
					stateTimer = 0f;
				}
				if (usesBatteryLogic)
				{
					BatteryCheckLogic();
				}
				if (!swingLogicOnly)
				{
					SwingPitchLogic();
					HurtColliderRotationLogic();
					ActivateHitbox();
					HitBoxTimerLogic();
					DinkCheckLogic();
				}
				SwingDirectionCheckLogic();
			}
		}
		if (!isFixedUpdate)
		{
			return;
		}
		RotatingPlayerLogic();
		GrabVerticalPositionOverride();
		TurnXYZLogic();
		if (isEquipping)
		{
			return;
		}
		PrevPositionUpdateLogic();
		GrabOverridesLogic();
		DistanceCheckLogic();
		TurnWeapon();
		SwingDetectionLogic();
		if (stateTimer >= stateTimerMax && forceSwingTimer <= 0f)
		{
			if (!swingLogicOnly && hitBoxTimer <= 0f)
			{
				((Component)hurtColliderTransform).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				particleSystemSwing.Stop();
			}
			StateSet(State.Idle);
		}
	}

	private void StateBroken()
	{
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = 999f;
			if (!swingLogicOnly)
			{
				((Component)hurtColliderTransform).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				particleSystemSwing.Stop();
			}
		}
		if (!isFixedUpdate && SemiFunc.IsMasterClientOrSingleplayer() && usesBatteryLogic && itemBattery.batteryLife > 0f)
		{
			MeleeFix();
		}
		if (isFixedUpdate)
		{
			physGrabObject.OverrideExtraGrabStrengthDisable();
			physGrabObject.OverrideExtraTorqueStrengthDisable();
			physGrabObject.OverrideTorqueStrength(0.001f);
		}
	}

	private void StateUnequipped()
	{
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = 0.025f;
			if (!swingLogicOnly)
			{
				((Component)hurtColliderTransform).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				particleSystemSwing.Stop();
			}
		}
		if (!isFixedUpdate && physGrabObject.playerGrabbing.Count > 0)
		{
			PhysGrabber physGrabber = physGrabObject.playerGrabbing[0];
			if (Object.op_Implicit((Object)(object)physGrabber))
			{
				physGrabber.OverrideGrabDistance(1.2f);
			}
		}
		if (isFixedUpdate)
		{
			GrabOverridesLogic();
			if (stateTimer >= stateTimerMax)
			{
				StateSet(State.Idle);
			}
		}
	}

	private void TimersTick()
	{
		if (hitCooldown > 0f)
		{
			hitCooldown -= Time.deltaTime;
		}
		if (enemyOrPVPDurabilityLossCooldown > 0f)
		{
			enemyOrPVPDurabilityLossCooldown -= Time.deltaTime;
		}
		if (groundHitCooldown > 0f)
		{
			groundHitCooldown -= Time.deltaTime;
		}
		if (groundHitSoundTimer > 0f)
		{
			groundHitSoundTimer -= Time.deltaTime;
		}
		if (hitSoundDelayTimer > 0f)
		{
			hitSoundDelayTimer -= Time.deltaTime;
		}
		if (durabilityLossCooldown > 0f)
		{
			durabilityLossCooldown -= Time.deltaTime;
		}
		if (hitTimer > 0f)
		{
			hitTimer -= Time.deltaTime;
		}
	}

	private void HitFreezeTick()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (hitFreezeDelay > 0f)
		{
			hitFreezeDelay -= Time.deltaTime;
			if (hitFreezeDelay <= 0f)
			{
				physGrabObject.FreezeForces(hitFreeze, Vector3.zero, Vector3.zero);
			}
		}
	}

	private void GrabbedTimerTick()
	{
		if (grabbedTimer > 0f)
		{
			grabbedTimer -= Time.deltaTime;
		}
		if (!physGrabObject.grabbed)
		{
			return;
		}
		grabbedTimer = 1f;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			item.OverrideDisableGrabLookAt(0.2f);
		}
	}

	private void EquipLogic()
	{
		if (itemEquippable.isEquipping)
		{
			newSwingCooldown = 0.1f;
		}
		else if (itemEquippable.isEquipped)
		{
			if (playerAvatarUnequippedGet)
			{
				playerAvatarUnequipped = itemEquippable.GetOwnerPlayerAvatar();
				playerAvatarUnequippedGet = false;
			}
		}
		else
		{
			playerAvatarUnequippedGet = true;
		}
		if (itemEquippable.isUnequipping)
		{
			if (!wasUnequipping)
			{
				if (currentState != State.Broken)
				{
					StateSet(State.Unequipped);
				}
				ResetForFreshUnequip();
				ApplyRestingRotation();
			}
			wasUnequipping = true;
		}
		else
		{
			wasUnequipping = false;
		}
	}

	private void ResetForFreshUnequip()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)rb) && !rb.isKinematic)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		scrollSwingTimer = 0f;
		scrollSwingDirection = 0;
		scrollSwingPhaseTwo = false;
		scrollSwingAxis = Vector3.zero;
		scrollRotateUpCooldown = 0f;
		scrollRotateDownCooldown = 0f;
		spinAccumulator = 0f;
		spinDirTimer = 0f;
		spinSwingDir = Vector3.zero;
		spinPrevUp = swingPoint.up;
		spinPrevPos = swingPoint.position;
		hitTimer = 0f;
		hitCooldown = 0f;
		groundHitCooldown = 0f;
		hitSoundDelayTimer = 0f;
		groundHitSoundTimer = 0f;
		enemyOrPVPDurabilityLossCooldown = 0f;
		durabilityLossCooldown = 0f;
		hitBoxTimer = 0f;
		dinkCooldown = 0f;
		DinkIgnoreClear();
		newSwingCooldown = 0.1f;
		newSwing = false;
		swingStartDirection = Vector3.zero;
		swingPitch = 1f;
		swingPitchTarget = 1f;
		swingLoopSpeed = 0f;
		swingRotationSpeed = 0f;
		swingLoopPrevPos = swingPoint.position;
		swingLoopPrevRot = swingPoint.rotation;
		currentTurnAngle = 0f;
		targetTurnAngle = 0f;
		currentYRotation = Quaternion.identity;
		prevPosition = swingPoint.position;
		prevPosUpdateTimer = 0f;
		prevPosDistance = 0f;
		distanceCheckTimer = 0f;
	}

	private void ApplyRestingRotation()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		if (!usesForceRotation)
		{
			return;
		}
		Transform val = null;
		if (Object.op_Implicit((Object)(object)playerAvatarUnequipped) && Object.op_Implicit((Object)(object)playerAvatarUnequipped.localCamera))
		{
			val = ((Component)playerAvatarUnequipped.localCamera).transform;
		}
		else if (Object.op_Implicit((Object)(object)Camera.main))
		{
			val = ((Component)Camera.main).transform;
		}
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		Quaternion val2 = currentYRotation;
		Quaternion val3;
		if (turnWeapon)
		{
			val3 = Quaternion.Euler(forwardTilt, 0f, 0f);
		}
		else
		{
			val3 = Quaternion.Euler(forwardTilt + orientationOffset.x, orientationOffset.y, orientationOffset.z);
			if (customRotation != Quaternion.identity)
			{
				val2 = customRotation;
			}
		}
		val2 *= Quaternion.Euler(0f, unequipYRotationOffset, 0f);
		Vector3 val4 = val2 * Vector3.forward;
		Vector3 val5 = val2 * Vector3.up;
		Vector3 val6 = val3 * val4;
		Vector3 val7 = val3 * val5;
		Vector3 val8 = val.TransformDirection(val6);
		Vector3 val9 = val.TransformDirection(val7);
		Quaternion rotation = Quaternion.LookRotation(val8, val9);
		((Component)physGrabObject).transform.rotation = rotation;
		rb.rotation = rotation;
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				physGrabObject.Teleport(((Component)physGrabObject).transform.position, rotation);
			}
		}
		else
		{
			physGrabObject.Teleport(((Component)physGrabObject).transform.position, rotation);
		}
	}

	[PunRPC]
	private void ApplyRestingRotationRPC(Quaternion _rotation, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClient() && SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)physGrabObject))
		{
			physGrabObject.Teleport(((Component)physGrabObject).transform.position, _rotation);
		}
	}

	private void HitBoxTimerLogic()
	{
		if (stateTimer >= stateTimerMax && hitBoxTimer > 0f)
		{
			hitBoxTimer -= Time.deltaTime;
		}
	}

	private void LowMassAngularDampingLogic()
	{
		if (physGrabObject.grabbed && !rb.isKinematic && !(physGrabObject.massOriginal <= 0.001f) && !(Mathf.Clamp01(rb.mass / physGrabObject.massOriginal) >= 1f))
		{
			physGrabObject.OverrideAngularDrag(20f);
		}
	}

	private void ReleaseTorqueLogic()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		if (!rb.isKinematic)
		{
			if (wasGrabbed && !physGrabObject.grabbed && swingLoopSpeed > 1f)
			{
				rb.angularVelocity = Vector3.zero;
				Vector3 val = swingPoint.position - forceGrabPoint.position;
				Vector3 normalized = val.normalized;
				val = swingPoint.position - swingLoopPrevPos;
				Vector3 normalized2 = val.normalized;
				val = Vector3.Cross(normalized, normalized2);
				Vector3 normalized3 = val.normalized;
				rb.AddTorque(normalized3 * swingLoopSpeed * swingLoopSpeed * 0.2f, (ForceMode)2);
			}
			wasGrabbed = physGrabObject.grabbed;
		}
	}

	private void HurtColliderIgnoreLocalPlayerLogic()
	{
		if (grabbedTimer > 0f && Object.op_Implicit((Object)(object)hurtCollider))
		{
			if (physGrabObject.playerGrabbing.Count == 1 && physGrabObject.playerGrabbing[0].isLocal)
			{
				hurtCollider.ignoreLocalPlayer = true;
			}
			else
			{
				hurtCollider.ignoreLocalPlayer = false;
			}
		}
	}

	private void DisableHurtBoxWhenEquipping()
	{
		if (itemEquippable.isEquipping)
		{
			((Component)hurtColliderTransform).gameObject.SetActive(false);
			stateTimer = stateTimerMax;
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				particleSystemSwing.Stop();
			}
		}
	}

	private void RotatingPlayerLogic()
	{
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isLocal && item.isRotating)
			{
				item.OverrideMinimumGrabDistance(1.2f);
			}
		}
	}

	private void GrabVerticalPositionOverride()
	{
		float pos = -0.2f;
		physGrabObject.OverrideGrabVerticalPosition(pos);
	}

	private void TurnXYZLogic()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (!usesForceRotation)
		{
			return;
		}
		bool flag = false;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isRotating)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Quaternion turnY = currentYRotation;
			Quaternion turnX = ((!turnWeapon) ? Quaternion.Euler(forwardTilt + orientationOffset.x, orientationOffset.y, orientationOffset.z) : Quaternion.Euler(forwardTilt, 0f, 0f));
			physGrabObject.TurnXYZ(turnX, turnY, Quaternion.identity);
		}
	}

	private void PrevPositionUpdateLogic()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (prevPosUpdateTimer > 0.1f)
		{
			prevPosition = swingPoint.position;
			prevPosUpdateTimer = 0f;
		}
		else
		{
			prevPosUpdateTimer += Time.fixedDeltaTime;
		}
	}

	private void GrabOverridesLogic()
	{
		bool flag = false;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isRotating)
			{
				flag = true;
				break;
			}
		}
		float num = ((physGrabObject.massOriginal > 0.001f) ? Mathf.Clamp01(rb.mass / physGrabObject.massOriginal) : 1f);
		float num2 = MeleeStrengthScale(1f);
		bool flag2 = currentState == State.Swinging;
		float num3 = (flag2 ? MeleeStrengthScale(0.5f) : 1f);
		if (!flag && physGrabObject.playerGrabbing.Count > 0)
		{
			physGrabObject.OverrideKnockOutOfGrabDisable();
			float value = (17f + 42f * MeleeStrengthBonus(90f)) * num;
			physGrabObject.OverrideMinGrabStrength(value);
			physGrabObject.OverrideExtraGrabStrengthDisable();
			physGrabObject.OverrideExtraTorqueStrengthDisable();
			float num4 = (customTorqueStrength ? torqueStrength : 0.4f);
			physGrabObject.OverrideTorqueStrength(num4 * num * num2 * num3);
			physGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialSlippery());
			if (!flag2)
			{
				float num5 = (num2 - 1f) * 6f;
				if (num5 > 0f)
				{
					physGrabObject.OverrideAngularDrag(num5);
				}
			}
		}
		if (flag)
		{
			physGrabObject.OverrideMinTorqueStrength(4f * num * num2);
		}
	}

	private float MeleeStrengthScale(float _maxBonus)
	{
		return 1f + _maxBonus * MeleeStrengthBonus(5f);
	}

	private float MeleeStrengthBonus(float _halfPoint)
	{
		if (physGrabObject.playerGrabbing.Count <= 0)
		{
			return 0f;
		}
		float grabStrength = physGrabObject.playerGrabbing[0].grabStrength;
		float num = Mathf.Max(0f, (grabStrength - 1f) / 0.2f);
		return num / (num + _halfPoint);
	}

	private void DistanceCheckLogic()
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (distanceCheckTimer > 0.1f)
		{
			prevPosDistance = Vector3.Distance(prevPosition, swingPoint.position) * 10f * rb.mass;
			distanceCheckTimer = 0f;
		}
		distanceCheckTimer += Time.fixedDeltaTime;
	}

	private void SwingDetectionLogic()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (newSwingCooldown > 0f)
		{
			if (!rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
		else
		{
			if (swingDisableTimer > 0f)
			{
				return;
			}
			Vector3 val = prevPosition - swingPoint.position;
			float num = 0.8f;
			if (!physGrabObject.grabbed)
			{
				num = 0.4f;
			}
			if (val.magnitude > num * swingDetectSpeedMultiplier && swingPoint.position - prevPosition != Vector3.zero)
			{
				if (currentState == State.Idle)
				{
					swingDirection = Quaternion.LookRotation(swingPoint.position - prevPosition);
					StateSet(State.Swinging);
				}
				else if (currentState == State.Swinging)
				{
					stateTimer = 0f;
					swingDirection = Quaternion.LookRotation(swingPoint.position - prevPosition);
				}
			}
		}
	}

	private void BatteryCheckLogic()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && itemBattery.batteryLife <= 0f)
		{
			MeleeBreak();
		}
	}

	private void SwingPitchLogic()
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Lerp(1f, 10f, Mathf.InverseLerp(3f, 20f, swingLoopSpeed));
		float num2 = 0f;
		if (physGrabObject.grabbed && physGrabObject.playerGrabbing.Count > 0)
		{
			Transform visionTransform = physGrabObject.playerGrabbing[0].playerAvatar.PlayerVisionTarget.VisionTransform;
			Vector3 val = swingPoint.position - visionTransform.position;
			Vector3 normalized = val.normalized;
			float num3 = Vector3.Angle(visionTransform.forward, normalized);
			num2 = Mathf.InverseLerp(30f, 120f, num3) * 5f;
		}
		float num4 = swingRotationSpeed * 0.005f;
		float num5 = Mathf.Clamp(num + num2 + num4, 1f, 10f);
		swingPitchTarget = num5 / Mathf.Max(soundSwingLoop.Pitch, 0.01f);
		swingPitch = Mathf.Lerp(swingPitch, swingPitchTarget, 10f * Time.deltaTime);
	}

	internal void SwingFeedback()
	{
		if (!(swingFeedbackCooldownTimer > 0f) && Object.op_Implicit((Object)(object)physGrabObject) && physGrabObject.grabbedLocal && !physGrabObject.dead && !itemEquippable.isEquipped && !itemEquippable.isEquipping && !itemEquippable.isUnequipping && currentState != State.Broken)
		{
			float num = Mathf.Max(0f, cameraImpactMultiplier);
			GameDirector.instance.CameraShake.Shake(0.5f * num, 0.05f);
			GameDirector.instance.CameraImpact.Shake(1.5f * num, 0.05f);
			swingFeedbackCooldownTimer = 0.2f;
		}
	}

	private void SpinSoundLogic()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Swinging)
		{
			spinAccumulator = 0f;
			spinPrevUp = swingPoint.up;
			spinPrevPos = swingPoint.position;
			spinSwingDir = Vector3.zero;
			return;
		}
		Vector3 up = swingPoint.up;
		spinAccumulator += Vector3.Angle(spinPrevUp, up) * (MathF.PI / 180f);
		spinPrevUp = up;
		spinDirTimer += Time.deltaTime;
		if (spinDirTimer >= 0.1f)
		{
			Vector3 val = swingPoint.position - spinPrevPos;
			spinPrevPos = swingPoint.position;
			float num = val.magnitude / spinDirTimer;
			spinDirTimer = 0f;
			if (num > 2f)
			{
				Vector3 normalized = val.normalized;
				if (spinSwingDir != Vector3.zero && Vector3.Dot(spinSwingDir, normalized) <= -0.4f)
				{
					if (Time.time - lastSwingSoundTime <= 0.3f)
					{
						float pitch = soundSwing.Pitch;
						soundSwing.Pitch = pitch * 2f + Random.Range(-0.15f, 0.15f);
						soundSwing.Play(swingPoint);
						soundSwing.Pitch = pitch;
					}
					else
					{
						soundSwing.Play(swingPoint);
					}
					SwingFeedback();
					lastSwingSoundTime = Time.time;
					spinAccumulator = 0f;
				}
				spinSwingDir = normalized;
			}
		}
		if (spinAccumulator >= MathF.PI)
		{
			spinAccumulator -= MathF.PI;
			if (Time.time - lastSwingSoundTime <= 0.3f)
			{
				float pitch2 = soundSwing.Pitch;
				soundSwing.Pitch = pitch2 * 2f + Random.Range(-0.15f, 0.15f);
				soundSwing.Play(swingPoint);
				soundSwing.Pitch = pitch2;
			}
			SwingFeedback();
			lastSwingSoundTime = Time.time;
		}
	}

	private void HurtColliderRotationLogic()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = prevPosition - swingPoint.position;
		if (val.magnitude > 0.1f)
		{
			hurtColliderRotation.LookAt(hurtColliderRotation.position - val, Vector3.up);
			hurtColliderRotation.localEulerAngles = new Vector3(0f, hurtColliderRotation.localEulerAngles.y, 0f);
			hurtColliderRotation.localEulerAngles = new Vector3(0f, Mathf.Round(hurtColliderRotation.localEulerAngles.y / 90f) * 90f, 0f);
		}
	}

	private void SwingDirectionCheckLogic()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = prevPosition - swingPoint.position;
		Vector3 normalized = swingStartDirection.normalized;
		Vector3 normalized2 = val.normalized;
		float num = Vector3.Dot(normalized, normalized2);
		float num2 = 0.85f;
		if (!physGrabObject.grabbed)
		{
			num2 = 0.1f;
		}
		if (num > num2)
		{
			stateTimer = stateTimerMax;
		}
	}

	private void ScrollRotateLogic()
	{
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		if (scrollRotateUpCooldown > 0f)
		{
			scrollRotateUpCooldown -= Time.fixedDeltaTime;
		}
		if (scrollRotateDownCooldown > 0f)
		{
			scrollRotateDownCooldown -= Time.fixedDeltaTime;
		}
		ScrollSwingAnimationTick();
		if (currentState == State.Spawn || currentState == State.Broken || itemEquippable.isEquipping || !physGrabObject.grabbedLocal)
		{
			return;
		}
		PhysGrabber physGrabber = null;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isLocal)
			{
				physGrabber = item;
				break;
			}
		}
		if ((Object)(object)physGrabber == (Object)null)
		{
			return;
		}
		bool isPushingAndChangedRange = physGrabber.isPushingAndChangedRange;
		bool isPullingAndChangedRange = physGrabber.isPullingAndChangedRange;
		if ((isPushingAndChangedRange || isPullingAndChangedRange) && (!isPushingAndChangedRange || !(scrollRotateUpCooldown > 0f)) && (!isPullingAndChangedRange || !(scrollRotateDownCooldown > 0f)))
		{
			float num = 0.2f / MeleeStrengthScale(0.5f);
			if (isPushingAndChangedRange)
			{
				scrollRotateUpCooldown = num;
			}
			if (isPullingAndChangedRange)
			{
				scrollRotateDownCooldown = num;
			}
			Vector3 right = ((Component)physGrabber.playerAvatar.localCamera).transform.right;
			int num2 = (isPushingAndChangedRange ? 1 : (-1));
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ScrollSwingStartRPC", (RpcTarget)2, new object[2] { right, num2 });
			}
			else
			{
				ScrollSwingStartRPC(right, num2);
			}
		}
	}

	private void ScrollSwingAnimationTick()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && !(scrollSwingTimer <= 0f))
		{
			scrollSwingTimer -= Time.fixedDeltaTime;
			if (!scrollSwingPhaseTwo && scrollSwingTimer <= 0.15f)
			{
				scrollSwingPhaseTwo = true;
				Vector3 val = scrollSwingAxis;
				Vector3 val2 = swingPoint.position - forceGrabPoint.position;
				Vector3 val3 = Vector3.Cross(val, val2.normalized) * scrollSwingForce * MeleeStrengthScale(0.3f) * (float)scrollSwingDirection;
				rb.AddForceAtPosition(val3 * rb.mass, swingPoint.position, (ForceMode)1);
			}
			if (scrollSwingTimer <= 0f)
			{
				scrollSwingTimer = 0f;
				scrollSwingDirection = 0;
				scrollSwingPhaseTwo = false;
			}
		}
	}

	[PunRPC]
	private void ScrollSwingStartRPC(Vector3 _torqueAxis, int _direction, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (SemiFunc.IsMultiplayer())
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonPlayer(_info.Sender);
			if (!Object.op_Implicit((Object)(object)playerAvatar) || !physGrabObject.playerGrabbing.Contains(playerAvatar.physGrabber) || !SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView))
			{
				return;
			}
		}
		if ((_direction == -1 || _direction == 1) && !float.IsNaN(_torqueAxis.sqrMagnitude) && !float.IsInfinity(_torqueAxis.sqrMagnitude) && !(_torqueAxis.sqrMagnitude > 1.01f))
		{
			scrollSwingTimer = 0.2f;
			scrollSwingDirection = _direction;
			scrollSwingAxis = _torqueAxis;
			scrollSwingPhaseTwo = false;
			Vector3 val = _torqueAxis;
			Vector3 val2 = swingPoint.position - forceGrabPoint.position;
			Vector3 val3 = Vector3.Cross(val, val2.normalized) * scrollSwingForce * MeleeStrengthScale(0.3f) * (float)(-_direction);
			rb.AddForceAtPosition(val3 * rb.mass, swingPoint.position, (ForceMode)1);
		}
	}

	private void TurnWeapon()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		if (!usesForceRotation || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (customRotation != Quaternion.identity && !turnWeapon)
		{
			Quaternion turnX = Quaternion.Euler(forwardTilt + orientationOffset.x, orientationOffset.y, orientationOffset.z);
			physGrabObject.TurnXYZ(turnX, customRotation, Quaternion.identity);
		}
		if (!turnWeapon)
		{
			return;
		}
		if (physGrabObject.grabbed && !Object.op_Implicit((Object)(object)playerAvatar))
		{
			playerAvatar = ((Component)physGrabObject.playerGrabbing[0]).GetComponent<PlayerAvatar>();
			grabPointPrevPos = physGrabObject.playerGrabbing[0].physGrabPoint.position;
		}
		if (!physGrabObject.grabbed && Object.op_Implicit((Object)(object)playerAvatar))
		{
			playerAvatar = null;
		}
		if (!physGrabObject.grabbed || physGrabObject.playerGrabbing.Count <= 0)
		{
			return;
		}
		Transform visionTransform = physGrabObject.playerGrabbing[0].playerAvatar.PlayerVisionTarget.VisionTransform;
		Vector3 position = physGrabObject.playerGrabbing[0].physGrabPoint.position;
		Vector3 val = position - grabPointPrevPos;
		grabPointPrevPos = position;
		if (val.magnitude > 0.001f)
		{
			Vector3 val2 = visionTransform.InverseTransformDirection(val);
			Vector2 val3 = new Vector2(val2.x, val2.y);
			if (val3.magnitude > 0.001f)
			{
				val3.Normalize();
				if (symmetricalSides)
				{
					if (Vector2.Dot(new Vector2(Mathf.Cos(currentTurnAngle * (MathF.PI / 180f)), Mathf.Sin(currentTurnAngle * (MathF.PI / 180f))), val3) < 0f)
					{
						val3 = -val3;
					}
				}
				else if (val3.y > 0f && Mathf.Abs(val3.y) > Mathf.Abs(val3.x))
				{
					val3 = -val3;
				}
				targetTurnAngle = Mathf.Atan2(val3.y, val3.x) * 57.29578f;
				if (!symmetricalSides)
				{
					targetTurnAngle -= hittingSideAngle;
				}
			}
		}
		float num = Mathf.Clamp01(swingLoopSpeed * 0.05f) * turnWeaponStrength;
		currentTurnAngle = Mathf.LerpAngle(currentTurnAngle, targetTurnAngle, Time.deltaTime * num);
		currentYRotation = Quaternion.Euler(0f, currentTurnAngle, 0f);
	}

	public void SwingHit()
	{
		if (!swingLogicOnly)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				SwingHitRPC(_durabilityLoss: true);
				return;
			}
			photonView.RPC("SwingHitRPC", (RpcTarget)0, new object[1] { true });
		}
	}

	public void EnemySwingHit()
	{
		if (!swingLogicOnly && enemyOrPVPDurabilityLossCooldown <= 0f)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				EnemyOrPVPSwingHitRPC(_playerHit: false);
				return;
			}
			photonView.RPC("EnemyOrPVPSwingHitRPC", (RpcTarget)0, new object[1] { false });
		}
	}

	public void PlayerSwingHit()
	{
		if (swingLogicOnly)
		{
			return;
		}
		PlayerDisarmCheck();
		if (enemyOrPVPDurabilityLossCooldown <= 0f)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				EnemyOrPVPSwingHitRPC(_playerHit: true);
				return;
			}
			photonView.RPC("EnemyOrPVPSwingHitRPC", (RpcTarget)0, new object[1] { true });
		}
	}

	private void PlayerDisarmCheck()
	{
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarLocal();
		if (Object.op_Implicit((Object)(object)playerAvatar) && Object.op_Implicit((Object)(object)playerAvatar.physGrabber) && playerAvatar.physGrabber.grabbed)
		{
			PhysGrabObject grabbedPhysGrabObject = playerAvatar.physGrabber.grabbedPhysGrabObject;
			if (Object.op_Implicit((Object)(object)grabbedPhysGrabObject) && grabbedPhysGrabObject.isMelee && !((Object)(object)grabbedPhysGrabObject == (Object)(object)physGrabObject))
			{
				playerAvatar.physGrabber.OverrideGrabRelease(grabbedPhysGrabObject.photonView.ViewID, 0.2f);
				playerAvatar.physGrabber.OverrideGrabDisable(0.2f);
			}
		}
	}

	[PunRPC]
	public void EnemyOrPVPSwingHitRPC(bool _playerHit)
	{
		if (!usesBatteryLogic)
		{
			return;
		}
		if (_playerHit)
		{
			if (SemiFunc.RunIsArena() || SemiFunc.RunIsShop())
			{
				itemBattery.batteryLife -= durabilityDrainOnEnemiesAndPVP;
				itemBattery.TriggerSmallBatteryDeduction();
			}
		}
		else
		{
			itemBattery.batteryLife -= durabilityDrainOnEnemiesAndPVP;
			itemBattery.TriggerSmallBatteryDeduction();
		}
		enemyOrPVPDurabilityLossCooldown = 0.1f;
	}

	[PunRPC]
	public void SwingHitRPC(bool _durabilityLoss)
	{
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		if (_durabilityLoss)
		{
			if (hitCooldown > 0f || hitSoundDelayTimer > 0f)
			{
				return;
			}
			hitSoundDelayTimer = 0.1f;
			hitCooldown = 0.2f;
		}
		else
		{
			if (groundHitCooldown > 0f || groundHitSoundTimer > 0f)
			{
				return;
			}
			groundHitCooldown = 0.2f;
			groundHitSoundTimer = 0.1f;
			flag = true;
		}
		if (!flag)
		{
			soundHit.Pitch = 1f;
			soundHit.Play(swingPoint);
			particleSystem.Play();
		}
		else
		{
			soundHit.Pitch = 2f;
			soundHit.Play(swingPoint, 0.5f);
			particleSystemGroundHit.Play();
		}
		if (physGrabObject.grabbed && !rb.isKinematic)
		{
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		if (hitBoxTimer > 0.05f)
		{
			hitBoxTimer = 0.05f;
		}
		hitTimer = 0.2f;
		if ((SemiFunc.IsMasterClientOrSingleplayer() & _durabilityLoss) && usesBatteryLogic && durabilityLossCooldown <= 0f && SemiFunc.RunIsLevel())
		{
			itemBattery.batteryLife -= durabilityDrain;
			itemBattery.TriggerSmallBatteryDeduction();
			durabilityLossCooldown = 0.1f;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && hitFreeze > 0f && !flag)
		{
			hitFreezeDelay = 0.06f;
		}
		if (onHit != null)
		{
			onHit.Invoke();
		}
		GameDirector.instance.CameraShake.ShakeDistance(flag ? 1f : 2f, 3f, 10f, swingPoint.position, 0.1f);
		GameDirector.instance.CameraImpact.ShakeDistance(5f * cameraImpactMultiplier, 3f, 10f, swingPoint.position, 0.1f);
	}

	public void GroundHit()
	{
		if (!swingLogicOnly && !(hitTimer > 0f) && !(hitBoxTimer <= 0f))
		{
			if (!SemiFunc.IsMultiplayer())
			{
				SwingHitRPC(_durabilityLoss: false);
				return;
			}
			photonView.RPC("SwingHitRPC", (RpcTarget)0, new object[1] { false });
		}
	}

	private void DinkCheckLogic()
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		if (swingLogicOnly || currentState != State.Swinging || dinkCooldown > 0f || !Object.op_Implicit((Object)(object)hurtCollider) || !((Component)hurtColliderTransform).gameObject.activeSelf || !physGrabObject.grabbedLocal)
		{
			return;
		}
		BoxCollider component = ((Component)hurtCollider).GetComponent<BoxCollider>();
		SphereCollider val = (Object.op_Implicit((Object)(object)component) ? null : ((Component)hurtCollider).GetComponent<SphereCollider>());
		Collider val2 = (Collider)(object)component;
		if (!Object.op_Implicit((Object)(object)val2))
		{
			val2 = (Collider)(object)val;
		}
		if (!Object.op_Implicit((Object)(object)val2))
		{
			return;
		}
		Collider[] array;
		Bounds bounds;
		if (Object.op_Implicit((Object)(object)component))
		{
			Vector3 val3 = ((Component)hurtCollider).transform.TransformPoint(component.center);
			Vector3 val4 = Vector3.Scale(((Component)hurtCollider).transform.lossyScale, component.size);
			array = Physics.OverlapBox(val3, val4 * 0.5f, ((Component)hurtCollider).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPhysGrabObject()), (QueryTriggerInteraction)2);
		}
		else
		{
			bounds = ((Collider)val).bounds;
			Vector3 center = bounds.center;
			float num = ((Component)hurtCollider).transform.lossyScale.x * val.radius;
			array = Physics.OverlapSphere(center, num, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPhysGrabObject()), (QueryTriggerInteraction)2);
		}
		Collider[] array2 = array;
		foreach (Collider val5 in array2)
		{
			if (!((Component)val5).CompareTag("Phys Grab Object"))
			{
				continue;
			}
			PhysGrabObject componentInParent = ((Component)val5).GetComponentInParent<PhysGrabObject>();
			if (Object.op_Implicit((Object)(object)componentInParent) && !((Object)(object)componentInParent == (Object)(object)physGrabObject) && componentInParent.isMelee && componentInParent.playerGrabbing.Count > 0)
			{
				Collider val6 = val2;
				bounds = val5.bounds;
				Vector3 val7 = val6.ClosestPoint(bounds.center);
				bounds = val2.bounds;
				Vector3 val8 = val5.ClosestPoint(bounds.center);
				Vector3 val9 = (val7 + val8) * 0.5f;
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("DinkRPC", (RpcTarget)0, new object[2]
					{
						componentInParent.photonView.ViewID,
						val9
					});
				}
				else
				{
					DinkRPC(componentInParent.photonView.ViewID, val9);
				}
				dinkCooldown = 0.2f;
				break;
			}
		}
	}

	[PunRPC]
	private void DinkRPC(int _targetViewID, Vector3 _contactPoint)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		PhotonView val = PhotonView.Find(_targetViewID);
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		PhysGrabObject component = ((Component)val).GetComponent<PhysGrabObject>();
		if (!Object.op_Implicit((Object)(object)component))
		{
			return;
		}
		ItemMelee component2 = ((Component)val).GetComponent<ItemMelee>();
		if (dinkCooldown <= 0f)
		{
			GameDirector.instance.CameraShake.ShakeDistance(1f, 1f, 5f, _contactPoint, 0.08f);
			GameDirector.instance.CameraImpact.ShakeDistance(3f, 1f, 5f, _contactPoint, 0.05f);
		}
		DinkIgnoreClear();
		if (Object.op_Implicit((Object)(object)hurtCollider) && !hurtCollider.ignoreObjects.Contains(component))
		{
			hurtCollider.ignoreObjects.Add(component);
		}
		dinkIgnoreTarget = component;
		DinkIgnorePlayersAdd(component, hurtCollider);
		dinkIgnoreTargetTimer = 0.2f;
		if (Object.op_Implicit((Object)(object)component2))
		{
			component2.DinkReceive(physGrabObject, _contactPoint);
		}
		if (soundDink != null)
		{
			soundDink.Play(_contactPoint);
		}
		if (Object.op_Implicit((Object)(object)particleSystemDink))
		{
			((Component)particleSystemDink).transform.position = _contactPoint;
			particleSystemDink.Play();
		}
		dinkCooldown = 0.2f;
		if (hitFreeze > 0f)
		{
			Vector3 val2 = component.rb.position - physGrabObject.rb.position;
			if (val2.sqrMagnitude < 0.0001f)
			{
				val2 = swingPoint.position - forceGrabPoint.position;
			}
			val2 = val2.normalized;
			float num = 0.2f;
			float num2 = 0.8f;
			physGrabObject.rb.linearVelocity = Vector3.zero;
			physGrabObject.rb.angularVelocity = Vector3.zero;
			physGrabObject.FreezeForces(hitFreeze, -val2 * num2, Vector3.zero);
			component.FreezeForces(hitFreeze, val2 * num, Vector3.zero);
		}
	}

	internal void DinkReceive(PhysGrabObject _attackerObj, Vector3 _contactPoint)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		bool flag = dinkCooldown > 0f;
		DinkIgnoreClear();
		if (Object.op_Implicit((Object)(object)hurtCollider) && !hurtCollider.ignoreObjects.Contains(_attackerObj))
		{
			hurtCollider.ignoreObjects.Add(_attackerObj);
		}
		dinkIgnoreTarget = _attackerObj;
		DinkIgnorePlayersAdd(_attackerObj, hurtCollider);
		dinkIgnoreTargetTimer = 0.2f;
		if (!flag)
		{
			dinkCooldown = 0.2f;
			((MonoBehaviour)this).StartCoroutine(DinkReceivePlayDelayed(_contactPoint));
		}
	}

	private IEnumerator DinkReceivePlayDelayed(Vector3 _contactPoint)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		yield return (object)new WaitForSeconds(0.2f);
		if (soundDink != null)
		{
			soundDink.Play(_contactPoint);
		}
		if (Object.op_Implicit((Object)(object)particleSystemDink))
		{
			((Component)particleSystemDink).transform.position = _contactPoint;
			particleSystemDink.Play();
		}
	}

	private void DinkIgnorePlayersAdd(PhysGrabObject _obj, HurtCollider _hurtCollider)
	{
		if (!Object.op_Implicit((Object)(object)_hurtCollider))
		{
			return;
		}
		foreach (PhysGrabber item in _obj.playerGrabbing)
		{
			PlayerAvatar playerAvatar = item.playerAvatar;
			if (Object.op_Implicit((Object)(object)playerAvatar))
			{
				if (!_hurtCollider.ignorePlayers.Contains(playerAvatar))
				{
					_hurtCollider.ignorePlayers.Add(playerAvatar);
				}
				dinkIgnorePlayerTimers[playerAvatar] = 0.2f;
			}
		}
	}

	private void DinkIgnoreTick()
	{
		if (dinkCooldown > 0f)
		{
			dinkCooldown -= Time.deltaTime;
		}
		if (dinkIgnoreTargetTimer > 0f)
		{
			dinkIgnoreTargetTimer -= Time.deltaTime;
			if (dinkIgnoreTargetTimer <= 0f)
			{
				if (Object.op_Implicit((Object)(object)dinkIgnoreTarget) && Object.op_Implicit((Object)(object)hurtCollider))
				{
					hurtCollider.ignoreObjects.Remove(dinkIgnoreTarget);
				}
				dinkIgnoreTarget = null;
				dinkIgnoreTargetTimer = 0f;
			}
		}
		if (dinkIgnorePlayerTimers.Count <= 0)
		{
			return;
		}
		dinkIgnorePlayersBuffer.Clear();
		foreach (PlayerAvatar key in dinkIgnorePlayerTimers.Keys)
		{
			dinkIgnorePlayersBuffer.Add(key);
		}
		foreach (PlayerAvatar item in dinkIgnorePlayersBuffer)
		{
			dinkIgnorePlayerTimers[item] -= Time.deltaTime;
			if (dinkIgnorePlayerTimers[item] <= 0f)
			{
				dinkIgnorePlayerTimers.Remove(item);
				if (Object.op_Implicit((Object)(object)item) && Object.op_Implicit((Object)(object)hurtCollider))
				{
					hurtCollider.ignorePlayers.Remove(item);
				}
			}
		}
	}

	private void DinkIgnoreClear()
	{
		if (Object.op_Implicit((Object)(object)dinkIgnoreTarget))
		{
			if (Object.op_Implicit((Object)(object)hurtCollider))
			{
				hurtCollider.ignoreObjects.Remove(dinkIgnoreTarget);
			}
			dinkIgnoreTarget = null;
		}
		if (Object.op_Implicit((Object)(object)hurtCollider))
		{
			foreach (PlayerAvatar key in dinkIgnorePlayerTimers.Keys)
			{
				if (Object.op_Implicit((Object)(object)key))
				{
					hurtCollider.ignorePlayers.Remove(key);
				}
			}
		}
		dinkIgnorePlayerTimers.Clear();
		dinkIgnoreTargetTimer = 0f;
	}

	private void MeleeBreak()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (usesBatteryLogic && currentState != State.Broken)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				MeleeBreakRPC();
			}
			else if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("MeleeBreakRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
	}

	[PunRPC]
	public void MeleeBreakRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && currentState != State.Broken)
		{
			particleSystem.Play();
			physGrabObject.isMelee = false;
			physGrabObject.forceGrabPoint = null;
			if (Object.op_Implicit((Object)(object)itemBattery))
			{
				itemBattery.BatteryToggle(toggle: false);
			}
			if (!swingLogicOnly)
			{
				((Component)hurtColliderTransform).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)particleSystemSwing))
			{
				((Component)particleSystemSwing).transform.SetParent((Transform)null);
				MainModule main = particleSystemSwing.main;
				main.stopAction = (ParticleSystemStopAction)2;
				particleSystemSwing.Stop();
				particleSystemSwing = null;
			}
			if (hasBrokenMesh)
			{
				((Component)meshHealthy).gameObject.SetActive(false);
				((Component)meshBroken).gameObject.SetActive(true);
			}
			GameDirector.instance.CameraShake.ShakeDistance(1f, 1f, 5f, swingPoint.position, 0.1f);
			GameDirector.instance.CameraImpact.ShakeDistance(2f, 1f, 5f, swingPoint.position, 0.05f);
			currentState = State.Broken;
			stateStart = true;
			stateTimer = 0f;
		}
	}

	private void MeleeFix()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (usesBatteryLogic && currentState == State.Broken)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				MeleeFixRPC();
			}
			else if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("MeleeFixRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
	}

	[PunRPC]
	public void MeleeFixRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && currentState == State.Broken)
		{
			particleSystem.Play();
			physGrabObject.isMelee = true;
			physGrabObject.forceGrabPoint = forceGrabPoint;
			if (Object.op_Implicit((Object)(object)itemBattery))
			{
				itemBattery.BatteryToggle(toggle: true);
			}
			if (hasBrokenMesh)
			{
				((Component)meshHealthy).gameObject.SetActive(true);
				((Component)meshBroken).gameObject.SetActive(false);
			}
			currentState = State.Idle;
			stateStart = true;
			stateTimer = 0f;
		}
	}

	public void ActivateHitbox()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (swingLogicOnly || hitTimer > 0f)
		{
			return;
		}
		if (newSwing)
		{
			soundSwing.Play(swingPoint);
			SwingFeedback();
			lastSwingSoundTime = Time.time;
			spinAccumulator = 0f;
			if (Object.op_Implicit((Object)(object)swingPoint))
			{
				swingStartDirection = swingPoint.position - prevPosition;
			}
			hitBoxTimer = 0.2f;
			newSwing = false;
		}
		if (Object.op_Implicit((Object)(object)hurtColliderTransform))
		{
			((Component)hurtColliderTransform).gameObject.SetActive(true);
		}
		if (Object.op_Implicit((Object)(object)particleSystemSwing))
		{
			particleSystemSwing.Play();
		}
	}

	public ItemMelee()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
	}
}
