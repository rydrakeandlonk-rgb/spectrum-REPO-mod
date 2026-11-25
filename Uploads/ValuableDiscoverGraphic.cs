using UnityEngine;
using UnityEngine.UI;

public class ValuableDiscoverGraphic : MonoBehaviour
{
	public enum State
	{
		Discover,
		Reminder,
		Bad,
		Custom
	}

	public PhysGrabObject target;

	private State state;

	private Camera mainCamera;

	private RectTransform canvasRect;

	private Vector3[] screenSpaceCorners;

	private bool hidden = true;

	private bool first = true;

	[Space]
	public Color colorDiscoverCorner;

	public Color colorDiscoverMiddle;

	[Space]
	public Color ColorReminderCorner;

	public Color ColorReminderMiddle;

	[Space]
	public Color ColorBadCorner;

	public Color ColorBadMiddle;

	[Space]
	public Sound sound;

	[Space]
	public AnimationCurve introCurve;

	public float introSpeed;

	public AnimationCurve outroCurve;

	public float outroSpeed;

	public float waitTime;

	private float waitTimer;

	private float animLerp;

	[Space]
	public RectTransform middle;

	private Vector2 middleTarget;

	private Vector2 middleTargetNew;

	private Vector2 middleTargetSize;

	private Vector2 middleTargetSizeNew;

	public RectTransform topLeft;

	private Vector2 topLeftTarget;

	private Vector2 topLeftTargetNew;

	public RectTransform topRight;

	private Vector2 topRightTarget;

	private Vector2 topRightTargetNew;

	public RectTransform botLeft;

	private Vector2 botLeftTarget;

	private Vector2 botLeftTargetNew;

	public RectTransform botRight;

	private Vector2 botRightTarget;

	private Vector2 botRightTargetNew;

	private CanvasGroup canvasGroup;

