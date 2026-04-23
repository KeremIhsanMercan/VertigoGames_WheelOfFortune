using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

using Random = UnityEngine.Random;

// --- ENUMs ---
public enum SpinType { Bronze, Silver, Gold }
public enum RewardType { Gold, Money, MeleeShard, ArmorShard, RifleShard, Bomb }
public enum RewardTier { Mid, High, Special, None } // None, for Skull (Bomb) slice which has no tier

// --- STRUCTs ---
public struct SliceData
{
    public RewardType type;
    public RewardTier tier;
    public int amount;
}

// --- UI REFERENCE OBJECT ---
[System.Serializable]
public class SliceUI
{
    public Image rewardImage;
    public TextMeshProUGUI amountText;
}

public class WheelManager : MonoBehaviour
{
    [Header("OnValidate References")]
    [SerializeField] private SliceUI[] slices = new SliceUI[8];
    [SerializeField] private Image wheelBackground; // Reference to the wheel background image (for changing sprite based on spin type)
    [SerializeField] private Image wheelPointer;
    [SerializeField] private TextMeshProUGUI TextTitle;
    [SerializeField] private TextMeshProUGUI TextFooter;
    [SerializeField] private Button buttonSpin;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private RectTransform wheelContainer; // Parent panel for the spin UI
    [SerializeField] private RectTransform wheelAnimationRoot;
    [SerializeField] private RectTransform spinButtonAnimationRoot;
    [SerializeField] private RectTransform titleAnimationRoot;
    [SerializeField] private RectTransform footerAnimationRoot;

    [Header("Sprite References")]
    public Sprite goldSprite;
    public Sprite moneySprite;
    public Sprite skullSprite;
    public Sprite[] meleeSprites = new Sprite[3];
    public Sprite[] armorSprites = new Sprite[2];
    public Sprite[] rifleSprites = new Sprite[5];
    
    // Bronze, Silver, Gold
    public Sprite[] wheelBackgroundSprites = new Sprite[3];
    public Sprite[] wheelPointerSprites = new Sprite[3];

    [Header("Spin Settings")]
    [SerializeField] private float spinDuration = 3f;
    [SerializeField] private int spinLaps = 5;
    [SerializeField] private float idleSpinDurationPerTurn = 60f;

    // Event to notify when spin is complete, passing the resulting SliceData of the landed slice
    public event Action<SliceData> OnSpinComplete;

    private List<SliceData> currentSlices = new List<SliceData>();
    private bool isSpinning = false;
    private Tween idleWheelTween;
    private const string WheelAnimRootName = "Wheel_AnimRoot";
    private IAudioService audioService;

