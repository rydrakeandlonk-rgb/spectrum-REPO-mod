using UnityEngine;

public class ItemDroneBattery : MonoBehaviour, ITargetingCondition
{
	private ItemDrone itemDrone;

	private PhysGrabObject myPhysGrabObject;

	private ItemEquippable itemEquippable;

	private ItemBattery itemBattery;

	private void Start()
	{
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemDrone = ((Component)this).GetComponent<ItemDrone>();
		myPhysGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
	}

	public bool CustomTargetingCondition(GameObject target)
	{
		ItemBattery component = target.GetComponent<ItemBattery>();
		return itemBattery.CanTransferChargeTo(component);
	}

	private void Update()
	{
		if (itemEquippable.isEquipped || !SemiFunc.IsMasterClientOrSingleplayer() || !itemDrone.itemActivated)
		{
			return;
		}
		myPhysGrabObject.OverrideZeroGravity();
		myPhysGrabObject.OverrideDrag(1f);
		myPhysGrabObject.OverrideAngularDrag(10f);
		if (itemDrone.magnetActive && Object.op_Implicit((Object)(object)itemDrone.magnetTargetPhysGrabObject))
		{
			ItemBattery component = ((Component)itemDrone.magnetTargetPhysGrabObject).GetComponent<ItemBattery>();
			itemBattery.TransferCharge(component, 5f);
			if (!itemBattery.CanTransferChargeTo(component))
			{
				itemDrone.MagnetActiveToggle(toggleBool: false);
				itemDrone.DeactivateDrone();
			}
		}
	}
}
