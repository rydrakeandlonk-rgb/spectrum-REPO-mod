using UnityEngine;

public class ValuablePropSwitch : MonoBehaviour
{
	public GameObject ValuableParent;

	public GameObject PropParent;

	internal bool SetupComplete;

	[HideInInspector]
	public string DebugState = "...";

	[HideInInspector]
	public bool DebugSwitch;

	[HideInInspector]
	public string ChildValuableString = "...";

	private void Awake()
	{
		if (!SetupComplete)
		{
			ValuableParent.SetActive(true);
			PropParent.SetActive(false);
		}
	}

	public void Setup()
	{
		ValuablePropSwitch[] componentsInParent = ((Component)this).gameObject.GetComponentsInParent<ValuablePropSwitch>(true);
		for (int i = 0; i < componentsInParent.Length; i++)
		{
			if ((Object)(object)componentsInParent[i] != (Object)(object)this)
			{
				Debug.LogError((object)"ValuablePropSwitch: Switches inside switches is not supported...", (Object)(object)((Component)this).gameObject);
			}
		}
		if (!Object.op_Implicit((Object)(object)((Component)this).gameObject.GetComponentInChildren<ValuableVolume>(true)))
		{
			Debug.LogError((object)(((Object)((Component)((Component)this).gameObject.GetComponentInParent<Module>()).gameObject).name + "  |  ValuablePropSwitch: No ValuableVolume found in children..."), (Object)(object)((Component)this).gameObject);
			return;
		}
		bool flag = false;
		ValuableObject[] componentsInChildren = ((Component)this).GetComponentsInChildren<ValuableObject>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (((Component)componentsInChildren[i]).gameObject.activeSelf)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			CosmeticWorldObject[] componentsInChildren2 = ((Component)this).GetComponentsInChildren<CosmeticWorldObject>(true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				if (((Component)componentsInChildren2[i]).gameObject.activeSelf)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			PropParent.SetActive(false);
			ValuableParent.SetActive(true);
		}
		else
		{
			ValuableParent.SetActive(false);
			PropParent.SetActive(true);
		}
		SetupComplete = true;
	}

	public void DebugToggle()
	{
		SetValuableActive(DebugSwitch);
	}

	public void SetValuableActive(bool _valuableActive)
	{
		if (_valuableActive)
		{
			DebugSwitch = false;
			DebugState = "Valuable Active";
			if ((Object)(object)ValuableParent != (Object)null)
			{
				ValuableParent.SetActive(true);
			}
			if ((Object)(object)PropParent != (Object)null)
			{
				PropParent.SetActive(false);
			}
		}
		else
		{
			DebugSwitch = true;
			DebugState = "Prop Active";
			if ((Object)(object)PropParent != (Object)null)
			{
				PropParent.SetActive(true);
			}
			if ((Object)(object)ValuableParent != (Object)null)
			{
				ValuableParent.SetActive(false);
			}
		}
	}

	private void OnValidate()
	{
		if (SemiFunc.OnValidateCheck() || Application.isPlaying)
		{
			return;
		}
		ValuableVolume componentInChildren = ((Component)this).gameObject.GetComponentInChildren<ValuableVolume>(true);
		if ((Object)(object)componentInChildren != (Object)null)
		{
			ChildValuableString = componentInChildren.VolumeType.ToString();
			if (((Object)((Component)this).gameObject).name != ChildValuableString)
			{
				((Object)((Component)this).gameObject).name = ChildValuableString;
			}
		}
	}

	private void OnDrawGizmos()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.DrawIcon(((Component)this).transform.position, "ValuablePropSwitch.png", true);
	}
}
