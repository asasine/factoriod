namespace Factoriod.Models.Game;

public record UnitGroupMapSettings(
    uint MinGroupGatheringTime = 3600,
    uint MaxGroupGatheringTime = 36000,
    uint MaxWaitTimeForLateMembers = 7200,
    double MinGroupRadius = 5.0,
    double MaxGroupRadius = 30.0,
    double MaxMemberSpeedupWhenBehind = 1.4,
    double MaxMemberSlowdownWhenAhead = 0.6,
    double MaxGroupSlowdownFactor = 0.3,
    double MaxGroupMemberFallbackFactor = 3.0,
    double MemberDisownDistance = 10.0,
    uint TickToleranceWhenMemberArrives = 60,
    uint MaxGatheringUnitGroups = 30,
    uint MaxUnitGroupSize = 200
);
