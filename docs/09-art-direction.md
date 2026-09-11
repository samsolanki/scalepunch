# Art Direction

**Recommendation: flat-shaded stylized low-poly 3D, bright saturated characters
on a dark desaturated environment.**

That is what *Endless Puncher*, *Archero* and most of this genre use, and it is
the only style that satisfies every constraint below at once.

---

## 1. The constraints that actually decide this

Style in this project is not a taste question. Five things narrow it to one
answer:

| Constraint | What it rules out |
|---|---|
| **150 zombies on screen** | Per-character detail, normal maps, per-enemy materials, realtime shadows. Everything must GPU-instance off one material |
| **Top-down 3/4 camera** | Faces, fine texture work, anything read below ~40 px on a phone |
| **No artist on the team** | Anything the Asset Store does not already sell in consistent packs |
| **Ad creatives decide UA** | Anything that does not read in a 6-second video at thumbnail size |
| **Threat ID is gameplay** | Any palette where a Brute and a Shambler look similar in a crowd |

The 150-enemy budget is the dominant one. It is also the one people discover
last, usually after commissioning art that cannot ship.

## 2. What is trending (2025-26)

- Pure hyper-casual minimalism is slowing, but its visual DNA persists in
  hybrid-casual **executed at higher production quality** — same simple forms,
  far better animation, VFX and UI. The phrase going around is the
  "premium-ization of hyper-casual".
- **Dopamine brights**: neon yellows, electric blues, high-contrast pairings.
  These read well as TikTok thumbnails, which is where discovery happens.
- Top-grossing mobile games increasingly **do not look designed for mobile** —
  they look designed for a good screen and then made to work on a phone.
- Stylised 3D remains the default for the survivors-like / arena-shooter family.
  *Survivor.io* is the outlier with a flatter arcade look, and it works because
  Habby commit to it completely.

**What this means for us:** keep the forms simple, and spend the entire polish
budget on animation, VFX and UI rather than on model detail. That is both the
trend and what our frame budget forces anyway.

## 3. Characters

**Flat-shaded low-poly, chunky proportions, one material, no textures.**

- Roughly **1:3 head-to-body**. Reads at small size and is what the Asset Store
  sells most of.
- **No faces.** At this camera they are invisible. Do not pay for them.
- **300-800 triangles** per zombie. Vertex colours or a single shared 256px
  colour atlas — never per-enemy textures.
- One material for every ordinary zombie, tinted per tier via
  `MaterialPropertyBlock` (already how `HitFlash` works).

### Silhouette before colour

A player must identify a threat in a crowd of 150, at a glance, while shooting.
Colour alone fails the moment two tiers overlap:

| Tier | Silhouette | Colour |
|---|---|---|
| Shambler | Baseline, upright, slouched | Desaturated green |
| Runner | Small, thin, leaning forward | Bright yellow |
| Brute | Wide, hunched, long arms | Heavy red-brown |
| Spitter | Tall, thin, distended torso | Acid purple |
| Boss | **2.2× scale**, distinct shape entirely | Near-black with an emissive rim |

Scale is already driven by `EnemyDefinition.scale` and tint by
`EnemyDefinition.tint`, so tiers are authorable today without new art.

## 4. Environment

**Dark, low-contrast, low-saturation. It exists to make the characters pop.**

The reference screenshot does exactly this — a near-black floor with dim
scattered props. Heightening contrast between character and environment is the
single most effective clarity fix in this genre.

- Ground: dark, near-flat, subtle tiling texture at most.
- Props: silhouetted, unlit or barely lit, no more than 2-3 repeated meshes.
  They are set dressing for the ad creative, not scenery to explore.
- **The engagement ring is the brightest thing on the ground.** It is the entire
  read of the game; nothing may compete with it.
- Never put a bright or busy floor under a 150-enemy crowd. It is the most
  common way this genre becomes unreadable.

## 5. Lighting and render setup

- **One directional light.** URP mobile renderer, forward.
- **No realtime shadows on zombies.** A blob-shadow quad or nothing at all.
  150 shadow casters is not affordable and buys nothing at this camera.
- **Bloom only**, tuned low, and only the ring, tracers, muzzle flash and VFX
  should reach the threshold.
- No post beyond bloom and a light vignette.
- Enable **GPU instancing** on the shared zombie material. Without it the
  150-enemy budget is not reachable.

## 6. Where the polish budget actually goes

In this genre, perceived quality comes from motion and effects, not models.
In rough order of return per hour:

1. **VFX** — muzzle flash, tracers, impact pops, death bursts, the ring pulse.
2. **Animation** — a good walk cycle and a death sell more than any amount of
   model detail. Three walk variants beats thirty unique zombies.
3. **UI** — draft cards, HUD, the end screen. Players stare at these.
4. **Character models** — last. Silhouette and colour do the work.

## 7. Alternatives considered

| Style | Verdict |
|---|---|
| **Stylised low-poly 3D** | **Chosen.** Cheap, instanceable, Asset-Store-rich, reads at thumbnail size, matches the genre's UA language |
| 2D sprites (*Survivor.io*) | Genuinely works, and cheapest of all — but 150 animated sprites needs disciplined atlasing, and consistent 2D packs are harder to buy than 3D ones. Viable if you find one artist and commit |
| Pixel art | Trending as a deliberate choice, but wrong here: a range ring and a 150-enemy crowd need clean silhouettes, and it reads as "indie" in a genre whose UA is dominated by glossy 3D |
| Realistic / PBR zombies | Rules itself out on the frame budget alone, and needs an artist you do not have |

## 8. Buying it

Search the Asset Store for: **"stylized low poly zombie"**, **"low poly
character pack flat shaded"**, **"POLYGON"** (Synty), **"cartoon FX"**,
**"stylized VFX mobile"**.

- **Synty POLYGON** packs are the safe default: consistent across packs, flat
  shaded, mobile-friendly, and they interoperate — which is the thing that
  actually saves you, because mixing unrelated packs is what makes a game look
  cheap.
- Budget roughly **$150-400** for characters, environment and a VFX pack.
- Buy one environment pack and one character pack from the **same** family.
  Consistency beats quality here by a wide margin.

Custom art is the single biggest schedule risk on a project this size and it is
not what determines whether the game retains. Buy for v1.

## 9. What to do before buying anything

The prototype currently runs on grey capsules. Before spending:

1. Set `EnemyDefinition.tint` and `.scale` per tier and check you can tell them
   apart in a crowd of 100, on a phone, while shooting.
2. If capsules at different scales and colours already read clearly, the art
   purchase is a skin over a working design.
3. If they do not, **no art pack fixes it** — the tier shapes need rethinking
   first.
