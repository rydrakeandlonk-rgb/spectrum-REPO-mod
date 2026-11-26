using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemBattery : MonoBehaviour
{
	private class BatteryCharger
	{
		public GameObject chargerObject;

		public float chargeRate;

		public float activeTimer;
	}

	public int batteryBars = 6;

	public bool isUnchargable;

	public bool hasBatteryVisuals = true;

	public Transform batteryTransform;

	private Camera mainCamera;

	public float upOffset = 0.5f;

	[HideInInspector]
	public bool batteryActive;

	[HideInInspector]
	public float batteryLife = 100f;

	private float batteryLifePrev = 100f;

	internal int batteryLifeInt = 6;

	private float batteryOutBlinkTimer;

	private PhotonView photonView;

	[HideInInspector]
	public Color batteryColor;

	internal Color batteryColorMedium;

	private float chargeTimer;

	private float chargeRate;

	private float transferChargeTimer;

	private bool usesFullBars;

	internal const float continuousRateMultiplier = 2f;

	private bool batteryEmptyPrev;

	private bool batteryFullPrev = true;

	internal const float transferEfficiency = 0.95f;

	private List<BatteryCharger> chargerList = new List<BatteryCharger>();

	internal bool isCharging;

	internal bool suppressBatteryUI;

	private float chargingBlinkTimer;

	private bool chargingBlink;

	private ItemAttributes itemAttributes;

	private float showTimer;

	private bool showBattery;

	public bool autoDrain = true;

	private ItemEquippable itemEquippable;

	public bool onlyShowWhenItemToggleIsOn;

	public float batteryDrainRate = 1f;

	private float drainRate;

	private float drainTimer;

	internal int currentBars = 6;

	private int batteryLifeCountBars = 6;

	internal int batteryLifeCountBarsPrev = 6;

	private BatteryVisualLogic batteryVisualLogic;

	private bool tutorialCheck;

	private PhysGrabObject physGrabObject;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		usesFullBars = Object.op_Implicit((Object)(object)((Component)this).GetComponent<ItemGun>()) || Object.op_Implicit((Object)(object)((Component)this).GetComponent<ItemStaffTorque>()) || Object.op_Implicit((Object)(object)((Component)this).GetComponent<ItemStaffZeroGravity>()) || Object.op_Implicit((Object)(object)((Component)this).GetComponent<ItemStaffVoid>()) || Object.op_Implicit((Object)(object)((Component)this).GetComponent<ItemCartCannonMain>());
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		if (!Object.op_Implicit((Object)(object)itemAttributes))
		{
			Debug.LogWarning((object)("ItemBattery.cs: No ItemAttributes found on " + ((Object)((Component)this).gameObject).name));
		}
	}

	private void Start()
	{
		batteryVisualLogic = ((Component)this).GetComponentInChildren<BatteryVisualLogic>();
		if (Object.op_Implicit((Object)(object)batteryVisualLogic))
		{
			batteryVisualLogic.itemBattery = this;
		}
		else
		{
			hasBatteryVisuals = false;
		}
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		mainCamera = Camera.main;
		BatteryStateInitialize();
		physGrabObject = ((Component)this).GetComponentInChildren<PhysGrabObject>();
		if (SemiFunc.RunIsLevel() && TutorialDirector.instance.TutorialSettingCheck(DataDirector.Setting.TutorialChargingStation, 1))
		{
			tutorialCheck = true;
		}
	}

	private int BatteryBarsGet()
	{
		int num = Mathf.RoundToInt(batteryLife / (100f / (float)batteryBars));
		if (!usesFullBars && batteryLife > 0f)
		{
			num = Mathf.Max(1, num);
		}
		return Mathf.Clamp(num, 0, batteryBars);
	}

	private void BatteryStateInitialize()
	{
		batteryLifeInt = BatteryBarsGet();
		batteryLifeCountBars = batteryLifeInt;
		batteryLifeCountBarsPrev = batteryLifeInt;
		batteryEmptyPrev = batteryLife <= 0f;
		batteryFullPrev = batteryLife >= 100f;
	}

	private IEnumerator BatteryInit()
	{
		while (!LevelGenerator.Instance.Generated)
		{
			yield return (object)new WaitForSeconds(0.2f);
		}
		if (SemiFunc.RunIsArena() || SemiFunc.RunIsShop())
		{
			batteryLife = 100f;
		}
		else if (!string.IsNullOrEmpty(itemAttributes.instanceName))
		{
			batteryLife = StatsManager.instance.GetBatteryLevel(itemAttributes.instanceName);
		}
		if (batteryLife > 0f)
		{
			batteryLifeInt = BatteryBarsGet();
			batteryColor = itemAttributes.colorPreset.GetColorLight();
			batteryColorMedium = itemAttributes.colorPreset.GetColorMain();
		}
		else
		{
			batteryLife = 0f;
			batteryLifeInt = 0;
			batteryColor = itemAttributes.colorPreset.GetColorLight();
			batteryColorMedium = itemAttributes.colorPreset.GetColorMain();
		}
		BatteryFullPercentChange(batteryLifeInt);
	}

	public void SetBatteryLife(int _batteryLife)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		batteryLife = Mathf.Clamp(_batteryLife, 0, 100);
		batteryLifeInt = BatteryBarsGet();
		batteryColor = itemAttributes.colorPreset.GetColorLight();
		batteryColorMedium = itemAttributes.colorPreset.GetColorMain();
		BatteryFullPercentChange(batteryLifeInt);
	}

	public void OverrideBatteryShow(float time = 0.1f)
	{
		showTimer = time;
	}

	public void ChargeBattery(GameObject chargerObject, float chargeAmount)
	{
		foreach (BatteryCharger charger in chargerList)
		{
			if ((Object)(object)charger.chargerObject == (Object)(object)chargerObject)
			{
				charger.chargeRate = chargeAmount;
				charger.activeTimer = 0.1f;
				chargeTimer = 0.1f;
				return;
			}
		}
		chargerList.Add(new BatteryCharger
		{
			chargerObject = chargerObject,
			chargeRate = chargeAmount,
			activeTimer = 0.1f
		});
		chargeTimer = 0.1f;
	}

	internal bool CanTransferChargeTo(ItemBattery _target)
	{
		if ((Object)(object)_target != (Object)(object)this)
		{
			return SemiFunc.BatteryChargeCondition(_target);
		}
		return false;
	}

	public float TransferCharge(ItemBattery _target, float _chargeRate)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return 0f;
		}
		if (!CanTransferChargeTo(_target))
		{
			return 0f;
		}
		if (batteryLife <= 0f || _chargeRate <= 0f || Time.deltaTime <= 0f)
		{
			return 0f;
		}
		batteryLife = Mathf.Clamp(batteryLife, 0f, 100f);
		float num = Mathf.Min(_chargeRate * 2f * Time.deltaTime, 100f - Mathf.Clamp(_target.batteryLife, 0f, 100f));
		num = Mathf.Min(num, batteryLife * 0.95f);
		if (num <= 0f)
		{
			return 0f;
		}
		num = _target.ReceiveCharge(num);
		if (!itemAttributes.shopItem && !SemiFunc.RunIsShop())
		{
			batteryLife = Mathf.Max(0f, batteryLife - num / 0.95f);
		}
		SaveBatteryCharge();
		return num;
	}

	public float ReceiveCharge(float _amount)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() || _amount <= 0f)
		{
			return 0f;
		}
		batteryLife = Mathf.Clamp(batteryLife, 0f, 100f);
		float num = Mathf.Min(_amount, 100f - batteryLife);
		if (num <= 0f)
		{
			return 0f;
		}
		batteryLife += num;
		transferChargeTimer = 0.1f;
		SaveBatteryCharge();
		return num;
	}

	private void SaveBatteryCharge()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && !string.IsNullOrEmpty(itemAttributes.instanceName) && !SemiFunc.RunIsShop() && !SemiFunc.RunIsArena())
		{
			int num = Mathf.FloorToInt(Mathf.Clamp(batteryLife, 0f, 100f));
			if (StatsManager.instance.GetBatteryLevel(itemAttributes.instanceName) != num)
			{
				StatsManager.instance.ItemUpdateStatBattery(itemAttributes.instanceName, num, sync: false);
			}
		}
	}

	private void FixedUpdate()
	{
		SemiFunc.IsMasterClientOrSingleplayer();
	}

	private void Update()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			batteryLife = Mathf.Clamp(batteryLife, 0f, 100f);
			if (batteryLifePrev != batteryLife)
			{
				TryVisualUpdate();
				batteryLifePrev = batteryLife;
				batteryLifeInt = BatteryBarsGet();
				BatteryUpdateBars(batteryLifeInt);
			}
			for (int num = chargerList.Count - 1; num >= 0; num--)
			{
				if (!Object.op_Implicit((Object)(object)chargerList[num].chargerObject) || chargerList[num].activeTimer <= 0f)
				{
					chargerList.RemoveAt(num);
				}
			}
			chargeRate = 0f;
			foreach (BatteryCharger charger in chargerList)
			{
				chargeRate += charger.chargeRate;
			}
			if ((!(chargeTimer > 0f) || !(batteryLife < 100f) || !(chargeRate > 0f)) && (chargeRate != 0f || isCharging))
			{
				chargeRate = 0f;
				chargeTimer = 0f;
				chargerList.Clear();
				if (isCharging && transferChargeTimer <= 0f)
				{
					BatteryChargeToggle(toggle: false);
				}
			}
			if (chargeTimer > 0f && batteryLife < 100f)
			{
				batteryLife = Mathf.Clamp(batteryLife + chargeRate * 2f * Time.deltaTime, 0f, 100f);
				if (!isCharging && chargeRate > 0f)
				{
					BatteryChargeToggle(toggle: true);
				}
				chargeTimer -= Time.deltaTime;
			}
			foreach (BatteryCharger charger2 in chargerList)
			{
				charger2.activeTimer -= Time.deltaTime;
			}
			if (transferChargeTimer > 0f)
			{
				if (!isCharging)
				{
					BatteryChargeToggle(toggle: true);
				}
				transferChargeTimer -= Time.deltaTime;
			}
			if ((!(drainTimer > 0f) || !(batteryLife > 0f)) && drainRate != 0f)
			{
				drainRate = 0f;
				drainTimer = 0f;
			}
			if (drainTimer > 0f && batteryLife > 0f)
			{
				DrainContinuous(drainRate, Time.deltaTime);
				drainTimer -= Time.deltaTime;
			}
		}
		if (SemiFunc.RunIsLobby() && batteryLifeInt < batteryBars)
		{
			OverrideBatteryShow();
		}
		if (Object.op_Implicit((Object)(object)batteryVisualLogic))
		{
			if (SemiFunc.RunIsLobby())
			{
				OverrideBatteryShow(0.2f);
			}
			if (!SemiFunc.RunIsShop() && !SemiFunc.RunIsArena())
			{
				bool flag = batteryVisualLogic.currentBars < 1;
				if (SemiFunc.RunIsLobby())
				{
					if (flag)
					{
						batteryVisualLogic.OverrideBatteryOutWarning(0.2f);
					}
					else if (batteryVisualLogic.currentBars <= batteryVisualLogic.batteryBars / 2)
					{
						batteryVisualLogic.OverrideChargeNeeded(0.2f);
					}
				}
				else if (flag)
				{
					batteryVisualLogic.OverrideChargeNeeded(0.2f);
				}
			}
		}
		if (showTimer <= 0f)
		{
			showBattery = false;
		}
		if (showTimer > 0f)
		{
			showTimer -= Time.deltaTime;
			showBattery = true;
		}
		if (showBattery && Object.op_Implicit((Object)(object)batteryVisualLogic) && !((Component)batteryVisualLogic).gameObject.activeSelf)
		{
			((Component)batteryVisualLogic).gameObject.SetActive(true);
			batteryVisualLogic.BatteryBarsSet();
		}
		if ((itemAttributes.shopItem && SemiFunc.IsMasterClientOrSingleplayer()) || RoundDirector.instance.debugInfiniteBattery)
		{
			batteryLife = 100f;
		}
		if (isCharging)
		{
			batteryVisualLogic.OverrideBatteryCharge(0.2f);
		}
		if (!SemiFunc.RunIsShop() && !SemiFunc.RunIsArena() && !MapToolController.instance.Active && Object.op_Implicit((Object)(object)PhysGrabber.instance) && PhysGrabber.instance.grabbed && (Object)(object)PhysGrabber.instance.grabbedPhysGrabObject == (Object)(object)physGrabObject)
		{
			if ((Object)(object)BatteryUI.instance.batteryVisualLogic.itemBattery != (Object)(object)this)
			{
				BatteryUI.instance.batteryVisualLogic.itemBattery = this;
				BatteryUI.instance.batteryVisualLogic.BatteryBarsSet();
			}
			if (!suppressBatteryUI && !PlayerExpressionsUI.instance.wheel.Visible)
			{
				BatteryUI.instance.Show();
			}
		}
		BatteryLookAt();
		BatteryChargingVisuals();
		if (showBattery && Object.op_Implicit((Object)(object)batteryVisualLogic) && !((Component)batteryVisualLogic).gameObject.activeSelf)
		{
			BatteryUpdateBars(batteryLifeInt);
		}
		if (tutorialCheck && batteryLife <= 0f && SemiFunc.PerSecond(15f, (Object)(object)this) && physGrabObject.playerGrabbing.Count > 0)
		{
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				if (item.isLocal && TutorialDirector.instance.TutorialSettingCheck(DataDirector.Setting.TutorialChargingStation, 1))
				{
					TutorialDirector.instance.ActivateTip("Charging Station", 2f, _interrupt: false, -1f, _scaleDown: false);
					tutorialCheck = false;
				}
			}
		}
		if (batteryActive)
		{
			if (SemiFunc.IsMasterClientOrSingleplayer() && autoDrain && !itemEquippable.isEquipped)
			{
				DrainContinuous(batteryDrainRate, Time.deltaTime);
			}
			if (batteryLifeInt < 1)
			{
				if (batteryLifeInt == 1)
				{
					batteryOutBlinkTimer += Time.deltaTime;
				}
				else
				{
					batteryOutBlinkTimer += 5f * Time.deltaTime;
				}
				batteryVisualLogic.OverrideBatteryOutWarning(0.2f);
			}
			if (Object.op_Implicit((Object)(object)batteryVisualLogic) && !((Component)batteryVisualLogic).gameObject.activeSelf && !SemiFunc.RunIsShop())
			{
				((Component)batteryVisualLogic).gameObject.SetActive(true);
				BatteryUpdateBars(batteryLifeInt);
			}
		}
		else if (!showBattery)
		{
			batteryOutBlinkTimer = 0f;
			if (Object.op_Implicit((Object)(object)batteryVisualLogic) && ((Component)batteryVisualLogic).gameObject.activeSelf && !isCharging)
			{
				batteryVisualLogic.BatteryOutro();
				BatteryUpdateBars(batteryLifeInt);
			}
		}
		BatteryStateUpdate();
	}

	private void BatteryStateUpdate()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		batteryLife = Mathf.Clamp(batteryLife, 0f, 100f);
		SaveBatteryCharge();
		batteryLifeCountBars = BatteryBarsGet();
		bool flag = batteryLife <= 0f;
		bool flag2 = batteryLife >= 100f;
		bool flag3 = batteryLifeCountBars != batteryLifeCountBarsPrev;
		if (flag3 || flag != batteryEmptyPrev || flag2 != batteryFullPrev)
		{
			bool charge = false;
			if (batteryLifeCountBarsPrev < batteryLifeCountBars)
			{
				charge = true;
			}
			BatteryFullPercentChange(batteryLifeCountBars, charge, flag3);
		}
	}

	public void RemoveFullBar(int _bars)
	{
		if (!itemAttributes.shopItem && !SemiFunc.RunIsShop() && batteryLifeInt > 0)
		{
			batteryLifeInt -= _bars;
			if (batteryLifeInt <= 0)
			{
				batteryLifeInt = 0;
				batteryLife = 0f;
			}
			else
			{
				batteryLife = (float)batteryLifeInt * (100f / (float)batteryBars);
			}
			BatteryFullPercentChange(batteryLifeInt);
		}
	}

	public void BatteryToggle(bool toggle)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				photonView.RPC("BatteryToggleRPC", (RpcTarget)0, new object[1] { toggle });
			}
		}
		else
		{
			BatteryToggleRPC(toggle);
		}
	}

	[PunRPC]
	public void BatteryToggleRPC(bool toggle, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			batteryActive = toggle;
		}
	}

	private void BatteryLookAt()
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		if (hasBatteryVisuals && batteryVisualLogic.cameraTurn && (showBattery || batteryActive || isCharging))
		{
			if (Object.op_Implicit((Object)(object)mainCamera))
			{
				batteryTransform.LookAt(((Component)mainCamera).transform);
				float num = Vector3.Distance(batteryTransform.position, ((Component)mainCamera).transform.position);
				batteryTransform.localScale = Vector3.one * num * 0.8f;
			}
			if (batteryTransform.localScale.x > 3f)
			{
				batteryTransform.localScale = Vector3.one * 3f;
			}
			batteryTransform.Rotate(0f, 180f, 0f);
			batteryTransform.position = ((Component)this).transform.position + Vector3.up * upOffset;
		}
	}

	private void BatteryChargingVisuals()
	{
		if (!isCharging)
		{
			return;
		}
		if (!((Component)batteryVisualLogic).gameObject.activeSelf)
		{
			((Component)batteryVisualLogic).gameObject.SetActive(true);
		}
		chargingBlinkTimer += Time.deltaTime;
		if (chargingBlinkTimer > 0.5f)
		{
			chargingBlink = !chargingBlink;
			if (chargingBlink)
			{
				BatteryUpdateBars(batteryLifeInt + 1);
			}
			else
			{
				BatteryUpdateBars(batteryLifeInt);
			}
			chargingBlinkTimer = 0f;
		}
	}

	private void BatteryChargeToggle(bool toggle)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				photonView.RPC("BatteryChargeStartRPC", (RpcTarget)0, new object[1] { toggle });
			}
		}
		else
		{
			BatteryChargeStartRPC(toggle);
		}
	}

	[PunRPC]
	private void BatteryChargeStartRPC(bool toggle, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			isCharging = toggle;
			BatteryUpdateBars(batteryLifeInt);
		}
	}

	private void BatteryUpdateBars(int batteryLevel)
	{
		if (Object.op_Implicit((Object)(object)batteryVisualLogic))
		{
			currentBars = Mathf.Clamp(batteryLevel, 0, batteryBars);
			batteryVisualLogic.BatteryBarsUpdate();
		}
	}

	private void BatteryFullPercentChangeLogic(int batteryLevel, bool charge, float _batteryLifeSnapshot = -1f, bool _playSound = true)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		if (_playSound && batteryLifeInt > batteryLevel && batteryLevel == 1 && batteryActive)
		{
			AssetManager.instance.batteryLowWarning.Play(((Component)this).transform.position);
		}
		batteryLifeInt = batteryLevel;
		float num = (float)batteryLifeInt * (100f / (float)batteryBars);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			batteryLife = num;
			if (_batteryLifeSnapshot >= 0f)
			{
				batteryLife = Mathf.Clamp(_batteryLifeSnapshot, 0f, 100f);
			}
		}
		else
		{
			batteryLife = Mathf.Clamp(batteryLife, 0f, 100f);
		}
		if (!string.IsNullOrEmpty(itemAttributes.instanceName))
		{
			SemiFunc.StatSetBattery(itemAttributes.instanceName, Mathf.FloorToInt(batteryLife));
		}
		BatteryUpdateBars(batteryLifeInt);
		if (_playSound && (batteryActive | charge))
		{
			if (charge)
			{
				AssetManager.instance.batteryChargeSound.Play(((Component)this).transform.position);
			}
			else
			{
				AssetManager.instance.batteryDrainSound.Play(((Component)this).transform.position);
			}
		}
	}

	internal void NetworkSnapshotApply(int batteryLevel, float charge, bool charging)
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			BatteryFullPercentChangeLogic(batteryLevel, charge: false, charge, _playSound: false);
			isCharging = charging;
			BatteryStateInitialize();
		}
	}

	private void OnDisable()
	{
	}

	private void OnEnable()
	{
	}

	private void BatteryFullPercentChange(int _batteryLifeInt, bool charge = false, bool _playSound = true)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			BatteryFullPercentChangeLogic(_batteryLifeInt, charge, -1f, _playSound);
			BatteryStateInitialize();
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("BatteryFullPercentChangeRPC", (RpcTarget)1, new object[4] { _batteryLifeInt, charge, batteryLife, _playSound });
			}
		}
	}

	[PunRPC]
	private void BatteryFullPercentChangeRPC(int _batteryLifeInt, bool charge, float _batteryLifeSnapshot, bool _playSound, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			BatteryFullPercentChangeLogic(_batteryLifeInt, charge, _batteryLifeSnapshot, _playSound);
		}
	}

	internal void DrainContinuous(float _rate, float _deltaTime)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && !itemAttributes.shopItem && !SemiFunc.RunIsShop())
		{
			batteryLife = Mathf.Max(0f, batteryLife - _rate * 2f * _deltaTime);
		}
	}

	public void Drain(float amount)
	{
		drainRate = amount;
		drainTimer = 0.1f;
	}

	public bool TryVisualUpdate()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return false;
		}
		if (BatteryBarsGet() != batteryLifeInt)
		{
			BatteryUI.instance.batteryVisualLogic.BatteryBarsSet();
			if (Object.op_Implicit((Object)(object)batteryVisualLogic))
			{
				batteryVisualLogic.BatteryBarsSet();
			}
			return true;
		}
		return false;
	}

	public void TriggerSmallBatteryDeduction(float time = 0.4f)
	{
		if (Object.op_Implicit((Object)(object)BatteryUI.instance) && Object.op_Implicit((Object)(object)BatteryUI.instance.batteryVisualLogic) && (Object)(object)BatteryUI.instance.batteryVisualLogic.itemBattery == (Object)(object)this)
		{
			BatteryUI.instance.batteryVisualLogic.SmallBatteryDeductionEffect(time);
		}
	}
}
