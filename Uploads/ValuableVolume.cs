using System.Collections;
using UnityEngine;

public class ValuableVolume : MonoBehaviour
{
	public enum Type
	{
		Tiny,
		Small,
		Medium,
		Big,
		Wide,
		Tall,
		VeryTall
	}

	public Type VolumeType;

	[HideInInspector]
	public Module Module;

	[SerializeField]
	private DebugValuableMeshes debugValuableMeshes;

	private void Start()
	{
		Module = ((Component)this).GetComponentInParent<Module>();
	}

	public void Setup()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		ValuablePropSwitch componentInParent = ((Component)this).GetComponentInParent<ValuablePropSwitch>();
		if (Object.op_Implicit((Object)(object)componentInParent) && (Object)(object)((Component)this).transform.parent != (Object)(object)componentInParent.ValuableParent.transform)
		{
			Debug.LogError((object)"Valuable Volume: Child of ValuablePropSwitch but not valuable parent...", (Object)(object)((Component)this).gameObject);
		}
		if (!((Behaviour)this).isActiveAndEnabled)
		{
			return;
		}
		if (Debug.isDebugBuild)
		{
			((MonoBehaviour)this).StartCoroutine(SafetyCheck());
		}
		foreach (ValuableObject valuable in ValuableDirector.instance.valuableList)
		{
			if (Object.op_Implicit((Object)(object)valuable) && valuable.volumeType == VolumeType && Vector3.Distance(valuable.spawnPosition, ((Component)this).transform.position) < 0.1f)
			{
				((Component)valuable).transform.parent = ((Component)this).transform.parent;
				return;
			}
		}
		foreach (CosmeticWorldObject cosmeticWorldObject in RoundDirector.instance.cosmeticWorldObjects)
		{
			if (Object.op_Implicit((Object)(object)cosmeticWorldObject) && cosmeticWorldObject.valuableVolumeDebugDisplay.VolumeType == VolumeType && Vector3.Distance(cosmeticWorldObject.spawnPosition, ((Component)this).transform.position) < 0.1f)
			{
				((Component)cosmeticWorldObject).transform.parent = ((Component)this).transform.parent;
				break;
			}
		}
	}

	public Collider[] OverlapBoxCheck(LayerMask _layerMask)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		OverlapBoxDataGet(out var _position, out var _halfExtents);
		return Physics.OverlapBox(_position, _halfExtents, ((Component)this).transform.rotation, LayerMask.op_Implicit(_layerMask), (QueryTriggerInteraction)1);
	}

	private void OverlapBoxDataGet(out Vector3 _position, out Vector3 _halfExtents)
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		Mesh val = null;
		if (Object.op_Implicit((Object)(object)debugValuableMeshes))
		{
			val = VolumeType switch
			{
				Type.Tiny => debugValuableMeshes.tiny, 
				Type.Small => debugValuableMeshes.small, 
				Type.Medium => debugValuableMeshes.medium, 
				Type.Big => debugValuableMeshes.big, 
				Type.Wide => debugValuableMeshes.wide, 
				Type.Tall => debugValuableMeshes.tall, 
				Type.VeryTall => debugValuableMeshes.veryTall, 
				_ => (Mesh)null, 
			};
		}
		Vector3 val2;
		if (!Object.op_Implicit((Object)(object)val))
		{
			val2 = Vector3.one;
		}
		else
		{
			Bounds bounds = val.bounds;
			val2 = bounds.size;
		}
		Vector3 val3 = val2;
		_position = ((Component)this).transform.position + ((Component)this).transform.forward * val3.z / 2f + ((Component)this).transform.up * val3.y / 2f + Vector3.up * 0.01f;
		_halfExtents = val3 / 2f;
	}

	private IEnumerator SafetyCheck()
	{
		while (!LevelGenerator.Instance.Generated)
		{
			yield return null;
		}
		Collider[] array = OverlapBoxCheck(LayerMask.op_Implicit(LayerMask.GetMask(new string[1] { "Default" })));
		if (array.Length == 0)
		{
			yield break;
		}
		string text = "not found";
		Module componentInParent = ((Component)this).GetComponentInParent<Module>();
		if (Object.op_Implicit((Object)(object)componentInParent))
		{
			text = ((Object)((Component)componentInParent).gameObject).name;
		}
		Debug.LogError((object)"Valuable Volume: Overlapping colliders:");
		Debug.LogError((object)("     Volume Name: " + ((Object)((Component)this).gameObject).name), (Object)(object)((Component)this).gameObject);
		Debug.LogError((object)("     Volume Module: " + text), (Object)(object)((componentInParent != null) ? ((Component)componentInParent).gameObject : null));
		if (Object.op_Implicit((Object)(object)componentInParent))
		{
			Debug.LogError((object)("     Volume Local Position: " + ((object)((Component)componentInParent).transform.InverseTransformPoint(((Component)this).transform.position)/*cast due to constrained. prefix*/).ToString()));
		}
		Collider[] array2 = array;
		foreach (Collider val in array2)
		{
			text = "not found";
			componentInParent = ((Component)val).gameObject.GetComponentInParent<Module>();
			if (Object.op_Implicit((Object)(object)componentInParent))
			{
				text = ((Object)((Component)componentInParent).gameObject).name;
			}
			Debug.LogError((object)("          Collider: " + ((Object)((Component)val).gameObject).name), (Object)(object)((Component)val).gameObject);
			Debug.LogError((object)("          Collider Module: " + text), (Object)(object)((componentInParent != null) ? ((Component)componentInParent).gameObject : null));
			if (Object.op_Implicit((Object)(object)componentInParent))
			{
				Debug.LogError((object)("          Collider Local Position: " + ((object)((Component)componentInParent).transform.InverseTransformPoint(((Component)val).transform.position)/*cast due to constrained. prefix*/).ToString()));
			}
			Debug.LogError((object)" ");
		}
	}
}
