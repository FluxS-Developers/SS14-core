using Robust.Shared.Serialization;

namespace Content.Shared._Parsec14.Pulling;

[Serializable, NetSerializable]
public enum PullingState
{
    /// <summary>
    /// nothing holds in its grip
    /// </summary>
    None,

    /// <summary>
    /// normal grip
    /// </summary>
    Grab,

    /// <summary>
    /// disarming capture
    /// </summary>
    Hold,

    /// <summary>
    /// seizure with intent to harm
    /// </summary>
    Hurt,

    /// <summary>
    /// chokehold
    /// </summary>
    Kill,
}
