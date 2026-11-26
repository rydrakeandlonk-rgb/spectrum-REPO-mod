using UnityEngine;

public class ItemOrbZeroGravity : MonoBehaviour
{
	private ItemOrb itemOrb;

	private PhysGrabObject physGrabObject;

	private void Start()
	{
		itemOrb = ((Component)this).GetComponent<ItemOrb>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
	}

	private void Update()
	{
		if (!itemOrb.itemActive)
		{
			return;
		}
		if (itemOrb.localPlayerAffected)
		{
			PlayerController.instance.AntiGravity(0.1f);
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		foreach (PhysGrabObject item in itemOrb.objectAffected)
		{
			if (Object.op_Implicit((Object)(object)item) && (Object)(object)physGrabObject != (Object)(object)item)
			{
				item.OverrideDrag(0.5f);
				item.OverrideAngularDrag(0.5f);
				item.OverrideZeroGravity();
			}
		}
	}
}
