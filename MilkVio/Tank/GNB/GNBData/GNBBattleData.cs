namespace MilkVio.Tank.GNB.GNBData;
/// <summary>
/// 存储当前ACR在一场战斗中的临时数据。
/// 会在战斗结束后被重置。
/// </summary>
public class GNBBattleData
{
    // 使用单例模式，方便在项目各处访问
    public static GNBBattleData Instance { get; set; } = new();
    
}
