using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelGame;

    [Header("Buttons")]
    [SerializeField] private Button buttonPlay;
    [SerializeField] private Button buttonLeave;
    [SerializeField] private RectTransform buttonPlayAnimationRoot;
    [SerializeField] private RectTransform buttonLeaveAnimationRoot;

    [SerializeField] private PersistenceManager persistenceManager;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private AudioManager audioManager;

    private IAudioService audioService;
    

    // Button references should be automatically set from OnValidate
    private void OnValidate()
    {
        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;

        if (canvasTransform != null) {
            if (panelMainMenu == null)
            {
                Transform mainMenuPanelTransform = canvasTransform.Find("Panel_MainMenu");
                if (mainMenuPanelTransform != null)
                {
                    panelMainMenu = mainMenuPanelTransform.gameObject;
                }
            }

            if (panelGame == null)
            {
                Transform gamePanelTransform = canvasTransform.Find("Panel_Game");
                if (gamePanelTransform != null)
                {
                    panelGame = gamePanelTransform.gameObject;
                }
            }

            if (buttonPlay == null)
            {
                Transform playBtnTransform = canvasTransform.Find("Panel_MainMenu/Panel_Play_Button/ui_button_play");
                if (playBtnTransform != null)
                {
                    buttonPlay = playBtnTransform.GetComponent<Button>();
                }
            }

            if (buttonPlay != null)
            {
                // Always normalize to button root so all visuals (including border) animate together.
                buttonPlayAnimationRoot = buttonPlay.transform as RectTransform;
            }

            if (buttonLeave == null)
            {
                Transform leaveBtnTransform = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/ui_button_leave");
                if (leaveBtnTransform != null)
                {
                    buttonLeave = leaveBtnTransform.GetComponent<Button>();
                }
            }

            if (buttonLeave != null)
            {
                // Always normalize to button root so all visuals (including border) animate together.
                buttonLeaveAnimationRoot = buttonLeave.transform as RectTransform;
            }
        }

        if (persistenceManager == null) persistenceManager = FindObjectOfType<PersistenceManager>();
        if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
        if (audioManager == null) audioManager = FindObjectOfType<AudioManager>();
    }

    // Do not use unity OnClick from Editor
    private void Start()
    {
        ResolveDependencies();

        // Assign button click listeners in code to ensure they are always set, even if there is no reference in the editor
        if (buttonPlay != null)
        {
            buttonPlay.onClick.AddListener(OnPlayClicked);
        }
        
        if (buttonLeave != null)
        {
            buttonLeave.onClick.AddListener(OnLeaveClicked);
        }

        // Start with main menu shown
        ShowMainMenu();
    }

    private void StartGame()
    {
        panelMainMenu.SetActive(false);
        panelGame.SetActive(true);
    }

    private void OnPlayClicked()
    {
        audioService?.PlayButtonClick();
        PlayClickAnimation(buttonPlay, StartGame);
    }

    private void OnLeaveClicked()
    {
        audioService?.PlayButtonClick();
        PlayClickAnimation(buttonLeave, BackToMainMenu);
    }

    private void PlayClickAnimation(Button button, System.Action onComplete)
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

    public void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelGame.SetActive(false);
    }

    private void BackToMainMenu()
    {
        // Get rewards from InventoryManager and save them using PersistenceManager before going back to main menu
        var sessionRewards = inventoryManager.ExitCurrentSession();
        persistenceManager.SaveSessionRewards(sessionRewards.gold, sessionRewards.money, sessionRewards.melee, sessionRewards.armor, sessionRewards.rifle);
        ShowMainMenu();
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
}