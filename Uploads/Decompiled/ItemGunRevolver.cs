using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ItemGunRevolver : MonoBehaviourPunCallbacks
{
	public enum State
	{
		Ready,
		Spinning,
		Settling,
		Recover,
		Indexing
	}

	private enum FlickState
	{
		Ready,
		Sampling,
		Driving,
		Rearming
	}

	internal State currentState;

	private bool stateStart = true;

	private float stateTimer;

	private double motionRemainder;

	private const double motionStep = 1.0 / 240.0;

	public Transform chamberPivot;

	public GameObject[] bullets = new GameObject[6];

	public Transform hammerPivot;

	[Range(1f, 15f)]
	public float alignmentTolerance = 5f;

	public float indexTime = 0.065f;

	public float recoverTime = 0.035f;

	public float settleTime = 0.055f;

	[Range(0f, 0.9f)]
	public float catchStrength = 0.65f;

	public ParticleSystem muzzleSparks;

	public ParticleSystem chamberSparks;

	public Sound soundShootLayer = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundChamberClick = new Sound();

	public Sound soundRicochet = new Sound();

	public float angularAccelerationThreshold = 240f;

	public float angularJerkThreshold = 8000f;

	public float flickAngularSpeed = 10f;

	public float flickAngle = 18f;

	public float angularRattleStrength = 3f;

	public float spinDrag = 600f;

	public float spinSpeedMax = 3300f;

	public float settlingSpeed = 90f;

	private ItemGun itemGun;

	private ItemBattery itemBattery;

	private ItemEquippable itemEquippable;

	private PhysGrabObject physGrabObject;

	private Rigidbody body;

	private Quaternion chamberRotation;

	private Quaternion hammerRotation;

	private Vector3 previousAngularVelocity;

	private Vector3 previousAngularAcceleration;

	private Vector3 previousPosition;

	private Quaternion previousRotation;

	private float rattleSuppressTimer;

	private double shootCooldownTimer;

	private float pendingTriggerTimer;

	private FlickState flickState = FlickState.Rearming;

	private float flickTimer;

	private Quaternion flickStartRotation;

	private Vector3 flickStartAngularVelocity;

	private float flickAngularPeak;

	private Vector3 flickAxis;

	private float flickAngularTravel;

	private float networkTimer;

	private bool networkDirty;

	private int loadedChambers;

	private int previousBatteryRounds = -1;

	private float chamberAngle;

	private float chamberSpeed;

	private float indexStartAngle;

	private float indexTargetAngle;

	private float triggerTimer;

	private const int chamberCount = 6;

	private const float chamberStep = 60f;

	private void Awake()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		itemGun = ((Component)this).GetComponent<ItemGun>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		body = ((Component)this).GetComponent<Rigidbody>();
		chamberRotation = chamberPivot.localRotation;
		hammerRotation = hammerPivot.localRotation;
	}

	public override void OnEnable()
	{
		((MonoBehaviourPunCallbacks)this).OnEnable();
		RattleReset();
		((MonoBehaviour)this).StartCoroutine(SnapshotRequest());
	}

	public override void OnDisable()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviourPunCallbacks)this).OnDisable();
		((MonoBehaviour)this).StopAllCoroutines();
		chamberSpeed = 0f;
		triggerTimer = 0f;
		shootCooldownTimer = 0.0;
		pendingTriggerTimer = 0f;
		currentState = State.Ready;
		stateStart = true;
		motionRemainder = 0.0;
		networkDirty = true;
		if (Object.op_Implicit((Object)(object)hammerPivot))
		{
			hammerPivot.localRotation = hammerRotation;
		}
		if (Object.op_Implicit((Object)(object)muzzleSparks))
		{
			muzzleSparks.Stop(true, (ParticleSystemStopBehavior)0);
		}
		if (Object.op_Implicit((Object)(object)chamberSparks))
		{
			chamberSparks.Stop(true, (ParticleSystemStopBehavior)0);
		}
	}

	private void Update()
	{
		BatteryUpdate();
		MotionUpdate(Time.deltaTime);
		PendingTriggerUpdate();
		VisualUpdate();
		NetworkUpdate();
		if (shootCooldownTimer > 0.0)
		{
			shootCooldownTimer = Math.Max(0.0, shootCooldownTimer - (double)Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			rattleSuppressTimer = Mathf.Max(0f, rattleSuppressTimer - Time.fixedDeltaTime);
		}
		else
		{
			RattleUpdate();
		}
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		float num = rattleSuppressTimer;
		RattleReset();
		rattleSuppressTimer = Mathf.Max(rattleSuppressTimer, num);
		networkTimer = 0f;
		networkDirty = true;
		((MonoBehaviour)this).StopAllCoroutines();
		((MonoBehaviour)this).StartCoroutine(SnapshotRequest());
	}

	private void StateReady()
	{
		if (stateStart)
		{
			stateStart = false;
			chamberSpeed = 0f;
		}
	}

	private void StateSpinning(float time, bool playSound)
	{
		stateStart = false;
		SpinUpdate(time, playSound);
		if (Mathf.Abs(chamberSpeed) <= settlingSpeed)
		{
			SettleStart();
		}
	}

	private void StateSettling(float time, bool playSound)
	{
		stateStart = false;
		ChamberIndexUpdate(SettleDuration(), time, playSound);
	}

	private void StateRecover(float time)
	{
		if (stateStart)
		{
			stateStart = false;
		}
		if (stateTimer <= 0f)
		{
			IndexStart();
		}
		else
		{
			stateTimer = Mathf.Max(0f, stateTimer - time);
		}
	}

	private void StateIndexing(float time, bool playSound)
	{
		stateStart = false;
		ChamberIndexUpdate(indexTime, time, playSound);
	}

	private void StateMachine(float time, bool playSound = true)
	{
		switch (currentState)
		{
		case State.Ready:
			StateReady();
			break;
		case State.Spinning:
			StateSpinning(time, playSound);
			break;
		case State.Settling:
			StateSettling(time, playSound);
			break;
		case State.Recover:
			StateRecover(time);
			break;
		case State.Indexing:
			StateIndexing(time, playSound);
			break;
		}
	}

	private void StateSet(State state)
	{
		if (currentState != state)
		{
			currentState = state;
			stateStart = true;
		}
	}

	private void MotionUpdate(float time, bool playSound = true)
	{
		motionRemainder += Mathf.Clamp(time, 0f, 10f);
		while (motionRemainder + 1E-07 >= 1.0 / 240.0)
		{
			motionRemainder = Math.Max(0.0, motionRemainder - 1.0 / 240.0);
			StateMachine(0.004166667f, playSound);
			if (currentState == State.Ready)
			{
				motionRemainder = 0.0;
				break;
			}
		}
	}

	internal bool TriggerPull()
	{
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return true;
		}
		if (itemEquippable.isEquipped || body.isKinematic || physGrabObject.dead)
		{
			return true;
		}
		if (currentState == State.Recover || currentState == State.Indexing)
		{
			return true;
		}
		if (shootCooldownTimer > 0.0)
		{
			return true;
		}
		if (currentState == State.Settling)
		{
			pendingTriggerTimer = SettleDuration() + 0.05f;
			return true;
		}
		pendingTriggerTimer = 0f;
		BatteryUpdate();
		int num = ChamberGet();
		bool flag = Mathf.Abs(Mathf.DeltaAngle(chamberAngle, Mathf.Round(chamberAngle / 60f) * 60f)) <= alignmentTolerance;
		bool flag2 = (loadedChambers & (1 << num)) != 0;
		bool flag3 = (flag & flag2) && Mathf.Abs(chamberSpeed) <= settlingSpeed && itemBattery.batteryLifeInt > 0;
		chamberSpeed = 0f;
		rattleSuppressTimer = recoverTime + indexTime;
		FlickRearm();
		shootCooldownTimer = Mathf.Max(recoverTime + indexTime, itemGun.shootCooldown);
		stateTimer = recoverTime;
		motionRemainder = 0.0;
		StateSet(State.Recover);
		if (flag3 && !SemiFunc.RunIsShop() && !RoundDirector.instance.debugInfiniteBattery)
		{
			loadedChambers &= ~(1 << num);
		}
		if (SemiFunc.IsMultiplayer())
		{
			((MonoBehaviourPun)this).photonView.RPC("TriggerRPC", (RpcTarget)0, new object[3] { flag3, loadedChambers, chamberAngle });
		}
		else
		{
			TriggerRPC(flag3, loadedChambers, chamberAngle);
		}
		if (flag3)
		{
			previousBatteryRounds = Mathf.Clamp(itemBattery.batteryLifeInt, 0, 6);
		}
		return true;
	}

	private void PendingTriggerUpdate()
	{
		if (!(pendingTriggerTimer <= 0f))
		{
			if (currentState == State.Ready)
			{
				pendingTriggerTimer = 0f;
				TriggerPull();
			}
			else
			{
				pendingTriggerTimer = Mathf.Max(0f, pendingTriggerTimer - Time.deltaTime);
			}
		}
	}

	private void IndexStart()
	{
		indexStartAngle = chamberAngle;
		indexTargetAngle = (Mathf.Round(chamberAngle / 60f) + 1f) * 60f;
		stateTimer = indexTime;
		StateSet(State.Indexing);
	}

	private void SettleStart()
	{
		indexStartAngle = chamberAngle;
		indexTargetAngle = Mathf.Round(chamberAngle / 60f) * 60f;
		chamberSpeed = 0f;
		stateTimer = SettleDuration();
		StateSet(State.Settling);
	}

	private float SettleDuration()
	{
		return Mathf.Max(0.01f, settleTime);
	}

	private void ChamberIndexUpdate(float duration, float time, bool playSound)
	{
		if (!(stateTimer < 0f))
		{
			float num = 1f - stateTimer / Mathf.Max(0.01f, duration);
			ChamberMove(Mathf.Lerp(indexStartAngle, indexTargetAngle, Mathf.SmoothStep(0f, 1f, num)), playSound);
			if (stateTimer <= 0f)
			{
				stateTimer = -1f;
				StateSet(State.Ready);
			}
			else
			{
				stateTimer = Mathf.Max(0f, stateTimer - time);
			}
		}
	}

	private void SpinUpdate(float time, bool playSound = true)
	{
		float num = Mathf.Min(time, 0.5f);
		while (num > 0f && chamberSpeed != 0f)
		{
			float num2 = Mathf.Min(num, 0.004166667f);
			float num3 = Mathf.MoveTowards(chamberSpeed, 0f, Mathf.Max(1f, spinDrag) * num2);
			float num4 = Mathf.Abs(Mathf.DeltaAngle(chamberAngle, Mathf.Round(chamberAngle / 60f) * 60f));
			float num5 = 1f - catchStrength * Mathf.Exp((0f - num4) * num4 / 12.5f) * Mathf.Lerp(1f, 0.05f, Mathf.Abs(chamberSpeed) / 900f);
			ChamberMove(chamberAngle + (chamberSpeed + num3) * 0.5f * num2 * num5, playSound);
			chamberSpeed = num3;
			num -= num2;
		}
	}

	private void ChamberMove(float angle, bool playSound = true)
	{
		float num = Mathf.DeltaAngle(chamberAngle, angle);
		float num2 = chamberAngle + num;
		int num3 = Mathf.FloorToInt((num2 + 0.001f) / 60f) - Mathf.FloorToInt((chamberAngle + 0.001f) / 60f);
		if (num < 0f)
		{
			num3 = Mathf.CeilToInt((chamberAngle - 0.001f) / 60f) - Mathf.CeilToInt((num2 - 0.001f) / 60f);
		}
		chamberAngle = Mathf.Repeat(angle, 360f);
		if ((num3 > 0) & playSound)
		{
			ChamberClick();
		}
	}

	internal void ShootRicochet(Vector3 startPosition, Vector3 endPosition)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = startPosition;
		Camera val2 = SemiFunc.MainCamera();
		Vector3 val3 = endPosition - startPosition;
		float sqrMagnitude = val3.sqrMagnitude;
		if (Object.op_Implicit((Object)(object)val2) && sqrMagnitude > 1E-06f)
		{
			float num = Mathf.Clamp01(Vector3.Dot(((Component)val2).transform.position - startPosition, val3) / sqrMagnitude);
			val += val3 * num;
		}
		soundRicochet.Play(val);
	}

	private void ChamberClick()
	{
		soundChamberClick.Play(chamberPivot);
	}

	private int ChamberGet()
	{
		int num = Mathf.RoundToInt(chamberAngle / 60f) % 6;
		return (6 - num) % 6;
	}

	private void BatteryUpdate()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		int num = Mathf.Clamp(itemBattery.batteryLifeInt, 0, 6);
		if (num == previousBatteryRounds)
		{
			return;
		}
		int num2 = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((loadedChambers & (1 << i)) != 0)
			{
				num2++;
			}
		}
		for (int j = 0; j < 6; j++)
		{
			if (num2 == num)
			{
				break;
			}
			int num3 = 1 << (ChamberGet() + j) % 6;
			if (num2 < num && (loadedChambers & num3) == 0)
			{
				loadedChambers |= num3;
				num2++;
			}
			else if (num2 > num && (loadedChambers & num3) != 0)
			{
				loadedChambers &= ~num3;
				num2--;
			}
		}
		previousBatteryRounds = num;
		networkDirty = true;
	}

	private void RattleReset()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		previousAngularVelocity = body.angularVelocity;
		previousAngularAcceleration = Vector3.zero;
		previousPosition = body.position;
		previousRotation = body.rotation;
		rattleSuppressTimer = 0.25f;
		FlickRearm();
	}

	private void RattleUpdate()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = body.angularVelocity - previousAngularVelocity;
		Vector3 val2 = val / Time.fixedDeltaTime;
		Vector3 angularJerk = (val2 - previousAngularAcceleration) / Time.fixedDeltaTime;
		previousAngularAcceleration = val2;
		float num = Vector3.Distance(body.position, previousPosition);
		if (flickState == FlickState.Ready)
		{
			flickStartRotation = previousRotation;
			flickStartAngularVelocity = previousAngularVelocity;
		}
		previousAngularVelocity = body.angularVelocity;
		previousPosition = body.position;
		previousRotation = body.rotation;
		if (itemEquippable.isEquipped || body.isKinematic || physGrabObject.dead || num > 1f)
		{
			rattleSuppressTimer = 0.25f;
			FlickRearm();
			return;
		}
		if (rattleSuppressTimer > 0f)
		{
			rattleSuppressTimer = Mathf.Max(0f, rattleSuppressTimer - Time.fixedDeltaTime);
			FlickRearm();
			return;
		}
		switch (flickState)
		{
		case FlickState.Ready:
			FlickStart(val2, angularJerk);
			break;
		case FlickState.Sampling:
			FlickSample();
			break;
		case FlickState.Driving:
			FlickDrive();
			break;
		case FlickState.Rearming:
			FlickRearmUpdate(val);
			break;
		}
	}

	internal void RecoilSuppress()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			rattleSuppressTimer = Mathf.Max(rattleSuppressTimer, itemGun.shootCooldown + 0.2f);
			FlickRearm();
		}
	}

	private void FlickStart(Vector3 angularAcceleration, Vector3 angularJerk)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		float magnitude = angularAcceleration.magnitude;
		if (!(magnitude <= angularAccelerationThreshold) && !(Vector3.Dot(angularAcceleration, body.angularVelocity) <= 0f) && !(Vector3.Dot(angularJerk, angularAcceleration) <= angularJerkThreshold * magnitude))
		{
			flickState = FlickState.Sampling;
			flickTimer = 0.06f;
			flickAngularPeak = 0f;
			flickAngularTravel = 0f;
			Vector3 val = Quaternion.Inverse(flickStartRotation) * (body.angularVelocity - flickStartAngularVelocity);
			flickAxis = Vector3.back;
			if (Mathf.Abs(val.x) * 0.7f > Mathf.Abs(Vector3.Dot(val, flickAxis)))
			{
				flickAxis = Vector3.right * 0.7f;
			}
			if (Mathf.Abs(val.y) * 0.7f > Mathf.Abs(Vector3.Dot(val, flickAxis)))
			{
				flickAxis = Vector3.down * 0.7f;
			}
			FlickSample();
		}
	}

	private void FlickSample()
	{
		if (flickTimer <= 0f)
		{
			FlickRearm();
			return;
		}
		float num = FlickAngularGet();
		if (flickAngularPeak != 0f && num * flickAngularPeak <= 0f)
		{
			FlickRearm();
			return;
		}
		flickAngularTravel += num * 57.29578f * Time.fixedDeltaTime;
		if (Mathf.Abs(num) > Mathf.Abs(flickAngularPeak))
		{
			flickAngularPeak = num;
		}
		if (Mathf.Abs(num) >= flickAngularSpeed && Mathf.Abs(flickAngularTravel) >= flickAngle)
		{
			FlickSpin(flickAngularPeak * 57.29578f * angularRattleStrength);
			flickState = FlickState.Driving;
		}
		else
		{
			flickTimer = Mathf.Max(0f, flickTimer - Time.fixedDeltaTime);
		}
	}

	private void FlickDrive()
	{
		if (flickTimer <= 0f)
		{
			FlickRearm();
			return;
		}
		float num = FlickAngularGet();
		if (Mathf.Sign(num) == Mathf.Sign(flickAngularPeak) && Mathf.Abs(num) > Mathf.Abs(flickAngularPeak))
		{
			FlickSpin((num - flickAngularPeak) * 57.29578f * angularRattleStrength);
			flickAngularPeak = num;
		}
		flickTimer = Mathf.Max(0f, flickTimer - Time.fixedDeltaTime);
	}

	private float FlickAngularGet()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		return Vector3.Dot(Quaternion.Inverse(flickStartRotation) * (body.angularVelocity - flickStartAngularVelocity), flickAxis);
	}

	private void FlickSpin(float impulse)
	{
		chamberSpeed = Mathf.Clamp(chamberSpeed + impulse, 0f - spinSpeedMax, spinSpeedMax);
		StateSet(State.Spinning);
		networkDirty = true;
	}

	private void FlickRearm()
	{
		flickState = FlickState.Rearming;
		flickTimer = 0.12f;
	}

	private void FlickRearmUpdate(Vector3 angularChange)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (!(angularChange.magnitude > angularAccelerationThreshold * Time.fixedDeltaTime * 0.25f))
		{
			Vector3 angularVelocity = body.angularVelocity;
			if (!(angularVelocity.magnitude > flickAngularSpeed))
			{
				if (flickTimer <= 0f)
				{
					flickState = FlickState.Ready;
				}
				else
				{
					flickTimer = Mathf.Max(0f, flickTimer - Time.fixedDeltaTime);
				}
				return;
			}
		}
		flickTimer = 0.12f;
	}

	internal void ShootEffects()
	{
		soundShootLayer.Play(itemGun.gunMuzzle);
		TriggerVisual();
		SparksPlay(muzzleSparks);
		SparksPlay(chamberSparks);
	}

	private void SparksPlay(ParticleSystem particles)
	{
		if (Object.op_Implicit((Object)(object)particles))
		{
			particles.Stop(true, (ParticleSystemStopBehavior)1);
			particles.Play(true);
		}
	}

	internal void TriggerVisual()
	{
		triggerTimer = 0.075f;
	}

	private void VisualUpdate()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		chamberPivot.localRotation = chamberRotation * Quaternion.AngleAxis(chamberAngle, Vector3.forward);
		for (int i = 0; i < 6; i++)
		{
			bool flag = (loadedChambers & (1 << i)) != 0;
			if (bullets[i].activeSelf != flag)
			{
				bullets[i].SetActive(flag);
			}
		}
		float num = 0f;
		if (triggerTimer > 0f)
		{
			num = Mathf.Sin(triggerTimer / 0.075f * MathF.PI) * -32f;
			triggerTimer = Mathf.Max(0f, triggerTimer - Time.deltaTime);
		}
		hammerPivot.localRotation = hammerRotation * Quaternion.Euler(num, 0f, 0f);
	}

	private void NetworkUpdate()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (networkTimer > 0f)
		{
			networkTimer = Mathf.Max(0f, networkTimer - Time.deltaTime);
		}
		else if (networkDirty)
		{
			networkDirty = false;
			networkTimer = 0.1f;
			if (SemiFunc.IsMultiplayer())
			{
				SnapshotSend((RpcTarget)1);
			}
		}
	}

	private IEnumerator SnapshotRequest()
	{
		yield return null;
		if (SemiFunc.IsMultiplayer() && !SemiFunc.IsMasterClientOrSingleplayer())
		{
			while (PhotonNetwork.InRoom && ((MonoBehaviourPun)this).photonView.ViewID == 0)
			{
				yield return null;
			}
			if (PhotonNetwork.InRoom)
			{
				((MonoBehaviourPun)this).photonView.RPC("SnapshotRequestRPC", (RpcTarget)2, Array.Empty<object>());
			}
		}
	}

	private void SnapshotSend(RpcTarget target)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviourPun)this).photonView.RPC("ChamberRPC", target, new object[12]
		{
			(int)currentState,
			chamberAngle,
			chamberSpeed,
			indexStartAngle,
			indexTargetAngle,
			stateTimer,
			(float)motionRemainder,
			loadedChambers,
			(float)shootCooldownTimer,
			rattleSuppressTimer,
			itemBattery.batteryLife,
			itemBattery.isCharging
		});
	}

	private float PacketAge(PhotonMessageInfo info)
	{
		if (!SemiFunc.IsMultiplayer())
		{
			return 0f;
		}
		return Mathf.Clamp((float)(PhotonNetwork.ServerTimestamp - info.SentServerTimestamp) * 0.001f, 0f, 10f);
	}

	private int RoundsCount(int rounds)
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			if ((rounds & (1 << i)) != 0)
			{
				num++;
			}
		}
		return num;
	}

	[PunRPC]
	private void SnapshotRequestRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && PhotonNetwork.InRoom && _info.Sender != null && PhotonNetwork.CurrentRoom.GetPlayer(_info.Sender.ActorNumber, false) == _info.Sender)
		{
			networkDirty = true;
		}
	}

	[PunRPC]
	private void ChamberRPC(int state, float angle, float speed, float startAngle, float targetAngle, float timer, float remainder, int rounds, float cooldown, float suppression, float charge, bool charging, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && state >= 0 && state <= 4 && rounds >= 0 && rounds < 64 && float.IsFinite(angle) && float.IsFinite(speed) && float.IsFinite(startAngle) && float.IsFinite(targetAngle) && float.IsFinite(timer) && float.IsFinite(remainder) && float.IsFinite(cooldown) && float.IsFinite(suppression) && float.IsFinite(charge))
		{
			currentState = (State)state;
			stateStart = true;
			chamberAngle = angle;
			chamberSpeed = speed;
			indexStartAngle = startAngle;
			indexTargetAngle = targetAngle;
			stateTimer = timer;
			motionRemainder = Mathf.Clamp(remainder, 0f, 0.004166667f);
			loadedChambers = rounds;
			previousBatteryRounds = RoundsCount(rounds);
			itemBattery.NetworkSnapshotApply(previousBatteryRounds, charge, charging);
			float num = PacketAge(_info);
			shootCooldownTimer = Mathf.Max(0f, cooldown - num);
			rattleSuppressTimer = Mathf.Max(0f, suppression - num);
			MotionUpdate(num, playSound: false);
			VisualUpdate();
		}
	}

	[PunRPC]
	private void TriggerRPC(bool fired, int rounds, float angle, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info) || rounds < 0 || rounds >= 64 || !float.IsFinite(angle))
		{
			return;
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			loadedChambers = rounds;
			previousBatteryRounds = RoundsCount(rounds);
			chamberAngle = angle;
			chamberSpeed = 0f;
			motionRemainder = 0.0;
			stateTimer = recoverTime;
			StateSet(State.Recover);
			float num = PacketAge(_info);
			shootCooldownTimer = Mathf.Max(0f, Mathf.Max(recoverTime + indexTime, itemGun.shootCooldown) - num);
			float num2 = recoverTime + indexTime;
			if (fired)
			{
				num2 = Mathf.Max(num2, itemGun.shootCooldown + 0.2f);
			}
			rattleSuppressTimer = Mathf.Max(0f, num2 - num);
			MotionUpdate(num, playSound: false);
		}
		if (fired)
		{
			itemGun.ShootRPC(_info);
		}
		else
		{
			itemGun.RevolverClick();
			TriggerVisual();
		}
		VisualUpdate();
	}
}
