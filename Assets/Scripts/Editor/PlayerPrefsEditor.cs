using UnityEngine;
using UnityEditor;

public class PlayerPrefsEditor : EditorWindow
{
    private int currentGold;
    private int currentMoney;
    private int currentMelee;
    private int currentArmor;
    private int currentRifle;

    // Adding menu item
    [MenuItem("Vertigo Case/PlayerPrefs Editor")]
    public static void ShowWindow()
    {
        GetWindow<PlayerPrefsEditor>("PlayerPrefs Editor");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        currentGold = PlayerPrefs.GetInt("TotalGold", 0);
        currentMoney = PlayerPrefs.GetInt("TotalMoney", 0);
        currentMelee = PlayerPrefs.GetInt("TotalMelee", 0);
        currentArmor = PlayerPrefs.GetInt("TotalArmor", 0);
        currentRifle = PlayerPrefs.GetInt("TotalRifle", 0);
    }

    private void OnGUI()
    {
        GUILayout.Label("PlayerPrefs", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        currentGold = EditorGUILayout.IntField("Total Gold", currentGold);
        currentMoney = EditorGUILayout.IntField("Total Money", currentMoney);
        currentMelee = EditorGUILayout.IntField("Total Melee", currentMelee);
        currentArmor = EditorGUILayout.IntField("Total Armor", currentArmor);
        currentRifle = EditorGUILayout.IntField("Total Rifle", currentRifle);

        EditorGUILayout.Space();

        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            PlayerPrefs.SetInt("TotalGold", currentGold);
            PlayerPrefs.SetInt("TotalMoney", currentMoney);
            PlayerPrefs.SetInt("TotalMelee", currentMelee);
            PlayerPrefs.SetInt("TotalArmor", currentArmor);
            PlayerPrefs.SetInt("TotalRifle", currentRifle);
            PlayerPrefs.Save();
            
            Debug.Log("<color=green>PlayerPrefs updated!</color>");
        }

        EditorGUILayout.Space();

        // Reset button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Reset All", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Sure?", "Sure?", "Yep", "Nope"))
            {
                PlayerPrefs.DeleteAll();
                LoadData(); // Reload data to update the fields after reset
                Debug.LogWarning("All PlayerPrefs data reset!");
            }
        }
        GUI.backgroundColor = Color.white; // Reset to default color
    }
}