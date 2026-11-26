using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemReviveItem : MonoBehaviour
{
	[Header("Mesh renderers")]
	public List<MeshRenderer> frameMeshRenderers;

	public MeshRenderer lampMeshRenderer;

	public MeshRenderer FaceMeshRenderer;

	[Header("Hands")]
	public Transform handL;

	public Transform handR;

	public Transform handsParent;

	[Header("Light")]
	public Transform lightTransform;

	public Light spotLight;

	public Light pointLight;

	[Header("Propeller")]
	public Transform propeller;

	[Header("Face")]
	public GameObject face;

	public AnimationCurve faceBounceCurve;

	[Header("Physic Materials")]
	public PhysicsMaterial physicMaterialSlippery;

	[Header("Line renderers")]
	public LineRenderer lineRendererL;

	public LineRenderer lineRendererR;

	[Header("Shockwave")]
	public GameObject shockwavePrefab;

	[Header("Particles")]
	public Transform reviveParticles;

	public Transform handChangeParticles;

	public Light healingLight;

	public GameObject teleportParticles;

	[Header("Revive Screen Effect")]
	public GameObject reviveScreenEffect;

	[Header("Sounds")]
	public Sound wiresOutSound;

	public Sound wiresInSound;

	public Sound expressionChangeSound;

	public Sound propellerLoopStart;

	public Sound propellerLoopEnd;

	public Sound propellerLoop;

	public Sound reviveSound;

	public Sound ReviveChargeUpSound;

	public Sound ReviveChargeUpInterruptedSound;

	public Sound screenOnSound;

	public Sound screenOffSound;

	public Sound teleportSound;

	[Header("Expression Sounds")]
	public Sound happyClosedSound;

	public Sound surprisedSound;

	public Sound sadOpenSound;

	public Sound sadClosedSound;

	public Sound neutralSound;

	public Sound sleepSound;

	public Sound strainSound;

	public Sound happyOpenSound;

	public Sound hiSound;

	public Sound byeSound;

	private bool hasTarget;

	private bool previousEquippedState;

	private bool previousHeldState;

	private bool emission;

	private PhotonView photonView;

	private ItemEquippable itemEquippable;

	private PhysGrabObject physGrabObject;

	private PhysGrabObjectImpactDetector impactDetector;

	private ItemAttributes itemAttributes;

	private Rigidbody rb;

	private PlayerAvatar playerTarget;

	private PlayerAvatar previousPlayerTarget;

	private PlayerAvatar lastEquipper;

	private PhysGrabObjectImpactDetector physGrabObjectImpactDetector;

	private float reviveTime = 5f;

	private float reviveTimer;

	private float visualTimer;

	private float handDistance = 2f;

	private float hoverHeight = 1f;

	private float hoverBobAmount = 0.3f;

	private float hoverBobSpeed = 0.2f;

	private float desiredDistance = 0.8f;

	private float springConstant = 30f;

	private float dampingCoefficient = 15f;

	private float failSafeReviveTime = 15f;

	private float teleportSpotTimer;

	private float heldTimer;

	private float hasTargetTimer;

	private float lostTargetTimer;

	private float releaseTimer;

	private float lastExpressionBeforeRelease;

	private float notMovingTimer;

	private bool isInReleaseSequence;

	private float happyClosed = 0.023f;

	private float surprised = 0.101f;

	private float sadOpen = 0.179f;

	private float sadClosed = 0.257f;

	private float neutral = 0.335f;

	private float sleep = 0.413f;

	private float strain = 0.491f;

	private float happyOpen = 0.569f;

	private float hi = 0.647f;

	private float bye = 0.728f;

	private float currentExpressionOffset;

	private float faceBounceTimer;

	private float faceBaseYOffset = 1.24f;

	private float propellerSpeed;

	private float propellerMaxSpeed = 2000f;

	private float propellerSpeedUpRate = 8f;

	private float propellerSlowDownRate = 2f;

	private bool wiresOut;

	private bool wasCharging;

	private AudioSource chargeUpAudioSource;

	private AudioSource strainSoundAudioSource;

	private Vector3 handsParentOriginalPosition;

	private Vector3 handLOriginalPosition;

	private Vector3 handROriginalPosition;

	private Vector3 lightOriginalRotation;

	private void Start()
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		physGrabObjectImpactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		handsParentOriginalPosition = handsParent.localPosition;
		handLOriginalPosition = handL.localPosition;
		handROriginalPosition = handR.localPosition;
		lightOriginalRotation = lightTransform.localEulerAngles;
		ToggleFrameEmission(_emission: false);
	}

	private void Update()
	{
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0312: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05db: Unknown result type (might be due to invalid IL or missing references)
		//IL_05eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0560: Unknown result type (might be due to invalid IL or missing references)
		//IL_056b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0570: Unknown result type (might be due to invalid IL or missing references)
		//IL_0575: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_0477: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
		bool flag = EquipChanged();
		bool flag2 = PlayerTargetChanged();
		bool flag3 = HeldChanged();
		bool flag4 = EveryOneDead();
		bool flag5 = physGrabObject.playerGrabbing.Count > 0;
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (hasTarget && (!playerTarget.isDisabled || (playerTarget.playerDeathHead.inExtractionPoint && Object.op_Implicit((Object)(object)RoundDirector.instance.extractionPointCurrent) && RoundDirector.instance.extractionPointCurrent.currentState != ExtractionPoint.State.Idle && RoundDirector.instance.extractionPointCurrent.currentState != ExtractionPoint.State.Active)))
			{
				UpdatePlayerTarget(null);
			}
			if (hasTarget && (flag5 || itemEquippable.isEquipped))
			{
				UpdatePlayerTarget(null);
			}
			if (SemiFunc.PerSecond(5f, (Object)(object)this) && !flag5 && !itemEquippable.isEquipped)
			{
				float range = (flag4 ? 6f : 3f);
				PlayerAvatar playerAvatar = FindClosestDeadPlayer(range);
				if (Object.op_Implicit((Object)(object)playerAvatar) && (Object)(object)playerAvatar != (Object)(object)playerTarget)
				{
					if (!hasTarget)
					{
						UpdatePlayerTarget(playerAvatar);
					}
					else
					{
						float num = Vector3.Distance(((Component)this).transform.position, ((Component)playerTarget.playerDeathHead).transform.position);
						if (Vector3.Distance(((Component)this).transform.position, ((Component)playerAvatar.playerDeathHead).transform.position) < num - 1.5f)
						{
							UpdatePlayerTarget(playerAvatar);
						}
					}
				}
			}
			if (hasTarget && !SemiFunc.RunIsShop() && Object.op_Implicit((Object)(object)playerTarget.playerDeathHead))
			{
				if (EveryOneDead() && Object.op_Implicit((Object)(object)SpectateCamera.instance))
				{
					SpectateCamera.instance.OverrideDeathDelay();
				}
				reviveTimer -= Time.deltaTime;
				Vector3 linearVelocity = playerTarget.playerDeathHead.physGrabObject.rb.linearVelocity;
				if (linearVelocity.magnitude < 0.15f)
				{
					notMovingTimer += Time.deltaTime;
				}
				else
				{
					notMovingTimer = 0f;
				}
				bool flag6 = notMovingTimer >= 2.5f;
				bool flag7 = reviveTimer <= reviveTime - failSafeReviveTime;
				if (((reviveTimer <= 0f) & flag6) | flag7)
				{
					ReviveAndDestroy(flag7);
				}
			}
			if (flag)
			{
				if (itemEquippable.isEquipped)
				{
					PlayerAvatar ownerPlayerAvatar = itemEquippable.GetOwnerPlayerAvatar();
					if (Object.op_Implicit((Object)(object)ownerPlayerAvatar))
					{
						lastEquipper = ownerPlayerAvatar;
					}
				}
				else
				{
					if (Object.op_Implicit((Object)(object)lastEquipper) && lastEquipper.isDisabled)
					{
						UpdatePlayerTarget(lastEquipper);
					}
					lastEquipper = null;
				}
			}
		}
		FacialExpressionLogic(flag5);
		bool flag8 = Vector3.Distance(handsParent.localPosition, handsParentOriginalPosition) >= 0.1f;
		if (flag8 && !wiresOut)
		{
			wiresOutSound.Play(((Component)this).transform.position);
		}
		else if (!flag8 && wiresOut)
		{
			wiresInSound.Play(((Component)this).transform.position);
		}
		wiresOut = flag8;
		if (!flag8)
		{
			FadeHandLineOut(lineRendererL);
			FadeHandLineOut(lineRendererR);
		}
		else
		{
			DrawHandLines();
		}
		if (!((Behaviour)spotLight).enabled && hasTarget)
		{
			((Behaviour)spotLight).enabled = true;
			((Behaviour)pointLight).enabled = true;
		}
		else if (((Behaviour)spotLight).enabled && !hasTarget)
		{
			((Behaviour)spotLight).enabled = false;
			((Behaviour)pointLight).enabled = false;
		}
		if (flag2 && !flag5)
		{
			ToggleFrameEmission(hasTarget);
		}
		if ((flag3 & flag5) && !emission)
		{
			ToggleFrameEmission(_emission: true);
		}
		if (Object.op_Implicit((Object)(object)propeller))
		{
			float num2 = (hasTarget ? propellerMaxSpeed : 0f);
			float num3 = (hasTarget ? propellerSpeedUpRate : propellerSlowDownRate);
			propellerSpeed = Mathf.Lerp(propellerSpeed, num2, Time.deltaTime * num3);
			propeller.Rotate(Vector3.up, propellerSpeed * Time.deltaTime);
			if (flag2)
			{
				if (hasTarget)
				{
					propellerLoopStart.Play(((Component)this).transform.position);
				}
				else
				{
					propellerLoopEnd.Play(((Component)this).transform.position);
				}
			}
			propellerLoop.PlayLoop(hasTarget, 1f, 1f);
		}
		if (hasTarget && !itemEquippable.isEquipped)
		{
			visualTimer += Time.deltaTime;
			lightTransform.rotation = Quaternion.Slerp(lightTransform.rotation, Quaternion.LookRotation(((Component)playerTarget.playerDeathHead).transform.position - lightTransform.position), Time.deltaTime * 5f);
			if (visualTimer > 2f)
			{
				MoveHandsAroundHead();
			}
			else
			{
				MoveHandsBack();
			}
			return;
		}
		if (flag)
		{
			HardResetVisuals();
		}
		visualTimer = 0f;
		MoveHandsBack();
		lightTransform.localRotation = Quaternion.Slerp(lightTransform.localRotation, Quaternion.Euler(lightOriginalRotation), Time.deltaTime * 5f);
	}

	private void FixedUpdate()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || itemEquippable.isEquipped)
		{
			return;
		}
		if (hasTarget)
		{
			Transform transform = ((Component)playerTarget.playerDeathHead).transform;
			if (Vector3.Distance(((Component)this).transform.position, transform.position) > 4f)
			{
				FindTeleportSpot(transform);
			}
			physGrabObject.OverrideMaterial(physicMaterialSlippery);
			Rigidbody val = playerTarget.playerDeathHead.physGrabObject.rb;
			Vector3 val2 = (Object.op_Implicit((Object)(object)val) ? val.linearVelocity : Vector3.zero);
			float magnitude = val2.magnitude;
			float num = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;
			Vector3 val3 = ((Component)this).transform.position - transform.position;
			val3.y = 0f;
			if (val3.magnitude < 0.1f)
			{
				val3 = Vector3.forward;
			}
			val3 = val3.normalized;
			Vector3 val4 = transform.position + val3 * desiredDistance + Vector3.up * (hoverHeight + num) - ((Component)this).transform.position;
			Vector3 val5 = springConstant * val4;
			Vector3 val6 = (0f - dampingCoefficient) * rb.linearVelocity;
			Vector3 val7 = Vector3.ClampMagnitude(val5 + val6, 20f);
			rb.AddForce(val7);
			if (magnitude > 2f)
			{
				Vector3 val8 = val2 - rb.linearVelocity;
				float num2 = Mathf.Clamp01((magnitude - 2f) / 8f) * 15f;
				rb.AddForce(val8 * num2);
			}
			float num3 = ((transform.position.y < ((Component)this).transform.position.y - 0.5f && val2.y < -2f) ? 0.5f : 1.1f);
			Rigidbody val9 = rb;
			Vector3 val10 = Vector3.up * rb.mass;
			Vector3 gravity = Physics.gravity;
			val9.AddForce(val10 * gravity.magnitude * num3);
			Quaternion targetRotation = Quaternion.LookRotation(transform.position - ((Component)this).transform.position);
			Vector3 val11 = SemiFunc.PhysFollowRotation(((Component)this).transform, targetRotation, rb, 2f);
			val11 = Vector3.Lerp(Vector3.zero, val11, 0.8f * Time.fixedDeltaTime);
			rb.AddTorque(val11, (ForceMode)1);
			physGrabObject.OverrideTorqueStrength(0f);
		}
		else
		{
			if (physGrabObject.playerGrabbing.Count <= 0)
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
				Quaternion turnX = Quaternion.Euler(0f, -180f, 0f);
				Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
				Quaternion identity = Quaternion.identity;
				physGrabObject.TurnXYZ(turnX, turnY, identity);
				physGrabObject.OverrideTorqueStrength(physGrabObject.massOriginal);
			}
		}
	}

	[PunRPC]
	private void ReviveParticlesRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			Object.Instantiate<GameObject>(shockwavePrefab, ((Component)playerTarget.playerDeathHead).transform.position, Quaternion.identity);
			reviveParticles.parent = null;
			reviveParticles.position = ((Component)playerTarget.playerDeathHead).transform.position;
			((Component)reviveParticles).GetComponent<ParticleSystem>().Play(true);
			reviveSound.Play(reviveParticles.position);
		}
	}

	private void ReviveParticles()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ReviveParticlesRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				ReviveParticlesRPC();
			}
		}
	}

	private void UpdatePlayerTarget(PlayerAvatar _player)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() || (Object)(object)_player == (Object)(object)playerTarget || SemiFunc.RunIsShop() || (Object.op_Implicit((Object)(object)_player) && (!_player.isDisabled || (_player.playerDeathHead.inExtractionPoint && Object.op_Implicit((Object)(object)RoundDirector.instance.extractionPointCurrent) && RoundDirector.instance.extractionPointCurrent.currentState != ExtractionPoint.State.Idle && RoundDirector.instance.extractionPointCurrent.currentState != ExtractionPoint.State.Active))))
		{
			return;
		}
		playerTarget = _player;
		hasTarget = (Object)(object)playerTarget != (Object)null;
		reviveTimer = reviveTime;
		notMovingTimer = 0f;
		if (SemiFunc.IsMultiplayer())
		{
			int num = -1;
			if (Object.op_Implicit((Object)(object)_player))
			{
				num = _player.photonView.ViewID;
			}
			photonView.RPC("UpdatePlayerTargetRPC", (RpcTarget)0, new object[1] { num });
		}
	}

	[PunRPC]
	private void UpdatePlayerTargetRPC(int _photonViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			playerTarget = SemiFunc.PlayerAvatarGetFromPhotonID(_photonViewID);
			hasTarget = (Object)(object)playerTarget != (Object)null;
		}
	}

	private void ReviveScreenEffectSpawn()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				int viewID = playerTarget.photonView.ViewID;
				photonView.RPC("ReviveScreenEffectSpawnRPC", (RpcTarget)0, new object[1] { viewID });
			}
			else
			{
				ReviveScreenEffectSpawnRPC(playerTarget.photonView.ViewID);
			}
		}
	}

	[PunRPC]
	private void ReviveScreenEffectSpawnRPC(int _photonViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_photonViewID);
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
		{
			Transform overrideTransform = playerAvatar.localCamera.GetOverrideTransform();
			GameObject val = Object.Instantiate<GameObject>(reviveScreenEffect, overrideTransform);
			val.transform.localPosition = Vector3.zero;
			val.transform.localRotation = Quaternion.identity;
			ReviveScreenEffect component = val.GetComponent<ReviveScreenEffect>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.Active();
			}
		}
	}

	[PunRPC]
	public void TeleportEffectRPC(Vector3 startPosition, Vector3 endPosition, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			if (teleportSound != null)
			{
				teleportSound.Play(startPosition);
				teleportSound.Play(endPosition);
			}
			if ((Object)(object)teleportParticles != (Object)null)
			{
				Object.Instantiate<GameObject>(teleportParticles, startPosition, Quaternion.identity);
				Object.Instantiate<GameObject>(teleportParticles, endPosition, Quaternion.identity);
			}
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

	private PlayerAvatar FindClosestDeadPlayer(float _range = 3f)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar result = null;
		float num = float.MaxValue;
		foreach (PlayerAvatar item in SemiFunc.PlayerGetList())
		{
			if (!((Object)(object)item == (Object)null) && item.isDisabled && Object.op_Implicit((Object)(object)item.playerDeathHead))
			{
				float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)item.playerDeathHead).transform.position);
				if (num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		if (num > _range)
		{
			return null;
		}
		return result;
	}

	private void MoveHandsAroundHead()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		if (hasTarget)
		{
			Vector3 val = ((Component)playerTarget.playerDeathHead).transform.position + Vector3.up * 0.2f;
			float num = Vector3.Distance(handsParent.position, val);
			float num2 = Vector3.Distance(((Component)this).transform.position, val);
			float num3 = Mathf.Clamp01((num2 - num) / Mathf.Max(num2, 0.01f));
			float num4 = 4f * num3 * (1f - num3);
			float num5 = 1f + num4 * 5f;
			handsParent.position = Vector3.Lerp(handsParent.position, val, Time.deltaTime * 0.6f * num5);
			handsParent.localRotation = Quaternion.Slerp(handsParent.localRotation, Quaternion.Euler(0f, 180f, 0f), Time.deltaTime * 5f);
			float num6 = Mathf.Sin(Time.time * 1.2f) * 0.6f;
			float num7 = Mathf.Sin(Time.time * 1.2f + MathF.PI) * 0.6f;
			Vector3 val2 = handLOriginalPosition + Vector3.up * num6 + Vector3.left * handDistance / 2f;
			Vector3 val3 = handROriginalPosition + Vector3.up * num7 - Vector3.left * handDistance / 2f;
			handL.localPosition = Vector3.Lerp(handL.localPosition, val2, Time.deltaTime * 3f);
			handR.localPosition = Vector3.Lerp(handR.localPosition, val3, Time.deltaTime * 3f);
		}
	}

	private void MoveHandsBack()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		handsParent.localPosition = Vector3.Lerp(handsParent.localPosition, handsParentOriginalPosition, Time.deltaTime * 5f);
		handL.localPosition = Vector3.Lerp(handL.localPosition, handLOriginalPosition, Time.deltaTime * 5f);
		handR.localPosition = Vector3.Lerp(handR.localPosition, handROriginalPosition, Time.deltaTime * 5f);
	}

	private void ReviveAndDestroy(bool _failSafe = false)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && hasTarget)
		{
			ReviveParticles();
			ReviveScreenEffectSpawn();
			playerTarget.Revive();
			playerTarget.playerHealth.HealOther(49, effect: true);
			StatsManager.instance.ItemRemove(itemAttributes.instanceName);
			physGrabObjectImpactDetector.destroyDisable = false;
			if (_failSafe && Object.op_Implicit((Object)(object)TruckSafetySpawnPoint.instance))
			{
				playerTarget.tumble.TumbleSet(_isTumbling: true, _playerInput: false);
				playerTarget.tumble.physGrabObject.Teleport(((Component)TruckSafetySpawnPoint.instance).transform.position, ((Component)TruckSafetySpawnPoint.instance).transform.rotation);
				playerTarget.tumble.physGrabObject.rb.linearVelocity = Vector3.zero;
				playerTarget.tumble.physGrabObject.rb.angularVelocity = Vector3.zero;
			}
			UpdatePlayerTarget(null);
			impactDetector.DestroyObject();
		}
	}

	private void HardResetVisuals()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		lineRendererL.widthMultiplier = 0f;
		lineRendererR.widthMultiplier = 0f;
		handsParent.localPosition = handsParentOriginalPosition;
		handL.localPosition = handLOriginalPosition;
		handR.localPosition = handROriginalPosition;
		((Behaviour)spotLight).enabled = false;
		ToggleFrameEmission(_emission: false);
	}

	private bool EquipChanged()
	{
		if (itemEquippable.isEquipped != previousEquippedState)
		{
			previousEquippedState = itemEquippable.isEquipped;
			return true;
		}
		return false;
	}

	private bool PlayerTargetChanged()
	{
		if ((Object)(object)playerTarget != (Object)(object)previousPlayerTarget)
		{
			previousPlayerTarget = playerTarget;
			return true;
		}
		return false;
	}

	private void FindTeleportSpot(Transform _target)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		if (!hasTarget || !Object.op_Implicit((Object)(object)_target))
		{
			return;
		}
		if (teleportSpotTimer <= 0f)
		{
			for (int i = 0; i < 10; i++)
			{
				Vector3 val = _target.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
				float num = Vector3.Distance(val, _target.position);
				float num2 = Mathf.Max(0f, num - 0.2f);
				RaycastHit[] array = Physics.RaycastAll(val, _target.position - val, num2, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()));
				bool flag = false;
				RaycastHit[] array2 = array;
				for (int j = 0; j < array2.Length; j++)
				{
					RaycastHit val2 = array2[j];
					if ((Object)(object)((Component)val2.transform).GetComponentInParent<Rigidbody>() != (Object)(object)((Component)_target).GetComponentInParent<Rigidbody>() && (Object)(object)val2.transform != (Object)(object)((Component)this).transform)
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
					rb.linearVelocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;
					break;
				}
			}
			teleportSpotTimer = 0.2f;
		}
		else
		{
			teleportSpotTimer -= Time.fixedDeltaTime;
		}
	}

	private void ToggleFrameEmission(bool _emission)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		Color val = (_emission ? Color.white : Color.black);
		foreach (MeshRenderer frameMeshRenderer in frameMeshRenderers)
		{
			((Renderer)frameMeshRenderer).material.SetColor("_EmissionColor", val);
		}
		if (_emission && !emission)
		{
			screenOnSound.Play(((Component)this).transform.position);
		}
		else if (!_emission && emission)
		{
			screenOffSound.Play(((Component)this).transform.position);
		}
		emission = _emission;
		if (hasTarget)
		{
			((Renderer)lampMeshRenderer).material.SetColor("_EmissionColor", Color.white);
		}
		else
		{
			((Renderer)lampMeshRenderer).material.SetColor("_EmissionColor", Color.black);
		}
		face.SetActive(_emission);
	}

	private bool EveryOneDead()
	{
		foreach (PlayerAvatar item in new List<PlayerAvatar>(SemiFunc.PlayerGetList()))
		{
			if ((Object)(object)item == (Object)null || !item.isDisabled)
			{
				return false;
			}
		}
		return true;
	}

	private bool HeldChanged()
	{
		if (physGrabObject.playerGrabbing.Count > 0 != previousHeldState)
		{
			previousHeldState = physGrabObject.playerGrabbing.Count > 0;
			return true;
		}
		return false;
	}

	private void FacialExpressionLogic(bool _isHeld)
	{
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		bool flag = false;
		if (hasTarget)
		{
			hasTargetTimer += Time.deltaTime;
			lostTargetTimer = 0f;
			if (hasTargetTimer < 1f)
			{
				SetFacialExpression(surprised);
			}
			else if (hasTargetTimer < 2.5f)
			{
				SetFacialExpression(happyOpen);
			}
			else if (hasTargetTimer < 4.7f)
			{
				SetFacialExpression(strain);
				flag = true;
				((Component)handChangeParticles).GetComponent<ParticleSystem>().Play(true);
				healingLight.intensity = Mathf.Lerp(healingLight.intensity, 5f, Time.deltaTime * 3f);
				((Component)healingLight).transform.position = ((Component)playerTarget.playerDeathHead).transform.position;
			}
			else
			{
				SetFacialExpression(bye);
				healingLight.intensity = Mathf.Lerp(healingLight.intensity, 0f, Time.deltaTime * 5f);
			}
			if (flag && !wasCharging)
			{
				chargeUpAudioSource = ReviveChargeUpSound.Play(((Component)this).transform.position);
			}
			wasCharging = flag;
			return;
		}
		if (hasTargetTimer > 0f)
		{
			if (wasCharging)
			{
				if ((Object)(object)chargeUpAudioSource != (Object)null && chargeUpAudioSource.isPlaying)
				{
					chargeUpAudioSource.Stop();
				}
				if ((Object)(object)strainSoundAudioSource != (Object)null && strainSoundAudioSource.isPlaying)
				{
					strainSoundAudioSource.Stop();
				}
				ReviveChargeUpInterruptedSound.Play(((Component)this).transform.position);
			}
			lostTargetTimer = 3f;
			if (Object.op_Implicit((Object)(object)healingLight))
			{
				healingLight.intensity = 0f;
			}
		}
		hasTargetTimer = 0f;
		wasCharging = false;
		if (lostTargetTimer > 0f)
		{
			((Component)handChangeParticles).GetComponent<ParticleSystem>().Stop(true);
			healingLight.intensity = 0f;
			lostTargetTimer -= Time.deltaTime;
			if (lostTargetTimer > 1.5f)
			{
				SetFacialExpression(sadOpen);
			}
			else
			{
				SetFacialExpression(sadClosed);
			}
			if (_isHeld)
			{
				heldTimer = 5f;
				releaseTimer = 0f;
				isInReleaseSequence = false;
			}
			if (lostTargetTimer <= 0f && !_isHeld)
			{
				isInReleaseSequence = true;
				releaseTimer = 0f;
				lastExpressionBeforeRelease = currentExpressionOffset;
			}
			return;
		}
		if (_isHeld)
		{
			heldTimer += Time.deltaTime;
			releaseTimer = 0f;
			isInReleaseSequence = false;
			if (heldTimer < 2f)
			{
				SetFacialExpression(neutral);
			}
			else if (heldTimer < 4f)
			{
				SetFacialExpression(happyClosed);
			}
			else if (heldTimer < 5f)
			{
				SetFacialExpression(hi);
			}
			else
			{
				SetFacialExpression(happyClosed);
			}
			lastExpressionBeforeRelease = currentExpressionOffset;
			return;
		}
		if (heldTimer > 0f)
		{
			isInReleaseSequence = true;
			heldTimer = 0f;
		}
		if (!isInReleaseSequence)
		{
			return;
		}
		releaseTimer += Time.deltaTime;
		if (releaseTimer < 1f)
		{
			SetFacialExpression(lastExpressionBeforeRelease);
			return;
		}
		if (releaseTimer < 2f)
		{
			SetFacialExpression(sleep);
			return;
		}
		isInReleaseSequence = false;
		if (emission && !hasTarget)
		{
			ToggleFrameEmission(_emission: false);
		}
	}

	private void SetFacialExpression(float _expressionOffset)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		if (currentExpressionOffset == _expressionOffset)
		{
			UpdateFaceBounce();
			return;
		}
		currentExpressionOffset = _expressionOffset;
		faceBounceTimer = 0f;
		expressionChangeSound.Play(((Component)this).transform.position);
		UpdateFaceBounce();
		int num = Random.Range(0, 9);
		if (_expressionOffset <= 0.335f)
		{
			if (_expressionOffset <= 0.101f)
			{
				if (_expressionOffset != 0.023f && _expressionOffset == 0.101f)
				{
					surprisedSound.Play(((Component)this).transform.position);
				}
			}
			else if (_expressionOffset != 0.179f)
			{
				if (_expressionOffset != 0.257f)
				{
					if (_expressionOffset == 0.335f)
					{
						neutralSound.Play(((Component)this).transform.position);
					}
				}
				else
				{
					sadClosedSound.Play(((Component)this).transform.position);
				}
			}
			else
			{
				sadOpenSound.Play(((Component)this).transform.position);
			}
		}
		else if (_expressionOffset <= 0.491f)
		{
			if (_expressionOffset != 0.413f)
			{
				if (_expressionOffset == 0.491f)
				{
					strainSoundAudioSource = strainSound.Play(((Component)this).transform.position);
				}
			}
			else if (num < 2)
			{
				sleepSound.Play(((Component)this).transform.position);
			}
		}
		else if (_expressionOffset != 0.569f)
		{
			if (_expressionOffset != 0.647f)
			{
				if (_expressionOffset == 0.728f)
				{
					byeSound.Play(((Component)this).transform.position);
				}
			}
			else
			{
				hiSound.Play(((Component)this).transform.position);
			}
		}
		else
		{
			happyOpenSound.Play(((Component)this).transform.position);
		}
	}

	private void UpdateFaceBounce()
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		if (faceBounceCurve != null && faceBounceCurve.length > 0)
		{
			float time = faceBounceCurve.keys[faceBounceCurve.length - 1].time;
			if (faceBounceTimer < time)
			{
				num = faceBounceCurve.Evaluate(faceBounceTimer);
				faceBounceTimer += Time.deltaTime * 10f;
			}
		}
		((Renderer)FaceMeshRenderer).material.mainTextureOffset = new Vector2(currentExpressionOffset, faceBaseYOffset + num);
	}

	private void DrawHandLines()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)lineRendererL) && Object.op_Implicit((Object)(object)lineRendererR))
		{
			Vector3 position = ((Component)lineRendererL).transform.position;
			Vector3 position2 = ((Component)lineRendererR).transform.position;
			DrawHandLine(lineRendererL, position, handL.position);
			DrawHandLine(lineRendererR, position2, handR.position);
		}
	}

	private void DrawHandLine(LineRenderer _lineRenderer, Vector3 _startPoint, Vector3 _endPoint)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)_lineRenderer))
		{
			FadeHandLineIn(_lineRenderer);
			Vector3[] array = new Vector3[20];
			for (int i = 0; i < 20; i++)
			{
				float num = (float)i / 19f;
				array[i] = Vector3.Lerp(_startPoint, _endPoint, num) - Vector3.up * Mathf.Sin(num * MathF.PI) * 0.1f;
				float num2 = 1f - Mathf.Abs(num - 0.5f) * 2f;
				ref Vector3 reference = ref array[i];
				reference += Vector3.right * Mathf.Sin(Time.time * 2f + (float)i) * 0.02f * num2;
				ref Vector3 reference2 = ref array[i];
				reference2 += Vector3.forward * Mathf.Cos(Time.time * 2f + (float)i) * 0.02f * num2;
			}
			((Renderer)_lineRenderer).material.mainTextureOffset = new Vector2(Time.time, 0f);
			_lineRenderer.positionCount = 20;
			_lineRenderer.SetPositions(array);
		}
	}

	private void FadeHandLineIn(LineRenderer _lineRenderer)
	{
		if (Object.op_Implicit((Object)(object)_lineRenderer))
		{
			if (_lineRenderer.widthMultiplier < 0.195f)
			{
				_lineRenderer.widthMultiplier = Mathf.Lerp(_lineRenderer.widthMultiplier, 0.2f, Time.deltaTime * 4f);
			}
			else
			{
				_lineRenderer.widthMultiplier = 0.2f;
			}
		}
	}

	private void FadeHandLineOut(LineRenderer _lineRenderer)
	{
		if (Object.op_Implicit((Object)(object)_lineRenderer))
		{
			if (_lineRenderer.widthMultiplier > 0.005f)
			{
				_lineRenderer.widthMultiplier = Mathf.Lerp(_lineRenderer.widthMultiplier, 0f, Time.deltaTime * 40f);
			}
			else
			{
				_lineRenderer.widthMultiplier = 0f;
			}
		}
	}
}
