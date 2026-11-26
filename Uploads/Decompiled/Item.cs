using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Other/Item")]
public class Item : ScriptableObject
{
	public enum Status
	{
		WIP,
		Finalized
	}

	public Status status;

	public bool disabled;

	[Space]
	public string itemName = "N/A";

	public LocalizedAsset itemNameLocalized;

	public string description;

	[Space]
	public SemiFunc.itemType itemType;

	public SemiFunc.emojiIcon emojiIcon;

	public SemiFunc.itemVolume itemVolume;

	public SemiFunc.itemSecretShopType itemSecretShopType;

	[Space]
	public ColorPresets colorPreset;

	public PrefabRef prefab;

	public Value value;

	[Space]
	public int maxAmount = 1;

	public int maxAmountInShop = 1;

	public int minPlayerCount = 1;

	[Space]
	public bool maxPurchase;

	public int maxPurchaseAmount = 1;

	[Space]
	public Quaternion spawnRotationOffset;

	public bool physicalItem = true;

	internal Color GetStatusColor()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		return status switch
		{
			Status.WIP => Color32.op_Implicit(new Color32(byte.MaxValue, (byte)16, (byte)0, byte.MaxValue)), 
			Status.Finalized => Color32.op_Implicit(new Color32((byte)0, byte.MaxValue, (byte)2, byte.MaxValue)), 
			_ => Color.white, 
		};
	}
}