    // Button (These are not buttons but still automated their references with OnValidate) references should be automatically set from OnValidate
    private void OnValidate()
    {
        if (slices == null || slices.Length != 8)
            slices = new SliceUI[8];

        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Transform sliceTransform = FindWheelElement(canvasTransform, $"Slice_{i}");
                if (sliceTransform != null)
                {
                    if (slices[i] == null) slices[i] = new SliceUI();

                    Transform imgTransform = sliceTransform.Find("ui_image_reward_value");
                    if (imgTransform != null)
                        slices[i].rewardImage = imgTransform.GetComponent<Image>();

                    Transform txtTransform = sliceTransform.Find("ui_text_amount_value");
                    if (txtTransform != null)
                        slices[i].amountText = txtTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (wheelBackground == null)
            {
                Transform backgroundTransform = FindWheelElement(canvasTransform, "ui_image_wheel_bg_value");
                if (backgroundTransform != null)
                    wheelBackground = backgroundTransform.GetComponent<Image>();
            }

            if (wheelPointer == null)
            {
                Transform pointerTransform = canvasTransform.Find("Panel_Game/ui_image_pointer_value");
                if (pointerTransform != null)
                    wheelPointer = pointerTransform.GetComponent<Image>();
            }

            if (TextTitle == null)
            {
                Transform titleTransform = canvasTransform.Find("Panel_Game/ui_text_title_value");
                if (titleTransform != null)
                    TextTitle = titleTransform.GetComponent<TextMeshProUGUI>();
            }

            if (TextFooter == null)
            {
                Transform footerTransform = canvasTransform.Find("Panel_Game/ui_text_footer_value");
                if (footerTransform != null)
                    TextFooter = footerTransform.GetComponent<TextMeshProUGUI>();
            }

            if (buttonSpin == null)
            {
                Transform buttonTransform = canvasTransform.Find("Panel_Game/ui_button_spin");
                if (buttonTransform != null)
                    buttonSpin = buttonTransform.GetComponent<Button>();
            }

            if (buttonSpin != null)
            {
                // Always normalize to button root so all visuals (including border) animate together.
                spinButtonAnimationRoot = buttonSpin.transform as RectTransform;
            }
            
            if (wheelContainer == null)
            {
                Transform containerTransform = canvasTransform.Find("Panel_Game/Wheel_Container");
                if (containerTransform != null)
                    wheelContainer = containerTransform.GetComponent<RectTransform>();
            }

            if (wheelAnimationRoot == null && wheelContainer != null)
            {
                // Reconnect to the dedicated wheel animation root if it already exists in hierarchy.
                Transform existingAnimRoot = wheelContainer.Find(WheelAnimRootName);
                if (existingAnimRoot is RectTransform existingAnimRect)
                    wheelAnimationRoot = existingAnimRect;
            }

            if (wheelAnimationRoot != null && wheelContainer != null && wheelAnimationRoot.name != WheelAnimRootName)
            {
                // If an old reference points to a child visual, normalize it back to Wheel_AnimRoot.
                Transform existingAnimRoot = wheelContainer.Find(WheelAnimRootName);
                if (existingAnimRoot is RectTransform existingAnimRect)
                    wheelAnimationRoot = existingAnimRect;
            }

            if (titleAnimationRoot == null && TextTitle != null)
            {
                titleAnimationRoot = Utils.ResolveUIAnimationTransform(TextTitle.transform);
                // Fallback keeps animation working for layouts without a dedicated child animation node.
                if (titleAnimationRoot == null)
                    titleAnimationRoot = TextTitle.transform as RectTransform;
            }

            if (footerAnimationRoot == null && TextFooter != null)
            {
                footerAnimationRoot = Utils.ResolveUIAnimationTransform(TextFooter.transform);
                // Fallback keeps animation working for layouts without a dedicated child animation node.
                if (footerAnimationRoot == null)
                    footerAnimationRoot = TextFooter.transform as RectTransform;
            }
        }

        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();
    }

    public void Start()
    {
        ResolveDependencies();

        EnsureWheelAnimationRoot();

        // Set up button listener
        if (buttonSpin != null)
        {
            buttonSpin.onClick.RemoveAllListeners();
            buttonSpin.onClick.AddListener(OnSpinClicked);
        }
        GenerateWheel(SpinType.Bronze, false); // Default to Bronze spin on start
    }

    private void OnSpinClicked()
    {
        audioService?.PlayButtonClick();
        PlayClickAnimation(buttonSpin, SpinWheel);
    }

