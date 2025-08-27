using Content.Shared._Parsec14.Pulling; // WD edit
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Pulling.Components;

/// <summary>
/// Specifies an entity as being pullable by an entity with <see cref="PullerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(Systems.PullingSystem), typeof(SharedParsecPullingSystem))] // WD edit
public sealed partial class PullableComponent : Component
{
    /// <summary>
    /// The current entity pulling this component.
    /// </summary>
    [AutoNetworkedField, DataField]
    public EntityUid? Puller;

    /// <summary>
    /// The pull joint.
    /// </summary>
    [AutoNetworkedField, DataField]
    public string? PullJointId;

    public bool BeingPulled => Puller != null;

    /// <summary>
    /// If the physics component has FixedRotation should we keep it upon being pulled
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [ViewVariables(VVAccess.ReadWrite), DataField("fixedRotation")]
    public bool FixedRotationOnPull;

    /// <summary>
    /// What the pullable's fixedrotation was set to before being pulled.
    /// </summary>
    [Access(typeof(Systems.PullingSystem), Other = AccessPermissions.ReadExecute)]
    [AutoNetworkedField, DataField]
    public bool PrevFixedRotation;

    // WD edit start
    /// <summary>
    /// what types of captures can an entity be in
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<PullingState> AllowedStates = new()
    {
        PullingState.None,
        PullingState.Grab,
    };

    /// <summary>
    /// what type of capture is the entity currently in
    /// </summary>
    [DataField, AutoNetworkedField]
    public PullingState State;

    /// <summary>
    /// is breathing blocked
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockedBreathing = false;
    // WD edit end
}
