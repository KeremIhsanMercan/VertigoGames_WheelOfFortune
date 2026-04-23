using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class ZoneManager : MonoBehaviour
{
    private class ZoneIndicatorView
    {
        public RectTransform root;
        public TextMeshProUGUI numberText;
        public Image backgroundImage;
    }

    [Header("UI References (Auto-Assigned)")]
    [SerializeField] private RectTransform zoneContainer; // layout_zone_track
    [SerializeField] private WheelManager wheelManager;
    
    [Header("Settings")]
    [SerializeField] private GameObject zoneIndicatorPrefab; // ui_image_zone_indicator prefab
    [SerializeField] private float stepHeight = 150f; // Height of each zone step + spacing in pixels
    [SerializeField] private float scrollDuration = 0.5f;

    private int currentZone = 1;
    private List<RectTransform> activeIndicators = new List<RectTransform>();
    private readonly Dictionary<RectTransform, ZoneIndicatorView> indicatorViews = new Dictionary<RectTransform, ZoneIndicatorView>();
    private Vector2 initialContainerPos;

    [SerializeField] private Sprite[] zoneIndicatorBackgrounds = new Sprite[3]; // 0: Bronze, 1: Silver, 2: Gold

    // References are automatically assigned with OnValidate
    private void OnValidate()
    {
        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform != null)
        {
            if (zoneContainer == null) 
                zoneContainer = canvasTransform.Find("Panel_Game/Panel_Right_Zones/layout_zone_track")?.GetComponent<RectTransform>();
        }

        if (wheelManager == null) wheelManager = FindObjectOfType<WheelManager>();
    }

    private void Start()
    {
        initialContainerPos = zoneContainer.anchoredPosition;
        InitializeZoneTrack();
        
        if (wheelManager != null)
            wheelManager.OnSpinComplete += HandleSpinComplete;
    }

    private void InitializeZoneTrack()
    {
        // Create initial 10 indicators (5 to show, 5 substitute) and keep them in the pool
        for (int i = 0; i < 10; i++)
        {
            CreateNewIndicator(i + 1);
        }
    }

    private void HandleSpinComplete(SliceData reward)
    {
        // Next zone if no bomb
        if (reward.type != RewardType.Bomb)
        {
            NextZone();
        }
    }

    public void NextZone()
    {
        currentZone++;
        
        // Scroll the container up by one step height
        zoneContainer.DOAnchorPosY(zoneContainer.anchoredPosition.y + stepHeight, scrollDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                // Reuse the top indicator for the new zone at the bottom (pooling)
                ReuseTopIndicator();

                // Move the container back to initial position to create a looping effect
                zoneContainer.anchoredPosition = new Vector2(
                    zoneContainer.anchoredPosition.x, 
                    zoneContainer.anchoredPosition.y - stepHeight
                );
            });

        // Update the wheel type based on the new zone
        UpdateWheelByZone();
    }

    private void UpdateWheelByZone()
    {
        SpinType nextType = SpinType.Bronze;

        if (currentZone % 30 == 0) nextType = SpinType.Gold; // every 30th zone is Super Zone
        else if (currentZone % 5 == 0) nextType = SpinType.Silver; // every 5th zone is Safe Zone

        wheelManager.GenerateWheel(nextType, true);
    }

    private void CreateNewIndicator(int zoneNum)
    {
        GameObject go = Instantiate(zoneIndicatorPrefab, zoneContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
            CacheIndicatorView(rt);

        UpdateIndicatorUI(rt, zoneNum);
        activeIndicators.Add(rt);
    }

    private void ReuseTopIndicator()
    {
        // Get the top indicator (the one that just scrolled out of view)
        RectTransform topItem = activeIndicators[0];
        activeIndicators.RemoveAt(0);
        
        // Move it to the bottom of the list and reposition it at the bottom of the track
        topItem.SetAsLastSibling();
        activeIndicators.Add(topItem);
        
        // Update its zone number and UI for the new zone it represents
        int nextZoneNum = currentZone + activeIndicators.Count - 1; 
        UpdateIndicatorUI(topItem, nextZoneNum);
    }

    private void UpdateIndicatorUI(RectTransform rt, int zoneNum)
    {
        if (rt == null)
            return;

        if (!indicatorViews.TryGetValue(rt, out ZoneIndicatorView view) || view == null)
            view = CacheIndicatorView(rt);

        if (view == null)
            return;

        // Set number text
        if (view.numberText != null)
            view.numberText.text = zoneNum.ToString();
        
        // Set text color based on zone type
        if (view.numberText != null)
        {
            if (zoneNum % 30 == 0) view.numberText.color = Color.yellow; // Super Zone (Gold)
            else if (zoneNum % 5 == 0) view.numberText.color = Color.green; // Safe Zone (Silver)
            else view.numberText.color = Color.white; // Normal Zone (Bronze)
        }

        // Set background image based on zone type
        if (view.backgroundImage != null)
        {
            if (zoneNum % 30 == 0) view.backgroundImage.sprite = zoneIndicatorBackgrounds[2]; // Super Zone (Gold)
            else if (zoneNum % 5 == 0) view.backgroundImage.sprite = zoneIndicatorBackgrounds[1]; // Safe Zone (Silver)
            else view.backgroundImage.sprite = zoneIndicatorBackgrounds[0]; // Normal Zone (Bronze)
        }
    }

    private ZoneIndicatorView CacheIndicatorView(RectTransform rt)
    {
        if (rt == null)
            return null;

        ZoneIndicatorView view = new ZoneIndicatorView
        {
            root = rt,
            numberText = rt.GetComponentInChildren<TextMeshProUGUI>(true),
            backgroundImage = rt.GetComponent<Image>()
        };

        indicatorViews[rt] = view;
        return view;
    }

    public void ResetToStart()
    {
        currentZone = 1;
        zoneContainer.anchoredPosition = initialContainerPos;
        
        // Reset all indicators in the pool
        for (int i = 0; i < activeIndicators.Count; i++)
        {
            UpdateIndicatorUI(activeIndicators[i], i + 1);
            activeIndicators[i].SetSiblingIndex(i);
        }
        
        wheelManager.GenerateWheel(SpinType.Bronze, true);
    }
}