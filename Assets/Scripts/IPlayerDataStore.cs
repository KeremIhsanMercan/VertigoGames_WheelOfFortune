public interface IPlayerDataStore
{
    int GetTotalGold();
    int GetTotalMoney();
    int GetTotalMelee();
    int GetTotalArmor();
    int GetTotalRifle();
    void SetTotals(int gold, int money, int melee, int armor, int rifle);
    void Save();
}