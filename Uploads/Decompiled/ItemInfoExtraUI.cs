using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoExtraUI : SemiUI
{
	private TextMeshProUGUI Text;

	public static ItemInfoExtraUI instance;

	private string messagePrev = "prev";

	private float messageTimer;

	private GameObject bigMessageEmojiObject;

	private TextMeshProUGUI emojiText;

	private Color textColor;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		instance = null;
	}

	protected override void Start()
	{
		base.Start();
		Text = ((Component)this).GetComponent<TextMeshProUGUI>();
		instance = this;
		((TMP_Text)Text).text = "";
	}

	public void ItemInfoText(string message, Color color)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		if (messageTimer > 0f)
		{
			return;
		}
		messageTimer = 0.2f;
		if (message != messagePrev)
		{
			((TMP_Text)Text).text = message;
			SemiUISpringShakeY(20f, 10f, 0.3f);
			SemiUISpringScale(0.4f, 5f, 0.2f);
			textColor = color;
			if (((Graphic)Text).color != textColor)
			{
				((Graphic)Text).color = textColor;
			}
			messagePrev = message;
		}
	}

	protected override void Update()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (Inventory.instance.InventorySpotsOccupied() > 0)
		{
			SemiUIScoot(new Vector2(0f, 5f));
		}
		else
		{
			SemiUIScoot(new Vector2(0f, -10f));
		}
		base.Update();
		if (SemiFunc.RunIsShop())
		{
			return;
		}
		if (!SemiFunc.RunIsShop())
		{
			((TMP_Text)Text).fontSize = 12f;
		}
		if (messageTimer > 0f)
		{
			messageTimer -= Time.deltaTime;
			return;
		}
		if (((Graphic)Text).color != Color.white)
		{
			((Graphic)Text).color = Color.white;
		}
		messagePrev = "prev";
		Hide();
	}
}
