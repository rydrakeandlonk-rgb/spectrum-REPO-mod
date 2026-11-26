using System.Collections;
using Photon.Pun;
using UnityEngine;

public class ItemGrenadeHuman : MonoBehaviour
{
	private ParticleScriptExplosion particleScriptExplosion;

	private ItemToggle itemToggle;

	private ItemGrenade itemGrenade;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private Rigidbody rb;

	public Sound soundExplosion;

	public Sound soundExplosionGlobal;

	private void Awake()
	{
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		itemToggle = ((Component)this).GetComponent<ItemToggle>();
		itemGrenade = ((Component)this).GetComponent<ItemGrenade>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		rb = ((Component)this).GetComponent<Rigidbody>();
	}

	public void Spawn()
	{
		itemGrenade.itemAttributes.blockExtraction = true;
		((MonoBehaviour)this).StartCoroutine(LateSpawn());
		itemGrenade.isSpawnedGrenade = true;
	}

	private IEnumerator LateSpawn()
	{
		while (!physGrabObject.spawned || rb.isKinematic)
		{
			yield return null;
		}
		itemToggle.ToggleItem(toggle: true, (Object.op_Implicit((Object)(object)itemGrenade) && Object.op_Implicit((Object)(object)itemGrenade.activatorPlayerAvatar)) ? itemGrenade.activatorPlayerAvatar.photonView.ViewID : (-1));
		itemGrenade.tickTime = Random.Range(1.5f, 3f);
		Vector3 val = Quaternion.Euler((float)Random.Range(-45, 45), (float)Random.Range(-180, 180), 0f) * Vector3.forward;
		rb.AddForce(val * (float)Random.Range(5, 10), (ForceMode)1);
		rb.AddTorque(Random.insideUnitSphere * Random.Range(5f, 10f), (ForceMode)1);
		itemGrenade.isSpawnedGrenade = true;
	}

	public void Explosion()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		if (Object.op_Implicit((Object)(object)itemGrenade) && Object.op_Implicit((Object)(object)itemGrenade.activatorPlayerAvatar))
		{
			particleScriptExplosion.playerCausingHurtOverride = itemGrenade.activatorPlayerAvatar;
		}
		particleScriptExplosion.playerHitFullRuckusOverride = true;
		particleScriptExplosion.Spawn(((Component)this).transform.position, 0.8f, 50, 100, 2f, onlyParticleEffect: false, disableSound: true);
		soundExplosion.Play(((Component)this).transform.position);
		soundExplosionGlobal.Play(((Component)this).transform.position);
	}
}
