using TMPro;
using UnityEngine;

public class ItemInfoUI : SemiUI
{
	private TextMeshProUGUI Text;

	public static ItemInfoUI instance;

	public float messageTimer;

	private string messagePrev = "prev";

	private GameObject bigMessageEmojiObject;

	private TextMeshProUGUI emojiText;

	private VertexGradient originalGradient;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		instance = null;
	}

	protected override void Start()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		base.Start();
		Text = ((Component)this).GetComponent<TextMeshProUGUI>();
		instance = this;
		((TMP_Text)Text).text = "";
		originalGradient = ((TMP_Text)Text).colorGradient;
	}

	public void ItemInfoText(ItemAttributes _itemAttributes, string message, bool enemy = false)
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		ItemAttributes currentlyLookingAtItemAttributes = PhysGrabber.instance.currentlyLookingAtItemAttributes;
		if (!PhysGrabber.instance.grabbed && Object.op_Implicit((Object)(object)_itemAttributes) && Object.op_Implicit((Object)(object)currentlyLookingAtItemAttributes) && (Object)(object)currentlyLookingAtItemAttributes != (Object)(object)_itemAttributes)
		{
			return;
		}
		if (message != ((TMP_Text)Text).text)
		{
			messageTimer = 0f;
			SemiUIResetAllShakeEffects();
		}
		if (enemy)
		{
			VertexGradient colorGradient = new VertexGradient(new Color(1f, 0f, 0f), new Color(1f, 0f, 0f), new Color(1f, 0.1f, 0f), new Color(1f, 0.1f, 0f));
			((TMP_Text)Text).fontSize = 30f;
			((TMP_Text)Text).colorGradient = colorGradient;
		}
		else
		{
			((TMP_Text)Text).colorGradient = originalGradient;
			if (!SemiFunc.RunIsShop())
			{
				((TMP_Text)Text).fontSize = 15f;
			}
		}
		messageTimer = 0.1f;
		if (message != messagePrev)
		{
			((TMP_Text)Text).text = message;
			SemiUISpringShakeY(5f, 5f, 0.3f);
			SemiUISpringScale(0.1f, 2.5f, 0.2f);
			messagePrev = message;
		}
	}

	protected override void Update()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.RunIsShop())
		{
			SemiUIScoot(new Vector2(0f, -20f));
		}
		else if (ItemInfoExtraUI.instance.hideTimer <= 0f)
		{
			if (BatteryUI.instance.showTimer > 0f)
			{
				if (Inventory.instance.InventorySpotsOccupied() > 0)
				{
					SemiUIScoot(new Vector2(0f, 27f));
				}
				else
				{
					SemiUIScoot(new Vector2(0f, 15f));
				}
			}
			else if (Inventory.instance.InventorySpotsOccupied() > 0)
			{
				SemiUIScoot(new Vector2(0f, 16f));
			}
			else
			{
				SemiUIScoot(new Vector2(0f, 2f));
			}
		}
		else if (BatteryUI.instance.showTimer > 0f)
		{
			if (Inventory.instance.InventorySpotsOccupied() > 0)
			{
				SemiUIScoot(new Vector2(0f, 35f));
			}
			else
			{
				SemiUIScoot(new Vector2(0f, 20f));
			}
		}
		else if (Inventory.instance.InventorySpotsOccupied() > 0)
		{
			SemiUIScoot(new Vector2(0f, 5f));
		}
		else
		{
			SemiUIScoot(new Vector2(0f, -15f));
		}
		base.Update();
		if (messageTimer > 0f)
		{
			messageTimer -= Time.deltaTime;
			return;
		}
		messagePrev = "prev";
		Hide();
	}
}
