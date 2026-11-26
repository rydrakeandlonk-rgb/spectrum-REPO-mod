using UnityEngine;

public class ItemGrenadeStun : MonoBehaviour
{
	public Sound soundExplosion;

	public Sound soundTinnitus;

	private Transform stunExplosion;

	private ItemGrenade itemGrenade;

	private void Start()
	{
		stunExplosion = ((Component)((Component)this).GetComponentInChildren<StunExplosion>()).transform;
		((Component)stunExplosion).gameObject.SetActive(false);
		itemGrenade = ((Component)this).GetComponent<ItemGrenade>();
	}

	public void Explosion()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		soundExplosion.Play(((Component)this).transform.position);
		soundTinnitus.Play(((Component)this).transform.position);
		GameDirector.instance.CameraShake.ShakeDistance(5f, 3f, 8f, ((Component)this).transform.position, 0.2f);
		GameDirector.instance.CameraImpact.ShakeDistance(12f, 3f, 8f, ((Component)this).transform.position, 0.1f);
		GameObject val = Object.Instantiate<GameObject>(((Component)stunExplosion).gameObject, ((Component)this).transform.position, ((Component)this).transform.rotation);
		val.transform.parent = null;
		val.GetComponent<StunExplosion>().itemGrenade = itemGrenade;
		val.SetActive(true);
	}
}
