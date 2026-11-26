using System;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class ItemWalkieTalkie : MonoBehaviour
{
	public Transform talkTopTransform;

	public Transform eyelidLeftTop;

	public Transform eyelidLeftBottom;

	public Transform eyelidRightTop;

	public Transform eyelidRightBottom;

	public Transform eyeL;

	public Transform eyeR;

	public Transform eyeTargetL;

	public Transform eyeTargetR;

	public ItemWalkieTalkie otherWalkie;

	public SpringQuaternion springQuaternionLeft;

	public SpringQuaternion springQuaternionRight;

	public SpringQuaternion springQuaternionArm;

	public Transform speaker;

	public Transform arm;

	public Transform armTarget;

	public Transform neck;

	public Transform cameraPosition;

	public Transform visuals;

	private float overrideVisualsDisableTimer;

	public Light usingLight;

	public LineRenderer lineRendererL;

	public LineRenderer lineRendererR;

	public float lineTextureScrollSpeed = 0.5f;

	public ParticleSystem sparkParticle;

	public ParticleSystem zapParticle;

	public TextMeshPro text;

	public GenericEyeLookAt genericEyeLookAt;

	[HideInInspector]
	public PlayerAvatar inThisWalkie;

	[HideInInspector]
	public bool someoneInThisWalkie;

	[HideInInspector]
	public bool walkiesBeingUsed;

	[HideInInspector]
	public ItemEquippable itemEquippable;

	private PlayerAvatar previouslyInthisWalkie;

	private PhysGrabObject physGrabObject;

	private ItemToggle itemToggle;

	public List<MeshRenderer> mshRenderersEmission;

	public List<MeshRenderer> mshRenderersColor;

	public List<MeshRenderer> mshRenderersEyes;

	public MeshRenderer antennaMS;

	public Material BlackMaterial;

	public Material NormalMaterial;

	public Sound tuneInSound;

	public Sound tuneOutSound;

	public Sound wireInSound;

	public Sound wireOutSound;

	public Sound eyeMovementSound;

	public Sound NeckMovementSound;

	public Sound armMovementSound;

	public Sound interferenceLoop;

	public Sound interferenceInsideLoop;

	public Sound interferenceBreaker;

	public Sound zapSound;

	public Sound ringToneSound;

	public Sound callingSound;

	public Sound denySound;

	public Sound denyBrokenSound;

	private bool previousGrabbed;

	private bool previousToggleState;

	private bool previousSomeoneInThisWalkie;

	private bool previousSomeoneInOtherWalkie;

	private bool previousAntennaGrabbed;

	private bool previousOtherWalkieEquipped;

	private bool disabled;

	private PhotonView photonView;

	private Vector3 eyeIdlePosition;

	private Vector3 eyeDartPosition;

	private Vector3 armRotation;

	private Vector3 neckRotation;

	private Quaternion originalNeckRot;

	private Quaternion originalArmRot;

	private PlayerAvatar previouslyInOtherWalkie;

	private float rotationTimer;

	private Quaternion prevEyeLRotation;

	private Quaternion prevEyeRRotation;

	private Quaternion prevNeckRotation;

	private Quaternion prevArmRotation;

	private float eyeSoundTimer;

	private float neckSoundTimer;

	private float armSoundTimer;

	private float spectatedLookAtCooldown;

	private float spectatedLookAtLerp;

	private float previousPlayerHealth;

	private float ringToneCooldown;

	public ItemLight itemLight;

	private List<InputKey> keysToDisable = new List<InputKey>
	{
		InputKey.Movement,
		InputKey.Jump,
		InputKey.Push,
		InputKey.Pull,
		InputKey.Map,
		InputKey.Grab,
		InputKey.Rotate,
		InputKey.Crouch,
		InputKey.Tumble,
		InputKey.Sprint,
		InputKey.Inventory1,
		InputKey.Inventory2,
		InputKey.Inventory3
	};

	private void Start()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		inThisWalkie = null;
		((TMP_Text)text).text = "";
		originalNeckRot = neck.localRotation;
		originalArmRot = arm.localRotation;
		((Behaviour)usingLight).enabled = false;
	}

	[PunRPC]
	private void UpdateInThisWalkiePlayerRPC(int photonViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			previouslyInthisWalkie = inThisWalkie;
			inThisWalkie = SemiFunc.PlayerAvatarGetFromPhotonID(photonViewID);
			someoneInThisWalkie = (Object)(object)inThisWalkie != (Object)null;
			((TMP_Text)text).text = (Object.op_Implicit((Object)(object)inThisWalkie) ? inThisWalkie.playerName : "");
		}
	}

	public void UpdateInThisWalkiePlayer(PlayerAvatar _player)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if ((Object)(object)_player != (Object)null)
		{
			previousPlayerHealth = _player.playerHealth.health;
		}
		if (SemiFunc.IsMultiplayer())
		{
			int num = -1;
			if (Object.op_Implicit((Object)(object)_player))
			{
				num = _player.photonView.ViewID;
			}
			photonView.RPC("UpdateInThisWalkiePlayerRPC", (RpcTarget)0, new object[1] { num });
		}
		else
		{
			int photonViewID = -1;
			if (Object.op_Implicit((Object)(object)_player))
			{
				photonViewID = _player.photonView.ViewID;
			}
			UpdateInThisWalkiePlayerRPC(photonViewID);
		}
	}

	[PunRPC]
	private void PlayDenyBrokenSoundRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			denyBrokenSound.Play(((Component)this).transform.position);
		}
	}

	private void PlayDenyBrokenSound()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.rb.AddTorque(Random.insideUnitSphere * 0.05f, (ForceMode)1);
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("PlayDenyBrokenSoundRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				PlayDenyBrokenSoundRPC();
			}
		}
	}

	[PunRPC]
	private void PlayCallingSoundRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			callingSound.Play(((Component)this).transform.position);
		}
	}

	private void PlayCallingSound()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.rb.AddTorque(Random.insideUnitSphere * 0.05f, (ForceMode)1);
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("PlayCallingSoundRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				PlayCallingSoundRPC();
			}
		}
	}

	private void Update()
	{
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_051d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0572: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0946: Unknown result type (might be due to invalid IL or missing references)
		//IL_0950: Unknown result type (might be due to invalid IL or missing references)
		//IL_0681: Unknown result type (might be due to invalid IL or missing references)
		//IL_0688: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0995: Unknown result type (might be due to invalid IL or missing references)
		//IL_099f: Unknown result type (might be due to invalid IL or missing references)
		//IL_096f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0979: Unknown result type (might be due to invalid IL or missing references)
		//IL_0820: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d74: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d83: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d99: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0de4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0875: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_1007: Unknown result type (might be due to invalid IL or missing references)
		//IL_1018: Unknown result type (might be due to invalid IL or missing references)
		//IL_1028: Unknown result type (might be due to invalid IL or missing references)
		//IL_1057: Unknown result type (might be due to invalid IL or missing references)
		//IL_105c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1063: Unknown result type (might be due to invalid IL or missing references)
		//IL_1073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efe: Unknown result type (might be due to invalid IL or missing references)
		if (overrideVisualsDisableTimer > 0f)
		{
			overrideVisualsDisableTimer -= Time.deltaTime;
			if (overrideVisualsDisableTimer <= 0f)
			{
				((Component)visuals).gameObject.SetActive(true);
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (someoneInThisWalkie && (!Object.op_Implicit((Object)(object)inThisWalkie) || !SemiFunc.PlayerGetList().Contains(inThisWalkie) || !((Behaviour)inThisWalkie).isActiveAndEnabled))
			{
				UpdateInThisWalkiePlayer(null);
			}
			if (itemEquippable.isEquipped || !Object.op_Implicit((Object)(object)otherWalkie) || otherWalkie.itemEquippable.isEquipped)
			{
				if (someoneInThisWalkie)
				{
					UpdateInThisWalkiePlayer(null);
				}
				if (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)
				{
					otherWalkie.UpdateInThisWalkiePlayer(null);
				}
			}
			if (WasLetGo() && Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)
			{
				otherWalkie.UpdateInThisWalkiePlayer(null);
			}
			if (someoneInThisWalkie && (float)inThisWalkie.playerHealth.health < previousPlayerHealth)
			{
				UpdateInThisWalkiePlayer(null);
			}
			if (someoneInThisWalkie && Object.op_Implicit((Object)(object)otherWalkie) && Vector3.Distance(((Component)inThisWalkie).transform.position, ((Component)otherWalkie).transform.position) > 3f)
			{
				UpdateInThisWalkiePlayer(null);
			}
			if (!Object.op_Implicit((Object)(object)otherWalkie) && !disabled)
			{
				UpdateInThisWalkiePlayer(null);
			}
		}
		if (Object.op_Implicit((Object)(object)inThisWalkie) && inThisWalkie.isLocal)
		{
			inThisWalkie.physGrabber.OverrideDiscoverSkipListSet(((Component)physGrabObject).transform);
		}
		float num = ((someoneInThisWalkie || (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)) ? (-80f) : 0f);
		float num2 = ((someoneInThisWalkie || (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)) ? 80f : 0f);
		eyelidLeftTop.localRotation = Quaternion.Slerp(eyelidLeftTop.localRotation, Quaternion.Euler(0f, 0f, num), 10f * Time.deltaTime);
		eyelidLeftBottom.localRotation = Quaternion.Slerp(eyelidLeftBottom.localRotation, Quaternion.Euler(0f, 0f, num2), 10f * Time.deltaTime);
		eyelidRightTop.localRotation = Quaternion.Slerp(eyelidRightTop.localRotation, Quaternion.Euler(0f, 0f, num), 10f * Time.deltaTime);
		eyelidRightBottom.localRotation = Quaternion.Slerp(eyelidRightBottom.localRotation, Quaternion.Euler(0f, 0f, num2), 10f * Time.deltaTime);
		if ((!Object.op_Implicit((Object)(object)otherWalkie) && !disabled) || SomeoneInTheOtherWalkieChanged())
		{
			if (!Object.op_Implicit((Object)(object)otherWalkie) || (!someoneInThisWalkie && !otherWalkie.someoneInThisWalkie))
			{
				foreach (MeshRenderer mshRenderersEye in mshRenderersEyes)
				{
					((Renderer)mshRenderersEye).material = NormalMaterial;
					((Renderer)mshRenderersEye).material.SetColor("_EmissionColor", Color.black);
				}
				foreach (MeshRenderer item in mshRenderersColor)
				{
					((Renderer)item).material.SetColor("_EmissionColor", Color.black);
				}
			}
			if (!someoneInThisWalkie && Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)
			{
				previouslyInOtherWalkie = otherWalkie.inThisWalkie;
				foreach (MeshRenderer mshRenderersEye2 in mshRenderersEyes)
				{
					if (Object.op_Implicit((Object)(object)mshRenderersEye2))
					{
						((Renderer)mshRenderersEye2).material = BlackMaterial;
					}
				}
			}
			foreach (MeshRenderer item2 in mshRenderersColor)
			{
				((Renderer)item2).material.SetColor("_EmissionColor", Color.black);
			}
			((TMP_Text)text).text = ((Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie) ? otherWalkie.inThisWalkie.playerName : "");
			if (!Object.op_Implicit((Object)(object)otherWalkie))
			{
				disabled = true;
			}
		}
		if (SomeoneInThisWalkieChanged())
		{
			if (someoneInThisWalkie)
			{
				sparkParticle.Play(true);
				tuneInSound.Play(((Component)this).transform.position);
				wireInSound.Play(((Component)this).transform.position);
				Color val = SemiFunc.PlayerGetColorMain(inThisWalkie);
				if (!inThisWalkie.isLocal)
				{
					foreach (MeshRenderer item3 in mshRenderersColor)
					{
						((Renderer)item3).material.SetColor("_EmissionColor", val * 2f);
					}
					foreach (MeshRenderer mshRenderersEye3 in mshRenderersEyes)
					{
						((Renderer)mshRenderersEye3).material.SetColor("_EmissionColor", Color.white * 1f);
					}
				}
				else
				{
					CameraGlitch.Instance.PlayLong();
					GameDirector.instance.CameraImpact.Shake(1f, 0.1f);
					((Component)visuals).gameObject.SetActive(false);
				}
				if (Object.op_Implicit((Object)(object)otherWalkie))
				{
					foreach (MeshRenderer item4 in otherWalkie.mshRenderersColor)
					{
						((Renderer)item4).material.SetColor("_EmissionColor", val * 2f);
					}
				}
				usingLight.color = val;
				((Behaviour)usingLight).enabled = true;
			}
			else
			{
				sparkParticle.Play(true);
				tuneOutSound.Play(((Component)this).transform.position);
				wireOutSound.Play(((Component)this).transform.position);
				((Behaviour)usingLight).enabled = false;
				foreach (MeshRenderer mshRenderersEye4 in mshRenderersEyes)
				{
					((Renderer)mshRenderersEye4).material.SetColor("_EmissionColor", Color.black);
				}
				if (Object.op_Implicit((Object)(object)previouslyInthisWalkie) && previouslyInthisWalkie.isLocal)
				{
					CameraGlitch.Instance.PlayLong();
					GameDirector.instance.CameraImpact.Shake(1f, 0.1f);
					PlayerController.instance.playerAvatarScript.playerAvatarVisuals.ShowSelfOverride(0f);
					SemiFunc.PlayerCameraPositionOverride(cameraPosition, 0f);
					PlayerController.instance.OverrideDisableTurn(0f);
					OverrideDisableVisuals(0f);
				}
				else
				{
					foreach (MeshRenderer item5 in mshRenderersColor)
					{
						((Renderer)item5).material.SetColor("_EmissionColor", Color.black);
					}
					if (Object.op_Implicit((Object)(object)otherWalkie))
					{
						foreach (MeshRenderer item6 in otherWalkie.mshRenderersColor)
						{
							((Renderer)item6).material.SetColor("_EmissionColor", Color.black);
						}
					}
				}
			}
		}
		bool grabbed = physGrabObject.grabbed;
		bool flag = Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.itemEquippable.isEquipped;
		if (((grabbed != previousAntennaGrabbed || flag != previousOtherWalkieEquipped) && !someoneInThisWalkie && (!Object.op_Implicit((Object)(object)otherWalkie) || !otherWalkie.someoneInThisWalkie)) || (grabbed != previousAntennaGrabbed && !Object.op_Implicit((Object)(object)otherWalkie)))
		{
			previousAntennaGrabbed = grabbed;
			previousOtherWalkieEquipped = flag;
			if (grabbed)
			{
				if (!Object.op_Implicit((Object)(object)otherWalkie))
				{
					((Renderer)antennaMS).material.SetColor("_EmissionColor", Color.red * 5f);
				}
				else if (flag)
				{
					((Renderer)antennaMS).material.SetColor("_EmissionColor", Color.yellow * 5f);
				}
				else
				{
					((Renderer)antennaMS).material.SetColor("_EmissionColor", Color.green * 5f);
				}
			}
			else
			{
				((Renderer)antennaMS).material.SetColor("_EmissionColor", Color.black);
			}
		}
		interferenceLoop.PlayLoop(someoneInThisWalkie && !inThisWalkie.isLocal, 1f, 1f);
		interferenceInsideLoop.PlayLoop(someoneInThisWalkie && inThisWalkie.isLocal, 1f, 1f);
		if (SemiFunc.IsMultiplayer() && someoneInThisWalkie && inThisWalkie.voiceChatFetched)
		{
			inThisWalkie.voiceChat.OverrideVoiceDistortion();
		}
		if (someoneInThisWalkie && physGrabObject.playerGrabbing.Count > 0 && !physGrabObject.playerGrabbing[0].playerAvatar.isLocal)
		{
			SemiFunc.PlayerEyesOverride(physGrabObject.playerGrabbing[0].playerAvatar, cameraPosition.position, 0.1f, ((Component)this).gameObject);
		}
		if (someoneInThisWalkie && !inThisWalkie.isLocal)
		{
			if (!((Behaviour)genericEyeLookAt).isActiveAndEnabled)
			{
				((Component)genericEyeLookAt).gameObject.SetActive(true);
			}
			if (physGrabObject.playerGrabbing.Count > 0)
			{
				genericEyeLookAt.SetTargetPlayer(physGrabObject.playerGrabbing[0].playerAvatar);
			}
			ArmHeadRotationLogic();
			TurnSoundLogic();
			if (SemiFunc.PerSecond(5f, (Object)(object)this) && !SemiFunc.Photosensitivity())
			{
				if (Random.Range(0, 10) == 0)
				{
					interferenceBreaker.Play(((Component)this).transform.position);
				}
				if (Random.Range(0, 20) == 0)
				{
					zapParticle.Play(true);
					zapSound.Play(((Component)this).transform.position);
				}
			}
		}
		else if (someoneInThisWalkie && inThisWalkie.isLocal)
		{
			if (SemiFunc.PerSecond(5f, (Object)(object)this) && !SemiFunc.Photosensitivity() && Random.Range(0, 10) == 0)
			{
				CameraGlitch.Instance.PlayTiny();
				GameDirector.instance.CameraImpact.Shake(1f, 0.1f);
			}
			PostProcessing.Instance.SaturationOverride(-100f, 15f, 5f, 0.1f, ((Component)this).gameObject);
			PostProcessing.Instance.ContrastOverride(-40f, 15f, 15f, 0.1f, ((Component)this).gameObject);
			CameraZoom.Instance.OverrideZoomSet(95f, 0.1f, 15f, 100f, ((Component)this).gameObject, 10);
			PostProcessing.Instance.VignetteOverride(Color.white, 0.3f, 1f, 15f, 15f, 0.1f, ((Component)this).gameObject);
			PostProcessing.Instance.ResolutionOverride(0.6f, 0.1f);
			PostProcessing.Instance.GrainIntensityOverride(1.1f, 15f, 15f, 0.1f, ((Component)this).gameObject);
			PostProcessing.Instance.LensDistortionOverride(45f, 15f, 15f, 0.1f, ((Component)this).gameObject);
		}
		else
		{
			if (((Behaviour)genericEyeLookAt).isActiveAndEnabled)
			{
				((Component)genericEyeLookAt).gameObject.SetActive(false);
			}
			eyeL.localRotation = Quaternion.Slerp(eyeL.localRotation, Quaternion.identity, 0.2f);
			eyeR.localRotation = Quaternion.Slerp(eyeR.localRotation, Quaternion.identity, 0.2f);
			neck.localRotation = Quaternion.Slerp(neck.localRotation, originalNeckRot, 0.2f);
			arm.localRotation = Quaternion.Slerp(arm.localRotation, originalArmRot, 0.2f);
		}
		if (someoneInThisWalkie)
		{
			itemLight.OverrideActive(_active: false);
		}
		if (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)
		{
			DrawEyeLines(otherWalkie.inThisWalkie);
			SemiFunc.PlayerEyesOverride(otherWalkie.inThisWalkie, cameraPosition.position, 0.2f, ((Component)this).gameObject);
			otherWalkie.inThisWalkie.OverridePupilSize(3f, 4, 3f, 1f, 15f, 0.3f);
		}
		else
		{
			FadeLineOut(lineRendererL);
			FadeLineOut(lineRendererR);
		}
		float num3 = 0f;
		if (Object.op_Implicit((Object)(object)inThisWalkie) && inThisWalkie.voiceChatFetched)
		{
			inThisWalkie.voiceChat.OverrideNoTalkAnimation(0.2f);
			inThisWalkie.voiceChat.OverridePosition(physGrabObject.centerPoint, 0.2f);
			if (!inThisWalkie.isLocal)
			{
				inThisWalkie.voiceChat.OverridePitch(1.15f, 0.1f, 0.1f, 0.2f, 0.05f, 100f);
				inThisWalkie.OverrideTTSPosition();
				inThisWalkie.voiceChat.OverridePitch(1.5f, 0.1f, 0.1f);
				if (inThisWalkie.voiceChat.clipLoudness > 0.005f)
				{
					num3 += inThisWalkie.voiceChat.clipLoudness;
				}
			}
		}
		if (!Object.op_Implicit((Object)(object)inThisWalkie) || (Object.op_Implicit((Object)(object)inThisWalkie) && !inThisWalkie.isLocal))
		{
			float num4 = Mathf.Lerp(0f, -90f, num3 * 4f);
			talkTopTransform.localRotation = Quaternion.Slerp(talkTopTransform.localRotation, Quaternion.Euler(num4, 0f, 0f), 50f * Time.deltaTime);
			float num5 = Mathf.Lerp(1f, 1.5f, num3 * 4f);
			speaker.localScale = Vector3.Slerp(speaker.localScale, Vector3.one * num5, 50f * Time.deltaTime);
		}
		if (someoneInThisWalkie && inThisWalkie.isLocal)
		{
			PlayerController.instance.playerAvatarScript.playerAvatarVisuals.ShowSelfOverride();
			SemiFunc.PlayerCameraPositionOverride(cameraPosition);
			PlayerController.instance.OverrideDisableTurn(0.1f, _reset: true);
			InputManager.instance.DisableControlsSpecific(0.1f, keysToDisable);
			if (Object.op_Implicit((Object)(object)otherWalkie))
			{
				inThisWalkie.physGrabber.OverrideGrab(otherWalkie.physGrabObject);
			}
			OverrideDisableVisuals();
		}
		if (!physGrabObject.grabbed || !SemiFunc.IsMasterClientOrSingleplayer() || !EPressed())
		{
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(itemToggle.playerTogglePhotonID);
		if (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.itemEquippable.isEquipped && ringToneCooldown <= 0f)
		{
			PlayCallingSound();
			otherWalkie.itemEquippable.SendPing(5f, 8f, 0.7f);
		}
		if (itemToggle.toggleState)
		{
			if (!Object.op_Implicit((Object)(object)otherWalkie))
			{
				PlayDenyBrokenSound();
				itemToggle.ToggleItem(toggle: false);
			}
			else if (someoneInThisWalkie || !Object.op_Implicit((Object)(object)otherWalkie) || otherWalkie.itemEquippable.isEquipped)
			{
				itemToggle.ToggleItem(toggle: false);
			}
			else if (!otherWalkie.someoneInThisWalkie && Object.op_Implicit((Object)(object)playerAvatar) && physGrabObject.playerGrabbing.Contains(playerAvatar.physGrabber))
			{
				otherWalkie.UpdateInThisWalkiePlayer(playerAvatar);
			}
		}
		else if (Object.op_Implicit((Object)(object)otherWalkie) && otherWalkie.someoneInThisWalkie)
		{
			if ((Object)(object)otherWalkie.inThisWalkie == (Object)(object)playerAvatar)
			{
				otherWalkie.UpdateInThisWalkiePlayer(null);
			}
			else
			{
				itemToggle.ToggleItem(toggle: true, itemToggle.playerTogglePhotonID);
			}
		}
	}

	private void FixedUpdate()
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		Quaternion targetRotation;
		float num3;
		if (someoneInThisWalkie)
		{
			if (spectatedLookAtCooldown <= 0f)
			{
				float num = 0f;
				if (Object.op_Implicit((Object)(object)inThisWalkie) && inThisWalkie.voiceChatFetched)
				{
					num = inThisWalkie.voiceChat.clipLoudnessNoTTS + inThisWalkie.voiceChat.clipLoudnessTTS * 0.2f;
				}
				Transform transform = ((Component)inThisWalkie.localCamera).transform;
				Quaternion rotation = transform.rotation;
				float num2 = rotation.eulerAngles.x - Mathf.Min(num * 100f, 40f);
				rotation = transform.rotation;
				float y = rotation.eulerAngles.y;
				rotation = transform.rotation;
				targetRotation = Quaternion.Euler(num2, y, rotation.eulerAngles.z);
				num3 = 10f * spectatedLookAtLerp;
				if (physGrabObject.playerGrabbing.Count <= 0)
				{
					Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
					if (linearVelocity.magnitude >= 2f)
					{
						linearVelocity = physGrabObject.rb.linearVelocity;
						num3 = 10f - linearVelocity.magnitude * 20f;
						spectatedLookAtLerp = 0f;
						goto IL_016c;
					}
				}
				spectatedLookAtLerp += 0.25f * Time.fixedDeltaTime;
				spectatedLookAtLerp = Mathf.Clamp01(spectatedLookAtLerp);
				goto IL_016c;
			}
			spectatedLookAtCooldown -= Time.fixedDeltaTime;
			return;
		}
		List<PhysGrabber> playerGrabbing = physGrabObject.playerGrabbing;
		bool flag = false;
		foreach (PhysGrabber item in playerGrabbing)
		{
			if (item.isRotating)
			{
				flag = true;
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
		spectatedLookAtCooldown = 1f;
		return;
		IL_016c:
		Vector3 val = SemiFunc.PhysFollowRotation(((Component)this).transform, targetRotation, physGrabObject.rb, 5f);
		val = Vector3.Lerp(Vector3.zero, val, num3 * Time.fixedDeltaTime);
		physGrabObject.rb.AddTorque(val, (ForceMode)1);
		physGrabObject.OverrideTorqueStrength(0f);
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

	private bool WasLetGo()
	{
		if (!physGrabObject.grabbed && previousGrabbed)
		{
			previousGrabbed = false;
			return true;
		}
		previousGrabbed = physGrabObject.grabbed;
		return false;
	}

	private bool SomeoneInThisWalkieChanged()
	{
		if (someoneInThisWalkie != previousSomeoneInThisWalkie)
		{
			previousSomeoneInThisWalkie = someoneInThisWalkie;
			return true;
		}
		return false;
	}

	private bool SomeoneInTheOtherWalkieChanged()
	{
		if (!Object.op_Implicit((Object)(object)otherWalkie))
		{
			return false;
		}
		bool flag = otherWalkie.someoneInThisWalkie;
		if (flag != previousSomeoneInOtherWalkie)
		{
			previousSomeoneInOtherWalkie = flag;
			return true;
		}
		return false;
	}

	private void ArmHeadRotationLogic()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (rotationTimer <= 0f)
		{
			if (Random.Range(0, 4) == 0)
			{
				rotationTimer = Random.Range(0.1f, 0.75f);
				armRotation = new Vector3((float)Random.Range(-15, 15), 0f, 0f);
				neckRotation = new Vector3(0f, (float)Random.Range(-10, 10), 0f);
			}
		}
		else
		{
			rotationTimer -= Time.deltaTime;
		}
		armTarget.localRotation = Quaternion.Euler(armRotation);
		arm.localRotation = SemiFunc.SpringQuaternionGet(springQuaternionArm, armTarget.localRotation);
		neck.localRotation = Quaternion.Lerp(neck.localRotation, Quaternion.Euler(neckRotation), 10f * Time.deltaTime);
	}

	private void TurnSoundLogic()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			if (Quaternion.Angle(prevEyeLRotation, eyeL.rotation) > 15f && eyeSoundTimer <= 0f)
			{
				eyeMovementSound.Play(eyeL.position);
				prevEyeLRotation = eyeL.rotation;
				eyeSoundTimer = 0.2f;
			}
			if (Quaternion.Angle(prevEyeRRotation, eyeR.rotation) > 15f && eyeSoundTimer <= 0f)
			{
				eyeMovementSound.Play(eyeR.position);
				prevEyeRRotation = eyeR.rotation;
				eyeSoundTimer = 0.2f;
			}
			if (Quaternion.Angle(prevNeckRotation, neck.rotation) > 5f && neckSoundTimer <= 0f)
			{
				NeckMovementSound.Play(neck.position);
				prevNeckRotation = neck.rotation;
				neckSoundTimer = 0.3f;
			}
			if (Quaternion.Angle(prevArmRotation, arm.rotation) > 8f && armSoundTimer <= 0f)
			{
				armMovementSound.Play(arm.position);
				prevArmRotation = arm.rotation;
				armSoundTimer = 0.3f;
			}
			prevEyeLRotation = eyeL.rotation;
			prevEyeRRotation = eyeR.rotation;
			prevNeckRotation = neck.rotation;
			prevArmRotation = arm.rotation;
		}
		if (eyeSoundTimer > 0f)
		{
			eyeSoundTimer -= Time.deltaTime;
		}
		if (neckSoundTimer > 0f)
		{
			neckSoundTimer -= Time.deltaTime;
		}
		if (armSoundTimer > 0f)
		{
			armSoundTimer -= Time.deltaTime;
		}
	}

	private void DrawEyeLines(PlayerAvatar _targetPlayer)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)_targetPlayer))
		{
			PlayerEyes playerEyes = _targetPlayer.playerAvatarVisuals.playerEyes;
			DrawLine(lineRendererL, eyeL.position, playerEyes.pupilRight.position);
			DrawLine(lineRendererR, eyeR.position, playerEyes.pupilLeft.position);
		}
	}

	private void DrawLine(LineRenderer _lineRenderer, Vector3 _startPoint, Vector3 _endPoint)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		FadeLineIn(_lineRenderer);
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
		((Renderer)_lineRenderer).material.mainTextureOffset = new Vector2(Time.time * lineTextureScrollSpeed, 0f);
		_lineRenderer.positionCount = 20;
		_lineRenderer.SetPositions(array);
	}

	private void FadeLineIn(LineRenderer _lineRenderer)
	{
		if (_lineRenderer.widthMultiplier < 0.195f)
		{
			_lineRenderer.widthMultiplier = Mathf.Lerp(_lineRenderer.widthMultiplier, 0.2f, Time.deltaTime * 2f);
		}
		else
		{
			_lineRenderer.widthMultiplier = 0.2f;
		}
	}

	private void FadeLineOut(LineRenderer _lineRenderer)
	{
		if (_lineRenderer.widthMultiplier > 0.005f)
		{
			_lineRenderer.widthMultiplier = Mathf.Lerp(_lineRenderer.widthMultiplier, 0f, Time.deltaTime * 15f);
		}
		else
		{
			_lineRenderer.widthMultiplier = 0f;
		}
	}

	public void SetMaterial(Material _material)
	{
		foreach (MeshRenderer item in mshRenderersColor)
		{
			if (Object.op_Implicit((Object)(object)item))
			{
				((Renderer)item).material = _material;
			}
		}
	}

	public void OverrideDisableVisuals(float _time = 0.1f)
	{
		if (_time <= 0f)
		{
			overrideVisualsDisableTimer = _time;
			if (!((Component)visuals).gameObject.activeSelf)
			{
				((Component)visuals).gameObject.SetActive(true);
			}
		}
		else
		{
			if (((Component)visuals).gameObject.activeSelf)
			{
				((Component)visuals).gameObject.SetActive(false);
			}
			overrideVisualsDisableTimer = _time;
		}
	}
}
