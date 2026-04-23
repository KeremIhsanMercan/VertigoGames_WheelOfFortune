using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject panelRevive;
    [SerializeField] private GameObject panelGame;
    [SerializeField] private GameObject panelMainMenu;

    [Header("Revive UI & Settings")]
    [SerializeField] private Button buttonGiveUp;
    [SerializeField] private Button buttonRevive;
    [SerializeField] private TextMeshProUGUI textReviveCost;
    [SerializeField] private int baseReviveCost = 2500;
    private int currentReviveCost;
    [SerializeField] private GameObject reviveGoldSavings;
    [SerializeField] private TextMeshProUGUI reviveGoldSavingsAmountText;
    [SerializeField] private RectTransform panelReviveAnimationRoot;
    [SerializeField] private RectTransform reviveGoldSavingsAnimationRoot;
    [SerializeField] private RectTransform buttonGiveUpAnimationRoot;
    [SerializeField] private RectTransform buttonReviveAnimationRoot;


    [Header("Manager References")]
    [SerializeField] private WheelManager wheelManager;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PersistenceManager persistenceManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;

    [Header("Game Loop Settings")]
    // This setting is added because playing itself does not cost anything
    // Playing again should not require another click on Play button from main menu
    // Connecting it to a variable makes it easy to modify
    [SerializeField] private bool returnToMainMenuAfterGiveUp = false;

    [Header("Revive Popup Animation")]
    [SerializeField] private float popupShowDuration = 0.2f;
    [SerializeField] private float popupHideDuration = 0.16f;
    [SerializeField] private float popupStartScale = 0.88f;
    [SerializeField] private float dimBackgroundAlpha = 0.7f;
    [SerializeField] private Color reviveButtonDisabledTextColor = new Color(0.62f, 0.62f, 0.62f, 1f);

    private CanvasGroup reviveCanvasGroup;
    private CanvasGroup reviveGoldSavingsCanvasGroup;
    private GameObject reviveDimBackground;
    private Image reviveDimBackgroundImage;
    private RectTransform reviveDimBackgroundRect;
    private RectTransform reviveGoldSavingsRect;
    private Vector3 reviveGoldSavingsBaseScale = Vector3.one;
    private Coroutine popupAnimationCoroutine;
    private readonly Dictionary<Button, Coroutine> buttonAnimationCoroutines = new Dictionary<Button, Coroutine>();
    private readonly Dictionary<TextMeshProUGUI, Color> reviveButtonNormalTextColors = new Dictionary<TextMeshProUGUI, Color>();
    private IAudioService audioService;
    private IPlayerDataStore playerDataStore;

    // References are automatically assigned with OnValidate
    private void OnValidate()
    {
        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform != null)
        {
            if (panelRevive == null) panelRevive = canvasTransform.Find("Panel_Revive")?.gameObject;
            if (panelGame == null) panelGame = canvasTransform.Find("Panel_Game")?.gameObject;
            if (panelMainMenu == null) panelMainMenu = canvasTransform.Find("Panel_MainMenu")?.gameObject;
            
            if (buttonGiveUp == null) 
                buttonGiveUp = canvasTransform.Find("Panel_Revive/ui_button_give_up")?.GetComponent<Button>();
            if (buttonGiveUp != null)
                // Always normalize to button root so all visuals (including border) animate together.
                buttonGiveUpAnimationRoot = buttonGiveUp.transform as RectTransform;
                
            if (buttonRevive == null) 
                buttonRevive = canvasTransform.Find("Panel_Revive/ui_button_revive")?.GetComponent<Button>();
            if (buttonRevive != null)
                // Always normalize to button root so all visuals (including border) animate together.
                buttonReviveAnimationRoot = buttonRevive.transform as RectTransform;

            if (textReviveCost == null) 
                textReviveCost = canvasTransform.Find("Panel_Revive/ui_button_revive/ui_image_revive_gold/ui_text_revive_gold_value")?.GetComponent<TextMeshProUGUI>();

            if (reviveGoldSavings == null) 
                reviveGoldSavings = canvasTransform.Find("ui_image_revive_gold_savings_bg")?.gameObject;
            if (reviveGoldSavingsAnimationRoot == null && reviveGoldSavings != null)
                reviveGoldSavingsAnimationRoot = Utils.ResolveUIAnimationTransform(reviveGoldSavings.transform);

            if (panelReviveAnimationRoot == null && panelRevive != null)
                panelReviveAnimationRoot = Utils.ResolveUIAnimationTransform(panelRevive.transform);

            if (reviveGoldSavingsAmountText == null)
                reviveGoldSavingsAmountText = canvasTransform.Find("ui_image_revive_gold_savings_bg/ui_image_revive_gold_savings/ui_text_revive_gold_savings_value")?.GetComponent<TextMeshProUGUI>();
        }

        if (wheelManager == null) wheelManager = FindObjectOfType<WheelManager>();
        if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
        if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
        if (persistenceManager == null) persistenceManager = FindObjectOfType<PersistenceManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
    }

    private void Start()
    {
        ResolveDependencies();

        currentReviveCost = baseReviveCost;

        InitializeReviveAnimationObjects();

        if (wheelManager != null)
            wheelManager.OnSpinComplete += CheckForBomb;

        AddButtonClickAnimation(buttonGiveUp);
        AddButtonClickAnimation(buttonRevive);

        CacheReviveButtonTextColors();

        if (buttonGiveUp != null) buttonGiveUp.onClick.AddListener(GiveUp);
        if (buttonRevive != null) buttonRevive.onClick.AddListener(Revive);

        SetReviveButtonInteractable(buttonRevive != null && buttonRevive.interactable);

        if (panelRevive != null) panelRevive.SetActive(false);
        if (reviveGoldSavings != null) reviveGoldSavings.SetActive(false);
        if (reviveDimBackground != null) reviveDimBackground.SetActive(false);
    }

    private void OnDestroy()
    {
        if (wheelManager != null)
            wheelManager.OnSpinComplete -= CheckForBomb;
    }

    private void CheckForBomb(SliceData reward)
    {
        if (reward.type == RewardType.Bomb)
        {
            audioService?.PlayBombExplosion();
            ShowRevivePanel();
        }
    }

    private void ShowRevivePanel()
    {
        if (panelRevive == null)
            return;

        if (popupAnimationCoroutine != null)
            StopCoroutine(popupAnimationCoroutine);

        popupAnimationCoroutine = StartCoroutine(ShowRevivePanelRoutine());
        
        if (textReviveCost != null) textReviveCost.text = Utils.FormatNumber(currentReviveCost);

        int playerTotalGold = playerDataStore.GetTotalGold();

        if (reviveGoldSavingsAmountText != null)
            reviveGoldSavingsAmountText.text = Utils.FormatNumber(playerTotalGold);

        SetReviveButtonInteractable(playerTotalGold >= currentReviveCost);
    }

    private void GiveUp()
    {
        audioService?.PlayButtonClick();
        HideRevivePanel(() =>
        {
            inventoryManager.ResetInventory();

            if (returnToMainMenuAfterGiveUp)
            {
                // Return to main menu, where player can see their updated totals and choose to start a new game session
                uiManager.ShowMainMenu();
            }
            else
            {
                // Start a new game session immediately
                wheelManager.GenerateWheel(SpinType.Bronze, true);
                zoneManager.ResetToStart();
                // Reset revive cost for next time game
                currentReviveCost = baseReviveCost;
            }
        });
        

    }

    private void Revive()
    {
        audioService?.PlayRevive();

        int currentGold = playerDataStore.GetTotalGold();
        playerDataStore.SetTotals(
            currentGold - currentReviveCost,
            playerDataStore.GetTotalMoney(),
            playerDataStore.GetTotalMelee(),
            playerDataStore.GetTotalArmor(),
            playerDataStore.GetTotalRifle());
        playerDataStore.Save();

        persistenceManager.UpdateMainMenuUI();

        // Next revive will cost double
        currentReviveCost *= 2; 

        HideRevivePanel(() =>
        {
            // Regenerate bronze wheel because other wheels dont have bombs
            wheelManager.GenerateWheel(SpinType.Bronze, true);
        });
    }

    private void HideRevivePanel(Action onHidden)
    {
        if (panelRevive == null)
        {
            onHidden?.Invoke();
            return;
        }

        if (popupAnimationCoroutine != null)
            StopCoroutine(popupAnimationCoroutine);

        popupAnimationCoroutine = StartCoroutine(HideRevivePanelRoutine(onHidden));
    }

    private void InitializeReviveAnimationObjects()
    {
        if (panelRevive == null)
            return;

        reviveCanvasGroup = panelRevive.GetComponent<CanvasGroup>();
        if (reviveCanvasGroup == null)
            reviveCanvasGroup = panelRevive.AddComponent<CanvasGroup>();

        Transform parent = panelRevive.transform.parent;
        Canvas rootCanvas = panelRevive.GetComponentInParent<Canvas>();
        Transform dimParent = rootCanvas != null ? rootCanvas.transform : parent;

        if (parent == null)
            return;

        Transform existingDim = dimParent != null ? dimParent.Find("Panel_Revive_Dim") : null;
        if (existingDim != null)
        {
            reviveDimBackground = existingDim.gameObject;
            reviveDimBackgroundImage = reviveDimBackground.GetComponent<Image>();
        }
        else
        {
            reviveDimBackground = new GameObject("Panel_Revive_Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            reviveDimBackground.transform.SetParent(dimParent, false);
            reviveDimBackgroundImage = reviveDimBackground.GetComponent<Image>();
        }

        if (reviveDimBackground != null && reviveDimBackground.transform.parent != dimParent)
            reviveDimBackground.transform.SetParent(dimParent, false);

        reviveDimBackgroundRect = reviveDimBackground != null ? reviveDimBackground.GetComponent<RectTransform>() : null;
        ConfigureFullscreenRect(reviveDimBackgroundRect);

        if (reviveDimBackgroundImage != null)
        {
            reviveDimBackgroundImage.color = new Color(0f, 0f, 0f, 0f);
            reviveDimBackgroundImage.raycastTarget = true;
        }

        if (reviveDimBackground != null && reviveDimBackground.transform.parent == panelRevive.transform.parent)
        {
            int panelIndex = panelRevive.transform.GetSiblingIndex();
            reviveDimBackground.transform.SetSiblingIndex(panelIndex);
            panelRevive.transform.SetSiblingIndex(reviveDimBackground.transform.GetSiblingIndex() + 1);
        }
        else if (reviveDimBackground != null)
        {
            reviveDimBackground.transform.SetAsLastSibling();
        }

        if (reviveGoldSavings != null)
        {
            reviveGoldSavingsCanvasGroup = reviveGoldSavings.GetComponent<CanvasGroup>();
            if (reviveGoldSavingsCanvasGroup == null)
                reviveGoldSavingsCanvasGroup = reviveGoldSavings.AddComponent<CanvasGroup>();

            if (reviveGoldSavingsAnimationRoot == null)
                reviveGoldSavingsAnimationRoot = Utils.ResolveUIAnimationTransform(reviveGoldSavings.transform);

            reviveGoldSavingsRect = reviveGoldSavingsAnimationRoot != null ? reviveGoldSavingsAnimationRoot : reviveGoldSavings.transform as RectTransform;
            if (reviveGoldSavingsRect != null)
                reviveGoldSavingsBaseScale = reviveGoldSavingsRect.localScale;
        }

        if (panelReviveAnimationRoot == null)
            panelReviveAnimationRoot = Utils.ResolveUIAnimationTransform(panelRevive.transform);
    }

    private void AddButtonClickAnimation(Button button)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() => PlayButtonClickAnimation(button));
    }

    private void CacheReviveButtonTextColors()
    {
        reviveButtonNormalTextColors.Clear();

        if (buttonRevive == null)
            return;

        TextMeshProUGUI[] reviveTexts = buttonRevive.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < reviveTexts.Length; i++)
        {
            TextMeshProUGUI text = reviveTexts[i];
            if (text != null && !reviveButtonNormalTextColors.ContainsKey(text))
                reviveButtonNormalTextColors.Add(text, text.color);
        }
    }

    private void SetReviveButtonInteractable(bool interactable)
    {
        if (buttonRevive == null)
            return;

        buttonRevive.interactable = interactable;

        if (reviveButtonNormalTextColors.Count == 0)
            CacheReviveButtonTextColors();

        foreach (KeyValuePair<TextMeshProUGUI, Color> pair in reviveButtonNormalTextColors)
        {
            TextMeshProUGUI text = pair.Key;
            if (text == null)
                continue;

            text.color = interactable ? pair.Value : reviveButtonDisabledTextColor;
        }
    }

    private void PlayButtonClickAnimation(Button button)
    {
        if (button == null)
            return;

        if (buttonAnimationCoroutines.TryGetValue(button, out Coroutine runningCoroutine) && runningCoroutine != null)
            StopCoroutine(runningCoroutine);

        RectTransform animationTarget = ResolveButtonAnimationTarget(button);
        buttonAnimationCoroutines[button] = StartCoroutine(AnimateButtonClickRoutine(animationTarget));
    }

    private IEnumerator ShowRevivePanelRoutine()
    {
        ConfigureFullscreenRect(reviveDimBackgroundRect);

        panelRevive.SetActive(true);
        if (reviveGoldSavings != null)
            reviveGoldSavings.SetActive(true);
        if (reviveDimBackground != null)
            reviveDimBackground.SetActive(true);

        RectTransform popupRect = panelReviveAnimationRoot != null ? panelReviveAnimationRoot : panelRevive.transform as RectTransform;
        if (popupRect == null)
            yield break;

        popupRect.localScale = Vector3.one * popupStartScale;

        if (reviveGoldSavingsRect != null)
            reviveGoldSavingsRect.localScale = reviveGoldSavingsBaseScale * popupStartScale;

        reviveCanvasGroup.alpha = 0f;
        reviveCanvasGroup.interactable = false;
        reviveCanvasGroup.blocksRaycasts = false;

        if (reviveGoldSavingsCanvasGroup != null)
        {
            reviveGoldSavingsCanvasGroup.alpha = 0f;
            reviveGoldSavingsCanvasGroup.interactable = false;
            reviveGoldSavingsCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < popupShowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popupShowDuration);
            float eased = EaseOutBack(t);

            reviveCanvasGroup.alpha = t;
            popupRect.localScale = Vector3.LerpUnclamped(Vector3.one * popupStartScale, Vector3.one, eased);
            if (reviveGoldSavingsCanvasGroup != null)
                reviveGoldSavingsCanvasGroup.alpha = t;
            if (reviveGoldSavingsRect != null)
                reviveGoldSavingsRect.localScale = Vector3.LerpUnclamped(reviveGoldSavingsBaseScale * popupStartScale, reviveGoldSavingsBaseScale, eased);
            SetDimAlpha(dimBackgroundAlpha * t);
            yield return null;
        }

        reviveCanvasGroup.alpha = 1f;
        popupRect.localScale = Vector3.one;
        if (reviveGoldSavingsCanvasGroup != null)
            reviveGoldSavingsCanvasGroup.alpha = 1f;
        if (reviveGoldSavingsRect != null)
            reviveGoldSavingsRect.localScale = reviveGoldSavingsBaseScale;
        SetDimAlpha(dimBackgroundAlpha);
        reviveCanvasGroup.interactable = true;
        reviveCanvasGroup.blocksRaycasts = true;

        if (reviveGoldSavingsCanvasGroup != null)
        {
            reviveGoldSavingsCanvasGroup.interactable = true;
            reviveGoldSavingsCanvasGroup.blocksRaycasts = true;
        }

        popupAnimationCoroutine = null;
    }

    private IEnumerator HideRevivePanelRoutine(Action onHidden)
    {
        RectTransform popupRect = panelReviveAnimationRoot != null ? panelReviveAnimationRoot : panelRevive.transform as RectTransform;
        if (popupRect == null)
        {
            panelRevive.SetActive(false);
            if (reviveGoldSavings != null)
                reviveGoldSavings.SetActive(false);
            if (reviveDimBackground != null)
                reviveDimBackground.SetActive(false);
            onHidden?.Invoke();
            popupAnimationCoroutine = null;
            yield break;
        }

        reviveCanvasGroup.interactable = false;
        reviveCanvasGroup.blocksRaycasts = false;

        if (reviveGoldSavingsCanvasGroup != null)
        {
            reviveGoldSavingsCanvasGroup.interactable = false;
            reviveGoldSavingsCanvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        Vector3 startScale = popupRect.localScale;
        Vector3 endScale = Vector3.one * 0.94f;
        float startAlpha = reviveCanvasGroup.alpha;
        Vector3 reviveGoldSavingsStartScale = reviveGoldSavingsRect != null ? reviveGoldSavingsRect.localScale : Vector3.one;
        Vector3 reviveGoldSavingsEndScale = reviveGoldSavingsBaseScale * 0.94f;
        float reviveGoldSavingsStartAlpha = reviveGoldSavingsCanvasGroup != null ? reviveGoldSavingsCanvasGroup.alpha : 0f;

        while (elapsed < popupHideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / popupHideDuration);

            reviveCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            popupRect.localScale = Vector3.Lerp(startScale, endScale, t);
            if (reviveGoldSavingsCanvasGroup != null)
                reviveGoldSavingsCanvasGroup.alpha = Mathf.Lerp(reviveGoldSavingsStartAlpha, 0f, t);
            if (reviveGoldSavingsRect != null)
                reviveGoldSavingsRect.localScale = Vector3.Lerp(reviveGoldSavingsStartScale, reviveGoldSavingsEndScale, t);
            SetDimAlpha(Mathf.Lerp(dimBackgroundAlpha, 0f, t));
            yield return null;
        }

        panelRevive.SetActive(false);
        if (reviveGoldSavings != null)
            reviveGoldSavings.SetActive(false);
        if (reviveDimBackground != null)
            reviveDimBackground.SetActive(false);

        popupRect.localScale = Vector3.one;
        if (reviveGoldSavingsRect != null)
            reviveGoldSavingsRect.localScale = reviveGoldSavingsBaseScale;
        if (reviveGoldSavingsCanvasGroup != null)
            reviveGoldSavingsCanvasGroup.alpha = 0f;
        SetDimAlpha(0f);

        onHidden?.Invoke();
        popupAnimationCoroutine = null;
    }

    private IEnumerator AnimateButtonClickRoutine(RectTransform buttonRect)
    {
        if (buttonRect == null)
            yield break;

        Vector3 originalScale = buttonRect.localScale;
        Vector3 pressScale = originalScale * 0.93f;
        const float halfDuration = 0.06f;

        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            buttonRect.localScale = Vector3.Lerp(originalScale, pressScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            buttonRect.localScale = Vector3.Lerp(pressScale, originalScale, t);
            yield return null;
        }

        buttonRect.localScale = originalScale;
    }

    private void SetDimAlpha(float alpha)
    {
        if (reviveDimBackgroundImage == null)
            return;

        Color c = reviveDimBackgroundImage.color;
        c.a = alpha;
        reviveDimBackgroundImage.color = c;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void ConfigureFullscreenRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private RectTransform ResolveButtonAnimationTarget(Button button)
    {
        if (button == null)
            return null;
        
        // Runtime safeguard: always animate the button root transform.
        return button.transform as RectTransform;
    }

    private void ResolveDependencies()
    {
        // Scene reference first; singleton fallback keeps runtime resilient in case reference is missing.
        if (audioManager == null)
            audioManager = AudioManager.Instance;

        if (audioService == null)
            audioService = audioManager;

        if (playerDataStore == null)
            playerDataStore = new PlayerPrefsDataStore();
    }

    internal void SetAudioServiceForTests(IAudioService service)
    {
        // Test seam: allows replacing real audio with a fake/mock service.
        audioService = service;
    }

    internal void SetPlayerDataStoreForTests(IPlayerDataStore store)
    {
        // Test seam: allows replacing PlayerPrefs-backed storage with a fake store.
        playerDataStore = store;
    }
}