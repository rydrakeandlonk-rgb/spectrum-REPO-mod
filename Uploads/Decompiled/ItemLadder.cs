using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class ItemLadder : MonoBehaviour
{
	public enum States
	{
		Denied,
		Neutral,
		Grabbed,
		OutOfBattery,
		Flickering
	}

	[Header("Assignables")]
	public Transform topTransformPivot;

	public Transform bottomTransform;

	public Transform plasmaBridgePivot;

	[Space]
	public MeshRenderer grabIcon;

	public MeshRenderer bridge;

	[Space]
	public Transform collisionCheckPivot;

	public Transform collisionCheck;

	public Transform groundCollisionCheck;

	[Space]
	public Transform colliderPivot;

	public Transform endCollider;

	[Space]
	public AnimationCurve animationCurve;

	[Header("Extension variables")]
	public float extendingSpeed = 1f;

	public float extensionBatteryDrain = 5f;

	public int extensionAmount = 2;

	[Header("Physic material")]
	public PhysicsMaterial defaultPhysicMaterial;

	public PhysicsMaterial littleStickyMaterial;

	[Header("Phys sounds")]
	public PhysAudio OffPhysAudio;

	public PhysAudio OnPhysAudio;

	[Header("Sounds")]
	public Sound grabSound;

	public Sound releaseSound;

	public Sound extendSound;

	public Sound retractSound;

	public Sound bridgeLoop;

	public Sound deniedSound;

	public Sound flickeringSound;

	[HideInInspector]
	public bool denied;

	private States currentState = States.Neutral;

	private States previousState = States.Neutral;

	private States previousPreviousState = States.Neutral;

	private Vector3 colliderPivotOriginalScale;

	private Vector3 collisionCheckPivotOriginalPosition;

	private Vector3 topTransformPivotOriginalPosition;

	private Vector3 plasmaBridgePivotOriginalScale;

	private Vector3 endColliderOriginalPosition;

	private PhotonView photonView;

	private ItemToggle itemToggle;

	private ItemEquippable itemEquippable;

	private ItemBattery itemBattery;

	private PhysGrabObject physGrabObject;

	private Rigidbody rb;

	private PhysGrabObjectImpactDetector impactDetector;

	private RoomVolumeCheck roomVolumeCheck;

	private Vector3 roomVolumeCheckOriginalPosition;

	private Vector3 roomVolumeCheckOriginalScale;

	private bool animate;

	private bool previousToggleState;

	private bool previousEquippedState;

	private int previousExtensionIndex;

	private int extensionIndex;

	private float deniedTime;

	private float animationCurveEval;

	private Material grabMaterial;

	private Color bridgeBaseColor;

	private Vector3 startTopPosition;

	private Vector3 startScale;

	private bool flickering;

	private float shopTimer = 10f;

	private bool shopTimerOn;

	private bool justGrabbed;

	private float flickeringTime = 3f;

	private float flickeringTimer;

	private float staticResetTimer;

	private Vector3 staticPosition;

	private Quaternion staticRotation;

	private float staticLerp;

	private void Start()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		itemToggle = ((Component)this).gameObject.GetComponent<ItemToggle>();
		itemEquippable = ((Component)this).gameObject.GetComponent<ItemEquippable>();
		photonView = ((Component)this).gameObject.GetComponent<PhotonView>();
		itemBattery = ((Component)this).gameObject.GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).gameObject.GetComponent<PhysGrabObject>();
		rb = ((Component)this).gameObject.GetComponent<Rigidbody>();
		impactDetector = ((Component)this).gameObject.GetComponent<PhysGrabObjectImpactDetector>();
		roomVolumeCheck = ((Component)this).gameObject.GetComponent<RoomVolumeCheck>();
		roomVolumeCheckOriginalPosition = roomVolumeCheck.CheckPosition;
		roomVolumeCheckOriginalScale = roomVolumeCheck.currentSize;
		previousToggleState = itemToggle.toggleState;
		previousEquippedState = itemEquippable.isEquipped;
		colliderPivotOriginalScale = colliderPivot.localScale;
		((Component)colliderPivot).GetComponent<Collider>().material = defaultPhysicMaterial;
		collisionCheckPivotOriginalPosition = collisionCheckPivot.localPosition;
		topTransformPivotOriginalPosition = topTransformPivot.localPosition;
		plasmaBridgePivotOriginalScale = plasmaBridgePivot.localScale;
		bridgeBaseColor = ((Renderer)bridge).material.GetColor("_Color");
		endColliderOriginalPosition = endCollider.localPosition;
		collisionCheckPivot.localScale = new Vector3(collisionCheckPivot.localScale.x, collisionCheckPivot.localScale.y, (float)extensionAmount);
	}

	private void Update()
	{
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.grabbed)
		{
			((Component)colliderPivot).GetComponent<Collider>().material = defaultPhysicMaterial;
		}
		else if (physGrabObject.playerGrabbing.Count > 0)
		{
			((Component)colliderPivot).GetComponent<Collider>().material = littleStickyMaterial;
		}
		bridgeLoop.PlayLoop(extensionIndex != 0, 1f, 1f);
		if (animate && !itemEquippable.isEquipped && !IsMinSize())
		{
			AnimateExtension();
		}
		else if (animate)
		{
			AnimateRetraction();
		}
		if (flickering)
		{
			FlickerBridge();
		}
		if (IndexChanged())
		{
			ScaleCollider();
			OffsetEndCollider();
			IncrementCollisionCheck();
			ChangePhysSound();
			UpdateRoomVolumeCheck();
			if (extensionIndex == 0)
			{
				retractSound.Play(physGrabObject.centerPoint);
				Retract();
				if (!itemEquippable.isEquipped)
				{
					StartAnimation();
				}
				flickering = false;
			}
			else
			{
				StartAnimation();
				extendSound.Play(physGrabObject.centerPoint);
				animationCurveEval = 0f;
				itemBattery.batteryLife -= extensionBatteryDrain;
			}
		}
		if (StateChanged())
		{
			if (currentState == States.Denied)
			{
				((Renderer)bridge).material.SetColor("_Color", new Color(1f, 0.8f, 0.2f) * 3f);
				((Renderer)grabIcon).material.SetColor("_EmissionColor", Color.green);
				((Renderer)grabIcon).material.mainTextureOffset = new Vector2(0.5f, 0f);
				deniedSound.Play(physGrabObject.centerPoint);
			}
			else if (currentState == States.Neutral)
			{
				if (previousPreviousState != States.Denied)
				{
					((Renderer)grabIcon).material.SetColor("_EmissionColor", Color.red);
					((Renderer)grabIcon).material.mainTextureOffset = new Vector2(0f, 0f);
				}
				((Renderer)bridge).material.SetColor("_Color", bridgeBaseColor);
			}
			else if (currentState == States.Grabbed)
			{
				((Renderer)grabIcon).material.SetColor("_EmissionColor", Color.green);
				((Renderer)grabIcon).material.mainTextureOffset = new Vector2(0.5f, 0f);
				((Renderer)bridge).material.SetColor("_Color", bridgeBaseColor);
				justGrabbed = true;
			}
			else if (currentState == States.OutOfBattery)
			{
				flickering = false;
				((Renderer)bridge).material.SetColor("_Color", Color.red * 0f);
				((Renderer)grabIcon).material.SetColor("_EmissionColor", Color.red);
				((Renderer)grabIcon).material.mainTextureOffset = new Vector2(0f, 0f);
				itemBattery.batteryLife = 0f;
			}
			else if (currentState == States.Flickering)
			{
				flickering = true;
				flickeringSound.Play(physGrabObject.centerPoint);
			}
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (EPressed())
		{
			if (!IsColliding() && !animate && !itemEquippable.isEquipped)
			{
				Extend();
			}
			else if (extensionIndex != 0 && !animate)
			{
				deniedTime = Time.time;
				if (!flickering)
				{
					StateChange(States.Denied);
				}
			}
		}
		if (currentState == States.Denied && Time.time - deniedTime >= 0.25f)
		{
			if (physGrabObject.grabbed)
			{
				StateChange(States.Grabbed);
			}
			else
			{
				StateChange(States.Neutral);
			}
		}
		if (EquipPressed() && extensionIndex != 0)
		{
			SetExtensionIndex(0);
		}
		if (itemBattery.batteryLife <= 0f && extensionIndex > 0 && !flickering)
		{
			StateChange(States.Flickering);
			flickering = true;
		}
		if (flickering)
		{
			flickeringTimer += Time.deltaTime;
			if (flickeringTimer >= flickeringTime)
			{
				flickeringTimer = 0f;
				GrabRelease();
				StateChange(States.OutOfBattery);
				SetExtensionIndex(0);
				flickering = false;
			}
		}
		if (!SemiFunc.RunIsShop())
		{
			return;
		}
		if (extensionIndex > 0)
		{
			bool flag = false;
			foreach (RoomVolume currentRoom in roomVolumeCheck.CurrentRooms)
			{
				if (currentRoom.Extraction)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				SetExtensionIndex(0);
				GrabRelease();
				shopTimerOn = false;
				shopTimer = 10f;
			}
		}
		if (shopTimerOn)
		{
			shopTimer -= Time.deltaTime;
			if (shopTimer <= 0f)
			{
				SetExtensionIndex(0);
				GrabRelease();
				shopTimerOn = false;
				shopTimer = 10f;
			}
		}
		void ChangePhysSound()
		{
			if (extensionIndex == 0)
			{
				impactDetector.ChangePhysAudio(OffPhysAudio);
			}
			else
			{
				impactDetector.ChangePhysAudio(OnPhysAudio);
			}
		}
		void OffsetEndCollider()
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			Vector3 localPosition = endCollider.localPosition;
			localPosition.z += extensionAmount;
			endCollider.localPosition = localPosition;
		}
		void UpdateRoomVolumeCheck()
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			int num = extensionAmount * extensionIndex;
			roomVolumeCheck.currentSize = new Vector3(roomVolumeCheckOriginalScale.x, roomVolumeCheckOriginalScale.y, roomVolumeCheckOriginalScale.z + (float)num * 0.165f);
			roomVolumeCheck.CheckPosition = new Vector3(roomVolumeCheckOriginalPosition.x, roomVolumeCheckOriginalPosition.y, roomVolumeCheckOriginalPosition.z + (float)num * 0.085f);
		}
	}

	private void FixedUpdate()
	{
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0324: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() && extensionIndex > 0 && physGrabObject.playerGrabbing.Count == 0)
		{
			physGrabObject.OverrideKinematic();
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		float value = 2f + 0.5f * (float)extensionIndex;
		physGrabObject.OverrideMass(value);
		if (extensionIndex > 0 && physGrabObject.playerGrabbing.Count == 0)
		{
			physGrabObject.OverrideZeroGravity();
			Vector3 val = SemiFunc.PhysFollowPosition(((Component)this).transform.position, staticPosition, physGrabObject.rb.linearVelocity, 5f);
			physGrabObject.rb.AddForce(Vector3.Lerp(Vector3.zero, val, staticLerp), (ForceMode)1);
			Vector3 val2 = SemiFunc.PhysFollowRotation(((Component)this).transform, staticRotation, physGrabObject.rb, 5f);
			physGrabObject.rb.AddTorque(Vector3.Lerp(Vector3.zero, val2, staticLerp), (ForceMode)1);
			staticLerp += Time.fixedDeltaTime;
			if (staticLerp >= 1f && (Vector3.Distance(((Component)this).transform.position, staticPosition) > 0.1f || Quaternion.Angle(((Component)this).transform.rotation, staticRotation) > 1f))
			{
				staticResetTimer += Time.fixedDeltaTime;
				if (staticResetTimer > 3f)
				{
					staticResetTimer = 0f;
					staticPosition = ((Component)this).transform.position;
					staticRotation = Quaternion.Euler(((Component)this).transform.eulerAngles.x, ((Component)this).transform.eulerAngles.y, 0f);
				}
			}
			else
			{
				staticResetTimer = 0f;
			}
		}
		else
		{
			staticPosition = ((Component)this).transform.position;
			staticRotation = Quaternion.Euler(((Component)this).transform.eulerAngles.x, ((Component)this).transform.eulerAngles.y, 0f);
			staticLerp = 0f;
		}
		if (!physGrabObject.grabbed)
		{
			return;
		}
		physGrabObject.OverrideExtraTorqueStrengthDisable();
		physGrabObject.OverrideTorqueStrength(Mathf.Min(4f + (float)extensionIndex * 0.15f, 2f));
		physGrabObject.OverrideGrabStrength(Mathf.Min(1f + 0.5f * (float)extensionIndex, 3f));
		if (justGrabbed && !IsCollidingWithGround())
		{
			justGrabbed = false;
			if (physGrabObject.playerGrabbing.Count == 1)
			{
				Quaternion turnX = Quaternion.Euler(0f, 0f, 0f);
				Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
				Quaternion identity = Quaternion.identity;
				physGrabObject.TurnXYZ(turnX, turnY, identity);
			}
		}
	}

	public void GrabRelease()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing.ToList())
		{
			if (!SemiFunc.IsMultiplayer())
			{
				item.ReleaseObject(photonView.ViewID);
			}
			else
			{
				item.OverrideGrabRelease(photonView.ViewID, 0.1f);
			}
			flag = true;
		}
		if (flag)
		{
			if (GameManager.instance.gameMode == 0)
			{
				GrabReleaseRPC();
			}
			else
			{
				photonView.RPC("GrabReleaseRPC", (RpcTarget)0, Array.Empty<object>());
			}
		}
	}

	[PunRPC]
	private void GrabReleaseRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			GameDirector.instance.CameraShake.ShakeDistance(5f, 3f, 8f, ((Component)this).transform.position, 0.5f);
			GameDirector.instance.CameraImpact.ShakeDistance(5f, 3f, 8f, ((Component)this).transform.position, 0.1f);
			physGrabObject.grabDisableTimer = 1f;
		}
	}

	private void StartAnimation()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		startTopPosition = topTransformPivot.localPosition;
		startScale = plasmaBridgePivot.localScale;
		if (extensionIndex > 0)
		{
			animationCurveEval = 0f;
		}
		else
		{
			animationCurveEval = 1f;
		}
		animate = true;
	}

	private void AnimateExtension()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		animationCurveEval += Time.deltaTime * extendingSpeed;
		float num = animationCurve.Evaluate(animationCurveEval);
		Vector3 val = startTopPosition;
		val.z = (float)(extensionIndex * extensionAmount) + 1f;
		topTransformPivot.localPosition = Vector3.Lerp(startTopPosition, val, num);
		Vector3 val2 = startScale;
		val2.z = extensionIndex * extensionAmount;
		plasmaBridgePivot.localScale = Vector3.Lerp(startScale, val2, num);
		if (animationCurveEval >= 1f)
		{
			animate = false;
			GameDirector.instance.CameraShake.ShakeDistance(0.5f, 1f, 4f, topTransformPivot.position, 0.05f);
			GameDirector.instance.CameraImpact.ShakeDistance(1f, 1f, 4f, topTransformPivot.position, 0.05f);
		}
	}

	private void AnimateRetraction()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		animationCurveEval -= Time.deltaTime * 3f;
		float num = animationCurve.Evaluate(animationCurveEval);
		Vector3 val = topTransformPivotOriginalPosition;
		topTransformPivot.localPosition = Vector3.Lerp(val, startTopPosition, num);
		Vector3 val2 = plasmaBridgePivotOriginalScale;
		plasmaBridgePivot.localScale = Vector3.Lerp(val2, startScale, num);
		if (animationCurveEval <= 0f)
		{
			animate = false;
			plasmaBridgePivot.localScale = plasmaBridgePivotOriginalScale;
			if (!itemEquippable.isEquipped)
			{
				GameDirector.instance.CameraShake.ShakeDistance(0.5f, 1f, 4f, physGrabObject.centerPoint, 0.05f);
				GameDirector.instance.CameraImpact.ShakeDistance(1.5f, 1f, 4f, physGrabObject.centerPoint, 0.05f);
			}
		}
	}

	[PunRPC]
	public void SetExtensionIndexRPC(int _index, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			extensionIndex = _index;
		}
	}

	private void SetExtensionIndex(int _index)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && extensionIndex != _index)
		{
			if (!SemiFunc.IsMultiplayer())
			{
				SetExtensionIndexRPC(_index);
				return;
			}
			photonView.RPC("SetExtensionIndexRPC", (RpcTarget)0, new object[1] { _index });
		}
	}

	public void StateChange(States _state)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState != _state)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("StateChangeRPC", (RpcTarget)0, new object[1] { _state });
			}
			else
			{
				StateChangeRPC(_state);
			}
		}
	}

	[PunRPC]
	public void StateChangeRPC(States _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
		}
	}

	private void Retract()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		colliderPivot.localScale = colliderPivotOriginalScale;
		endCollider.localPosition = endColliderOriginalPosition;
		ResetCollisionCheckPosition();
		if (!itemEquippable.isEquipped)
		{
			StartAnimation();
			return;
		}
		topTransformPivot.localPosition = topTransformPivotOriginalPosition;
		plasmaBridgePivot.localScale = plasmaBridgePivotOriginalScale;
	}

	private void Extend()
	{
		if (flickering || itemBattery.batteryLife >= extensionBatteryDrain)
		{
			SetExtensionIndex(extensionIndex + 1);
		}
	}

	private bool IsMinSize()
	{
		return extensionIndex == 0;
	}

	private Collider[] GetCollidingColliders()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		return Physics.OverlapBox(collisionCheck.position, collisionCheck.lossyScale / 2f, collisionCheck.rotation);
	}

	private Collider[] GetCollidingCollidersGround()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		return Physics.OverlapBox(groundCollisionCheck.position, groundCollisionCheck.lossyScale / 2f, groundCollisionCheck.rotation);
	}

	private bool IsColliding()
	{
		Collider[] collidingColliders = GetCollidingColliders();
		foreach (Collider val in collidingColliders)
		{
			if (((Component)val).gameObject.layer != LayerMask.NameToLayer("RoomVolume") && !((Component)val).transform.IsChildOf(((Component)this).transform))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCollidingWithGround()
	{
		Collider[] collidingCollidersGround = GetCollidingCollidersGround();
		foreach (Collider val in collidingCollidersGround)
		{
			if (((Component)val).gameObject.layer != LayerMask.NameToLayer("RoomVolume") && !((Component)val).transform.IsChildOf(((Component)this).transform))
			{
				return true;
			}
		}
		return false;
	}

	private bool EPressed()
	{
		if (itemToggle.toggleState != previousToggleState)
		{
			previousToggleState = itemToggle.toggleState;
			return true;
		}
		return false;
	}

	private bool EquipPressed()
	{
		if (itemEquippable.isEquipped != previousEquippedState)
		{
			previousEquippedState = itemEquippable.isEquipped;
			return true;
		}
		return false;
	}

	private bool StateChanged()
	{
		if (previousState != currentState)
		{
			previousPreviousState = previousState;
			previousState = currentState;
			return true;
		}
		return false;
	}

	private void ResetCollisionCheckPosition()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		collisionCheckPivot.localPosition = collisionCheckPivotOriginalPosition;
	}

	private void ScaleCollider()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Vector3 localScale = colliderPivot.localScale;
		localScale.z += extensionAmount;
		colliderPivot.localScale = localScale;
	}

	private void IncrementCollisionCheck()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		Vector3 localPosition = collisionCheckPivot.localPosition;
		localPosition.z += extensionAmount;
		collisionCheckPivot.localPosition = localPosition;
	}

	private bool IndexChanged()
	{
		if (previousExtensionIndex != extensionIndex)
		{
			previousExtensionIndex = extensionIndex;
			return true;
		}
		return false;
	}

	public void OnGrab()
	{
		if (currentState != States.Grabbed && !flickering)
		{
			StateChange(States.Grabbed);
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.RunIsShop())
		{
			shopTimerOn = false;
			shopTimer = 10f;
		}
	}

	public void OnRelease()
	{
		if (physGrabObject.playerGrabbing.Count == 0 && !flickering && currentState != States.OutOfBattery)
		{
			StateChange(States.Neutral);
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.RunIsShop() && extensionIndex > 0)
		{
			shopTimerOn = true;
			shopTimer = 10f;
		}
		justGrabbed = false;
	}

	public void FlickerBridge()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		float num = 0.7f + Mathf.PingPong(Time.time, 0.1f);
		((Renderer)bridge).material.SetColor("_Color", Color.red * num * 3f);
	}
}
