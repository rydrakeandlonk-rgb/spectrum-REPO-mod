using UnityEngine;

public class ItemGrenadeShockwave : MonoBehaviour
{
	public GameObject shockwavePrefab;

	private ItemGrenade itemGrenade;

	private void Start()
	{
		itemGrenade = ((Component)this).GetComponent<ItemGrenade>();
	}

	public void Explosion()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Object.Instantiate<GameObject>(shockwavePrefab, ((Component)this).transform.position, Quaternion.identity);
		if (Object.op_Implicit((Object)(object)itemGrenade) && Object.op_Implicit((Object)(object)itemGrenade.activatorPlayerAvatar))
		{
			HurtCollider componentInChildren = val.GetComponentInChildren<HurtCollider>(true);
			if (Object.op_Implicit((Object)(object)componentInChildren))
			{
				componentInChildren.playerCausingHurtOverride = itemGrenade.activatorPlayerAvatar;
			}
		}
	}
}
