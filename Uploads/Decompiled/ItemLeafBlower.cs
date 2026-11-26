using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemLeafBlower : MonoBehaviour
{
	public HurtCollider hurtCollider;

	public float batteryDrainRate = 0.5f;

	public float batteryDrainOnEnemyHit = 2f;

	public PhysGrabObjectGrabArea grabArea;

	public Transform fan;

	private float maxFanSpeed = 6000f;

	private float fanAcceleration = 1500f;

	private float fanDeceleration = 800f;

	private float currentFanSpeed;

	private float shakeIntervalMin = 0.1f;

	private float shakeIntervalMax = 0.25f;

	private float shakeForceMin = 0.1f;

	private float shakeForceMax = 0.5f;

	private float shakeTorqueMultiplier = 0.1f;

	private float shakeTimer;

	private Rigidbody rb;

	public ParticleSystem blowParticles;

	public MeshRenderer ms;

	public Sound blowSoundLoop;

	public Sound onSound;

	public Sound offSound;

	public Sound blowLoopStartSound;

	public Sound blowLoopEndSound;

	private bool blowing;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private ItemToggle itemToggle;

	private ItemBattery itemBattery;

	private float redFlashTimer;

	private float redFlashDuration = 1f;

	private bool prevToggleState;

	private float enemyHitTimer;

	private void Start()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		grabArea = ((Component)this).GetComponent<PhysGrabObjectGrabArea>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		itemBattery.autoDrain = false;
	}

	public void OnEnemyHit()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			enemyHitTimer = 0.5f;
		}
	}

	private void BatteryDrain(float _amount)
	{
		itemBattery.DrainContinuous(_amount, Time.deltaTime);
	}

	private void Update()
	{
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		if (!blowing && blowParticles.isPlaying)
		{
			blowParticles.Stop(true);
		}
		else if (blowing && !blowParticles.isPlaying)
		{
			blowParticles.Play(true);
		}
		if (blowing)
		{
			currentFanSpeed = Mathf.MoveTowards(currentFanSpeed, maxFanSpeed, fanAcceleration * Time.deltaTime);
		}
		else
		{
			currentFanSpeed = Mathf.MoveTowards(currentFanSpeed, 0f, fanDeceleration * Time.deltaTime);
		}
		if (currentFanSpeed > 0f)
		{
			fan.Rotate(Vector3.forward, currentFanSpeed * Time.deltaTime);
		}
		blowSoundLoop.PlayLoop(blowing, 1f, 1f);
		if (redFlashTimer > 0f)
		{
			redFlashTimer -= Time.deltaTime;
			float num = redFlashTimer / redFlashDuration;
			((Renderer)ms).material.SetColor("_EmissionColor", Color.red * 2f * num);
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (itemToggle.toggleState && !prevToggleState && itemBattery.batteryLife <= 0f)
		{
			NoBatteryFlash();
			itemToggle.ToggleItem(toggle: false);
		}
		prevToggleState = itemToggle.toggleState;
		if (!physGrabObject.grabbed || itemBattery.batteryLife <= 0f)
		{
			if (blowing)
			{
				BlowActivate(_active: false);
				NoBatteryFlash(_buttonSound: false);
				itemToggle.ToggleItem(toggle: false);
				itemBattery.BatteryToggle(toggle: false);
			}
			return;
		}
		if (itemBattery.batteryActive)
		{
			itemBattery.BatteryToggle(toggle: false);
		}
		enemyHitTimer -= Time.deltaTime;
		if (itemToggle.toggleState && itemBattery.batteryLife > 0f)
		{
			if (!blowing)
			{
				BlowActivate(_active: true);
			}
			if (enemyHitTimer > 0f)
			{
				BatteryDrain(batteryDrainOnEnemyHit);
			}
			else
			{
				BatteryDrain(batteryDrainRate);
			}
		}
		else if (blowing)
		{
			BlowActivate(_active: false);
		}
	}

	private void FixedUpdate()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (blowing)
		{
			shakeTimer -= Time.fixedDeltaTime;
			if (shakeTimer <= 0f)
			{
				Vector3 val = Random.insideUnitSphere * Random.Range(shakeForceMin, shakeForceMax);
				rb.AddForce(val, (ForceMode)1);
				Vector3 val2 = Random.insideUnitSphere * shakeTorqueMultiplier;
				rb.AddTorque(val2, (ForceMode)1);
				shakeTimer = Random.Range(shakeIntervalMin, shakeIntervalMax);
			}
		}
		if (grabArea.listOfAllGrabbers.Count <= 0)
		{
			return;
		}
		List<PhysGrabber> listOfAllGrabbers = grabArea.listOfAllGrabbers;
		bool flag = false;
		foreach (PhysGrabber item in listOfAllGrabbers)
		{
			if (item.isRotating)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			Quaternion turnX = Quaternion.Euler(-25f, 0f, 0f);
			Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
			Quaternion identity = Quaternion.identity;
			physGrabObject.TurnXYZ(turnX, turnY, identity);
			physGrabObject.OverrideTorqueStrength(physGrabObject.massOriginal);
		}
	}

	public void BlowActivate(bool _active)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (_active != blowing)
		{
			int num = ((_active && Object.op_Implicit((Object)(object)physGrabObject.lastPlayerGrabbing)) ? physGrabObject.lastPlayerGrabbing.photonView.ViewID : (-1));
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("BlowActivateRPC", (RpcTarget)0, new object[2] { _active, num });
			}
			else
			{
				BlowActivateRPC(_active, num);
			}
		}
	}

	[PunRPC]
	private void BlowActivateRPC(bool _active, int _grabberID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		blowing = _active;
		((Component)hurtCollider).gameObject.SetActive(_active);
		if (_active)
		{
			PlayerAvatar playerCausingHurt = ((_grabberID >= 0) ? SemiFunc.PlayerAvatarGetFromPhotonID(_grabberID) : physGrabObject.lastPlayerGrabbing);
			hurtCollider.playerCausingHurt = playerCausingHurt;
			redFlashTimer = 0f;
			((Renderer)ms).material.SetColor("_EmissionColor", Color.green * 2f);
			onSound.Play(((Component)this).transform.position);
			blowLoopStartSound.Play(((Component)this).transform.position);
		}
		else
		{
			((Renderer)ms).material.SetColor("_EmissionColor", Color.black);
			offSound.Play(((Component)this).transform.position);
			blowLoopEndSound.Play(((Component)this).transform.position);
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				NoBatteryFlash(_buttonSound: false);
			}
		}
	}

	public void NoBatteryFlash(bool _buttonSound = true)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("NoBatteryFlashRPC", (RpcTarget)0, new object[1] { _buttonSound });
		}
		else
		{
			NoBatteryFlashRPC();
		}
	}

	[PunRPC]
	private void NoBatteryFlashRPC(bool _buttonSound = true, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			redFlashTimer = redFlashDuration;
			if (_buttonSound)
			{
				onSound.Play(((Component)this).transform.position);
			}
		}
	}
}
