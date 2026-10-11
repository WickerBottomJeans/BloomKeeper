# Board presentation

Start with `BoardVisual`. GameBoard sends it completed logic results; it never asks animation code to decide gameplay.

`GameBoard` is the composition root and turn orchestrator. Its cascade stays there so the turn order is visible. `BoardBoosterInteraction` owns chooser/highlight handling and the pending booster selection; `BoardPetalEditor` owns the admin popup and delegates edits to `BoardLogic`. Keep those implementation details out of `GameBoard`.

## Logic ownership

`BoardLogic` owns the live grid after `BoardInitializer` constructs it. Read its public operations first: `TryBeginSwap`, `BeginBooster`, `Resolve`, `ApplyGravity`, `Fill`, and `Reroll`. `GameBoard` calls them in turn order and presents their returned results.

Invalid player swaps are restored inside `TryBeginSwap`; their views still animate out and back. Glove swaps remain applied even without a match. `HasValidMove` inspects hypothetical cells without writing to the live grid. Admin edits also go through this owner.

Match detection and stateless skill services calculate results; they do not apply them. Tile and feature objects retain their own local rules. Initial construction remains separate. The raw tile array is still shared with read consumers, so this ownership is a code convention, not a compiler-enforced read-only boundary.

## Responsibilities

- **BoardVisual** is the entry point for swap, resolution, gravity, fill, shuffle, and boosters. Simple board motions use a short-lived view scope.
- **ResolutionPlayback** owns one resolution: start animations, wait for actual impacts and created views, present feature reactions in order, and finish or cancel. Its `Play` method shows the sequence.
- **PetalViewScope** owns the views used by one animation. Acquire views through the scope; disposal releases remaining ownership. Playback transfers a triggered skill's source view directly to its new owner.
- **SkillAnimationLibrary** maps skill animation data to an `ISkillAnimation`. Each animation declares the views it requires and describes its choreography. It does not own turn scheduling.
- **SkillPresentationContext** gives an animation its changes, view scope, and impact signals. Stripes share its immediate-clear operation.
- **BoosterPresenter** handles targeting and dispatches booster animations. The glove uses the same swap animation and view ownership as an ordinary move.
- **PetalViewManager / TileViewManager** operate views and pools. **BoardVFXManager / BoardAudioManager** supply effects and sound.

The small `ViewAccessKey` type is internal to view ownership. Animations do not create or release keys.

## Behavior preserved

| Action | Existing visual behavior |
| --- | --- |
| Swap | Swap sound and petal movement; invalid swaps move back with their existing sound. |
| Match | Ordinary petals disappear; contributors gather into a newly created skill. |
| Stripe | Source preparation and halo, beam firing, immediate clears, then beam/halo finish. |
| Stripe combination | Consume both inputs during preparation, fire both axes, clear affected petals, finish effects. |
| Bubble | Inflate, shoot bubbles, then stagger their pops across the existing finish duration. |
| Butterfly | Prepare, fly to its selected target, signal impact on arrival, finish/disappear. A targetless butterfly disappears. |
| Prismatic Bloom | Spin, shoot projectiles, transform or clear targets when projectiles arrive, play the finisher. |
| Skill chain | A child starts when its triggering impact is presented, without waiting for its parent's finishing animation. Newly created views must exist first. |
| Tile feature | Present each recorded feature change in order; adjacent reactions follow their recorded cause. |
| Gravity / fill | Drop existing petals, then spawn and drop the petals recorded by logic. |
| Shuffle | Disappear and respawn the recorded petals with the existing column delays. |
| Boosters | Keep target materials, glove selection pulses, wand impact/ripple, and glove swap. |

Durations, easing, VFX prefabs, and sound cues remain in their existing animation/view components. Decorative tails retain their existing timing.

## Extending an animation

1. Describe required starting views through `ISkillAnimation.RequiredViews`; playback handles acquisition and waiting for creation.
2. Use the context's view scope for later targets. Keep triggered skill views alive for their own animations.
3. Signal the corresponding tile impact at the actual impact point. Signal consumed combo inputs when their removal starts.
4. Await work that must finish before gravity. Release rented VFX in `finally` where the effect owns the rental.

## Verification

Compilation and focused logic/playback tests check chains, causal feature changes, view handoff, failure cleanup, cancellation, created-view dependencies, and stable fill/shuffle results. The playback tests simulate Unity views and effects; they do not verify rendered appearance.

Unity play-mode review should cover every row above, particularly overlapping skills, a newly created skill hit in the same resolution, repeated hits on a layered web, both boosters, and leaving the level during an animation.
