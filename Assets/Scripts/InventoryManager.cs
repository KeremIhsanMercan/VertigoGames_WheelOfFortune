using UnityEngine;
using TMPro;
using DG.Tweening;

public struct SessionRewards
{
    public int gold;
    public int money;
    public int melee;
    public int armor;
    public int rifle;
}

public class InventoryManager : MonoBehaviour
{
    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI textGold;
    [SerializeField] private TextMeshProUGUI textMoney;
    [SerializeField] private TextMeshProUGUI textMelee;
    [SerializeField] private TextMeshProUGUI textArmor;
    [SerializeField] private TextMeshProUGUI textRifle;

    [Header("Managers")]
    [SerializeField] private WheelManager wheelManager;
    [SerializeField] private ZoneManager zoneManager;


    [Header("Animation Settings")]
    [SerializeField] private float countDuration = 1.0f;
    [SerializeField] private float bombDuration = 1.5f;

    // Current inventory values
    private int currentGold = 0;
    private int currentMoney = 0;
    private int currentMelee = 0;
    private int currentArmor = 0;
    private int currentRifle = 0;

    // References are automatically assigned with OnValidate
    private void OnValidate()
    {
        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform != null)
        {
            // Find the TextMeshProUGUI components in the scene
            if (textGold == null) textGold = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/layout_collected_items/ui_image_reward_gold/ui_text_reward_gold_value")?.GetComponent<TextMeshProUGUI>();
            if (textMoney == null) textMoney = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/layout_collected_items/ui_image_reward_money/ui_text_reward_money_value")?.GetComponent<TextMeshProUGUI>();
            if (textMelee == null) textMelee = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/layout_collected_items/ui_image_reward_melee/ui_text_reward_melee_value")?.GetComponent<TextMeshProUGUI>();
            if (textArmor == null) textArmor = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/layout_collected_items/ui_image_reward_armor/ui_text_reward_armor_value")?.GetComponent<TextMeshProUGUI>();
            if (textRifle == null) textRifle = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/layout_collected_items/ui_image_reward_rifle/ui_text_reward_rifle_value")?.GetComponent<TextMeshProUGUI>();
        }

        if (wheelManager == null)
        {
            wheelManager = FindObjectOfType<WheelManager>();
        }

        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
        }
    }

    // Find wheelManager on start
    private void OnEnable()
    {
        if (wheelManager != null)
        {
            wheelManager.OnSpinComplete += HandleSpinComplete;
        }
    }

    private void OnDisable()
    {
        // To prevent potential memory leaks, unsubscribed from the event when the object is disabled or destroyed.
        if (wheelManager != null)
        {
            wheelManager.OnSpinComplete -= HandleSpinComplete;
        }
    }

    // This method is called when the wheel spin is complete and a reward is given
    private void HandleSpinComplete(SliceData reward)
    {
        if (reward.type == RewardType.Bomb)
        {
            return;
        }

        AddReward(reward.type, reward.amount);
    }

    private void AddReward(RewardType type, int amount)
    {
        switch (type)
        {
            case RewardType.Gold:
                AnimateValue(currentGold, currentGold + amount, textGold, (val) => currentGold = val);
                break;
            case RewardType.Money:
                AnimateValue(currentMoney, currentMoney + amount, textMoney, (val) => currentMoney = val);
                break;
            case RewardType.MeleeShard:
                AnimateValue(currentMelee, currentMelee + amount, textMelee, (val) => currentMelee = val);
                break;
            case RewardType.ArmorShard:
                AnimateValue(currentArmor, currentArmor + amount, textArmor, (val) => currentArmor = val);
                break;
            case RewardType.RifleShard:
                AnimateValue(currentRifle, currentRifle + amount, textRifle, (val) => currentRifle = val);
                break;
        }
    }

    private void AnimateValue(int startValue, int endValue, TextMeshProUGUI textComp, System.Action<int> onUpdate)
    {
        if (textComp == null)
        {
            return;
        }

        // Stop any existing tweens on this text component to prevent conflicts
        DOTween.Kill(textComp);

        if (startValue == endValue)
        {
            onUpdate(endValue);
            textComp.text = Utils.FormatNumber(endValue);
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.localScale = Vector3.one;
            return;
        }

        Color originalColor = textComp.color;
        int displayedValue = startValue;

        // Count up from startValue to endValue over countDuration seconds
        DOTween.To(() => displayedValue, x => {
            displayedValue = x;
            onUpdate(x);
            textComp.text = Utils.FormatNumber(x);
        }, endValue, countDuration)
        .SetEase(Ease.OutQuad)
        .SetTarget(textComp)
        .OnPlay(() => {
            // Make the text pop when the animation starts
            textComp.color = Color.green;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one * 1.2f, 0.1f).SetTarget(textComp);
        })
        .OnComplete(() => {
            // Return the text to normal scale when the animation completes
            textComp.color = originalColor;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one, 0.1f).SetTarget(textComp);
        });
    }

    public void ResetInventory()
    {
        // Animate all values down to zero - bomb effect
        AnimateValueDown(currentGold, textGold, (val) => currentGold = val);
        AnimateValueDown(currentMoney, textMoney, (val) => currentMoney = val);
        AnimateValueDown(currentMelee, textMelee, (val) => currentMelee = val);
        AnimateValueDown(currentArmor, textArmor, (val) => currentArmor = val);
        AnimateValueDown(currentRifle, textRifle, (val) => currentRifle = val);
    }

    private void AnimateValueDown(int startValue, TextMeshProUGUI textComp, System.Action<int> onUpdate)
    {
        if (textComp == null)
        {
            return;
        }

        DOTween.Kill(textComp);

        if (startValue == 0)
        {
            onUpdate(0);
            textComp.text = Utils.FormatNumber(0);
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.localScale = Vector3.one;
            return;
        }

        int displayedValue = startValue;

        DOTween.To(() => displayedValue, x => {
            displayedValue = x;
            onUpdate(x);
            textComp.text = Utils.FormatNumber(x);
        }, 0, bombDuration)
        .SetEase(Ease.OutQuad)
        .SetTarget(textComp)
        .OnPlay(() => {
            // Make the text red and shrink slowly
            textComp.color = Color.red;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one * 0.8f, bombDuration).SetEase(Ease.InQuad).SetTarget(textComp);

        })
        .OnComplete(() => {
            // Return the text to normal color and scale when the animation completes
            textComp.color = Color.white;
            RectTransform animationTarget = Utils.ResolveUIAnimationTransform(textComp.transform);
            if (animationTarget != null)
                animationTarget.DOScale(Vector3.one, 0.1f).SetTarget(textComp);
        });
    }

    public SessionRewards ExitCurrentSession()
    {
        SessionRewards rewards = new SessionRewards
        {
            gold = currentGold,
            money = currentMoney,
            melee = currentMelee,
            armor = currentArmor,
            rifle = currentRifle
        };

        if (zoneManager != null)
        {
            zoneManager.ResetToStart();
        }

        // clear inventory for next session
        clearValues();

        return rewards;
    }

    private void clearValues()
    {
        currentGold = 0;
        currentMoney = 0;
        currentMelee = 0;
        currentArmor = 0;
        currentRifle = 0;

        textGold.text = Utils.FormatNumber(0);
        textMoney.text = Utils.FormatNumber(0);
        textMelee.text = Utils.FormatNumber(0);
        textArmor.text = Utils.FormatNumber(0);
        textRifle.text = Utils.FormatNumber(0);
    }
    
}