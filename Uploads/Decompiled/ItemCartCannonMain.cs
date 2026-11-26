using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemCartCannonMain : MonoBehaviour
{
	public enum state
	{
		inactive,
		active,
		buildup,
		shooting,
		goingBack
	}

	public float shootBuildUpTime = 0.5f;

	public float shootTime = 0.5f;

	public float goBackFromShootTime = 0.5f;

	public bool singleShot;

	public Color mainOnColor = Color.green;

	public float investigationRange = 35f;

	private ItemBattery battery;

	private PhysGrabObject physGrabObject;

	internal bool impulseShoot;

	internal bool impulseShooting;

	public Transform muzzle;

	private ItemToggle itemToggle;

	private bool prevToggleState;

	private Rigidbody rb;

	private PhysGrabObjectImpactDetector impactDetector;

	internal bool isActive;

	internal Quaternion rotationTargetY;

	public GameObject currentCorrector;

	public GameObject cannonGrabPoint;

	private PhysGrabObjectGrabArea physGrabObjectGrabArea;

	public List<MeshRenderer> grabMeshRenderers = new List<MeshRenderer>();

	private List<Material> grabMaterials = new List<Material>();

	public MeshRenderer cartLogoScreen;

	public Light cartGrabLight;

	private PhotonView photonView;

	public MeshRenderer mainMesh;

	public Light mainLight;

	public Sound soundBootUp;

	public Sound soundShutdown;

	public Sound soundAimLoop;

	public Sound soundQuickTurn;

	private Quaternion prevRotation;

	private float quickTurnSoundCooldown;

	private float smoothPitch;

	internal float stateTimer;

	internal float stateTimerMax = 0.5f;

	internal bool stateStart = true;

	internal state stateCurrent;

	internal state statePrev;

	private bool isFixedUpdate;

	private bool singleShotNextFrame;

	public Sound soundGrabStart;

	public Sound soundGrabEnd;

	private bool handleGrabbed;

	private void Start()
	{
		battery = ((Component)this).GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		physGrabObjectGrabArea = ((Component)this).GetComponent<PhysGrabObjectGrabArea>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		foreach (MeshRenderer grabMeshRenderer in grabMeshRenderers)
		{
			if ((Object)(object)grabMeshRenderer != (Object)null)
			{
				Material material = ((Renderer)grabMeshRenderer).material;
				if ((Object)(object)material != (Object)null)
				{
					grabMaterials.Add(material);
				}
			}
		}
	}

	private void Update()
	{
		impulseShoot = false;
		StateMachine();
		MovementSounds();
		if (physGrabObject.grabbed)
		{
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				item.OverrideDisableGrabLookAt(0.2f);
			}
		}
		if (stateTimer <= stateTimerMax)
		{
			stateTimer += Time.deltaTime;
		}
	}

	private void FixedUpdate()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			isFixedUpdate = true;
			StateMachine();
			isFixedUpdate = false;
		}
	}

	private void StateSet(int state)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (state == (int)stateCurrent)
		{
			return;
		}
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { state });
			}
		}
		else
		{
			StateSetRPC(state);
		}
	}

	[PunRPC]
	private void StateSetRPC(int state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			stateStart = true;
			statePrev = stateCurrent;
			stateCurrent = (state)state;
			stateTimer = 0f;
		}
	}

	private void StateInactive()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !isFixedUpdate)
		{
			((Renderer)mainMesh).material.SetColor("_EmissionColor", Color.red);
			mainLight.color = Color.red;
			((Renderer)cartLogoScreen).material.SetColor("_EmissionColor", Color.red);
			soundShutdown.Play(((Component)this).transform.position);
			int num = 0;
			foreach (Material grabMaterial in grabMaterials)
			{
				grabMaterial.SetColor("_EmissionColor", Color.red);
				if (num == 1)
				{
					grabMaterial.mainTextureOffset = new Vector2(0f, 0f);
				}
				num++;
			}
			cartGrabLight.color = Color.red;
			stateStart = false;
		}
		if (!isFixedUpdate)
		{
			if (itemToggle.toggleState && SemiFunc.IsMasterClientOrSingleplayer())
			{
				prevToggleState = false;
				itemToggle.ToggleItem(toggle: false);
			}
			if (SemiFunc.PerSecond(5f, (Object)(object)this))
			{
				bool flag = true;
				if (!impactDetector.inCart)
				{
					flag = false;
				}
				if (!Object.op_Implicit((Object)(object)impactDetector.currentCart))
				{
					flag = false;
				}
				if (battery.batteryLifeInt <= 0)
				{
					flag = false;
				}
				if (flag)
				{
					StateSet(1);
				}
			}
		}
		_ = isFixedUpdate;
	}

	private void StateActive()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !isFixedUpdate)
		{
			((Renderer)mainMesh).material.SetColor("_EmissionColor", mainOnColor);
			mainLight.color = mainOnColor;
			((Renderer)cartLogoScreen).material.SetColor("_EmissionColor", Color.green);
			stateStart = false;
			if (statePrev == state.inactive)
			{
				soundBootUp.Play(((Component)this).transform.position);
			}
		}
		if (!isFixedUpdate)
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				if (!Object.op_Implicit((Object)(object)impactDetector.currentCart) || !impactDetector.inCart || battery.batteryLifeInt <= 0)
				{
					StateSet(0);
					return;
				}
				if (itemToggle.toggleState != prevToggleState && battery.batteryLifeInt > 0)
				{
					prevToggleState = itemToggle.toggleState;
					StateSet(2);
					itemToggle.toggleImpulse = false;
				}
			}
			CorrectorAndLightLogic();
		}
		if (isFixedUpdate)
		{
			GrabLogic();
		}
	}

	private void StateBuildup()
	{
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = shootBuildUpTime;
		}
		if (!isFixedUpdate)
		{
			CorrectorAndLightLogic();
		}
		if (isFixedUpdate)
		{
			GrabLogic();
		}
		if (stateTimer >= stateTimerMax)
		{
			StateSet(3);
		}
	}

	private void StateShooting()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = shootTime;
			singleShotNextFrame = false;
			EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, investigationRange);
			impulseShoot = true;
		}
		if (!isFixedUpdate)
		{
			if (singleShot)
			{
				if (singleShotNextFrame)
				{
					StateSet(4);
					singleShotNextFrame = false;
					return;
				}
				singleShotNextFrame = true;
			}
			impulseShoot = true;
			CorrectorAndLightLogic();
			if (!impactDetector.inCart)
			{
				stateTimer = stateTimerMax;
			}
		}
		if (isFixedUpdate)
		{
			GrabLogic();
		}
		if (stateTimer >= stateTimerMax)
		{
			StateSet(4);
		}
	}

	private void StateGoingBack()
	{
		if (stateStart && !isFixedUpdate)
		{
			stateStart = false;
			stateTimerMax = goBackFromShootTime;
		}
		if (!isFixedUpdate)
		{
			CorrectorAndLightLogic();
		}
		if (isFixedUpdate)
		{
			GrabLogic();
		}
		if (stateTimer >= stateTimerMax)
		{
			StateSet(1);
		}
	}

	private void StateMachine()
	{
		switch (stateCurrent)
		{
		case state.inactive:
			StateInactive();
			break;
		case state.active:
			StateActive();
			break;
		case state.buildup:
			StateBuildup();
			break;
		case state.shooting:
			StateShooting();
			break;
		case state.goingBack:
			StateGoingBack();
			break;
		}
	}

	private void GrabLogic()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)currentCorrector) || !Object.op_Implicit((Object)(object)impactDetector.currentCart))
		{
			return;
		}
		float y = rotationTargetY.eulerAngles.y;
		Quaternion rotation = ((Component)impactDetector.currentCart).transform.rotation;
		float y2 = rotation.eulerAngles.y;
		float num = Mathf.DeltaAngle(y2, y);
		Quaternion val = Quaternion.Euler(0f, y2 - num, 0f);
		bool flag = false;
		List<PhysGrabber> listOfAllGrabbers = physGrabObjectGrabArea.listOfAllGrabbers;
		bool flag2 = false;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (item.isRotating)
			{
				flag2 = true;
				break;
			}
		}
		if (physGrabObject.playerGrabbing.Count > 0 && listOfAllGrabbers.Count > 0)
		{
			foreach (PhysGrabber item2 in listOfAllGrabbers)
			{
				item2.OverrideGrabPoint(cannonGrabPoint.transform);
			}
			if (Object.op_Implicit((Object)(object)currentCorrector))
			{
				Transform overrideTransform = physGrabObject.playerGrabbing[0].playerAvatar.localCamera.GetOverrideTransform();
				currentCorrector.transform.rotation = overrideTransform.rotation;
				Transform transform = currentCorrector.transform;
				rotation = currentCorrector.transform.rotation;
				transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
				Vector3 val2 = overrideTransform.position + overrideTransform.rotation * Vector3.forward * 0.5f;
				Vector3 position = new Vector3(val2.x, ((Component)this).transform.position.y, val2.z);
				currentCorrector.transform.position = position;
			}
			flag = true;
			if (!flag2)
			{
				physGrabObject.OverrideTorqueStrength(0f);
			}
			physGrabObject.OverrideGrabStrength(0f);
			physGrabObject.OverrideMass(2f);
			Vector3 val3 = Vector3.down * 2f;
			rb.AddForce(val3, (ForceMode)0);
		}
		if (!flag2)
		{
			val = currentCorrector.transform.rotation;
			float num2 = 0.5f;
			if (flag)
			{
				num2 = 0.8f;
			}
			float num3 = Quaternion.Angle(val, ((Component)this).transform.rotation);
			num2 *= num3;
			physGrabObject.OverrideAngularDrag(5f);
			num2 = Mathf.Min(num2, 12f);
			Vector3 val4 = SemiFunc.PhysFollowRotation(((Component)this).transform, val, rb, num2 * 0.1f);
			rb.AddTorque(val4, (ForceMode)1);
		}
		Vector3 position2 = ((Component)this).transform.position;
		Vector3 position3 = currentCorrector.transform.position;
		if (flag)
		{
			Vector3 val5 = SemiFunc.PhysFollowPosition(position2, position3, rb.linearVelocity, 5f);
			rb.AddForce(val5 * 0.1f, (ForceMode)1);
		}
	}

	private void MovementSounds()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		float num = Quaternion.Angle(prevRotation, ((Component)this).transform.rotation);
		prevRotation = ((Component)this).transform.rotation;
		float num2 = num / Time.deltaTime;
		bool flag = num2 > 6f && physGrabObjectGrabArea.listOfAllGrabbers.Count > 0 && stateCurrent != state.inactive;
		float num3 = Mathf.Max(num2 * 0.01f, 0.5f);
		smoothPitch = Mathf.Lerp(smoothPitch, num3, Time.deltaTime * 5f);
		soundAimLoop.PlayLoop(flag, 1f, 0.5f, smoothPitch);
		if (SemiFunc.PerSecond(5f, (Object)(object)this) && ((num2 > 120f && quickTurnSoundCooldown <= 0f) & flag))
		{
			soundQuickTurn.Play(((Component)this).transform.position);
			quickTurnSoundCooldown = 1f;
		}
		if (quickTurnSoundCooldown > 0f)
		{
			quickTurnSoundCooldown -= Time.deltaTime;
		}
	}

	private void CorrectorAndLightLogic()
	{
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected Obj, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)impactDetector.currentCart))
		{
			if (!Object.op_Implicit((Object)(object)currentCorrector) || (Object.op_Implicit((Object)(object)currentCorrector) && (Object)(object)currentCorrector.transform.parent != (Object)(object)((Component)impactDetector.currentCart).transform))
			{
				if (Object.op_Implicit((Object)(object)currentCorrector))
				{
					Object.Destroy((Object)(object)currentCorrector);
				}
				GameObject val = new GameObject("GreatCorrector");
				currentCorrector = Object.Instantiate<GameObject>(val, ((Component)impactDetector.currentCart).transform);
				currentCorrector.transform.localPosition = Vector3.zero;
				currentCorrector.transform.localRotation = Quaternion.identity;
				currentCorrector.transform.localScale = Vector3.one;
				currentCorrector.transform.SetParent(((Component)impactDetector.currentCart).transform);
			}
			if (physGrabObject.playerGrabbing.Count > 0)
			{
				rotationTargetY = physGrabObject.playerGrabbing[0].playerAvatar.localCamera.GetOverrideTransform().rotation;
			}
		}
		if (!SemiFunc.PerSecond(15f, (Object)(object)this))
		{
			return;
		}
		if (physGrabObjectGrabArea.listOfAllGrabbers.Count > 0)
		{
			if (!handleGrabbed)
			{
				soundGrabStart.Play(((Component)this).transform.position);
				handleGrabbed = true;
			}
			int num = 0;
			foreach (Material grabMaterial in grabMaterials)
			{
				grabMaterial.SetColor("_EmissionColor", Color.green);
				if (num == 1)
				{
					grabMaterial.mainTextureOffset = new Vector2(0.5f, 0f);
				}
				num++;
			}
			cartGrabLight.color = Color.green;
			return;
		}
		if (handleGrabbed)
		{
			soundGrabEnd.Play(((Component)this).transform.position);
			handleGrabbed = false;
		}
		int num2 = 0;
		foreach (Material grabMaterial2 in grabMaterials)
		{
			grabMaterial2.SetColor("_EmissionColor", Color.red);
			if (num2 == 1)
			{
				grabMaterial2.mainTextureOffset = new Vector2(0f, 0f);
			}
			num2++;
		}
		cartGrabLight.color = Color.red;
	}

	public ItemCartCannonMain()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
	}
}
