using MilkVio.Healer.Common;
using PromeRotation.Data;
using PromeRotation.Managers;
using PromeRotation.Resolvers;

namespace MilkVio.Healer.SGE.Resolvers.Gcd;

public sealed class 均衡注药Gcd : IDecisionResolver
{
    private bool _pendingEukrasianDosis;
    private DateTime _pendingSince = DateTime.MinValue;

    private static readonly IReadOnlyList<uint> ActionList =
    [
        SgeAction.均衡,
        SgeAction.注药,
    ];

    public CheckResult Check()
    {
        var targetCheck = HealerUtils.RequireEnemyTarget(25f, 400);
        if (!targetCheck.Success) return targetCheck;
        if (HealerUtils.Level < 30) return HealerUtils.Fail("等级不足");

        if (HealerUtils.SgeEukrasia())
            return HealerUtils.Pass("已有均衡，打均衡注药");

        var shouldRefresh = ShouldRefreshEukrasianDosis();

        if (_pendingEukrasianDosis)
        {
            if (!shouldRefresh)
            {
                _pendingEukrasianDosis = false;
                return HealerUtils.Fail("DOT已刷新");
            }

            if ((DateTime.UtcNow - _pendingSince).TotalSeconds > 5)
            {
                _pendingEukrasianDosis = false;
                return HealerUtils.Fail("均衡BUFF等待超时，重置均衡注药队列");
            }

            return HealerUtils.Pass("均衡注药队列：等待均衡BUFF，继续打均衡");
        }

        if (!shouldRefresh) return HealerUtils.Fail("DOT时间充足");

        return HealerUtils.Pass("均衡注药队列：均衡 -> 均衡注药");
    }

    public PAction GetAction()
    {
        if (HealerUtils.SgeEukrasia())
        {
            _pendingEukrasianDosis = false;
            // 均衡注药由“均衡状态下的注药”替换发动；直接提交均衡注药会被游戏判定条件不足。
            return HealerUtils.Gcd(ActionList[1], ActionTargetType.Target);
        }

        _pendingEukrasianDosis = true;
        if (_pendingSince == DateTime.MinValue || (DateTime.UtcNow - _pendingSince).TotalSeconds > 5)
            _pendingSince = DateTime.UtcNow;

        return HealerUtils.Gcd(ActionList[0], ActionTargetType.Self);
    }

    private static bool ShouldRefreshEukrasianDosis()
    {
        var refreshAt = ShouldRefreshEarlyWhileMoving() ? 10f : 2.5f;
        return HealerUtils.ShouldRefreshOwnTargetStatusBelow(
            refreshAt,
            SgeBuff.均衡注药,
            SgeBuff.均衡注药II,
            SgeBuff.均衡注药III,
            SgeBuff.均衡注药III新版);
    }

    private static bool ShouldRefreshEarlyWhileMoving()
        => MoveManager.IsLocalPlayerMoving
           && HealerUtils.SgeAddersting() == 0
           && !HealerUtils.HasCharge(SgeAction.发炎, 26);
}
