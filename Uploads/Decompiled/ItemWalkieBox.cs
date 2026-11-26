using Photon.Pun;
using UnityEngine;

public class ItemWalkieBox : MonoBehaviour
{
	public Transform walkie1SpawnPoint;

	public Transform walkie2SpawnPoint;

	public Item walkieTalkieItem;

	public Item walkieBoxItem;

	public Material[] walkieMaterials;

	public MeshRenderer[] walkieMeshRenderers;

	public GameObject enable;

	private int materialIndex;

	private PhotonView photonView;

	private ItemAttributes itemAttributes;

	private bool walkiesSpawned;

	private void Start()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemAttributes = ((Component)this).GetComponent<ItemAttributes>();
		if (SemiFunc.RunIsShop())
		{
			SemiLogger.LogMonika("Hi I am in RunIsShop");
			foreach (Item purchasedItem in ItemManager.instance.purchasedItems)
			{
				string itemName = purchasedItem.itemName;
				SemiLogger.LogMonika(itemName);
				if (itemName == walkieBoxItem.itemName)
				{
					SemiLogger.LogMonika("match");
					materialIndex++;
				}
			}
			foreach (ItemAttributes spawnedItem in ItemManager.instance.spawnedItems)
			{
				if (!((Object)(object)spawnedItem == (Object)null))
				{
					string itemName2 = spawnedItem.itemName;
					SemiLogger.LogMonika("spawned item: " + itemName2);
					if (itemName2 == walkieBoxItem.itemName)
					{
						SemiLogger.LogMonika("match spawned");
						materialIndex++;
					}
				}
			}
		}
		else
		{
			materialIndex = GetWalkieBoxIndex();
		}
		materialIndex = Mathf.Clamp(materialIndex, 0, walkieMaterials.Length - 1);
		MeshRenderer[] array = walkieMeshRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			((Renderer)array[i]).material = walkieMaterials[materialIndex];
		}
	}

	private int GetWalkieBoxIndex()
	{
		int num = 0;
		foreach (ItemAttributes spawnedItem in ItemManager.instance.spawnedItems)
		{
			if (!((Object)(object)spawnedItem == (Object)null))
			{
				SemiLogger.LogMonika("item " + spawnedItem.itemName);
				if ((Object)(object)spawnedItem == (Object)(object)itemAttributes)
				{
					return num;
				}
				if ((Object)(object)spawnedItem.item == (Object)(object)walkieBoxItem)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void Update()
	{
		if (!SemiFunc.RunIsShop() && !walkiesSpawned && SemiFunc.IsMasterClientOrSingleplayer())
		{
			OutOfStore();
		}
	}

	private void OutOfStore()
	{
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		enable.SetActive(false);
		if (SemiFunc.IsMultiplayer())
		{
			GameObject val = PhotonNetwork.InstantiateRoomObject(walkieTalkieItem.prefab.ResourcePath, walkie1SpawnPoint.position, walkie1SpawnPoint.rotation, (byte)0, (object[])null);
			GameObject val2 = PhotonNetwork.InstantiateRoomObject(walkieTalkieItem.prefab.ResourcePath, walkie2SpawnPoint.position, walkie2SpawnPoint.rotation, (byte)0, (object[])null);
			if (!Object.op_Implicit((Object)(object)val) || !Object.op_Implicit((Object)(object)val2))
			{
				return;
			}
			int viewID = val.GetComponent<PhotonView>().ViewID;
			int viewID2 = val2.GetComponent<PhotonView>().ViewID;
			photonView.RPC("LinkWalkiesRPC", (RpcTarget)0, new object[3] { viewID, viewID2, materialIndex });
		}
		else
		{
			GameObject val = Object.Instantiate<GameObject>(walkieTalkieItem.prefab.Prefab, walkie1SpawnPoint.position, walkie1SpawnPoint.rotation);
			GameObject val2 = Object.Instantiate<GameObject>(walkieTalkieItem.prefab.Prefab, walkie2SpawnPoint.position, walkie2SpawnPoint.rotation);
			ItemWalkieTalkie component = val.GetComponent<ItemWalkieTalkie>();
			ItemWalkieTalkie itemWalkieTalkie = (component.otherWalkie = val2.GetComponent<ItemWalkieTalkie>());
			itemWalkieTalkie.otherWalkie = component;
			if (walkieMaterials != null && walkieMaterials.Length != 0)
			{
				component.SetMaterial(walkieMaterials[materialIndex]);
				itemWalkieTalkie.SetMaterial(walkieMaterials[materialIndex]);
			}
		}
		walkiesSpawned = true;
	}

	[PunRPC]
	private void LinkWalkiesRPC(int walkie1ViewID, int walkie2ViewID, int matIndex, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		PhotonView val = PhotonView.Find(walkie1ViewID);
		PhotonView val2 = PhotonView.Find(walkie2ViewID);
		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
		{
			return;
		}
		ItemWalkieTalkie component = ((Component)val).GetComponent<ItemWalkieTalkie>();
		ItemWalkieTalkie component2 = ((Component)val2).GetComponent<ItemWalkieTalkie>();
		if (!((Object)(object)component == (Object)null) && !((Object)(object)component2 == (Object)null))
		{
			component.otherWalkie = component2;
			component2.otherWalkie = component;
			if (walkieMaterials != null && walkieMaterials.Length != 0 && matIndex < walkieMaterials.Length)
			{
				component.SetMaterial(walkieMaterials[matIndex]);
				component2.SetMaterial(walkieMaterials[matIndex]);
			}
		}
	}
}
