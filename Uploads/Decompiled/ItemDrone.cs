using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemDrone : MonoBehaviour
{
	public enum State
	{
		Off,
		Searching,
		BeamDeployed,
		Attached,
		NoBattery
	}

	public static List<ItemDrone> activeDrones = new List<ItemDrone>();

	public State currentState;

	private bool stateStart;

	private float stateTimer;

	private float stateTimerMax;

	public GameObject teleportParticles;

	private ItemAttributes itemAttributes;

	internal SemiFunc.emojiIcon emojiIcon;

	public Texture droneIcon;

	internal ColorPresets colorPreset;

	public BatteryDrainPresets batteryDrainPreset;

	internal float batteryDrainRate = 0.1f;

	internal Color droneColor;

	internal Color batteryColor;

	internal Color beamColor;

	private Transform magnetTarget;

	internal PhysGrabObject magnetTargetPhysGrabObject;

	internal Rigidbody magnetTargetRigidbody;

	internal bool magnetActive;

	public PlayerTumble playerTumbleTarget;

	private Rigidbody rb;

	private bool attachPointFound;

	private Vector3 attachPoint;

	private float springConstant = 50f;

	private float dampingCoefficient = 5f;

	private float newAttachPointTimer;

	internal bool itemActivated;

	private PhotonView photonView;

	private Vector3 rayHitPosition;

	private Vector3 animatedRayHitPosition;

	public LineBetweenTwoPoints lineBetweenTwoPoints;

	public Transform lineStartPoint;

	private float rayTimer;

	private Transform droneTransform;

	private List<Transform> dronePyramidTransforms = new List<Transform>();

	private List<Transform> droneTriangleTransforms = new List<Transform>();

	private float lerpAnimationProgress;

	private bool hasBattery = true;

	private ItemBattery itemBattery;

	private bool animationOpen;

	private Transform onSwitchTransform;

	public ItemDroneSounds itemDroneSounds;

	internal Sound soundDroneLoop = new Sound();

	internal Sound soundDroneBeamLoop = new Sound();

	public PhysicsMaterial physicMaterialSlippery;

	public bool targetValuables;

	public bool targetPlayers;

	public bool targetEnemies;

	public bool targetNonValuables;

	internal Vector3 connectionPoint;

	internal Transform lastPlayerToTouch;

	private PhysGrabObject physGrabObject;

	private float randomNudgeTimer;

	private Collider droneCollider;

	private PhysicsMaterial physicMaterialOriginal;

	private ItemToggle itemToggle;

	internal PlayerAvatar playerAvatarTarget;

	private bool targetIsPlayer;

	internal bool targetIsLocalPlayer;

	private ItemEquippable itemEquippable;

	private Camera cameraMain;

	internal PlayerAvatar droneOwner;

	private float teleportSpotTimer;

	private bool hadTarget;

	private bool targetIsEnemy;

	private EnemyParent enemyTarget;

	private ITargetingCondition customTargetingCondition;

	private bool speedingUp;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		activeDrones = new List<ItemDrone>();
	}

	private void OnEnable()
	{
		if (!activeDrones.Contains(this))
		{
			activeDrones.Add(this);
		}
	}

	private void OnDisable()
	{
		activeDrones.Remove(this);
	}

	public void StateSet(State newState)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != newState && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { (int)newState });
			}
			else
			{
				StateSetRPC((int)newState);
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
				stateStart = true;
				currentState = (State)_newState;
			}
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Off:
			StateOff();
			break;
		case State.Searching:
			StateSearching();
			break;
		case State.BeamDeployed:
			StateBeamDeployed();
			break;
		case State.Attached:
			StateAttached();
			break;
		case State.NoBattery:
			StateNoBattery();
			break;
		}
	}

	private void StateStartResets()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		magnetTarget = null;
		magnetTargetPhysGrabObject = null;
		magnetTargetRigidbody = null;
		playerTumbleTarget = null;
		playerAvatarTarget = null;
		enemyTarget = null;
		targetIsPlayer = false;
		targetIsLocalPlayer = false;
		targetIsEnemy = false;
		attachPointFound = false;
		attachPoint = Vector3.zero;
		rayHitPosition = Vector3.zero;
		animatedRayHitPosition = Vector3.zero;
		rayTimer = 0f;
		teleportSpotTimer = 0f;
		randomNudgeTimer = 0f;
		newAttachPointTimer = 0f;
	}

	private void StateOff()
	{
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = 0f;
			stateTimer = 0f;
			StateStartResets();
			hadTarget = false;
			lerpAnimationProgress = 0f;
			itemActivated = false;
			BatteryToggle(activated: false);
			if (magnetActive)
			{
				MagnetActiveToggle(toggleBool: false);
			}
			physGrabObject.impactDetector.canHurtLogic = true;
			if (Object.op_Implicit((Object)(object)rb) && SemiFunc.IsMasterClientOrSingleplayer() && !rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
			itemDroneSounds.DroneEnd.Play(((Component)this).transform.position);
			stateStart = false;
		}
		soundDroneLoop.PlayLoop(playing: false, 2f, 2f);
		AnimateCloseDrone();
		if (SemiFunc.IsMasterClientOrSingleplayer() && itemToggle.toggleState && (!hasBattery || itemBattery.batteryLife > 0f))
		{
			StateSet(State.Searching);
		}
	}

	private void StateSearching()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = 0.5f;
			stateTimer = 0f;
			BatteryToggle(activated: false);
			lerpAnimationProgress = 0f;
			itemActivated = true;
			physGrabObject.impactDetector.canHurtLogic = false;
			((Component)onSwitchTransform).GetComponent<Renderer>().material.SetColor("_EmissionColor", droneColor);
			itemDroneSounds.DroneStart.Play(((Component)this).transform.position);
			droneOwner = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
			stateStart = false;
		}
		soundDroneLoop.PlayLoop(playing: true, 2f, 2f);
		AnimateDrone();
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (!itemToggle.toggleState)
		{
			StateSet(State.Off);
			return;
		}
		if (hasBattery && itemBattery.batteryLife <= 0f)
		{
			StateSet(State.NoBattery);
			return;
		}
		stateTimer += Time.deltaTime;
		if (!(stateTimer >= stateTimerMax))
		{
			return;
		}
		stateTimer = 0f;
		playerTumbleTarget = null;
		playerAvatarTarget = null;
		targetIsPlayer = false;
		targetIsEnemy = false;
		targetIsLocalPlayer = false;
		bool flag = false;
		if (targetPlayers)
		{
			flag = TargetFindPlayer();
		}
		if (targetValuables || targetNonValuables || targetEnemies)
		{
			if (flag)
			{
				SphereCheck(0.5f);
			}
			else
			{
				flag = SphereCheck(1f);
			}
		}
		if (flag)
		{
			hadTarget = true;
			ActivateMagnet();
			StateSet(State.BeamDeployed);
			CubeRejectCheck();
		}
	}

	private void StateBeamDeployed()
	{
		if (stateStart)
		{
			stateTimerMax = 0.5f;
			stateTimer = 0f;
			BatteryToggle(activated: true);
			lerpAnimationProgress = 0f;
			rayTimer = 0f;
			stateStart = false;
		}
		soundDroneLoop.PlayLoop(playing: true, 2f, 2f);
		AnimateDrone();
		DrawBeamLine();
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		GetPlayerTumbleTarget();
		CheckTargetDeath();
		if (!itemToggle.toggleState)
		{
			DeactivateMagnet();
			StateSet(State.Off);
		}
		else if (!Object.op_Implicit((Object)(object)magnetTarget) || !((Component)magnetTarget).gameObject.activeSelf)
		{
			DeactivateMagnet();
			StateSet(State.Searching);
		}
		else if (hasBattery && itemBattery.batteryLife <= 0f)
		{
			StateSet(State.NoBattery);
		}
		else
		{
			if (targetIsPlayer)
			{
				return;
			}
			rayTimer -= Time.deltaTime;
			if (rayTimer <= 0f)
			{
				FindBeamAttachPosition();
				rayTimer = 0.5f;
				if (attachPointFound)
				{
					StateSet(State.Attached);
				}
			}
		}
	}

	private void StateAttached()
	{
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = 0f;
			stateTimer = 0f;
			springConstant = 40f;
			dampingCoefficient = 10f;
			newAttachPointTimer = 0f;
			randomNudgeTimer = 0f;
			teleportSpotTimer = 0f;
			stateStart = false;
		}
		soundDroneLoop.PlayLoop(playing: true, 2f, 2f);
		AnimateDrone();
		DrawBeamLine();
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		GetPlayerTumbleTarget();
		CheckTargetDeath();
		if (!itemToggle.toggleState)
		{
			DeactivateMagnet();
			StateSet(State.Off);
			return;
		}
		if (!Object.op_Implicit((Object)(object)magnetTarget) || !((Component)magnetTarget).gameObject.activeSelf)
		{
			DeactivateMagnet();
			StateSet(State.Searching);
			return;
		}
		if (hasBattery && itemBattery.batteryLife <= 0f)
		{
			StateSet(State.NoBattery);
			return;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude > 0.2f)
		{
			newAttachPointTimer += Time.deltaTime;
			if (newAttachPointTimer > 0.5f)
			{
				attachPointFound = false;
				newAttachPointTimer = 0f;
				StateSet(State.BeamDeployed);
			}
		}
		else
		{
			newAttachPointTimer = 0f;
		}
	}

	private void StateNoBattery()
	{
		if (stateStart)
		{
			stateTimerMax = 1.5f;
			stateTimer = 0f;
			if (hasBattery && !itemBattery.batteryActive)
			{
				itemBattery.BatteryToggle(toggle: true);
			}
			stateStart = false;
		}
		soundDroneLoop.PlayLoop(playing: false, 2f, 2f);
		AnimateDrone();
		DrawBeamLine();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			stateTimer += Time.deltaTime;
			if (stateTimer >= stateTimerMax)
			{
				ForceTurnOff();
				StateSet(State.Off);
			}
		}
	}

	private void AnimateCloseDrone()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (!animationOpen)
		{
			return;
		}
		lerpAnimationProgress += Time.deltaTime * 10f;
		if (lerpAnimationProgress > 1f)
		{
			lerpAnimationProgress = 1f;
			animationOpen = false;
		}
		foreach (Transform dronePyramidTransform in dronePyramidTransforms)
		{
			dronePyramidTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(-33f, 0f, lerpAnimationProgress), 0f);
		}
		foreach (Transform droneTriangleTransform in droneTriangleTransforms)
		{
			droneTriangleTransform.localRotation = Quaternion.Euler(Mathf.Lerp(45f, 0f, lerpAnimationProgress), 0f, 0f);
		}
	}

	private void DrawBeamLine()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Off || !magnetActive || !Object.op_Implicit((Object)(object)magnetTarget) || itemEquippable.isEquipped)
		{
			return;
		}
		if (rayHitPosition != Vector3.zero && !targetIsPlayer)
		{
			if (!Object.op_Implicit((Object)(object)playerTumbleTarget) || !playerTumbleTarget.playerAvatar.isLocal)
			{
				animatedRayHitPosition = Vector3.Lerp(animatedRayHitPosition, rayHitPosition, Time.deltaTime * 10f);
				connectionPoint = magnetTarget.TransformPoint(animatedRayHitPosition);
				lineBetweenTwoPoints.DrawLineAnimated(lineStartPoint.position, connectionPoint);
			}
			else
			{
				Vector3 val = new Vector3(0f, -0.5f, 0f);
				Vector3 val2 = ((Component)cameraMain).transform.position + val;
				connectionPoint = val2;
				lineBetweenTwoPoints.DrawLineAnimated(lineStartPoint.position, connectionPoint);
			}
			return;
		}
		animatedRayHitPosition = Vector3.Lerp(animatedRayHitPosition, rayHitPosition, Time.deltaTime * 10f);
		if (!targetIsPlayer)
		{
			connectionPoint = magnetTargetPhysGrabObject.midPoint;
			lineBetweenTwoPoints.DrawLineAnimated(lineStartPoint.position, connectionPoint);
		}
		if (targetIsPlayer)
		{
			Vector3 val3 = new Vector3(0f, -0.5f, 0f);
			if (Object.op_Implicit((Object)(object)playerAvatarTarget) && playerAvatarTarget.isTumbling)
			{
				val3 = Vector3.zero;
			}
			if (!targetIsLocalPlayer)
			{
				connectionPoint = magnetTarget.position + val3;
				lineBetweenTwoPoints.DrawLineAnimated(lineStartPoint.position, connectionPoint);
			}
			else
			{
				Vector3 val4 = ((Component)cameraMain).transform.position + val3;
				connectionPoint = val4;
				lineBetweenTwoPoints.DrawLineAnimated(lineStartPoint.position, connectionPoint);
			}
		}
	}

	private void CheckTargetDeath()
	{
		if (!SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			return;
		}
		if (targetIsPlayer)
		{
			if (Object.op_Implicit((Object)(object)playerAvatarTarget) && playerAvatarTarget.isDisabled && itemToggle.toggleState)
			{
				ButtonToggleSet(toggle: false);
				playerAvatarTarget = null;
			}
			if (Object.op_Implicit((Object)(object)playerTumbleTarget) && playerTumbleTarget.playerAvatar.isDisabled && itemToggle.toggleState)
			{
				ButtonToggleSet(toggle: false);
				playerTumbleTarget = null;
			}
		}
		if (!targetIsEnemy || !Object.op_Implicit((Object)(object)magnetTarget))
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)enemyTarget))
		{
			if (!enemyTarget.Spawned && itemToggle.toggleState)
			{
				ForceTurnOff();
				enemyTarget = null;
			}
		}
		else if (itemToggle.toggleState)
		{
			ForceTurnOff();
			enemyTarget = null;
		}
	}

	private void Awake()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		colorPreset = itemAttributes.colorPreset;
		droneColor = colorPreset.GetColorMain();
		batteryColor = colorPreset.GetColorLight();
		beamColor = colorPreset.GetColorDark();
	}

	private void Start()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected Obj, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		foreach (Transform item in ((Component)this).transform)
		{
			Transform val = item;
			if (((Object)val).name == "Particles")
			{
				teleportParticles = ((Component)val).gameObject;
				break;
			}
		}
		customTargetingCondition = ((Component)this).GetComponent<ITargetingCondition>();
		droneCollider = ((Component)this).GetComponentInChildren<Collider>();
		cameraMain = Camera.main;
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemEquippable.itemEmoji = emojiIcon.ToString();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		lineBetweenTwoPoints.SetColor(beamColor);
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		if (!Object.op_Implicit((Object)(object)itemBattery))
		{
			hasBattery = false;
		}
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		emojiIcon = itemAttributes.emojiIcon;
		batteryDrainRate = batteryDrainPreset.GetBatteryDrainRate();
		itemBattery.batteryDrainRate = batteryDrainRate;
		itemBattery.batteryColor = batteryColor;
		Collider[] componentsInChildren = ((Component)this).GetComponentsInChildren<Collider>();
		int num = 0;
		if (num < componentsInChildren.Length)
		{
			Collider val2 = componentsInChildren[num];
			physicMaterialOriginal = val2.material;
		}
		Sound.CopySound(itemDroneSounds.DroneLoop, soundDroneLoop);
		Sound.CopySound(itemDroneSounds.DroneBeamLoop, soundDroneBeamLoop);
		AudioSource component = ((Component)this).GetComponent<AudioSource>();
		soundDroneLoop.Source = component;
		soundDroneBeamLoop.Source = component;
		SetupVisuals(colorPreset);
		physGrabObject.clientNonKinematic = true;
	}

	public void SetupVisuals(ColorPresets _colors, Func<Renderer, Material> _materialGet = null)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected Obj, but got Unknown
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Expected Obj, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected Obj, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected Obj, but got Unknown
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		if (_materialGet == null)
		{
			_materialGet = (Renderer _renderer) => _renderer.material;
		}
		droneColor = _colors.GetColorMain();
		dronePyramidTransforms.Clear();
		droneTriangleTransforms.Clear();
		ItemLight componentInChildren = ((Component)this).GetComponentInChildren<ItemLight>();
		if (Object.op_Implicit((Object)(object)componentInChildren))
		{
			componentInChildren.itemLight.color = droneColor;
		}
		foreach (Transform item2 in ((Component)this).transform)
		{
			Transform val = item2;
			if (((Object)val).name == "Drone Icon")
			{
				onSwitchTransform = val;
				_materialGet(((Component)onSwitchTransform).GetComponent<Renderer>()).SetTexture("_EmissionMap", droneIcon);
				_materialGet(((Component)onSwitchTransform).GetComponent<Renderer>()).SetColor("_EmissionColor", droneColor);
			}
			if (!(((Object)val).name == "Drone"))
			{
				continue;
			}
			droneTransform = val;
			foreach (Transform item3 in val)
			{
				Transform val2 = item3;
				if (((Object)val2).name.Contains("Drone Triangle"))
				{
					foreach (Transform item4 in val2)
					{
						Transform item = item4;
						droneTriangleTransforms.Add(item);
					}
				}
				if (!((Object)val2).name.Contains("Drone Pyramid"))
				{
					continue;
				}
				foreach (Transform item5 in val2)
				{
					Transform val3 = item5;
					dronePyramidTransforms.Add(val3);
					_materialGet(((Component)val3).GetComponent<Renderer>()).SetColor("_EmissionColor", droneColor);
				}
			}
		}
		_materialGet(((Component)droneTransform).GetComponent<Renderer>()).SetColor("_EmissionColor", droneColor);
	}

	private void AnimateDrone()
	{
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Off)
		{
			return;
		}
		lerpAnimationProgress += Time.deltaTime * 10f;
		if (lerpAnimationProgress > 1f)
		{
			lerpAnimationProgress = 1f;
			animationOpen = true;
		}
		float num = 15f;
		if (magnetActive)
		{
			num = 60f;
		}
		foreach (Transform dronePyramidTransform in dronePyramidTransforms)
		{
			float num2 = -33f;
			if (lerpAnimationProgress != 1f)
			{
				dronePyramidTransform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, num2, lerpAnimationProgress), 0f);
				continue;
			}
			float num3 = Mathf.Sin(Time.time * num) * 5f;
			dronePyramidTransform.localRotation = Quaternion.Euler(0f, num2 + num3, 0f);
		}
		foreach (Transform droneTriangleTransform in droneTriangleTransforms)
		{
			float num4 = 45f;
			if (lerpAnimationProgress != 1f)
			{
				droneTriangleTransform.localRotation = Quaternion.Euler(Mathf.Lerp(0f, num4, lerpAnimationProgress), 0f, 0f);
				continue;
			}
			float num5 = Mathf.Sin(Time.time * num / 3f) * 10f;
			droneTriangleTransform.localRotation = Quaternion.Euler(num4 + num5, 0f, 0f);
		}
	}

	private bool TargetFindPlayer()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		if (itemBattery.batteryLife <= 0f)
		{
			return false;
		}
		float num = 10000f;
		Collider[] array = Physics.OverlapSphere(((Component)this).transform.position, 1f, LayerMask.GetMask(new string[1] { "Player" }));
		foreach (Collider val in array)
		{
			PlayerAvatar playerAvatar = ((Component)val).GetComponentInParent<PlayerAvatar>();
			if (!Object.op_Implicit((Object)(object)playerAvatar))
			{
				PlayerController componentInParent = ((Component)val).GetComponentInParent<PlayerController>();
				if (Object.op_Implicit((Object)(object)componentInParent))
				{
					playerAvatar = componentInParent.playerAvatarScript;
				}
			}
			if (!Object.op_Implicit((Object)(object)playerAvatar) || (customTargetingCondition != null && !customTargetingCondition.CustomTargetingCondition(((Component)playerAvatar).gameObject)))
			{
				continue;
			}
			float num2 = Vector3.Distance(((Component)this).transform.position, playerAvatar.PlayerVisionTarget.VisionTransform.position);
			if (num2 < num)
			{
				num = num2;
				playerAvatarTarget = playerAvatar;
				targetIsPlayer = true;
				if (playerAvatarTarget.isLocal)
				{
					targetIsLocalPlayer = true;
				}
			}
		}
		if (Object.op_Implicit((Object)(object)playerAvatarTarget))
		{
			Transform val2 = playerAvatarTarget.PlayerVisionTarget.VisionTransform;
			Vector3 newAttachPoint = val2.position;
			if (playerAvatarTarget.isTumbling && Object.op_Implicit((Object)(object)((Component)playerAvatarTarget).transform))
			{
				playerTumbleTarget = playerAvatarTarget.tumble;
				val2 = ((Component)playerTumbleTarget).transform;
				newAttachPoint = playerTumbleTarget.physGrabObject.centerPoint;
			}
			targetIsLocalPlayer = playerAvatarTarget.isLocal;
			targetIsPlayer = true;
			NewRayHitPoint(newAttachPoint, ((Component)playerAvatarTarget).GetComponent<PhotonView>().ViewID, -1, val2);
			attachPoint = rayHitPosition;
			ActivateMagnet();
			return true;
		}
		return false;
	}

	private void GetPlayerTumbleTarget()
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)magnetTarget))
		{
			if (Object.op_Implicit((Object)(object)playerTumbleTarget) && Object.op_Implicit((Object)(object)playerTumbleTarget.playerAvatar) && !playerTumbleTarget.playerAvatar.isTumbling)
			{
				playerAvatarTarget = playerTumbleTarget.playerAvatar;
				targetIsLocalPlayer = playerAvatarTarget.isLocal;
				targetIsPlayer = true;
				ActivateMagnet();
				playerTumbleTarget = null;
				Transform visionTransform = playerAvatarTarget.PlayerVisionTarget.VisionTransform;
				Vector3 position = visionTransform.position;
				NewRayHitPoint(position, ((Component)playerAvatarTarget).GetComponent<PhotonView>().ViewID, -1, visionTransform);
			}
			if (Object.op_Implicit((Object)(object)playerAvatarTarget) && playerAvatarTarget.isTumbling)
			{
				playerTumbleTarget = playerAvatarTarget.tumble;
				targetIsLocalPlayer = false;
				targetIsPlayer = false;
				magnetTarget = ((Component)playerTumbleTarget).transform;
				magnetTargetPhysGrabObject = playerTumbleTarget.physGrabObject;
				playerAvatarTarget = null;
				attachPoint = rayHitPosition;
				ActivateMagnet();
			}
		}
	}

	private void ForceTurnOff()
	{
		itemBattery.BatteryToggle(toggle: false);
		ButtonToggleSet(toggle: false);
		itemToggle.ToggleItem(toggle: false);
		hadTarget = false;
		itemActivated = false;
	}

	public bool IsConnectedToPlayer(PlayerAvatar _player)
	{
		if (!Object.op_Implicit((Object)(object)_player))
		{
			return false;
		}
		if ((Object)(object)droneOwner == (Object)(object)_player)
		{
			return true;
		}
		if ((Object)(object)playerAvatarTarget == (Object)(object)_player)
		{
			return true;
		}
		if (Object.op_Implicit((Object)(object)playerTumbleTarget) && (Object)(object)playerTumbleTarget.playerAvatar == (Object)(object)_player)
		{
			return true;
		}
		return false;
	}

	public void DeactivateDrone()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState != State.Off)
		{
			ForceTurnOff();
			StateSet(State.Off);
		}
	}

	public void RejectFromCube()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			DeactivateMagnet();
			ForceTurnOff();
			StateSet(State.Off);
			StateOff();
		}
	}

	private bool CubeRejectCheck()
	{
		if (!itemToggle.toggleState || !magnetActive)
		{
			return false;
		}
		ValuableCubeOfKnowledge valuableCubeOfKnowledge = default;
		if (Object.op_Implicit((Object)(object)magnetTargetPhysGrabObject) && ((Component)magnetTargetPhysGrabObject).TryGetComponent<ValuableCubeOfKnowledge>(ref valuableCubeOfKnowledge) && valuableCubeOfKnowledge.ItemReject(physGrabObject))
		{
			return true;
		}
		return false;
	}

	private void Update()
	{
		if (itemEquippable.isEquipped || CubeRejectCheck())
		{
			return;
		}
		if (hadTarget && !magnetActive)
		{
			ForceTurnOff();
		}
		else if (SemiFunc.RunIsLevel() || SemiFunc.RunIsLobby() || SemiFunc.RunIsShop() || SemiFunc.RunIsArena() || SemiFunc.RunIsTutorial())
		{
			if (physGrabObject.playerGrabbing.Count == 1)
			{
				lastPlayerToTouch = ((Component)physGrabObject.playerGrabbing[0]).transform;
			}
			StateMachine();
		}
	}

	public void ButtonToggleSet(bool toggle)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMultiplayer())
		{
			ButtonToggleRPC(toggle);
		}
		else if (SemiFunc.IsMasterClient())
		{
			photonView.RPC("ButtonToggleRPC", (RpcTarget)0, new object[1] { toggle });
		}
	}

	private void FixedUpdate()
	{
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_040b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0410: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0308: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_0470: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Off)
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)magnetTarget))
		{
			ItemEquippable componentInParent = ((Component)magnetTarget).GetComponentInParent<ItemEquippable>();
			if (Object.op_Implicit((Object)(object)componentInParent) && componentInParent.isEquipped && magnetActive)
			{
				ForceTurnOff();
				DeactivateMagnet();
				StateSet(State.Off);
			}
		}
		if (itemEquippable.isEquipped)
		{
			if (magnetActive)
			{
				ForceTurnOff();
				DeactivateMagnet();
				StateSet(State.Off);
			}
		}
		else
		{
			if ((!SemiFunc.RunIsLevel() && !SemiFunc.RunIsLobby() && !SemiFunc.RunIsShop() && !SemiFunc.RunIsArena() && !SemiFunc.RunIsTutorial()) || !SemiFunc.IsMasterClientOrSingleplayer() || (currentState != State.BeamDeployed && currentState != State.Attached))
			{
				return;
			}
			if (!Object.op_Implicit((Object)(object)magnetTarget))
			{
				DeactivateMagnet();
				StateSet(State.Searching);
				return;
			}
			float num = Vector3.Distance(((Component)this).transform.position, magnetTarget.position);
			Vector3 val;
			if (num > 4f)
			{
				if (num > 18f)
				{
					FindTeleportSpot();
					speedingUp = false;
				}
				else
				{
					val = magnetTarget.position - ((Component)this).transform.position;
					Vector3 normalized = val.normalized;
					RaycastHit val2 = default;
					if (Physics.SphereCast(((Component)this).transform.position, 0.2f, normalized, ref val2, num, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct())))
					{
						if ((Object)(object)((Component)val2.transform).GetComponentInParent<Rigidbody>() != (Object)(object)magnetTargetRigidbody && (Object)(object)val2.transform != (Object)(object)((Component)this).transform)
						{
							FindTeleportSpot();
							speedingUp = false;
						}
						else
						{
							speedingUp = true;
						}
					}
					else
					{
						speedingUp = true;
					}
				}
			}
			else
			{
				speedingUp = false;
			}
			Collider component = ((Component)magnetTarget).GetComponent<Collider>();
			if (!Object.op_Implicit((Object)(object)playerTumbleTarget) && (!((Component)magnetTarget).gameObject.activeSelf || !((Component)magnetTarget).gameObject.activeInHierarchy || (Object.op_Implicit((Object)(object)component) && !component.enabled)))
			{
				DeactivateMagnet();
				StateSet(State.Searching);
				return;
			}
			physGrabObject.OverrideMaterial(physicMaterialSlippery);
			if (randomNudgeTimer <= 0f)
			{
				if (Vector3.Distance(((Component)this).transform.position, ((Component)magnetTarget).transform.position) > 1.5f)
				{
					Vector3 val3 = ((Component)this).transform.position - ((Component)magnetTarget).transform.position;
					Vector3[] array = new Vector3[4]
					{
						Vector3.up,
						Vector3.down,
						Vector3.left,
						Vector3.right
					};
					Vector3 val4 = array[Random.Range(0, array.Length)];
					val = Vector3.Cross(val3, val4);
					Vector3 normalized2 = val.normalized;
					if (normalized2 != Vector3.zero)
					{
						rb.AddForce(normalized2 * 1f, (ForceMode)1);
						rb.AddTorque(normalized2 * 10f, (ForceMode)1);
					}
				}
				randomNudgeTimer = 0.5f;
			}
			else
			{
				randomNudgeTimer -= Time.fixedDeltaTime;
			}
			if (currentState == State.Attached)
			{
				Vector3 val5 = magnetTarget.TransformPoint(attachPoint) - ((Component)this).transform.position;
				Vector3 val6 = springConstant * val5;
				Vector3 val7 = (0f - dampingCoefficient) * rb.linearVelocity;
				Vector3 val8 = Vector3.ClampMagnitude(val6 + val7, 20f);
				rb.AddForce(val8);
				if (!((Component)magnetTarget).gameObject.activeSelf)
				{
					DeactivateMagnet();
					StateSet(State.Searching);
				}
				SemiFunc.PhysLookAtPositionWithForce(rb, ((Component)this).transform, magnetTarget.TransformPoint(rayHitPosition), 1f);
				return;
			}
			Vector3 val9 = magnetTarget.position - ((Component)this).transform.position;
			float magnitude = val9.magnitude;
			if (magnitude > 0.8f)
			{
				float num2 = 3f;
				if (speedingUp)
				{
					float num3 = Mathf.Clamp(Mathf.Lerp(1f, 3f, (magnitude - 4f) / 14f), 1f, 3f);
					rb.AddForce(val9.normalized * num2 * num3);
				}
				else
				{
					rb.AddForce(val9.normalized * num2);
				}
			}
			else
			{
				rb.AddForce(-rb.linearVelocity * 0.9f);
			}
			if (!((Component)magnetTarget).gameObject.activeSelf)
			{
				DeactivateMagnet();
				StateSet(State.Searching);
			}
			SemiFunc.PhysLookAtPositionWithForce(rb, ((Component)this).transform, magnetTarget.position, 1f);
		}
	}

	[PunRPC]
	public void TeleportEffectRPC(Vector3 startPosition, Vector3 endPosition, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			itemDroneSounds.DroneRetract.Pitch = 3f;
			itemDroneSounds.DroneRetract.Play(startPosition);
			itemDroneSounds.DroneRetract.Pitch = 4f;
			itemDroneSounds.DroneRetract.Play(endPosition);
			Object.Instantiate<GameObject>(teleportParticles, startPosition, Quaternion.identity);
			Object.Instantiate<GameObject>(teleportParticles, endPosition, Quaternion.identity);
		}
	}

	private void TeleportEffect(Vector3 startPosition, Vector3 endPosition)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("TeleportEffectRPC", (RpcTarget)0, new object[2] { startPosition, endPosition });
			}
		}
		else
		{
			TeleportEffectRPC(startPosition, endPosition);
		}
	}

	private void FindTeleportSpot()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		if (!magnetActive || !Object.op_Implicit((Object)(object)magnetTarget))
		{
			return;
		}
		if (teleportSpotTimer <= 0f)
		{
			ItemEquippable componentInParent = ((Component)magnetTarget).GetComponentInParent<ItemEquippable>();
			if (Object.op_Implicit((Object)(object)componentInParent) && componentInParent.isEquipped)
			{
				return;
			}
			Vector3 val = magnetTarget.position + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
			for (int i = 0; i < 10; i++)
			{
				val = magnetTarget.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
				float num = Vector3.Distance(val, magnetTarget.position);
				float num2 = Mathf.Max(0f, num - 0.2f);
				RaycastHit[] array = Physics.RaycastAll(val, magnetTarget.position - val, num2, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()));
				bool flag = false;
				RaycastHit[] array2 = array;
				for (int j = 0; j < array2.Length; j++)
				{
					RaycastHit val2 = array2[j];
					if ((Object)(object)((Component)val2.transform).GetComponentInParent<Rigidbody>() != (Object)(object)((Component)magnetTarget).GetComponentInParent<Rigidbody>() && (Object)(object)val2.transform != (Object)(object)((Component)this).transform)
					{
						flag = true;
						break;
					}
				}
				if (!flag && Physics.OverlapBox(val, new Vector3(0.2f, 0.2f, 0.2f), ((Component)this).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct())).Length == 0)
				{
					TeleportEffect(((Component)this).transform.position, val);
					if (SemiFunc.IsMultiplayer())
					{
						physGrabObject.photonTransformView.Teleport(val, ((Component)this).transform.rotation);
					}
					else
					{
						((Component)this).transform.position = val;
					}
					break;
				}
			}
			teleportSpotTimer = 0.2f;
		}
		else
		{
			teleportSpotTimer -= Time.deltaTime;
		}
	}

	private void DeactivateMagnet()
	{
		if (magnetActive)
		{
			attachPointFound = false;
			playerAvatarTarget = null;
			targetIsPlayer = false;
			targetIsLocalPlayer = false;
			targetIsEnemy = false;
			playerTumbleTarget = null;
			magnetTargetPhysGrabObject = null;
			magnetTargetRigidbody = null;
			MagnetActiveToggle(toggleBool: false);
		}
	}

	private void ActivateMagnet()
	{
		if (!magnetActive)
		{
			MagnetActiveToggle(toggleBool: true);
		}
	}

	private void BatteryToggle(bool activated)
	{
		if (hasBattery)
		{
			itemBattery.batteryActive = activated;
		}
	}

	private void ButtonToggleLogic(bool activated)
	{
		if (activated)
		{
			if (currentState == State.Off)
			{
				stateStart = true;
				currentState = State.Searching;
			}
		}
		else if (currentState != State.Off)
		{
			stateStart = true;
			currentState = State.Off;
		}
	}

	public void ButtonToggle()
	{
		bool flag = !itemActivated;
		if (GameManager.instance.gameMode == 0)
		{
			ButtonToggleLogic(flag);
		}
		else if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("ButtonToggleRPC", (RpcTarget)0, new object[1] { flag });
		}
	}

	[PunRPC]
	private void ButtonToggleRPC(bool activated, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			ButtonToggleLogic(activated);
		}
	}

	private Transform GetHighestParentWithRigidbody(Transform _child)
	{
		if ((Object)(object)((Component)this).GetComponent<Rigidbody>() != (Object)null && (Object)(object)((Component)_child).GetComponent<PhotonView>() != (Object)null)
		{
			return _child;
		}
		Transform val = _child;
		while ((Object)(object)val.parent != (Object)null)
		{
			if ((Object)(object)((Component)val.parent).GetComponent<Rigidbody>() != (Object)null && (Object)(object)((Component)val.parent).GetComponent<PhotonView>() != (Object)null)
			{
				return val.parent;
			}
			val = val.parent;
		}
		return null;
	}

	private void MagnetActiveToggleLogic(bool activated)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		magnetActive = activated;
		lerpAnimationProgress = 0f;
		if (!activated)
		{
			itemDroneSounds.DroneRetract.Play(((Component)this).transform.position);
			rayHitPosition = Vector3.zero;
		}
		else
		{
			itemDroneSounds.DroneDeploy.Play(((Component)this).transform.position);
		}
	}

	public void MagnetActiveToggle(bool toggleBool)
	{
		if (GameManager.instance.gameMode == 0)
		{
			MagnetActiveToggleLogic(toggleBool);
		}
		else if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("MagnetActiveToggleRPC", (RpcTarget)0, new object[1] { toggleBool });
		}
	}

	[PunRPC]
	private void MagnetActiveToggleRPC(bool activated, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			MagnetActiveToggleLogic(activated);
		}
	}

	private void NewRayHitPointLogic(Vector3 newRayHitPosition, int photonViewId, int colliderID, Transform newMagnetTarget)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		if (magnetActive)
		{
			lineBetweenTwoPoints.TriggerPointSwitch();
		}
		if (Object.op_Implicit((Object)(object)newMagnetTarget))
		{
			magnetTargetPhysGrabObject = ((Component)newMagnetTarget).GetComponent<PhysGrabObject>();
			if (colliderID != -1)
			{
				magnetTarget = ((Component)newMagnetTarget).GetComponent<PhysGrabObject>().FindColliderFromID(colliderID);
				targetIsPlayer = false;
				targetIsLocalPlayer = false;
			}
			else
			{
				magnetTarget = newMagnetTarget;
			}
			animatedRayHitPosition = rayHitPosition;
			rayHitPosition = magnetTarget.InverseTransformPoint(newRayHitPosition);
			magnetTargetRigidbody = ((Component)GetHighestParentWithRigidbody(magnetTarget)).GetComponent<Rigidbody>();
			PlayerTumble component = ((Component)magnetTargetRigidbody).GetComponent<PlayerTumble>();
			if (Object.op_Implicit((Object)(object)component))
			{
				if (component.isTumbling)
				{
					playerTumbleTarget = component;
				}
				else
				{
					DeactivateMagnet();
				}
			}
			return;
		}
		PhotonView val = PhotonView.Find(photonViewId);
		if (Object.op_Implicit((Object)(object)val))
		{
			magnetTargetPhysGrabObject = ((Component)val).gameObject.GetComponent<PhysGrabObject>();
			if (colliderID != -1)
			{
				magnetTarget = ((Component)val).gameObject.GetComponent<PhysGrabObject>().FindColliderFromID(colliderID);
				targetIsPlayer = false;
				targetIsLocalPlayer = false;
			}
			else
			{
				targetIsPlayer = true;
				PlayerAvatar component2 = ((Component)val).GetComponent<PlayerAvatar>();
				playerAvatarTarget = component2;
				magnetTarget = playerAvatarTarget.PlayerVisionTarget.VisionTransform;
				targetIsLocalPlayer = playerAvatarTarget.isLocal;
			}
			animatedRayHitPosition = rayHitPosition;
			rayHitPosition = magnetTarget.InverseTransformPoint(newRayHitPosition);
			magnetTargetRigidbody = ((Component)GetHighestParentWithRigidbody(magnetTarget)).GetComponent<Rigidbody>();
		}
	}

	private void NewRayHitPoint(Vector3 newAttachPoint, int photonViewId, int colliderID, Transform newMagnetTarget)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (!GameManager.Multiplayer())
		{
			NewRayHitPointLogic(newAttachPoint, photonViewId, colliderID, newMagnetTarget);
		}
		else if (PhotonNetwork.IsMasterClient)
		{
			photonView.RPC("NewRayHitPointRPC", (RpcTarget)0, new object[3] { newAttachPoint, photonViewId, colliderID });
		}
	}

	[PunRPC]
	private void NewRayHitPointRPC(Vector3 newAttachPoint, int photonViewId, int colliderID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			NewRayHitPointLogic(newAttachPoint, photonViewId, colliderID, null);
		}
	}

	private bool SphereCheck(float _radius)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		if (itemBattery.batteryLife <= 0f)
		{
			return false;
		}
		Collider[] array = Physics.OverlapSphere(((Component)this).transform.position, _radius);
		Transform val = null;
		PhysGrabObject physGrabObject = null;
		Rigidbody val2 = null;
		Transform val3 = null;
		int colliderID = -1;
		bool flag2 = false;
		EnemyParent enemyParent = null;
		PlayerTumble playerTumble = null;
		float num = 10000f;
		Collider[] array2 = array;
		RaycastHit val7 = default;
		foreach (Collider val4 in array2)
		{
			Transform highestParentWithRigidbody = GetHighestParentWithRigidbody(((Component)val4).transform);
			PhysGrabObjectCollider component = ((Component)val4).GetComponent<PhysGrabObjectCollider>();
			PhysGrabObject physGrabObject2 = null;
			bool flag3 = false;
			bool flag4 = false;
			EnemyParent enemyParent2 = null;
			PlayerTumble playerTumble2 = null;
			playerTumble2 = ((Component)val4).GetComponentInParent<PlayerTumble>();
			if (Object.op_Implicit((Object)(object)highestParentWithRigidbody))
			{
				PhysGrabObjectImpactDetector component2 = ((Component)highestParentWithRigidbody).GetComponent<PhysGrabObjectImpactDetector>();
				physGrabObject2 = ((Component)highestParentWithRigidbody).GetComponent<PhysGrabObject>();
				if (Object.op_Implicit((Object)(object)component2))
				{
					if (component2.isValuable)
					{
						flag4 = true;
					}
					if (component2.isEnemy)
					{
						flag3 = true;
						enemyParent2 = ((Component)component2).GetComponentInParent<EnemyParent>();
					}
				}
			}
			bool flag5 = true;
			if (customTargetingCondition != null && Object.op_Implicit((Object)(object)highestParentWithRigidbody))
			{
				flag5 = customTargetingCondition.CustomTargetingCondition(((Component)highestParentWithRigidbody).gameObject);
			}
			bool flag6 = targetValuables & flag4;
			if (!flag6)
			{
				flag6 = Object.op_Implicit((Object)(object)physGrabObject2) && physGrabObject2.isNonValuable && physGrabObject2.notValuableObject.hasHealth;
			}
			if (!flag6)
			{
				flag6 = targetEnemies & flag3;
			}
			if (!flag6)
			{
				flag6 = targetNonValuables && !flag4;
			}
			if (Object.op_Implicit((Object)(object)physGrabObject2) && physGrabObject2.droneTargetDisableTimer > 0f)
			{
				flag6 = false;
			}
			if (!((Object.op_Implicit((Object)(object)component) && (Object)(object)highestParentWithRigidbody != (Object)(object)((Component)this).transform && Object.op_Implicit((Object)(object)highestParentWithRigidbody)) & flag6 & flag5) || !((Component)highestParentWithRigidbody).gameObject.activeSelf)
			{
				continue;
			}
			float num2 = Vector3.Distance(((Component)this).transform.position, physGrabObject2.centerPoint);
			if (num2 < num)
			{
				bool flag7 = false;
				Vector3 position = ((Component)this).transform.position;
				Vector3 val5 = physGrabObject2.centerPoint - ((Component)this).transform.position;
				Vector3 val6 = physGrabObject2.centerPoint - ((Component)this).transform.position;
				if (Physics.Raycast(position, val5, ref val7, val6.magnitude, LayerMask.GetMask(new string[1] { "Default" })) && (Object)(object)((Component)val7.collider).transform != (Object)(object)((Component)val4).transform && (Object)(object)((Component)val7.collider).transform != (Object)(object)((Component)this).transform)
				{
					flag7 = true;
				}
				if (!flag7)
				{
					num = num2;
					val = ((Component)val4).transform;
					physGrabObject = physGrabObject2;
					val2 = ((Component)highestParentWithRigidbody).GetComponent<Rigidbody>();
					val3 = highestParentWithRigidbody;
					colliderID = component.colliderID;
					flag2 = flag3;
					enemyParent = enemyParent2;
					playerTumble = playerTumble2;
					flag = true;
				}
			}
		}
		if (flag)
		{
			playerTumbleTarget = playerTumble;
			playerAvatarTarget = null;
			targetIsPlayer = false;
			targetIsEnemy = false;
			targetIsLocalPlayer = false;
			magnetTarget = val;
			magnetTargetPhysGrabObject = physGrabObject;
			magnetTargetRigidbody = val2;
			NewRayHitPoint(val.position, ((Component)val3).GetComponent<PhotonView>().ViewID, colliderID, val3);
			attachPoint = rayHitPosition;
			targetIsEnemy = flag2;
			enemyTarget = enemyParent;
		}
		return flag;
	}

	private void FindBeamAttachPosition()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)magnetTarget))
		{
			return;
		}
		RaycastHit val2 = default;
		for (int i = 0; i < 6; i++)
		{
			float num = 0.5f;
			Vector3 val = new Vector3(Random.Range(0f - num, num), Random.Range(0f - num, num), Random.Range(0f - num, num));
			if (Physics.Raycast(((Component)this).transform.position, magnetTarget.position - ((Component)this).transform.position + val, ref val2, 1f, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPhysGrabObject())))
			{
				Transform highestParentWithRigidbody = GetHighestParentWithRigidbody(((Component)val2.collider).transform);
				PhysGrabObjectCollider component = ((Component)((Component)val2.collider).transform).GetComponent<PhysGrabObjectCollider>();
				if (Object.op_Implicit((Object)(object)component) && (Object)(object)highestParentWithRigidbody == (Object)(object)((Component)magnetTargetPhysGrabObject).transform)
				{
					Vector3 val3 = ((Component)this).transform.position - val2.point;
					Vector3 normalized = val3.normalized;
					NewRayHitPoint(val2.point, ((Component)highestParentWithRigidbody).GetComponent<PhotonView>().ViewID, component.colliderID, highestParentWithRigidbody);
					Vector3 val4 = val2.point + normalized * 0.5f;
					attachPoint = magnetTarget.InverseTransformPoint(val4);
					attachPointFound = true;
				}
			}
		}
	}

	public void SetTumbleTarget(PlayerTumble _tumble)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMultiplayer())
		{
			SetTumbleTargetRPC(_tumble.photonView.ViewID);
		}
		else if (SemiFunc.IsMasterClient())
		{
			photonView.RPC("SetTumbleTargetRPC", (RpcTarget)0, new object[1] { _tumble.photonView.ViewID });
		}
	}

	[PunRPC]
	public void SetTumbleTargetRPC(int _photonViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		PhotonView val = PhotonView.Find(_photonViewID);
		if (Object.op_Implicit((Object)(object)val))
		{
			PlayerTumble component = ((Component)val).GetComponent<PlayerTumble>();
			if (Object.op_Implicit((Object)(object)component))
			{
				playerTumbleTarget = component;
			}
		}
	}
}
