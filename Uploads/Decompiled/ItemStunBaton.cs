using System.Collections.Generic;
using UnityEngine;

public class ItemStunBaton : MonoBehaviour
{
	public Transform stunBatonEffects;

	public Sound stunBatonSound;

	private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

	private void Start()
	{
		ParticleSystem[] componentsInChildren = ((Component)stunBatonEffects).GetComponentsInChildren<ParticleSystem>();
		foreach (ParticleSystem item in componentsInChildren)
		{
			particleSystems.Add(item);
		}
	}

	public void PlayParticles()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		stunBatonSound.Play(((Component)this).transform.position);
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			particleSystem.Play();
		}
	}
}
