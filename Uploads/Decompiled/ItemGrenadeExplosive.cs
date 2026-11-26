using System.Collections;
using UnityEngine;

public class ItemGrenadeExplosive : MonoBehaviour
{
	private ParticleScriptExplosion particleScriptExplosion;

	private ItemGrenade itemGrenade;

	private void Start()
	{
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		itemGrenade = ((Component)this).GetComponent<ItemGrenade>();
		if (SemiFunc.RunIsShop() && SemiFunc.IsMasterClientOrSingleplayer())
		{
			ItemToggle component = ((Component)this).GetComponent<ItemToggle>();
			if (ShopManager.instance.isThief)
			{
				((MonoBehaviour)this).StartCoroutine(ThiefLaunch());
				component.ToggleItem(toggle: true);
				itemGrenade.isSpawnedGrenade = true;
				itemGrenade.itemAttributes.blockExtraction = true;
			}
		}
	}

	private IEnumerator ThiefLaunch()
	{
		yield return (object)new WaitForSeconds(0.2f);
		Rigidbody component = ((Component)this).GetComponent<Rigidbody>();
		Vector3 forward = ShopManager.instance.extractionPoint.forward;
		forward += Vector3.up * Random.Range(0.1f, 0.5f);
		forward += ShopManager.instance.extractionPoint.right * Random.Range(-0.5f, 0.5f);
		component.AddForce(forward * (float)Random.Range(3, 7), (ForceMode)1);
	}

	public void Explosion()
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)itemGrenade) && Object.op_Implicit((Object)(object)itemGrenade.activatorPlayerAvatar))
		{
			particleScriptExplosion.playerCausingHurtOverride = itemGrenade.activatorPlayerAvatar;
		}
		particleScriptExplosion.playerHitFullRuckusOverride = true;
		particleScriptExplosion.Spawn(((Component)this).transform.position, 1.2f, 75, 160, 4f);
	}
}
