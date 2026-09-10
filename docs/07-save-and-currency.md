# Save & Currency (P2)

Runs now bank what they earn, and the profile survives closing the app.

## The rule that matters

**Adding a field to `SaveData` is free. Renaming, removing, or retyping one is
a migration.**

`SaveMigrations` exists on day one, before there is anything to migrate, on
purpose: the first time a shipped save format changes without a migration path,
every existing player's profile is wiped, and there is no undo after the build
is live.

To add a version:
1. Bump `SaveMigrations.CurrentVersion`.
2. Add a `case` for the version you are migrating **from**.
3. **Never edit an existing step.** Players arrive from every version you have
   ever shipped, not just the last one.

## Shape

```
SaveService   static. Load / Save / Delete. Atomic writes, one backup,
              checksummed. Not a MonoBehaviour — the profile outlives scenes
SaveHooks     MonoBehaviour on Systems. Saves on pause, focus loss, quit,
              and a 60 s backstop timer
SaveData      the whole profile, one flat [Serializable] class
CurrencyService  static. The ONLY thing allowed to change a balance
```

### Why these choices

**Atomic writes with a backup.** Write to `.tmp`, then swap. A process killed
partway through leaves the previous profile intact rather than a half-written
one. `File.Replace` is the atomic path; it is not implemented on every runtime
Unity targets, so there is a copy-then-move fallback.

**`OnApplicationPause` is the important hook**, not `OnApplicationQuit`. Android
and iOS can kill a backgrounded app without ever calling quit, so a game that
only saves on quit loses the session roughly whenever the OS feels like it.

**Saving happens the instant a run ends**, not on the autosave timer. The end
screen is exactly where players close the app, and a reward that is not on disk
by then never happened as far as the player is concerned.

**Currencies are `long`.** An idle economy with offline income crosses 2.1
billion sooner than seems possible, and an overflowed balance going negative is
unrecoverable. Earning saturates at the ceiling rather than wrapping.

**The checksum is not security.** A client-side save can always be edited by
someone determined. It makes casual edits *detectable* so a tampered profile can
be logged. A mismatch still loads — refusing would destroy real progress every
time a hashing detail changes, which costs far more than the cheating it
prevents.

**Everything goes through `Earn`/`Spend` with a `reason` string.** That tag is
the analytics source at M4, and tracking sources against sinks is the only way
to catch coin inflation before it makes base upgrades trivial (docs/04 §4).

## Editor tools

`ScalePunch ▸ Save ▸ …`

| Item | Use |
|---|---|
| Log Profile | Dump balances, run counts and the file path |
| Reveal Profile Folder | Open `persistentDataPath` |
| Delete Profile | Wipe. **Use this constantly while balancing** |
| Grant 10,000 Coins | Test a price without playing for it |

Balancing an economy means starting from zero repeatedly. Without the wipe,
every run after the first starts from the wrong place and you draw the wrong
conclusion from it.

## What P2 deliberately does not do

- **Coins have no sink yet.** They accumulate with nothing to spend them on
  until the base rooms land at P6. That is the correct order — the plumbing has
  to exist before there is anything to plumb — but do not tune `coinsOnClear`
  seriously until there is something to buy.
- **No cloud save.** Device loss is a top-3 one-star cause in this genre and
  this must ship before launch, but it needs Google Play Games / Firebase and
  belongs with the rest of the SDK work at M4.
- **No gem or scrap sources.** Both currencies exist end to end; nothing grants
  them yet.

## Payout

```
coins = (victory ? coinsOnClear : coinsOnClear * failPayoutFraction)
      + kills * coinsPerKill
```

Set on `Stage_01`. `failPayoutFraction` defaults to 0.35 — never zero. Four
minutes of effort rewarded with nothing is how a player is lost permanently.

Turn off `RunController.bankRewards` when balance-testing so throwaway runs do
not inflate a real profile.
