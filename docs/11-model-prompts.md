# Text-to-Image Prompts — Character Concepts

Five prompts, one per model in `docs/10-asset-list.md`. Built to produce
**modelling reference**, not finished art: A-pose, front-on, flat lighting,
plain background.

## How to use these

1. Paste the **Style block** + the character's **Subject** + the **Negative
   prompt** as one prompt.
2. Generate all five **in one session with the same seed and the same style
   block**. Consistency across the family matters far more than any single
   character's quality — mixing looks is the fastest way to make a game read as
   cheap.
3. Use the result as a **design**, then model from it. Do not expect a usable
   orthographic blueprint (see caveats at the bottom).

---

## Style block — prefix every prompt with this

```
flat shaded low-poly 3D character render, stylized mobile game asset, Synty
POLYGON art style, chunky exaggerated proportions with an oversized head roughly
one third of total body height, clean hard-edged faceted geometry, no textures,
solid saturated colour blocks, simple minimal facial features, A-pose with arms
slightly away from the body, full body from head to feet, centred in frame,
front orthographic view, plain flat mid-grey background, even flat studio
lighting, no cast shadows, no depth of field, character concept sheet
```

## Negative prompt — append to every prompt

```
photorealistic, realistic, PBR materials, detailed textures, normal maps, fabric
wrinkles, skin pores, dramatic lighting, rim lighting, dynamic action pose,
foreshortening, motion blur, environment, scenery, background props, multiple
characters, cropped limbs, text, watermark, signature, extreme gore, blood
spatter, body horror
```

---

## 1. Player — Survivor

```
a determined human survivor standing rooted in a wide braced stance with both
feet planted, holding a chunky stylized pistol in both hands at chest height,
practical gear: a fitted tactical vest over a t-shirt, cargo trousers,
fingerless gloves, short practical hair, a small backpack. Confident, grounded,
heroic. Palette: cool steel-blue vest, warm tan trousers, one bright orange
accent on the shoulder
```

**Watch for:** this is the only warm, bright, high-contrast character in the
game — everything else is a zombie on a dark floor. If it does not immediately
read as the hero at thumbnail size, push the orange accent.

---

## 2. Shambler — baseline zombie

```
a slow shambling zombie in torn everyday clothes, standing upright but slouched
forward, arms hanging loose at its sides, head tilted to one side, average adult
build, tattered button shirt and worn trousers, bare feet. Ordinary and
unremarkable — the baseline every other zombie is read against. Palette:
desaturated sickly green skin, muted grey-brown clothing
```

**Watch for:** deliberately the least interesting silhouette in the set. It is
the reference shape; if it is distinctive, the other tiers stop reading as
different.

---

## 3. Runner — fast, fragile

```
a fast lean zombie, small and wiry, crouched low and leaning aggressively
forward as if caught mid-sprint, thin spindly limbs, hunched shoulders, head
thrust ahead of the body, ragged torn-off sleeves, barefoot. Noticeably smaller
and thinner than an average zombie. Palette: bright acid yellow-green skin, dark
ragged clothing
```

**Watch for:** it must read as *small and forward-leaning* from directly above.
The forward lean is the whole tell — a Runner standing upright is just a smaller
Shambler.

---

## 4. Brute — heavy, armoured

```
a massive hulking zombie, very wide and heavy, hunched far forward with enormous
long arms hanging past its knees, a tiny head sunk between huge shoulders, thick
stumpy legs, torn work overalls stretched over a bulky frame. The silhouette
must read as wide and low rather than tall. Palette: heavy mottled red-brown
skin, dark grey torn overalls
```

**Watch for:** width, not height. Generators default to "big = tall". If it
comes out tall, add `low centre of gravity, squat, gorilla-like proportions,
wider than it is tall`.

---

## 5. Boss

```
a towering monstrous zombie boss, more than twice the height of a normal zombie,
broad shoulders armoured with fused bone plating, heavy asymmetric build with
one oversized arm, hunched powerful stance, a tattered heavy coat hanging off
its frame. A distinct monstrous silhouette, not a scaled-up ordinary zombie.
Palette: near-black charcoal skin and clothing, with glowing hot orange emissive
cracks running along the shoulders and spine
```

**Watch for:** the emissive cracks are functional, not decoration — the boss
fights on a dark floor and the glow is what keeps it readable. They also give
the phase-two enrage something to brighten.

---

## Caveats

**Text-to-image will not give you a clean orthographic turnaround.** Multi-view
consistency is the weakest thing these models do. Two options that work:

- Generate the 3/4 or front view you like, then feed it back as an image
  reference for `side view` and `back view` — accept that they will drift, and
  reconcile by eye while modelling.
- Or treat the output purely as a **design and palette target** and block out
  the model from the silhouette spec in `docs/09-art-direction.md` §3.

**The proportions in the prompts are the design, not the spec.** The binding
numbers are in `docs/10-asset-list.md`: scale 1.00 / 0.85 / 1.45 / 2.2, pivot at
the feet, facing +Z, 300-800 tris for ordinary zombies.

**Generate the family together.** Five characters generated on five different
days with five different phrasings will not look like one game.