	private void Start()
	{
		canvasRect = ValuableDiscover.instance.canvasRect;
		mainCamera = Camera.main;
		waitTimer = waitTime;
		if (state == State.Reminder)
		{
			waitTimer = waitTime * 0.5f;
		}
		if (state == State.Bad)
		{
			waitTimer = waitTime * 3f;
		}
		canvasGroup = ((Component)this).GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Unknown result type (might be due to invalid IL or missing references)
		//IL_0604: Unknown result type (might be due to invalid IL or missing references)
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0630: Unknown result type (might be due to invalid IL or missing references)
		//IL_0636: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_065d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0663: Unknown result type (might be due to invalid IL or missing references)
		//IL_0679: Unknown result type (might be due to invalid IL or missing references)
		//IL_068a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0690: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		//IL_053d: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0554: Unknown result type (might be due to invalid IL or missing references)
		//IL_056a: Unknown result type (might be due to invalid IL or missing references)
		//IL_057b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
		//IL_0597: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)target))
		{
			bool flag = true;
			Bounds bigBounds = new Bounds(target.centerPoint, Vector3.zero);
			MeshRenderer[] componentsInChildren = ((Component)target).GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer val in componentsInChildren)
			{
				if (((Renderer)val).enabled)
				{
					bigBounds.Encapsulate(((Renderer)val).bounds);
				}
			}
			if (SemiFunc.OnScreen(bigBounds.center, 0.5f, 0.5f))
			{
				Rect val2 = RendererBoundsInScreenSpace(bigBounds);
				if (val2.width > 2f || val2.height > 2f)
				{
					topLeftTargetNew = val2.center;
					topRightTargetNew = val2.center;
					botLeftTargetNew = val2.center;
					botRightTargetNew = val2.center;
					middleTargetNew = val2.center;
					middleTargetSizeNew = new Vector2(0f, 0f);
				}
				else
				{
					topLeftTargetNew = Vector2.op_Implicit(GetScreenPosition(new Vector3(val2.xMin, val2.yMax, 0f)));
					topRightTargetNew = Vector2.op_Implicit(GetScreenPosition(new Vector3(val2.xMax, val2.yMax, 0f)));
					botLeftTargetNew = Vector2.op_Implicit(GetScreenPosition(new Vector3(val2.xMin, val2.yMin, 0f)));
					botRightTargetNew = Vector2.op_Implicit(GetScreenPosition(new Vector3(val2.xMax, val2.yMin, 0f)));
					middleTargetNew = Vector2.op_Implicit(GetScreenPosition(Vector2.op_Implicit(val2.center)));
					middleTargetSizeNew = new Vector2(val2.width * 1.9f + 0.025f, val2.height + 0.025f);
				}
			}
			else
			{
				flag = false;
			}
			if (flag)
			{
				if (first)
				{
					if (state == State.Reminder)
					{
						sound.Play(target.centerPoint, 0.3f);
					}
					else
					{
						sound.Play(target.centerPoint);
					}
					((Component)middle).gameObject.SetActive(true);
					((Component)topLeft).gameObject.SetActive(true);
					((Component)topRight).gameObject.SetActive(true);
					((Component)botLeft).gameObject.SetActive(true);
					((Component)botRight).gameObject.SetActive(true);
					first = false;
				}
				if (hidden)
				{
					middleTarget = middleTargetNew;
					middleTargetSize = middleTargetSizeNew;
					topLeftTarget = topLeftTargetNew;
					topRightTarget = topRightTargetNew;
					botLeftTarget = botLeftTargetNew;
					botRightTarget = botRightTargetNew;
					hidden = false;
				}
				middleTarget = Vector2.Lerp(middleTarget, middleTargetNew, 50f * Time.deltaTime);
				middleTargetSize = Vector2.Lerp(middleTargetSize, middleTargetSizeNew, 50f * Time.deltaTime);
				topLeftTarget = Vector2.Lerp(topLeftTarget, topLeftTargetNew, 50f * Time.deltaTime);
				topRightTarget = Vector2.Lerp(topRightTarget, topRightTargetNew, 50f * Time.deltaTime);
				botLeftTarget = Vector2.Lerp(botLeftTarget, botLeftTargetNew, 50f * Time.deltaTime);
				botRightTarget = Vector2.Lerp(botRightTarget, botRightTargetNew, 50f * Time.deltaTime);
				canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, 50f * Time.deltaTime);
			}
			else
			{
				hidden = true;
				topLeftTarget = middleTarget;
				topRightTarget = middleTarget;
				botLeftTarget = middleTarget;
				botRightTarget = middleTarget;
				middleTargetSize = Vector2.zero;
				canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, 50f * Time.deltaTime);
			}
		}
		else
		{
			waitTimer = 0f;
		}
		middle.anchoredPosition = middleTarget;
		if (waitTimer > 0f)
		{
			animLerp = Mathf.Clamp01(animLerp + introSpeed * Time.deltaTime);
			middle.sizeDelta = Vector2.LerpUnclamped(Vector2.zero, middleTargetSize, introCurve.Evaluate(animLerp));
			topLeft.anchoredPosition = Vector2.LerpUnclamped(middleTarget, topLeftTarget, introCurve.Evaluate(animLerp));
			topRight.anchoredPosition = Vector2.LerpUnclamped(middleTarget, topRightTarget, introCurve.Evaluate(animLerp));
			botLeft.anchoredPosition = Vector2.LerpUnclamped(middleTarget, botLeftTarget, introCurve.Evaluate(animLerp));
			botRight.anchoredPosition = Vector2.LerpUnclamped(middleTarget, botRightTarget, introCurve.Evaluate(animLerp));
			if (animLerp >= 1f)
			{
				waitTimer -= Time.deltaTime;
				if (waitTimer <= 0f)
				{
					animLerp = 0f;
				}
			}
		}
		else
		{
			animLerp = Mathf.Clamp01(animLerp + outroSpeed * Time.deltaTime);
			middle.sizeDelta = Vector2.LerpUnclamped(middleTargetSize, Vector2.zero, outroCurve.Evaluate(animLerp));
			topLeft.anchoredPosition = Vector2.LerpUnclamped(topLeftTarget, middleTarget, outroCurve.Evaluate(animLerp));
			topRight.anchoredPosition = Vector2.LerpUnclamped(topRightTarget, middleTarget, outroCurve.Evaluate(animLerp));
			botLeft.anchoredPosition = Vector2.LerpUnclamped(botLeftTarget, middleTarget, outroCurve.Evaluate(animLerp));
			botRight.anchoredPosition = Vector2.LerpUnclamped(botRightTarget, middleTarget, outroCurve.Evaluate(animLerp));
			if (animLerp >= 1f)
			{
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
		}
	}

	public void ReminderSetup()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		state = State.Reminder;
		((Graphic)((Component)middle).GetComponent<Image>()).color = ColorReminderMiddle;
		((Graphic)((Component)topLeft).GetComponent<Image>()).color = ColorReminderCorner;
		((Graphic)((Component)topRight).GetComponent<Image>()).color = ColorReminderCorner;
		((Graphic)((Component)botLeft).GetComponent<Image>()).color = ColorReminderCorner;
		((Graphic)((Component)botRight).GetComponent<Image>()).color = ColorReminderCorner;
	}

	public void BadSetup()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		state = State.Bad;
		((Graphic)((Component)middle).GetComponent<Image>()).color = ColorBadMiddle;
		((Graphic)((Component)topLeft).GetComponent<Image>()).color = ColorBadCorner;
		((Graphic)((Component)topRight).GetComponent<Image>()).color = ColorBadCorner;
		((Graphic)((Component)botLeft).GetComponent<Image>()).color = ColorBadCorner;
		((Graphic)((Component)botRight).GetComponent<Image>()).color = ColorBadCorner;
	}

	public void CustomSetup(ValuableDiscoverCustom _custom)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		state = State.Custom;
		((Graphic)((Component)topLeft).GetComponent<Image>()).color = _custom.colorCorner;
		((Graphic)((Component)topRight).GetComponent<Image>()).color = _custom.colorCorner;
		((Graphic)((Component)botLeft).GetComponent<Image>()).color = _custom.colorCorner;
		((Graphic)((Component)botRight).GetComponent<Image>()).color = _custom.colorCorner;
		((Graphic)((Component)middle).GetComponent<Image>()).color = _custom.colorMiddle;
		waitTime = _custom.waitTime;
		if (_custom.hasCustomSound)
		{
			Sound.CopySound(_custom.customSound, sound);
		}
	}

	private Vector3 GetScreenPosition(Vector3 _position)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(_position.x * canvasRect.sizeDelta.x - canvasRect.sizeDelta.x * 0.5f, _position.y * canvasRect.sizeDelta.y - canvasRect.sizeDelta.y * 0.5f, _position.z) / SemiFunc.UIMulti();
	}

	private Rect RendererBoundsInScreenSpace(Bounds bigBounds)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		if (screenSpaceCorners == null)
		{
			screenSpaceCorners = new Vector3[8];
		}
		screenSpaceCorners[0] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x + bigBounds.extents.x, bigBounds.center.y + bigBounds.extents.y, bigBounds.center.z + bigBounds.extents.z));
		screenSpaceCorners[1] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x + bigBounds.extents.x, bigBounds.center.y + bigBounds.extents.y, bigBounds.center.z - bigBounds.extents.z));
		screenSpaceCorners[2] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x + bigBounds.extents.x, bigBounds.center.y - bigBounds.extents.y, bigBounds.center.z + bigBounds.extents.z));
		screenSpaceCorners[3] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x + bigBounds.extents.x, bigBounds.center.y - bigBounds.extents.y, bigBounds.center.z - bigBounds.extents.z));
		screenSpaceCorners[4] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x - bigBounds.extents.x, bigBounds.center.y + bigBounds.extents.y, bigBounds.center.z + bigBounds.extents.z));
		screenSpaceCorners[5] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x - bigBounds.extents.x, bigBounds.center.y + bigBounds.extents.y, bigBounds.center.z - bigBounds.extents.z));
		screenSpaceCorners[6] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x - bigBounds.extents.x, bigBounds.center.y - bigBounds.extents.y, bigBounds.center.z + bigBounds.extents.z));
		screenSpaceCorners[7] = mainCamera.WorldToViewportPoint(new Vector3(bigBounds.center.x - bigBounds.extents.x, bigBounds.center.y - bigBounds.extents.y, bigBounds.center.z - bigBounds.extents.z));
		float x = screenSpaceCorners[0].x;
		float y = screenSpaceCorners[0].y;
		float x2 = screenSpaceCorners[0].x;
		float y2 = screenSpaceCorners[0].y;
		for (int i = 1; i < 8; i++)
		{
			if (screenSpaceCorners[i].x < x)
			{
				x = screenSpaceCorners[i].x;
			}
			if (screenSpaceCorners[i].y < y)
			{
				y = screenSpaceCorners[i].y;
			}
			if (screenSpaceCorners[i].x > x2)
			{
				x2 = screenSpaceCorners[i].x;
			}
			if (screenSpaceCorners[i].y > y2)
			{
				y2 = screenSpaceCorners[i].y;
			}
		}
		return Rect.MinMaxRect(x, y, x2, y2);
	}
}
