# Monetization, Analytics & Launch

## 1. Model

Hybrid-casual: **rewarded video is the primary revenue line**, IAP is secondary
and mostly converts your top ~2% of players. Expect roughly 70/30 ads/IAP at
launch.

The design rule that makes this work: **every ad is a player-initiated
upgrade, never an interruption.** Forced interstitials do most of the damage
to D1 retention in this genre. Keep them rare and gated.

## 2. Ad placements

| Placement | Type | Trigger | Value to player |
|---|---|---|---|
| **Take all 3** | Rewarded | Level-up draft screen | All three cards instead of one. *The highest-engagement placement in the game* |
| **Revive** | Rewarded | On death, once per run | Continue with 50% HP |
| **Double loot** | Rewarded | Run-end summary | 2x coins and scrap |
| **Vault boost** | Rewarded | Offline collect screen | 2x offline income |
| **Free chest** | Rewarded | Meta screen, 30 min cooldown | One gear chest |
| **Reroll** | Rewarded | Draft screen | New set of three cards |
| Between runs | Interstitial | Every 3rd run end, min 90 s apart | — |

Cap rewarded views at ~20/day per user. Always show a fallback if no ad
fills — never leave a dead button.

## 3. IAP catalogue

| Product | Price | Notes |
|---|---|---|
| Remove Ads | $4.99 | Removes interstitials only; rewarded stays opt-in. Often the top seller |
| Starter Pack | $2.99 | One-time, 24 h after install, high perceived value |
| Gem packs | $0.99 – $99.99 | Five tiers |
| Battle Pass | $9.99 | Seasonal, 30 days |
| VIP subscription | $7.99/mo | Idle multiplier, daily gems, no interstitials |

Ship Remove Ads, Starter Pack, and gem packs for v1. Battle pass and
subscription come after you know the game retains.

## 4. Analytics — instrument before soft launch

You cannot tune an economy on intuition. Minimum event set:

```
session_start / session_end
run_start        { stage, playerPower, runNumber }
run_end          { stage, result, duration, waveReached, coinsEarned }
level_up         { runLevel, timeIntoRun }
ability_picked   { abilityId, level, wasAdBoosted }
ad_shown         { placement, result }
iap_purchase     { productId, price }
gear_equipped / gear_upgraded  { gearId, newLevel }
room_upgraded    { roomId, newLevel, coinsSpent }
currency_sink    { currency, amount, reason }
tutorial_step    { stepIndex }
```

Track **currency sources and sinks separately**. The most common failure in
this genre is coin inflation making the base upgrades trivial by chapter 3 —
you only see it in the source/sink ratio.

## 5. Targets

| Metric | Minimum viable | Good |
|---|---|---|
| D1 retention | 35% | 45% |
| D7 retention | 12% | 18% |
| D30 retention | 4% | 7% |
| Session length | 8 min | 12 min |
| Sessions/day | 3 | 5 |
| ARPDAU | $0.05 | $0.15 |
| Tutorial completion | 85% | 92% |

If D1 is under 30% after soft-launch tuning, the core loop is the problem —
more content will not fix it.

## 6. Play Store release checklist

**Account & policy**
- [ ] Google Play Developer account ($25 one-time)
- [ ] For personal accounts: 12 testers running a closed test for 14
      consecutive days before production access is granted. **Plan this into
      the schedule — it is a hard two-week gate.**
- [ ] Privacy policy hosted at a public URL (required — ad SDKs collect data)
- [ ] Data safety form filled in accurately, matching what your SDKs do
- [ ] Content rating questionnaire (IARC)
- [ ] Ads declaration, target audience declaration

**Build**
- [ ] Android App Bundle (`.aab`), IL2CPP, ARM64 + ARMv7
- [ ] `targetSdkVersion` at Google's current requirement
- [ ] Upload keystore backed up somewhere you will not lose it
- [ ] Play App Signing enabled
- [ ] Build under ~150 MB; move the rest to Addressables/Play Asset Delivery

**Store listing**
- [ ] Icon 512×512, feature graphic 1024×500
- [ ] 5-8 phone screenshots showing gameplay, not menus
- [ ] 30 s preview video, gameplay in the first 3 seconds
- [ ] Title with one keyword, short description doing the selling
- [ ] Run store-listing experiments on the icon — 20%+ CVR swings are normal

**Compliance**
- [ ] GDPR/CCPA consent (via the mediation SDK's CMP)
- [ ] Families policy compliance if targeting under-13 (it changes which ad
      networks you may use — decide early)
- [ ] iOS: ATT prompt if you also ship there

## 7. Legal note on cloning

Game **mechanics, systems, and genre conventions are not protected** — the
survivors-like loop, ability drafting, and idle base meta are all fair to
build. What you must not copy: art assets, character designs, UI layouts,
icons, music, specific names, and store creatives. Do not use the name
"Endless Puncher", do not reuse their icon composition, and write your own
copy. Build the same *kind* of game, not the same game.
