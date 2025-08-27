using System.Diagnostics.CodeAnalysis;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Parsec14.Pulling;

public abstract class SharedParsecPullingSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] protected readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] protected readonly SharedStunSystem _stun = default!;
    [Dependency] protected readonly StaminaSystem _stamina = default!;
    [Dependency] protected readonly DamageableSystem _damageable = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] protected readonly INetManager _net = default!;

    private readonly SoundSpecifier _pullSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };
    private readonly EntProtoId GrabEffect = "GrabPullingEffect";
    private readonly EntProtoId HoldEffect = "HoldPullingEffect";
    private readonly EntProtoId HurtEffect = "HurtPullingEffect";
    private readonly EntProtoId KillEffect = "KillPullingEffect";

    public override void Initialize()
    {
        SubscribeLocalEvent<PullerComponent, PullingStateChangedEvent>(OnPullingStateChanged);
        SubscribeLocalEvent<VirtualItemComponent, ActivateInWorldEvent>(OnVirtualItemInteractionAttempt);
    }


    private void OnPullingStateChanged(Entity<PullerComponent> puller, ref PullingStateChangedEvent args)
    {
        var pulled = args.Pullable;
        if (puller.Owner == pulled)
            return;

        var pulledComp = EnsureComp<PullableComponent>(pulled);

        switch (args.NewState)
        {
            case PullingState.None:
                pulledComp.BlockedBreathing = false;
                return;
            case PullingState.Grab:
                PlayPullEffect(puller, pulled, Color.Yellow, GrabEffect);
                pulledComp.BlockedBreathing = false;
                return;
            case PullingState.Hold:
                PlayPullEffect(puller, pulled, Color.LightBlue, HoldEffect);
                _stamina.TryTakeStamina(pulled, puller.Comp.StaminaDamage, source: puller, with: puller);
                pulledComp.BlockedBreathing = false;
                return;
            case PullingState.Hurt:
                PlayPullEffect(puller, pulled, Color.OrangeRed, HurtEffect);
                _stun.TryParalyze(pulled, puller.Comp.StunTime, true);
                pulledComp.BlockedBreathing = true;
                return;
            case PullingState.Kill:
                PlayPullEffect(puller, pulled, Color.DarkRed, KillEffect);
                _damageable.TryChangeDamage(pulled, puller.Comp.Damage, origin: puller);
                pulledComp.BlockedBreathing = true;
                return;
        }
    }

    private void OnVirtualItemInteractionAttempt(Entity<VirtualItemComponent> item, ref ActivateInWorldEvent args)
    {
        if (!TryComp(item.Comp.BlockingEntity, out PullableComponent? pullableComponent))
            return;

        if (!pullableComponent.Puller.HasValue)
            return;

        var pullerComponent = EnsureComp<PullerComponent>(pullableComponent.Puller.Value);
        if (_timing.CurTime < pullerComponent.NextPullAt)
            return;

        switch (pullableComponent.State)
        {
            case PullingState.None:
            case PullingState.Kill:
                return;
            case PullingState.Grab:
                TryChangePullingState(pullableComponent.Puller.Value, item.Comp.BlockingEntity, PullingState.Hold);
                return;
            case PullingState.Hold:
                TryChangePullingState(pullableComponent.Puller.Value, item.Comp.BlockingEntity, PullingState.Hurt);
                return;
            case PullingState.Hurt:
                TryChangePullingState(pullableComponent.Puller.Value, item.Comp.BlockingEntity, PullingState.Kill);
                return;
        }
    }

    public bool TryGetPullable(Entity<PullerComponent?> puller, [NotNullWhen(true)] out EntityUid? pullable)
    {
        pullable = default!;
        if (!Resolve(puller, ref puller.Comp))
            return false;

        pullable = puller.Comp.Pulling;
        return pullable.HasValue;
    }

    public bool TryGetPuller(Entity<PullableComponent?> pullable, [NotNullWhen(true)] out EntityUid? puller)
    {
        puller = default!;
        if(!Resolve(pullable, ref pullable.Comp))
            return false;

        puller = pullable.Comp.Puller;
        return puller.HasValue;
    }

    public void ChangePullingState(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, PullingState state)
    {
        puller.Comp.NextPullAt = _timing.CurTime + puller.Comp.NextPullCooldown;
        puller.Comp.State = state;
        var pullerEv = new PullingStateChangedEvent(puller, pullable, state);
        RaiseLocalEvent(puller, ref pullerEv);

        pullable.Comp.State = state;
        var pullableEv = new PullingStateChangedEvent(puller, pullable, state);
        RaiseLocalEvent(pullable, ref pullableEv);
    }

    public bool CanGrabInState(Entity<PullerComponent?> puller, Entity<PullableComponent?> pullable, PullingState state)
    {
        if (!Resolve(puller, ref puller.Comp))
            return false;

        if (!Resolve(pullable, ref pullable.Comp))
            return false;

        return puller.Comp.AllowedStates.Contains(state) &&
            pullable.Comp.AllowedStates.Contains(state);
    }

    public bool TryChangePullingState(Entity<PullerComponent?> puller, Entity<PullableComponent?> pullable, PullingState state)
    {
        if (puller.Owner == pullable.Owner)
            return false;

        if (!CanGrabInState(puller, pullable, state))
            return false;

        if (!Resolve(puller, ref puller.Comp))
            return false;

        if (!Resolve(pullable, ref pullable.Comp))
            return false;

        ChangePullingState((puller, puller.Comp), (pullable, pullable.Comp), state);

        return true;
    }

    public void PlayPullEffect(EntityUid puller, EntityUid pulled, Color color, EntProtoId effectId)
    {
        var userXform = Transform(puller);
        var targetPos = _transform.GetWorldPosition(pulled);
        var localPos = _transform.GetInvWorldMatrix(userXform).Transform(targetPos);
        localPos = userXform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(puller, puller, Angle.Zero, localPos, null);
        _audio.PlayPredicted(_pullSound, pulled, puller);

        if (_net.IsServer)
            SpawnAttachedTo(effectId, pulled.ToCoordinates());

        var filter = Filter.Pvs(pulled, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == puller);
        _colorFlash.RaiseEffect(color, new List<EntityUid> { pulled }, filter);
    }
}
