using UnityEngine;

public class PlayerPrefsDataStore : IPlayerDataStore
{
    public int GetTotalGold() => PlayerPrefs.GetInt("TotalGold", 0);

    public int GetTotalMoney() => PlayerPrefs.GetInt("TotalMoney", 0);

    public int GetTotalMelee() => PlayerPrefs.GetInt("TotalMelee", 0);

    public int GetTotalArmor() => PlayerPrefs.GetInt("TotalArmor", 0);

    public int GetTotalRifle() => PlayerPrefs.GetInt("TotalRifle", 0);

    public void SetTotals(int gold, int money, int melee, int armor, int rifle)
    {
        PlayerPrefs.SetInt("TotalGold", gold);
        PlayerPrefs.SetInt("TotalMoney", money);
        PlayerPrefs.SetInt("TotalMelee", melee);
        PlayerPrefs.SetInt("TotalArmor", armor);
        PlayerPrefs.SetInt("TotalRifle", rifle);
    }

    public void Save() => PlayerPrefs.Save();
}