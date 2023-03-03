namespace NRegEx;

public enum GroupType : int
{
    NotGroup = -1,
    NormalGroup = 0,
    NotCaptiveGroup = 1,
    AtomicGroup = 2,
    ForwardPositiveGroup = 3,
    ForwardNegativeGroup = 4,
    BackwardPositiveGroup = 5,
    BackwardNegativeGroup = 6,

    NamedBackReferenceCondition = 7,
    IndexedBackReferenceCondition = 8,

    LookAroundConditionGroup = 9,
    NamedBackReferenceConditionGroup = 10,
    IndexedBackReferenceConditionGroup = 11,
}
