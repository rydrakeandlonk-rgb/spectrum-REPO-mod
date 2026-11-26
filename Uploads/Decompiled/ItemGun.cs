using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ItemGun : MonoBehaviour
{
	public enum State
	{
		Idle,
		OutOfAmmo,
		Buildup,
		Shooting,
		Reloading
	}

	private PhysGrabObject physGrabObject;

	private ItemToggle itemToggle;

	private ItemGunRevolver itemGunRevolver;

	public bool hasOneShot = true;

	public float shootTime = 1f;

	public bool hasBuildUp;

	public float buildUpTime = 1f;

	public int numberOfBullets = 1;

	[Range(0f, 65f)]
	public float gunRandomSpread;

	public float gunRange = 50f;

	public float distanceKeep = 0.8f;

	public float gunRecoilForce = 1f;

	public float cameraShakeMultiplier = 1f;

	public float cameraImpactMultiplier = 1f;

	public float torqueMultiplier = 1f;

	public float grabStrengthMultiplier = 1f;

	public float shootCooldown = 1f;

	public float batteryDrain = 0.1f;

	public bool batteryDrainFullBar;

	public int batteryDrainFullBars = 1;

	[Range(0f, 100f)]
	public float misfirePercentageChange = 50f;

	public AnimationCurve shootLineWidthCurve;

	public float grabVerticalOffset = -0.2f;

	public float aimVerticalOffset = -5f;

	public float investigateRadius = 20f;

	private float investigateCooldown;

	public Transform gunMuzzle;

	public GameObject bulletPrefab;

	public GameObject muzzleFlashPrefab;

	public Transform gunTrigger;

	internal HurtCollider hurtCollider;

	public Sound soundShoot;

	public Sound soundShootGlobal;

	public Sound soundNoAmmoClick;

	public Sound soundHit;

	private readonly RaycastHit[] aimHits = new RaycastHit[16];

	private Vector3 aimCorrection;

	private ItemBattery itemBattery;

	private PhotonView photonView;

	private PhysGrabObjectImpactDetector impactDetector;

	private bool prevToggleState;

	private AnimationCurve triggerAnimationCurve;

	private float triggerAnimationEval;

	private bool triggerAnimationActive;

	public UnityEvent onStateIdleStart;

	public UnityEvent onStateIdleUpdate;

	public UnityEvent onStateIdleFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateOutOfAmmoStart;

	public UnityEvent onStateOutOfAmmoUpdate;

	public UnityEvent onStateOutOfAmmoFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateBuildupStart;

	public UnityEvent onStateBuildupUpdate;

	public UnityEvent onStateBuildupFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateShootingStart;

	public UnityEvent onStateShootingUpdate;

	public UnityEvent onStateShootingFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateReloadingStart;

	public UnityEvent onStateReloadingUpdate;

	public UnityEvent onStateReloadingFixedUpdate;

	private bool hasIdleUpdate = true;

	private bool hasIdleFixedUpdate = true;

	private bool hasOutOfAmmoUpdate = true;

	private bool hasOutOfAmmoFixedUpdate = true;

	private bool hasBuildupUpdate = true;

	private bool hasBuildupFixedUpdate = true;

	private bool hasShootingUpdate = true;

	private bool hasShootingFixedUpdate = true;

	private bool hasReloadingUpdate = true;

	private bool hasReloadingFixedUpdate = true;

	private RoomVolumeCheck roomVolumeCheck;

	internal float stateTimer;

	internal float stateTimeMax;

	internal State stateCurrent;

	private State statePrev;

	private bool stateStart;

	private ItemEquippable itemEquippable;

	private void Awake()
	{
		itemGunRevolver = ((Component)this).GetComponent<ItemGunRevolver>();
		roomVolumeCheck = ((Component)this).GetComponent<RoomVolumeCheck>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
	}

	private void Start()
	{
		triggerAnimationCurve = AssetManager.instance.animationCurveClickInOut;
		if (onStateIdleUpdate == null)
		{
			hasIdleUpdate = false;
		}
		if (onStateIdleFixedUpdate == null)
		{
			hasIdleFixedUpdate = false;
		}
		if (onStateOutOfAmmoUpdate == null)
		{
			hasOutOfAmmoUpdate = false;
		}
		if (onStateOutOfAmmoFixedUpdate == null)
		{
			hasOutOfAmmoFixedUpdate = false;
		}
		if (onStateBuildupUpdate == null)
		{
			hasBuildupUpdate = false;
		}
		if (onStateBuildupFixedUpdate == null)
		{
			hasBuildupFixedUpdate = false;
		}
		if (onStateShootingUpdate == null)
		{
			hasShootingUpdate = false;
		}
		if (onStateShootingFixedUpdate == null)
		{
			hasShootingFixedUpdate = false;
		}
		if (onStateReloadingUpdate == null)
		{
			hasReloadingUpdate = false;
		}
		if (onStateReloadingFixedUpdate == null)
		{
			hasReloadingFixedUpdate = false;
		}
	}

	private void FixedUpdate()
	{
		StateMachine(_fixedUpdate: true);
		AimTargetUpdate();
	}

	private void Update()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		StateMachine(_fixedUpdate: false);
		if (physGrabObject.grabbed)
		{
			if (physGrabObject.grabbedLocal)
			{
				PhysGrabber.instance.OverrideGrabDistance(distanceKeep);
			}
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				item.OverrideDisableGrabLookAt(0.2f);
			}
		}
		if (triggerAnimationActive)
		{
			float num = 45f;
			triggerAnimationEval += Time.deltaTime * 4f;
			gunTrigger.localRotation = Quaternion.Euler(num * triggerAnimationCurve.Evaluate(triggerAnimationEval), 0f, 0f);
			if (triggerAnimationEval >= 1f)
			{
				gunTrigger.localRotation = Quaternion.Euler(0f, 0f, 0f);
				triggerAnimationActive = false;
				triggerAnimationEval = 1f;
			}
		}
		UpdateMaster();
	}

	private void UpdateMaster()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || physGrabObject.playerGrabbing.Count <= 0)
		{
			return;
		}
		Quaternion turnX = Quaternion.Euler(aimVerticalOffset, 0f, 0f);
		Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
		Quaternion identity = Quaternion.identity;
		bool flag = false;
		bool flag2 = true;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (flag2)
			{
				flag2 = false;
			}
			if (item.isRotating)
			{
				flag = true;
			}
		}
		if (!flag && (stateCurrent == State.OutOfAmmo || !Object.op_Implicit((Object)(object)gunMuzzle)))
		{
			physGrabObject.TurnXYZ(turnX, turnY, identity);
		}
		float pos = grabVerticalOffset;
		physGrabObject.OverrideGrabVerticalPosition(pos);
		if (!flag)
		{
			if (stateCurrent == State.OutOfAmmo)
			{
				physGrabObject.OverrideTorqueStrength(0.01f);
				physGrabObject.OverrideExtraTorqueStrengthDisable();
				physGrabObject.OverrideExtraGrabStrengthDisable();
			}
			else if (physGrabObject.grabbed)
			{
				physGrabObject.OverrideTorqueStrength(12f);
				physGrabObject.OverrideAngularDrag(20f);
			}
		}
		if (flag)
		{
			physGrabObject.OverrideTorqueStrength(2f);
			physGrabObject.OverrideAngularDrag(20f);
		}
	}

	private void AimTargetReset()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		aimCorrection = Vector3.zero;
	}

	private void OnDisable()
	{
		AimTargetReset();
	}

	private void AimTargetUpdate()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		aimCorrection = Vector3.zero;
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !Object.op_Implicit((Object)(object)physGrabObject) || !Object.op_Implicit((Object)(object)gunMuzzle) || !Object.op_Implicit((Object)(object)physGrabObject.rb) || physGrabObject.rb.isKinematic || physGrabObject.dead || physGrabObject.playerGrabbing.Count == 0 || stateCurrent == State.OutOfAmmo)
		{
			AimTargetReset();
			return;
		}
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (!Object.op_Implicit((Object)(object)item) || item.isRotating)
			{
				AimTargetReset();
				return;
			}
		}
		bool flag = physGrabObject.playerGrabbing.Count == 1;
		Rigidbody rb = physGrabObject.rb;
		Quaternion muzzleRotation = Quaternion.Inverse(((Component)this).transform.rotation) * gunMuzzle.rotation;
		Vector3 muzzleOffset = Vector3.Scale(((Component)this).transform.InverseTransformPoint(gunMuzzle.position), ((Component)this).transform.lossyScale);
		foreach (PhysGrabber item2 in physGrabObject.playerGrabbing)
		{
			if (!Object.op_Implicit((Object)(object)item2.playerAvatar) || item2.playerAvatar.isDisabled || item2.playerAvatar.deadSet || !Object.op_Implicit((Object)(object)item2.playerAvatar.localCamera) || item2.physGrabForcesDisabledTimer > 0f)
			{
				AimTargetReset();
				continue;
			}
			Transform overrideTransform = item2.playerAvatar.localCamera.GetOverrideTransform();
			Vector3 direction = item2.physGrabPointPullerPosition - overrideTransform.position;
			if (direction.sqrMagnitude < 0.0001f)
			{
				direction = overrideTransform.forward;
			}
			direction.Normalize();
			Vector3 aimPosition = AimPositionGet(item2, overrideTransform.position, direction, rb.position, muzzleOffset.magnitude);
			Quaternion val = AimRotationGet(rb.position, aimPosition, overrideTransform.up, muzzleOffset, muzzleRotation);
			if (flag)
			{
				val = AimGravityRotationGet(item2, val);
			}
			Quaternion val2 = Quaternion.Inverse(item2.GamepadMeleeRotationGet() * item2.physRotation) * Quaternion.Inverse(overrideTransform.rotation) * val;
			item2.cameraRelativeGrabbedForward = val2 * Vector3.forward;
			item2.cameraRelativeGrabbedUp = val2 * Vector3.up;
		}
	}

	private Quaternion AimRotationGet(Vector3 bodyPosition, Vector3 aimPosition, Vector3 up, Vector3 muzzleOffset, Quaternion muzzleRotation)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.Inverse(muzzleRotation);
		Vector3 val2 = val * muzzleOffset;
		Vector3 val3 = aimPosition - bodyPosition;
		float num = Mathf.Sqrt(Mathf.Max(0.0001f, val3.sqrMagnitude - val2.x * val2.x - val2.y * val2.y));
		Vector3 val4 = new Vector3(val2.x, val2.y, num);
		return Quaternion.LookRotation(val3, up) * Quaternion.FromToRotation(val4, Vector3.forward) * val;
	}

	private Vector3 AimPositionGet(PhysGrabber player, Vector3 origin, Vector3 direction, Vector3 bodyPosition, float muzzleDistance)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(gunRange, 1f);
		int num2 = LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) | LayerMask.GetMask(new string[1] { "Enemy" });
		RaycastHit[] array = aimHits;
		int num3 = Physics.RaycastNonAlloc(origin, direction, array, num, num2, (QueryTriggerInteraction)1);
		if (num3 == array.Length)
		{
			array = Physics.RaycastAll(origin, direction, num, num2, (QueryTriggerInteraction)1);
			num3 = array.Length;
		}
		for (int i = 0; i < num3; i++)
		{
			RaycastHit val = array[i];
			if (!((Object)(object)val.rigidbody == (Object)(object)physGrabObject.rb) && !val.transform.IsChildOf(((Component)this).transform) && !val.transform.IsChildOf(((Component)player.playerAvatar).transform) && val.distance < num)
			{
				num = val.distance;
			}
		}
		float num4 = Vector3.Dot(bodyPosition - origin, direction) + muzzleDistance;
		return origin + direction * Mathf.Max(num, num4 + 0.5f);
	}

	private Quaternion AimGravityRotationGet(PhysGrabber player, Quaternion rotation)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		Rigidbody rb = physGrabObject.rb;
		if (!rb.useGravity || !Object.op_Implicit((Object)(object)player.physGrabPoint))
		{
			return rotation;
		}
		Vector3 val = Quaternion.Inverse(rb.rotation) * (player.physGrabPoint.position - rb.worldCenterOfMass);
		Vector3 val2 = Vector3.Cross(rotation * val, Physics.gravity * rb.mass);
		Quaternion val3 = rotation * rb.inertiaTensorRotation;
		Vector3 val4 = Quaternion.Inverse(val3) * val2;
		Vector3 inertiaTensor = rb.inertiaTensor;
		Vector3 val5 = val3 * new Vector3((inertiaTensor.x > 0f) ? (val4.x / inertiaTensor.x) : 0f, (inertiaTensor.y > 0f) ? (val4.y / inertiaTensor.y) : 0f, (inertiaTensor.z > 0f) ? (val4.z / inertiaTensor.z) : 0f);
		if (val5.sqrMagnitude < 0.0001f)
		{
			return rotation;
		}
		Vector3 val6 = val5.normalized * 2f;
		for (int i = 0; i < 8; i++)
		{
			Vector3 val7 = AimTorqueGet(player, rotation, val6);
			Vector3 val8 = val5 - val7;
			if (val8.sqrMagnitude < 0.0001f)
			{
				break;
			}
			Vector3 val9 = (AimTorqueGet(player, rotation, val6 + Vector3.right * 0.1f) - val7) * 10f;
			Vector3 val10 = (AimTorqueGet(player, rotation, val6 + Vector3.up * 0.1f) - val7) * 10f;
			Vector3 val11 = (AimTorqueGet(player, rotation, val6 + Vector3.forward * 0.1f) - val7) * 10f;
			Vector3 val12 = Vector3.Cross(val10, val11);
			float num = Vector3.Dot(val9, val12);
			if (Mathf.Abs(num) < 0.0001f)
			{
				break;
			}
			Vector3 val13 = new Vector3(Vector3.Dot(val8, val12), Vector3.Dot(val9, Vector3.Cross(val8, val11)), Vector3.Dot(val9, Vector3.Cross(val10, val8))) / num;
			val6 = Vector3.ClampMagnitude(val6 + val13, 25f);
		}
		aimCorrection = val6;
		return Quaternion.AngleAxis(val6.magnitude, val6) * rotation;
	}

	private Vector3 AimTorqueGet(PhysGrabber player, Quaternion rotation, Vector3 correction)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.AngleAxis(correction.magnitude, correction) * rotation;
		return physGrabObject.GrabTorqueGet(player, rotation, val * Vector3.forward, val * Vector3.up);
	}

	public void Misfire()
	{
		if (!roomVolumeCheck.inTruck && !physGrabObject.grabbed && !physGrabObject.hasNeverBeenGrabbed && SemiFunc.IsMasterClientOrSingleplayer() && (float)Random.Range(0, 100) < misfirePercentageChange)
		{
			Shoot();
		}
	}

	public bool Shoot()
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)itemGunRevolver))
		{
			return itemGunRevolver.TriggerPull();
		}
		bool flag = false;
		if (itemBattery.batteryLifeInt <= 0)
		{
			flag = true;
		}
		if (Random.Range(0, 10000) == 0)
		{
			flag = false;
		}
		if (flag)
		{
			return false;
		}
		if (hasOneShot)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ShootRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				ShootRPC();
			}
			StateSet(State.Reloading);
		}
		else if (hasBuildUp)
		{
			StateSet(State.Buildup);
		}
		else
		{
			StateSet(State.Shooting);
		}
		return true;
	}

	internal void RevolverClick()
	{
		DryFireEffects(itemToggle.playerTogglePhotonID);
	}

	private void DryFireEffects(int _shooterID)
	{
		soundNoAmmoClick.Play(((Component)this).transform);
		StartTriggerAnimation();
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_shooterID);
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
		{
			SemiFunc.CameraShakeImpact(1f, 0.1f);
		}
	}

	[PunRPC]
	private void DryFireRPC(int _shooterID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			DryFireEffects(_shooterID);
		}
	}

	private void MuzzleFlash()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		Object.Instantiate<GameObject>(muzzleFlashPrefab, gunMuzzle.position, gunMuzzle.rotation, gunMuzzle).GetComponent<ItemGunMuzzleFlash>().ActivateAllEffects();
	}

	private void StartTriggerAnimation()
	{
		triggerAnimationActive = true;
		triggerAnimationEval = 0f;
	}

	[PunRPC]
	public void ShootRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		SemiFunc.CameraShakeImpactDistance(gunMuzzle.position, 5f * cameraShakeMultiplier * cameraImpactMultiplier, 0.1f, 3f, 16f);
		SemiFunc.CameraShakeDistance(gunMuzzle.position, 0.1f * cameraShakeMultiplier, 0.1f * cameraShakeMultiplier, 3f, 16f);
		soundShoot.Play(gunMuzzle.position);
		soundShootGlobal.Play(gunMuzzle.position);
		MuzzleFlash();
		StartTriggerAnimation();
		if (Object.op_Implicit((Object)(object)itemGunRevolver))
		{
			itemGunRevolver.ShootEffects();
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (investigateRadius > 0f)
		{
			EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigateRadius);
		}
		if (Object.op_Implicit((Object)(object)itemGunRevolver))
		{
			itemGunRevolver.RecoilSuppress();
		}
		physGrabObject.rb.AddForceAtPosition(-gunMuzzle.forward * gunRecoilForce, gunMuzzle.position, (ForceMode)1);
		if (Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
		{
			SemiFunc.PlayerShopKeeperShotGunHost(physGrabObject.lastPlayerGrabbing);
		}
		if (!batteryDrainFullBar)
		{
			itemBattery.batteryLife -= batteryDrain;
		}
		else
		{
			itemBattery.RemoveFullBar(batteryDrainFullBars);
		}
		RaycastHit val4 = default;
		for (int i = 0; i < numberOfBullets; i++)
		{
			Vector3 endPosition = gunMuzzle.position;
			bool hit = false;
			bool flag = false;
			Vector3 val = gunMuzzle.forward;
			if (gunRandomSpread > 0f)
			{
				float num = Random.Range(0f, gunRandomSpread / 2f);
				float num2 = Random.Range(0f, 360f);
				Vector3 val2 = Vector3.Cross(val, Random.onUnitSphere);
				Vector3 normalized = val2.normalized;
				Quaternion val3 = Quaternion.AngleAxis(num, normalized);
				val = Quaternion.AngleAxis(num2, val) * val3 * val;
				val = val.normalized;
			}
			bool flag2 = Physics.Raycast(gunMuzzle.position, val, ref val4, gunRange, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()) + LayerMask.GetMask(new string[1] { "Enemy" }));
			if (flag2)
			{
				endPosition = val4.point;
				hit = true;
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				endPosition = gunMuzzle.position + gunMuzzle.forward * gunRange;
				hit = true;
			}
			ShootBullet(endPosition, hit, flag2);
		}
	}

	private void ShootBullet(Vector3 _endPosition, bool _hit, bool _surfaceHit)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			int num = (Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing) ? physGrabObject.lastPlayerGrabbing.photonView.ViewID : (-1));
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ShootBulletRPC", (RpcTarget)0, new object[4] { _endPosition, _hit, num, _surfaceHit });
			}
			else
			{
				ShootBulletRPC(_endPosition, _hit, num, _surfaceHit);
			}
		}
	}

	[PunRPC]
	public void ShootBulletRPC(Vector3 _endPosition, bool _hit, int _shooterID, bool _surfaceHit, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			if (!Object.op_Implicit((Object)(object)physGrabObject))
			{
				physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
			}
			if (physGrabObject.playerGrabbing.Count > 1 && physGrabObject.grabbedLocal)
			{
				PlayerAvatar.instance.physGrabber.OverrideGrabRelease(photonView.ViewID);
			}
			GameObject val = Object.Instantiate<GameObject>(bulletPrefab, gunMuzzle.position, gunMuzzle.rotation);
			ItemGunBullet component = val.GetComponent<ItemGunBullet>();
			component.hitPosition = _endPosition;
			component.bulletHit = _hit;
			component.surfaceHit = _surfaceHit;
			hurtCollider = ((Component)component).GetComponentInChildren<HurtCollider>(true);
			soundHit.Play(_endPosition);
			component.shootLineWidthCurve = shootLineWidthCurve;
			component.ActivateAll();
			if (Object.op_Implicit((Object)(object)itemGunRevolver))
			{
				itemGunRevolver.ShootRicochet(val.transform.position, _endPosition);
			}
			if (Object.op_Implicit((Object)(object)hurtCollider))
			{
				PlayerAvatar playerCausingHurt = ((_shooterID >= 0) ? SemiFunc.PlayerAvatarGetFromPhotonID(_shooterID) : physGrabObject.lastPlayerGrabbing);
				hurtCollider.playerCausingHurt = playerCausingHurt;
				hurtCollider.playerHitFullRuckus = true;
			}
		}
	}

	private void StateSet(State _state)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (_state == stateCurrent)
		{
			return;
		}
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { (int)_state });
			}
		}
		else
		{
			StateSetRPC((int)_state);
		}
	}

	private void ShootLogic()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || itemToggle.toggleState == prevToggleState)
		{
			return;
		}
		if (!Shoot())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DryFireRPC", (RpcTarget)0, new object[1] { itemToggle.playerTogglePhotonID });
			}
			else
			{
				DryFireRPC(itemToggle.playerTogglePhotonID);
			}
			physGrabObject.rb.AddForceAtPosition(-gunMuzzle.forward * 1f, gunMuzzle.position, (ForceMode)1);
		}
		prevToggleState = itemToggle.toggleState;
	}

	[PunRPC]
	private void StateSetRPC(int state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			stateStart = true;
			statePrev = stateCurrent;
			stateCurrent = (State)state;
		}
	}

	private void StateMachine(bool _fixedUpdate)
	{
		switch (stateCurrent)
		{
		case State.Idle:
			StateIdle(_fixedUpdate);
			break;
		case State.OutOfAmmo:
			StateOutOfAmmo(_fixedUpdate);
			break;
		case State.Buildup:
			StateBuildup(_fixedUpdate);
			break;
		case State.Shooting:
			StateShooting(_fixedUpdate);
			break;
		case State.Reloading:
			StateReloading(_fixedUpdate);
			break;
		}
	}

	private void StateIdle(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateIdleStart != null)
			{
				onStateIdleStart.Invoke();
			}
			stateStart = false;
			prevToggleState = itemToggle.toggleState;
		}
		if (!_fixedUpdate)
		{
			if (itemBattery.batteryLifeInt <= 0 && SemiFunc.IsMasterClientOrSingleplayer())
			{
				StateSet(State.OutOfAmmo);
				return;
			}
			ShootLogic();
			if (hasIdleUpdate)
			{
				onStateIdleUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasIdleFixedUpdate)
		{
			onStateIdleFixedUpdate.Invoke();
		}
	}

	private void StateOutOfAmmo(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateOutOfAmmoStart != null)
			{
				onStateOutOfAmmoStart.Invoke();
			}
			stateStart = false;
			prevToggleState = itemToggle.toggleState;
		}
		if (!_fixedUpdate)
		{
			if (itemBattery.batteryLifeInt > 0)
			{
				StateSet(State.Idle);
				return;
			}
			ShootLogic();
			if (hasOutOfAmmoUpdate)
			{
				onStateOutOfAmmoUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasOutOfAmmoFixedUpdate)
		{
			onStateOutOfAmmoFixedUpdate.Invoke();
		}
	}

	private void StateBuildup(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateBuildupStart != null)
			{
				onStateBuildupStart.Invoke();
			}
			stateTimer = 0f;
			stateTimeMax = buildUpTime;
			stateStart = false;
		}
		if (!_fixedUpdate)
		{
			if (hasBuildupUpdate)
			{
				onStateBuildupUpdate.Invoke();
			}
			stateTimer += Time.deltaTime;
			if (Object.op_Implicit((Object)(object)itemEquippable) && itemEquippable.isEquipped)
			{
				StateSet(State.Idle);
			}
			if (stateTimer >= stateTimeMax && itemBattery.batteryLifeInt > 0)
			{
				StateSet(State.Shooting);
			}
		}
		if (_fixedUpdate && hasBuildupFixedUpdate)
		{
			onStateBuildupFixedUpdate.Invoke();
		}
	}

	private void StateShooting(bool _fixedUpdate)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !_fixedUpdate)
		{
			stateStart = false;
			if (onStateShootingStart != null)
			{
				onStateShootingStart.Invoke();
			}
			if (!hasOneShot)
			{
				stateTimeMax = shootTime;
				stateTimer = 0f;
				investigateCooldown = 0f;
			}
			else
			{
				stateTimer = 0.001f;
			}
			if (itemBattery.batteryLifeInt > 0)
			{
				itemBattery.RemoveFullBar(1);
			}
		}
		if (!_fixedUpdate)
		{
			if (investigateRadius > 0f)
			{
				if (investigateCooldown <= 0f)
				{
					EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigateRadius);
					investigateCooldown = 0.5f;
				}
				else
				{
					investigateCooldown -= Time.deltaTime;
				}
			}
			stateTimer += Time.deltaTime;
			if (stateTimer >= stateTimeMax || (Object.op_Implicit((Object)(object)itemEquippable) && itemEquippable.isEquipped))
			{
				StateSet(State.Reloading);
			}
			if (hasShootingUpdate)
			{
				onStateShootingUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasShootingFixedUpdate)
		{
			onStateShootingFixedUpdate.Invoke();
		}
	}

	private void StateReloading(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			stateStart = false;
			if (onStateReloadingStart != null)
			{
				onStateReloadingStart.Invoke();
			}
			stateTimeMax = shootCooldown;
			stateTimer = 0f;
		}
		if (!_fixedUpdate)
		{
			stateTimer += Time.deltaTime;
			if (stateTimer >= stateTimeMax)
			{
				if (itemBattery.batteryLifeInt > 0)
				{
					StateSet(State.Idle);
				}
				else
				{
					StateSet(State.OutOfAmmo);
				}
			}
			if (hasReloadingUpdate)
			{
				onStateReloadingUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasReloadingFixedUpdate)
		{
			onStateReloadingFixedUpdate.Invoke();
		}
	}
}
