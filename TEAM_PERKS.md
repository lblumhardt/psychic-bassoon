# Team perks

After rounds 3, 6, and 9, the next shop presents three unowned perks. Choose one;
it stays active for the rest of the run. A finished run never opens another draft.
New runs clear perks, draft choices, shop offers, and shop locks.

## Add a configurable perk

In Unity's Project window, use **Create > Mons > Team Perks** and save the asset
anywhere under `Assets/Resources/Perks`. Set its display name, description, and
effect values. No scene wiring or list registration is needed.

- **Team Bonus**: flat defense, movement/attack speed multiplier, opening shield.
- **Overheal Shield**: overheal conversion and shield cap as a fraction of max HP.
- **Knockout Burst**: movement/attack speed multiplier and duration; activates on
  the player's first enemy knockout in each battle.
- **Isolation Defense**: bonus defense and the radius used to check living allies.

Update the description when changing values: descriptions are authored text.
Each asset is a distinct perk; don't duplicate an asset unless you want both
variants available in the pool. With six starting assets, all three scheduled
drafts have three choices. A smaller custom pool offers only what's available
and skips drafting if all perks are owned.

## Add a new behavior

Create a `TeamPerkSO` subclass with a `CreateAssetMenu` attribute and override
`CreateRuntime()` to return a new `PerkRuntime`. Follow `KnockoutBurstPerkSO.cs`
for an example with battle-local state. Never store timers or trigger flags on
the ScriptableObject: assets are shared, but runtimes are recreated each battle.

Available hooks:

- `OnBattleStart(BattlePerks)`: participants are initialized; apply opening effects.
- `OnKnockout(BattlePerks, victim, source)`: one call per death; source can be null.
- `OnOverheal(StatsComponent, excess)`: player recipients only; excess excludes HP restored.
- `DefenseBonus(CreatureController)`: evaluated at impact, added before mitigation.
- `SpeedMultiplier`: multiplies player movement speed and global attack cadence.
  Individual move cooldowns stay unchanged.

`BattlePerks` routes these hooks; additional perk classes using existing hooks
require no changes to the shop, draft, or combat dispatcher. Defense bonuses add;
speed multipliers multiply. Combat changes needing new events can add a hook
to `PerkRuntime` and route it from `BattlePerks` at the relevant combat event.

Shields absorb damage after defense and before health. The blue strip above each
health bar shows remaining shield. Damage/healing summary totals still measure
actual HP changes and exclude shields. All effects stop with the battle.

## Shop freezing

Freeze any individual creature or item offer for free. Its exact instance stays
through rerolls and subsequent rounds until purchased or unfrozen. Buying or
using an offer for an upgrade clears that slot's lock. Frozen creatures retain
their rolled stats, moves, and ability. Freeze flags do not change the round budget.
