using Photon.Pun;
using UnityEngine;

public class ItemDroneTorque : MonoBehaviour
{
	private ItemDrone itemDrone;

	private PhysGrabObject myPhysGrabObject;

	private ItemEquippable itemEquippable;

	private ItemToggle itemToggle;

	private ItemBattery itemBattery;

	private ItemAttributes itemAttributes;

	private float tumbleEnemyTimer;

	private bool tumbledPlayer;

	private void Start()
	{
		itemEquippable = ((Component)this).GetComponent<ItemEquippable>();
		itemDrone = ((Component)this).GetComponent<ItemDrone>();
		myPhysGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
	}

	private void RollTowards(Vector3 direction, Rigidbody targetRb)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.Cross(Vector3.up, direction);
		Vector3 val2 = val.normalized * 6f;
		float num = Mathf.Clamp(3f / targetRb.mass, 1f, 10f);
		val2 *= num;
		targetRb.angularVelocity = val2 / targetRb.mass;
	}

	private void BatteryDrain(float amount)
	{
		itemBattery.DrainContinuous(amount, Time.fixedDeltaTime);
	}

	private void FixedUpdate()
	{
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0534: Unknown result type (might be due to invalid IL or missing references)
		//IL_0539: Unknown result type (might be due to invalid IL or missing references)
		//IL_054b: Unknown result type (might be due to invalid IL or missing references)
		//IL_055d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Unknown result type (might be due to invalid IL or missing references)
		//IL_056f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0600: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0328: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		if (!itemDrone.itemActivated)
		{
			tumbledPlayer = false;
			tumbleEnemyTimer = 0f;
		}
		if (itemEquippable.isEquipped || (GameManager.instance.gameMode == 1 && !PhotonNetwork.IsMasterClient) || !itemDrone.itemActivated)
		{
			return;
		}
		if (!Object.op_Implicit((Object)(object)itemDrone.droneOwner) || (Object.op_Implicit((Object)(object)itemDrone.droneOwner) && itemDrone.droneOwner.isDisabled))
		{
			itemToggle.ToggleItem(toggle: false);
			return;
		}
		myPhysGrabObject.OverrideZeroGravity();
		myPhysGrabObject.OverrideDrag(1f);
		myPhysGrabObject.OverrideAngularDrag(10f);
		if (!itemDrone.magnetActive)
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)itemDrone.playerAvatarTarget) && !tumbledPlayer)
		{
			if (!itemDrone.playerAvatarTarget.tumble.isTumbling)
			{
				itemDrone.playerAvatarTarget.tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
			}
			tumbledPlayer = true;
		}
		if (Object.op_Implicit((Object)(object)itemDrone.playerTumbleTarget))
		{
			Vector3 forward = itemDrone.playerTumbleTarget.playerAvatar.localCamera.GetOverrideTransform().forward;
			if (SemiFunc.OnGroundCheck(itemDrone.magnetTargetRigidbody.position, 1.5f, itemDrone.magnetTargetPhysGrabObject))
			{
				Vector3 val = SemiFunc.PhysFollowDirection(((Component)itemDrone.magnetTargetRigidbody).transform, forward, itemDrone.magnetTargetRigidbody, 20f);
				itemDrone.magnetTargetRigidbody.AddTorque(val * 2.5f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
				Vector3 val2 = SemiFunc.PhysFollowPosition(itemDrone.magnetTargetRigidbody.position, itemDrone.magnetTargetRigidbody.position + forward * 2.5f, itemDrone.magnetTargetRigidbody.linearVelocity, 25f);
				itemDrone.magnetTargetRigidbody.AddForce(val2 * 2f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
				BatteryDrain(2f);
				if (Object.op_Implicit((Object)(object)itemDrone.magnetTargetPhysGrabObject))
				{
					itemDrone.magnetTargetPhysGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialSticky());
				}
			}
			itemDrone.playerTumbleTarget.OverrideDisableLookAtCamera(0.5f);
		}
		if (!Object.op_Implicit((Object)(object)itemDrone.magnetTargetPhysGrabObject) || Object.op_Implicit((Object)(object)itemDrone.playerAvatarTarget) || Object.op_Implicit((Object)(object)itemDrone.playerTumbleTarget))
		{
			return;
		}
		Rigidbody magnetTargetRigidbody = itemDrone.magnetTargetRigidbody;
		Transform transform = ((Component)itemDrone.droneOwner).transform;
		Vector3 val3;
		if (Object.op_Implicit((Object)(object)transform))
		{
			float num = Vector3.Distance(new Vector3(magnetTargetRigidbody.position.x, 0f, magnetTargetRigidbody.position.z), new Vector3(transform.position.x, 0f, transform.position.z));
			val3 = transform.position - magnetTargetRigidbody.position;
			Vector3 val4 = val3.normalized;
			if (itemDrone.magnetTargetPhysGrabObject.isEnemy)
			{
				EnemyParent componentInParent = ((Component)itemDrone.magnetTargetPhysGrabObject).GetComponentInParent<EnemyParent>();
				if (Object.op_Implicit((Object)(object)componentInParent))
				{
					SemiFunc.ItemAffectEnemyBatteryDrain(componentInParent, itemBattery, tumbleEnemyTimer, Time.fixedDeltaTime);
					tumbleEnemyTimer += Time.fixedDeltaTime;
				}
				val4 = -val4;
			}
			float num2 = 2f;
			float num3 = Mathf.Clamp(magnetTargetRigidbody.mass / 1f, 0.2f, 1f);
			float num4 = num3 * 2f;
			if (num < num4)
			{
				num2 = Mathf.Clamp(num - num3, 0f, num4) / num4;
			}
			Vector3 val5 = SemiFunc.PhysFollowDirection(((Component)itemDrone.magnetTargetRigidbody).transform, val4, itemDrone.magnetTargetRigidbody, 10f) * num2;
			itemDrone.magnetTargetRigidbody.AddTorque(val5 * 5f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
			Vector3 val6 = SemiFunc.PhysFollowPosition(itemDrone.magnetTargetRigidbody.position, itemDrone.magnetTargetRigidbody.position + val4, itemDrone.magnetTargetRigidbody.linearVelocity, 10f) * num2;
			itemDrone.magnetTargetRigidbody.AddForce(val6 * 2f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
			itemDrone.magnetTargetPhysGrabObject.OverrideFragility(0.65f);
		}
		else
		{
			val3 = ((Component)this).transform.forward;
			Vector3 val7 = -val3.normalized;
			Vector3 val8 = SemiFunc.PhysFollowDirection(((Component)itemDrone.magnetTargetRigidbody).transform, val7, itemDrone.magnetTargetRigidbody, 10f);
			itemDrone.magnetTargetRigidbody.AddTorque(val8 * 2f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
			Vector3 val9 = SemiFunc.PhysFollowPosition(itemDrone.magnetTargetRigidbody.position, itemDrone.magnetTargetRigidbody.position + val7, itemDrone.magnetTargetRigidbody.linearVelocity, 10f);
			itemDrone.magnetTargetRigidbody.AddForce(val9 * 1f / itemDrone.magnetTargetRigidbody.mass, (ForceMode)0);
		}
	}
}
