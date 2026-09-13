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

## 6. Environment — arena

```
top-down 3/4 view of a small empty urban plaza at night, flat shaded low-poly 3D
stylized mobile game environment, Synty POLYGON art style, dark desaturated
asphalt ground in near-black blue-grey, a few scattered silhouetted props —
overturned chairs, a bench, a crate, a bollard — all very dark and low contrast,
no bright surfaces, no signage, no lighting fixtures, empty clear centre with
nothing in it, clean hard-edged faceted geometry, no textures, moody cool ambient
light, environment concept, no characters
```

**Watch for:** the generator will want to light it dramatically and fill the
centre. Both are wrong. The centre must stay **empty** — that is where the
player and the engagement ring live — and the whole ground must stay dark enough
that a green ring and a crowd of bright zombies sit clearly on top of it.

---

## 7. Gameplay scene — the style target

The single most useful image to generate. Not a screenshot — a target for what
the game should look like once the art lands, and the thing to hold every asset
against.

```
portrait 9:16 mobile game screenshot, top-down three-quarter view angled about 55
degrees, flat shaded low-poly 3D, stylized Synty POLYGON art style, chunky
exaggerated character proportions, no textures, solid saturated colour blocks.

Centre: a lone survivor in a steel-blue tactical vest and tan trousers standing
completely still in a braced stance, firing a pistol, a bright muzzle flash at
the barrel. A wide glowing neon-green circle is drawn flat on the ground around
them, about five times their height across, thin and bright.

Surrounding them: a crowd of about thirty stylized zombies converging inward from
every direction — slouched desaturated-green shamblers, small acid-yellow runners
crouched forward mid-sprint, and two wide hunched red-brown brutes. Some are
outside the green circle, some are crossing it.

Bright yellow bullet tracer streaks fly from the survivor outward. Small floating
damage numbers in white and orange.

Ground: very dark near-black blue-grey asphalt, low contrast, a few silhouetted
overturned chairs and crates barely visible. Dark background, no sky, moody cool
ambient light. The characters and the green circle are the only bright things in
frame.
```

**Watch for:** the generator will want to light the ground and add scenery. Both
break the read. If the zombies do not pop clearly against the floor, push the
ground darker rather than the characters brighter — the palette has to survive a
crowd of 150, not 30.

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