    private void PlayClickAnimation(Button button, Action onComplete)
    {
        if (button == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Runtime safeguard: always animate the button root transform.
        RectTransform rectTransform = button.transform as RectTransform;

        if (rectTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        Utils.PlayButtonClickTween(rectTransform, onComplete);
    }

    private void SpinWheel()
    {
        if (isSpinning) return; // Prevent clicking spin while already spinning
        isSpinning = true;

        StopIdleWheelSpin();

        if (buttonSpin != null)
            buttonSpin.interactable = false; // Button will be re-enabled after new wheel is generated in GenerateWheel()

        RectTransform wheelSpinTarget = wheelAnimationRoot != null ? wheelAnimationRoot : wheelContainer;
        if (wheelSpinTarget == null)
            return;

        // Reset wheel rotation to 0 before spinning
        wheelSpinTarget.eulerAngles = Vector3.zero;

        // Select a random winning slice index (0-7) and get its data
        int winningIndex = Random.Range(0, 8);
        SliceData winningData = currentSlices[winningIndex];

        // Calculate target angle: Each slice is 45 degrees, and we want to do several full laps before landing on the winning slice
        float targetAngle = -(360f * spinLaps) + (winningIndex * 45f);

        // Animate the wheel rotation using DOTween
        wheelSpinTarget.DORotate(new Vector3(0, 0, targetAngle), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuart) // Start fast and slow down towards the end for a satisfying effect
            .OnComplete(() => 
            {

                // Wheel has stopped, send the reward to other scripts (Inventory and Zone)
                OnSpinComplete?.Invoke(winningData); 
                
                // TEST FOR CONSOLE OUTPUT:
                // Debug.Log($"<color=green>Reward: {winningData.type} x{winningData.amount}</color>");
            });
    }

    public void GenerateWheel(SpinType spinType, bool animate = false)
    {

        if (animate)
        {
            RectTransform wheelScaleTarget = wheelAnimationRoot != null ? wheelAnimationRoot : wheelContainer;
            if (wheelScaleTarget == null)
            {
                SetupWheelData(spinType);
                isSpinning = false;
                if (buttonSpin != null)
                    buttonSpin.interactable = true;
                StartIdleWheelSpin();
                return;
            }

            // Animated transition: First shrink the wheel to 0, then change data, then pop back to normal size
            wheelScaleTarget.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
            {
                // Setup new wheel data while it's shrunk
                SetupWheelData(spinType);
                
                // Pop back to normal size with a nice bounce
                wheelScaleTarget.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    isSpinning = false; // Allow spinning again after the transition
                    if (buttonSpin != null)
                        buttonSpin.interactable = true;

                    StartIdleWheelSpin();
                });
            });
        }
        else
        {
            // Immediate transition without animation
            SetupWheelData(spinType);
            RectTransform wheelScaleTarget = wheelAnimationRoot != null ? wheelAnimationRoot : wheelContainer;
            if (wheelScaleTarget != null)
                wheelScaleTarget.localScale = Vector3.one;

            isSpinning = false; // Allow spinning immediately
            if (buttonSpin != null)
                buttonSpin.interactable = true;

            StartIdleWheelSpin();
        }
    }

    private void StartIdleWheelSpin()
    {
        RectTransform wheelSpinTarget = wheelAnimationRoot != null ? wheelAnimationRoot : wheelContainer;
        if (wheelSpinTarget == null || isSpinning) return;

        StopIdleWheelSpin();

        idleWheelTween = wheelSpinTarget.DOLocalRotate(new Vector3(0f, 0f, -360f), idleSpinDurationPerTurn, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    private void StopIdleWheelSpin()
    {
        if (idleWheelTween != null && idleWheelTween.IsActive())
        {
            idleWheelTween.Kill();
            idleWheelTween = null;
        }
    }

    private void OnDisable()
    {
        StopIdleWheelSpin();
    }

    private void SetupWheelData(SpinType spinType)
    {
        List<RewardTier> tierPool = GetTierPool(spinType);
        currentSlices.Clear(); // Clear previous slices data

        // Update wheel and pointer sprites based on spin type
        int typeIndex = (int)spinType; // Enum is ordered as Bronze=0, Silver=1, Gold=2
        if (wheelBackground != null && typeIndex < wheelBackgroundSprites.Length)
            wheelBackground.sprite = wheelBackgroundSprites[typeIndex];

        if (wheelPointer != null && typeIndex < wheelPointerSprites.Length)
            wheelPointer.sprite = wheelPointerSprites[typeIndex];

        SetTitleAndFooterText(spinType);

        // Generate slice data based on the tier pool
        foreach (RewardTier tier in tierPool)
        {
            if (tier == RewardTier.None)
            {
                currentSlices.Add(new SliceData { type = RewardType.Bomb, tier = RewardTier.None, amount = 0 });
                continue;
            }

            // Assign reward type randomly based on predefined probabilities
            RewardType randomType = GetRandomRewardType();
            // Multiplier value based on tier (Mid = x1, High = x5, Special = x10)
            int multiplier = GetTierMultiplier(tier);
            // Base value for the reward type (Gold = 100, Money = 10, Shard's = 1)
            int baseValue = GetBaseValue(randomType);

            currentSlices.Add(new SliceData
            {
                type = randomType,
                tier = tier,
                amount = baseValue * multiplier
            });
        }

        ShuffleList(currentSlices);
        RenderToUI(currentSlices);
    }

    private void EnsureWheelAnimationRoot()
    {
        if (wheelContainer == null)
            return;

        if (wheelAnimationRoot == null)
        {
            Transform existingAnimRoot = wheelContainer.Find(WheelAnimRootName);
            if (existingAnimRoot is RectTransform existingAnimRect)
            {
                wheelAnimationRoot = existingAnimRect;
            }
            else
            {
                GameObject animRootObject = new GameObject(WheelAnimRootName, typeof(RectTransform));
                RectTransform newAnimRoot = animRootObject.GetComponent<RectTransform>();
                newAnimRoot.SetParent(wheelContainer, false);
                newAnimRoot.anchorMin = Vector2.zero;
                newAnimRoot.anchorMax = Vector2.one;
                newAnimRoot.pivot = new Vector2(0.5f, 0.5f);
                newAnimRoot.anchoredPosition = Vector2.zero;
                newAnimRoot.sizeDelta = Vector2.zero;
                newAnimRoot.localScale = Vector3.one;
                wheelAnimationRoot = newAnimRoot;
            }
        }

        if (wheelAnimationRoot == null)
            return;

        List<Transform> childrenToMove = new List<Transform>();
        for (int i = 0; i < wheelContainer.childCount; i++)
        {
            Transform child = wheelContainer.GetChild(i);
            if (child != wheelAnimationRoot)
                childrenToMove.Add(child);
        }

        for (int i = 0; i < childrenToMove.Count; i++)
            childrenToMove[i].SetParent(wheelAnimationRoot, false);
    }

    private Transform FindWheelElement(Transform canvasTransform, string elementName)
    {
        if (canvasTransform == null)
            return null;

        Transform direct = canvasTransform.Find($"Panel_Game/Wheel_Container/{elementName}");
        if (direct != null)
            return direct;

        return canvasTransform.Find($"Panel_Game/Wheel_Container/{WheelAnimRootName}/{elementName}");
    }

    private void ResolveDependencies()
    {
        // Scene reference first; singleton fallback keeps runtime resilient in case reference is missing.
        if (audioManager == null)
            audioManager = AudioManager.Instance;

        if (audioService == null)
            audioService = audioManager;
    }

    internal void SetAudioServiceForTests(IAudioService service)
    {
        // Test seam: allows replacing real audio with a fake/mock service.
        audioService = service;
    }

    // --- HELPER FUNCTIONS ---

    private void SetTitleAndFooterText(SpinType spinType)
    {
        if (TextTitle == null || TextFooter == null) return;

        string newTitle = TextTitle.text;
        string newFooter = TextFooter.text;

        switch (spinType)
        {
            case SpinType.Bronze:
                newTitle = "BRONZE SPIN";
                newFooter = "SPIN THE WHEEL!";
                break;
            case SpinType.Silver:
                newTitle = "SILVER SPIN";
                newFooter = "NO BOMB!";
                break;
            case SpinType.Gold:
                newTitle = "GOLD SPIN";
                newFooter = "NO BOMB + SPECIAL REWARDS!";
                break;
        }

        bool titleChanged = TextTitle.text != newTitle;
        bool footerChanged = TextFooter.text != newFooter;

        TextTitle.text = newTitle;
        TextFooter.text = newFooter;

        // Animate a pop up effect on the title and footer text if they are changing
        // Bronze spin to Bronze spin transition will not trigger this animation since the text is the same, but Bronze to Silver or Silver to Gold and vice versa will trigger it
        if (titleChanged)
        {
            Transform titleTarget = titleAnimationRoot != null ? titleAnimationRoot : TextTitle.transform;
            titleTarget.DOKill();
            titleTarget.localScale = Vector3.one;
            titleTarget.DOPunchScale(Vector3.one * 0.2f, 0.25f, 8, 0.8f);
        }

        if (footerChanged)
        {
            Transform footerTarget = footerAnimationRoot != null ? footerAnimationRoot : TextFooter.transform;
            footerTarget.DOKill();
            footerTarget.localScale = Vector3.one;
            footerTarget.DOPunchScale(Vector3.one * 0.15f, 0.22f, 8, 0.8f);
        }
    }

    private List<RewardTier> GetTierPool(SpinType spinType)
    {
        List<RewardTier> pool = new List<RewardTier>();
        switch (spinType)
        {
            case SpinType.Bronze: // Bronze spin has 5 Mid, 2 High, 1 Bomb
                AddTiersToPool(pool, RewardTier.Mid, 5);
                AddTiersToPool(pool, RewardTier.High, 2);
                pool.Add(RewardTier.None); // 1 Skull (Bomb)
                break;
            case SpinType.Silver: // Silver spin has 3 Mid, 4 High, 1 Special
                AddTiersToPool(pool, RewardTier.Mid, 3);
                AddTiersToPool(pool, RewardTier.High, 4);
                AddTiersToPool(pool, RewardTier.Special, 1);
                break;
            case SpinType.Gold: // Gold spin has 1 Mid, 3 High, 4 Special
                AddTiersToPool(pool, RewardTier.Mid, 1);
                AddTiersToPool(pool, RewardTier.High, 3);
                AddTiersToPool(pool, RewardTier.Special, 4);
                break;
        }
        return pool;
    }

    private void AddTiersToPool(List<RewardTier> pool, RewardTier tier, int count)
    {
        for (int i = 0; i < count; i++) pool.Add(tier);
    }

    private RewardType GetRandomRewardType()
    {
        int randomVal = Random.Range(0, 1000);
        if (randomVal < 275) return RewardType.Gold;         // 0 - 275 (%27.5)
        if (randomVal < 550) return RewardType.Money;        // 276 - 550 (%27.5)
        if (randomVal < 699) return RewardType.MeleeShard;   // 551 - 699 (%15)
        if (randomVal < 849) return RewardType.ArmorShard;   // 700 - 849 (%15)
        return RewardType.RifleShard;                       // 850 - 999 (%15)
    }

    private int GetTierMultiplier(RewardTier tier)
    {
        switch (tier)
        {
            case RewardTier.High: return 5;
            case RewardTier.Special: return 10;
            default: return 1; // Mid
        }
    }

    private int GetBaseValue(RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold: return 100;
            case RewardType.Money: return 10;
            default: return 1; // Shard's
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        // Fisher-Yates Algorithm
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void RenderToUI(List<SliceData> dataList)
    {
        for (int i = 0; i < slices.Length; i++)
        {
            SliceData data = dataList[i];
            SliceUI ui = slices[i];

            ui.rewardImage.preserveAspect = true; // Ensure no stretching

            if (data.type == RewardType.Bomb)
            {
                ui.rewardImage.sprite = skullSprite;
                ui.amountText.gameObject.SetActive(false); // No amount for Bomb slice
            }
            else
            {
                ui.rewardImage.sprite = GetSpriteForType(data.type);
                ui.amountText.gameObject.SetActive(true);
                ui.amountText.text = "x" + data.amount.ToString();
            }
        }
    }

    private Sprite GetSpriteForType(RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold: return goldSprite;
            case RewardType.Money: return moneySprite;
            case RewardType.MeleeShard: return meleeSprites[Random.Range(0, meleeSprites.Length)];
            case RewardType.ArmorShard: return armorSprites[Random.Range(0, armorSprites.Length)];
            case RewardType.RifleShard: return rifleSprites[Random.Range(0, rifleSprites.Length)];
            default: return null;
        }
    }
}