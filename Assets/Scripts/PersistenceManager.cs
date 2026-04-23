using UnityEngine;
using TMPro;
using DG.Tweening;

public class PersistenceManager : MonoBehaviour
{
    [Header("Main Menu Resource UI")]
    [SerializeField] private TextMeshProUGUI mainMenuGoldText;
    [SerializeField] private TextMeshProUGUI mainMenuMoneyText;
    [SerializeField] private TextMeshProUGUI mainMenuMeleeText;
    [SerializeField] private TextMeshProUGUI mainMenuArmorText;
    [SerializeField] private TextMeshProUGUI mainMenuRifleText;

    [Header("Animation Settings")]
    [SerializeField] private float countDuration = 1.0f;

    private IPlayerDataStore playerDataStore;

    // References are automatically assigned with OnValidate
    private void OnValidate()
    {
        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform != null)
        {
            mainMenuGoldText = canvasTransform.Find("Panel_MainMenu/Panel_Resources/layout_resources/ui_image_resource_gold/ui_text_resource_gold_value")?.GetComponent<TextMeshProUGUI>();
            mainMenuMoneyText = canvasTransform.Find("Panel_MainMenu/Panel_Resources/layout_resources/ui_image_resource_money/ui_text_resource_money_value")?.GetComponent<TextMeshProUGUI>();
            mainMenuMeleeText = canvasTransform.Find("Panel_MainMenu/Panel_Resources/layout_resources/ui_image_resource_melee/ui_text_resource_melee_value")?.GetComponent<TextMeshProUGUI>();
            mainMenuArmorText = canvasTransform.Find("Panel_MainMenu/Panel_Resources/layout_resources/ui_image_resource_armor/ui_text_resource_armor_value")?.GetComponent<TextMeshProUGUI>();
            mainMenuRifleText = canvasTransform.Find("Panel_MainMenu/Panel_Resources/layout_resources/ui_image_resource_rifle/ui_text_resource_rifle_value")?.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        ResolveDependencies();

        // Load saved totals and update main menu UI at the start of the game
        UpdateMainMenuUI();
    }

    // This method is called at the end of each game session to save the session rewards into PlayerPrefs
    public void SaveSessionRewards(int gold, int money, int melee, int armor, int rifle)
    {
        // Load current persistent values first so UI can animate from old totals to new totals.
        int currentGold = playerDataStore.GetTotalGold();
        int currentMoney = playerDataStore.GetTotalMoney();
        int currentMelee = playerDataStore.GetTotalMelee();
        int currentArmor = playerDataStore.GetTotalArmor();
        int currentRifle = playerDataStore.GetTotalRifle();

        int totalGold = currentGold + gold;
        int totalMoney = currentMoney + money;
        int totalMelee = currentMelee + melee;
        int totalArmor = currentArmor + armor;
        int totalRifle = currentRifle + rifle;


        // Save the updated totals back to PlayerPrefs
        playerDataStore.SetTotals(totalGold, totalMoney, totalMelee, totalArmor, totalRifle);
        playerDataStore.Save();

        AnimateMainMenuValue(currentGold, totalGold, mainMenuGoldText);
        AnimateMainMenuValue(currentMoney, totalMoney, mainMenuMoneyText);
        AnimateMainMenuValue(currentMelee, totalMelee, mainMenuMeleeText);
        AnimateMainMenuValue(currentArmor, totalArmor, mainMenuArmorText);
        AnimateMainMenuValue(currentRifle, totalRifle, mainMenuRifleText);
    }

    private void AnimateMainMenuValue(int startValue, int endValue, TextMeshProUGUI textComp)
    {
        if (textComp == null)
        {
            return;
        }

        DOTween.Kill(textComp);

        if (startValue == endValue)
        {
            textComp.text = Utils.FormatNumber(endValue);
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.localScale = Vector3.one;
            return;
        }

        Color originalColor = textComp.color;

        int displayedValue = startValue;
        DOTween.To(() => displayedValue, x =>
        {
            displayedValue = x;
            textComp.text = Utils.FormatNumber(x);
        }, endValue, countDuration)
        .SetEase(Ease.OutQuad)
        .SetTarget(textComp)
        .OnPlay(() =>
        {
            textComp.color = Color.green;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one * 1.2f, 0.1f).SetTarget(textComp);
        })
        .OnComplete(() =>
        {
            textComp.color = originalColor;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one, 0.1f).SetTarget(textComp);
        });
    }

    public void UpdateMainMenuUI()
    {
        if (mainMenuGoldText != null) mainMenuGoldText.text = Utils.FormatNumber(playerDataStore.GetTotalGold());
        if (mainMenuMoneyText != null) mainMenuMoneyText.text = Utils.FormatNumber(playerDataStore.GetTotalMoney());
        if (mainMenuMeleeText != null) mainMenuMeleeText.text = Utils.FormatNumber(playerDataStore.GetTotalMelee());
        if (mainMenuArmorText != null) mainMenuArmorText.text = Utils.FormatNumber(playerDataStore.GetTotalArmor());
        if (mainMenuRifleText != null) mainMenuRifleText.text = Utils.FormatNumber(playerDataStore.GetTotalRifle());
    }

    private void ResolveDependencies()
    {
        // Default production implementation; tests can inject a fake store.
        if (playerDataStore == null)
            playerDataStore = new PlayerPrefsDataStore();
    }

    internal void SetPlayerDataStoreForTests(IPlayerDataStore store)
    {
        // Test seam: allows replacing PlayerPrefs-backed storage with a fake store.
        playerDataStore = store;
    }
}