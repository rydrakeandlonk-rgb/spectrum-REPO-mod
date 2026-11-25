using System;
using System.Collections.Generic;
using UnityEngine;

public class ValuableSprings : MonoBehaviour
{
	[Serializable]
	public class ValuableSpring
	{
		public string name;

		public SpringQuaternionSystem springSystem;

		public bool Setup(Cosmetic _cosmetic)
		{
			if (Debug.isDebugBuild && ((Object)(object)springSystem.target == (Object)null || (Object)(object)springSystem.transform == (Object)null))
			{
				Debug.LogError((object)("<b><color=yellow>" + ((_cosmetic != null) ? ((Object)_cosmetic).name.Replace("(Clone)", "") : null) + "</color></b> has springs with missing transforms!"), (Object)(object)((_cosmetic != null) ? ((Component)_cosmetic).gameObject : null));
				return false;
			}
			return true;
		}
	}

	public List<ValuableSpring> springs;

	private void Update()
	{
		foreach (ValuableSpring spring in springs)
		{
			spring.springSystem.UpdateWorldSpace();
		}
	}
}
