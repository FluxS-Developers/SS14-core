namespace Content.Shared._Parsec14.Pulling;

[ByRefEvent]
public readonly record struct PullingStateChangedEvent(EntityUid Puller, EntityUid Pullable, PullingState NewState);
