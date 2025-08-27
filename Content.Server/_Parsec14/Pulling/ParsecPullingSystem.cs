using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Parsec14.Pulling;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;

namespace Content.Server._Parsec14.Pulling;

public sealed class ParsecPullingSystem : SharedParsecPullingSystem
{
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var pulledQuery = EntityQueryEnumerator<PullableComponent, RespiratorComponent>();
        while (pulledQuery.MoveNext(out var pulled, out var pulledComp, out var respiratorComp))
        {
            if (!TryGetPuller(pulled, out var puller))
            {
                _respirator.ChangeBreathing((pulled, respiratorComp), true);
                continue;
            }

            switch (pulledComp.State)
            {
                case PullingState.None:
                case PullingState.Grab:
                case PullingState.Hold:
                    if (!pulledComp.BlockedBreathing)
                        _respirator.ChangeBreathing((pulled, respiratorComp), true);
                    return;
                case PullingState.Hurt:
                    _respirator.ChangeBreathing((pulled, respiratorComp), false);
                    return;
                case PullingState.Kill:
                    _respirator.ChangeBreathing((pulled, respiratorComp), false);
                    var damage = new DamageSpecifier
                    {
                        DamageDict = new Dictionary<string, FixedPoint2>
                        {
                            { "Asphyxiation", frameTime }
                        }
                    };
                    _damageable.TryChangeDamage(pulled, damage, origin: puller);
                    _stun.TryParalyze(pulled, TimeSpan.FromSeconds(frameTime), true);
                    return;
            }
        }
    }
}
