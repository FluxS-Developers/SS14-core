using Content.Shared._Parsec14.Pulling; // WD edit
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being able to pull another entity with <see cref="PullableComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PullingSystem), typeof(SharedParsecPullingSystem))] // WD edit
public sealed partial class PullerComponent : Component
{
    // My raiding guild
    /// <summary>
    /// Next time the puller can throw what is being pulled.
    /// Used to avoid spamming it for infinite spin + velocity.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, Access(Other = AccessPermissions.ReadWriteExecute)]
    public TimeSpan NextThrow;

    [DataField]
    public TimeSpan ThrowCooldown = TimeSpan.FromSeconds(1);

    // Before changing how this is updated, please see SharedPullerSystem.RefreshMovementSpeed
    public float WalkSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    public float SprintSpeedModifier => Pulling == default ? 1.0f : 0.95f;

    /// <summary>
    /// Entity currently being pulled if applicable.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Pulling;

    /// <summary>
    ///     Does this entity need hands to be able to pull something?
    /// </summary>
    [DataField]
    public bool NeedsHands = true;

    // WD edit start
    /// <summary>
    /// what types of capture are available to the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<PullingState> AllowedStates = new()
    {
        PullingState.None,
        PullingState.Grab,
        PullingState.Hold,
        PullingState.Hurt,
        PullingState.Kill,
    };

    /// <summary>
    /// what type of capture does the entity have now
    /// </summary>
    [DataField, AutoNetworkedField]
    public PullingState State;

    /// <summary>
    /// delay before changing capture type
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextPullCooldown = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// time from which can you change the grip
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextPullAt;

    /// <summary>
    /// stun time in Hurt state
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// stamina damage in Hold state
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StaminaDamage = 30f;

    /// <summary>
    /// damage in Kill state
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new DamageSpecifier
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Asphyxiation", 10 },
            { "Blunt", 5 }
        }
    };
    // WD edit end
}
