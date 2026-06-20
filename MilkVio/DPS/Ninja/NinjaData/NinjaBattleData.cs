namespace MilkVio.DPS.Ninja.NinjaData;
/// <summary>
/// 存储当前ACR在一场战斗中的临时数据。
/// 会在战斗结束后被重置。
/// </summary>
public class NinjaBattleData
{
    // 使用单例模式，方便在项目各处访问
    public static NinjaBattleData Instance { get; set; } = new();
    
    public bool Is60 { get; set; } = false;
    public bool Is120 { get; set; } = false;

    /// <summary>
    /// 重置所有战斗相关数据
    /// </summary>
    public void Reset()
    {
        Is60 = false;
        Is120 = false;
    }
}
