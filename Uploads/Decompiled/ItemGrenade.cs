using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class ItemGrenade : MonoBehaviour
{
	public Color blinkColor;

	public UnityEvent onDetonate;

	private ItemToggle itemToggle;

	internal ItemAttributes itemAttributes;

	internal bool isActive;

	internal PlayerAvatar activatorPlayerAvatar;

	private float grenadeTimer;

	public float tickTime = 3f;

	private PhotonView photonView;

	private PhysGrabObjectImpactDetector physGrabObjectImpactDetector;

	public Sound soundSplinter;

	public Sound soundTick;

	private float splinterAnimationProgress;

	public AnimationCurve splinterAnimationCurve;

	private Transform splinterTransform;

	private Material grenadeEmissionMaterial;

	private ItemEquippable itemEquippable;

	private Vector3 grenadeStartPosition;

	private Quaternion grenadeStartRotation;

	private PhysGrabObject physGrabObject;

	private Vector3 prevPosition;

	[FormerlySerializedAs("isThiefGrenade")]
	[HideInInspector]
	public bool isSpawnedGrenade;

	public GameObject throwLine;

	private Rigidbody rb;

	private float throwLineTimer;

	private TrailRenderer throwLineTrail;

	private void Awake()
	{
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		physGrabObjectImpactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		rb = ((Component)this).GetComponent<Rigidbody>();
	}

	private void Start()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		splinterTransform = ((Component)this).transform.Find("Splinter");
		GameObject gameObject = ((Component)((Component)this).transform.Find("Mesh")).gameObject;
		grenadeEmissionMaterial = gameObject.GetComponent<Renderer>().material;
		grenadeStartPosition = ((Component)this).transform.position;
		grenadeStartRotation = ((Component)this).transform.rotation;
		throwLineTrail = throwLine.GetComponent<TrailRenderer>();
	}

	private void FixedUpdate()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if (itemEquippable.isEquipped || itemEquippable.wasEquippedTimer > 0f)
		{
			prevPosition = rb.position;
			return;
		}
		Vector3 val = (rb.position - prevPosition) / Time.fixedDeltaTime;
		Vector3 val2 = rb.position - prevPosition;
		_ = val2.normalized;
		prevPosition = rb.position;
		if (!physGrabObject.grabbed && val.magnitude > 2f)
		{
			throwLineTimer = 0.2f;
		}
		if (throwLineTimer > 0f)
		{
			throwLineTrail.emitting = true;
			throwLineTimer -= Time.fixedDeltaTime;
		}
		else
		{
			throwLineTrail.emitting = false;
		}
	}

	private void Update()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		soundTick.PlayLoop(isActive, 2f, 2f);
		if (itemEquippable.isEquipped)
		{
			if (isActive)
			{
				isActive = false;
				grenadeTimer = 0f;
				splinterAnimationProgress = 0f;
				itemToggle.ToggleItem(toggle: false);
				splinterTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
				grenadeEmissionMaterial.SetColor("_EmissionColor", Color.black);
			}
			return;
		}
		if (isActive)
		{
			if (splinterAnimationProgress < 1f)
			{
				splinterAnimationProgress += 5f * Time.deltaTime;
				float num = splinterAnimationCurve.Evaluate(splinterAnimationProgress);
				splinterTransform.localEulerAngles = new Vector3(num * 90f, 0f, 0f);
			}
			float num2 = Mathf.PingPong(Time.time * 8f, 1f);
			Color val = blinkColor * Mathf.LinearToGammaSpace(num2);
			grenadeEmissionMaterial.SetColor("_EmissionColor", val);
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (itemToggle.toggleState && !isActive)
		{
			isActive = true;
			if (Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing))
			{
				SetActivator(physGrabObject.lastPlayerGrabbing);
				SemiFunc.PlayerShopKeeperExplosiveHost(physGrabObject.lastPlayerGrabbing);
			}
			TickStart();
		}
		if (isActive)
		{
			grenadeTimer += Time.deltaTime;
			if (grenadeTimer >= tickTime)
			{
				grenadeTimer = 0f;
				TickEnd();
			}
		}
	}

	private void GrenadeReset()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		isActive = false;
		grenadeTimer = 0f;
		throwLine.SetActive(false);
		splinterAnimationProgress = 0f;
		itemToggle.ToggleItem(toggle: false);
		splinterTransform.localEulerAngles = new Vector3(0f, 0f, 0f);
		grenadeEmissionMaterial.SetColor("_EmissionColor", Color.black);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		rb.linearVelocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
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
		}
	}

	private void TickStart()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClient())
		{
			photonView.RPC("TickStartRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			TickStartRPC();
		}
	}

	public void SetActivator(PlayerAvatar _player)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)_player))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetActivatorRPC", (RpcTarget)0, new object[1] { _player.photonView.ViewID });
			}
			else
			{
				SetActivatorRPC(_player.photonView.ViewID);
			}
		}
	}

	[PunRPC]
	public void SetActivatorRPC(int _viewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			activatorPlayerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_viewID);
		}
	}

	private void TickEnd()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClient())
		{
			photonView.RPC("TickEndRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			TickEndRPC();
		}
	}

	[PunRPC]
	private void TickStartRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			soundSplinter.Play(((Component)this).transform.position);
			isActive = true;
		}
	}

	[PunRPC]
	private void TickEndRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info) || (Object.op_Implicit((Object)(object)itemEquippable) && itemEquippable.isEquipped))
		{
			return;
		}
		onDetonate.Invoke();
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (Object.op_Implicit((Object)(object)activatorPlayerAvatar))
			{
				SemiFunc.PlayerShopKeeperExplosionDetonatedHost(activatorPlayerAvatar);
			}
			if ((!SemiFunc.RunIsShop() && !RunManager.instance.levelIsShop) || isSpawnedGrenade)
			{
				if (!isSpawnedGrenade)
				{
					StatsManager.instance.ItemRemove(itemAttributes.instanceName);
				}
				physGrabObjectImpactDetector.DestroyObject();
			}
			else
			{
				physGrabObject.Teleport(grenadeStartPosition, grenadeStartRotation);
			}
		}
		if ((SemiFunc.RunIsShop() || RunManager.instance.levelIsShop) && !isSpawnedGrenade)
		{
			GrenadeReset();
		}
	}
}
