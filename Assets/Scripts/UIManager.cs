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

            if (buttonLeave == null)
            {
                Transform leaveBtnTransform = canvasTransform.Find("Panel_Game/Panel_Left_Rewards/ui_button_leave");
                if (leaveBtnTransform != null)
                {
                    buttonLeave = leaveBtnTransform.GetComponent<Button>();
                }
            }
        }
    }

    // Do not use unity OnClick from Editor
    private void Start()
    {
        // Assign button click listeners in code to ensure they are always set, even if there is no reference in the editor
        if (buttonPlay != null)
        {
            buttonPlay.onClick.AddListener(StartGame);
        }
        
        if (buttonLeave != null)
        {
            buttonLeave.onClick.AddListener(BackToMainMenu);
        }

        // Start with main menu shown
        ShowMainMenu();
    }

    private void StartGame()
    {
        panelMainMenu.SetActive(false);
        panelGame.SetActive(true);
    }

    public void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelGame.SetActive(false);
    }

    private void BackToMainMenu()
    {
        // REWARD ASSIGN LOGIC WILL BE ADDED HERE IN THE FUTURE
        ShowMainMenu();
    }
}