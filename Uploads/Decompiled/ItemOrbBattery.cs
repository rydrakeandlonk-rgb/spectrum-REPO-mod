using System.Collections.Generic;
using UnityEngine;

public class ItemOrbBattery : MonoBehaviour, ITargetingCondition
{
	private ItemOrb itemOrb;

	private PhysGrabObject physGrabObject;

	private ItemToggle itemToggle;

	private ItemBattery itemBattery;

	private bool didCharge;

	private readonly List<ItemBattery> chargeBatteries = new List<ItemBattery>();

	public bool CustomTargetingCondition(GameObject target)
	{
		ItemBattery component = target.GetComponent<ItemBattery>();
		return itemBattery.CanTransferChargeTo(component);
	}

	private void Start()
	{
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemOrb = ((Component)this).GetComponent<ItemOrb>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
	}

	private void Update()
	{
		if (!itemOrb.itemActive)
		{
			didCharge = false;
		}
		else if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			ChargeTargetsGet();
			ChargeTargets();
		}
	}

	private void ChargeTargetsGet()
	{
		chargeBatteries.Clear();
		foreach (PhysGrabObject item in itemOrb.objectAffected)
		{
			if (Object.op_Implicit((Object)(object)item) && !((Object)(object)item == (Object)(object)physGrabObject))
			{
				ItemBattery component = ((Component)item).GetComponent<ItemBattery>();
				if (itemBattery.CanTransferChargeTo(component) && !chargeBatteries.Contains(component))
				{
					chargeBatteries.Add(component);
				}
			}
		}
	}

	private void ChargeTargets()
	{
		if (chargeBatteries.Count == 0)
		{
			if (didCharge)
			{
				itemToggle.ToggleItem(toggle: false);
			}
			didCharge = false;
		}
		else
		{
			if (Time.deltaTime <= 0f)
			{
				return;
			}
			float num = Mathf.Min(SemiFunc.BatteryGetChargeRate(3), Mathf.Max(0f, itemBattery.batteryLife) * 0.95f / (2f * Time.deltaTime));
			num /= (float)chargeBatteries.Count;
			foreach (ItemBattery chargeBattery in chargeBatteries)
			{
				if (itemBattery.TransferCharge(chargeBattery, num) > 0f)
				{
					didCharge = true;
				}
			}
		}
	}
}
