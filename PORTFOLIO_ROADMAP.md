# BloomKeeper Job-Ready Portfolio Roadmap

## Purpose

This is the shortest roadmap from the current BloomKeeper repository to a portfolio package that can help land a junior Unity/C# job.

`ROADMAP.md` remains the long-term production-engineering roadmap. It is not a prerequisite for applying. This roadmap ends when a recruiter can understand the project quickly, an interviewer can inspect credible engineering evidence, and another person can run the game without opening Unity.

Target effort: roughly 7–12 focused working days, depending on build issues and presentation polish.

## Current Portfolio Evidence

BloomKeeper already contains enough technical scope for a junior portfolio:

- A model-driven match-3 board coordinated by an explicit turn state machine.
- Swaps, match shapes, cascades, gravity, refill, deadlock detection, and shuffling.
- Multiple skills, skill combinations, chained activations, obstacles, objectives, constraints, scoring, and stars.
- Ten authored levels with progression locks and result navigation.
- A persistent single-scene application flow with recoverable asynchronous UI states.
- PlayFab guest authentication.
- Azure Functions progression loading and completion persistence.
- Idempotent level-attempt handling and optimistic-concurrency conflict recovery.
- Addressable art, a virtualized level map, responsive board layout, and editor content tools.

The problem is not insufficient feature count. The problem is that this evidence is difficult for an interviewer to discover and some public evidence is currently misleading or incomplete.

## Current Hiring Blockers

- There is no root README.
- There are no portfolio screenshots, gameplay video, downloadable build, or release link.
- The two existing PlayMode tests target removed gameplay types and do not demonstrate the current architecture.
- The backend has no first-party automated tests.
- `ARCHITECTURE.md` contains stale claims about progression locking, result navigation, and failure recovery.
- Featured code contains unresolved bug notes, informal comments, spelling problems, and temporary-work warnings.
- Project presentation still exposes development defaults, including the default company name and broad rotation settings.
- `LaunchBloomKeeper.bat` contains machine-specific Unity and Rider paths and is not a portable setup path for reviewers.
- The repository does not state which platforms were actually built and tested.

## Definition Of Done

BloomKeeper is job-ready when all of the following are true:

- A recruiter can understand the game and its strongest engineering work within two minutes of opening the repository.
- A reviewer can download and run a Windows build without Unity.
- A 90–150 second video demonstrates the player loop and the online progression loop.
- The public repository contains no knowingly obsolete tests, stale architecture claims, secrets, or misleading platform claims.
- Focused automated tests prove representative gameplay rules and backend idempotency.
- The showcase path can be completed without crashes, dead actions, broken transitions, or visible development UI.
- The repository clearly distinguishes implemented behavior, sandbox/cloud dependencies, tested platforms, and known limitations.

## Phase 1: Freeze Scope And Stabilize The Showcase

Goal: make one complete path dependable instead of expanding the game.

- [ ] Freeze new feature development until this roadmap is complete.
- [ ] Finish or safely close the current uncommitted progression and failure-recovery work.
- [ ] Define the showcase path: launch, guest login, load progression, select an unlocked level, demonstrate a skill and obstacle, win, unlock the next level, use Next, return Home, restart, and show persisted progression.
- [ ] Manually walk the showcase path and record every visible failure or dead action.
- [ ] Fix only issues that block or visibly damage that showcase path.
- [ ] Confirm locked levels cannot be selected and the final available level does not show a dead Next action.
- [ ] Confirm retry, home, next, loss, and repeated-level paths do not duplicate input or event handling.
- [ ] Triage remaining known issues into either `fix before release` or an honest Known Limitations list.

Exit condition: the chosen demonstration path is stable and no new portfolio feature is required.

## Phase 2: Add Minimum Player-Facing Polish

Goal: prevent the first thirty seconds from looking like an unfinished engineering prototype.

- [ ] Remove or hide tester-only controls and development presentation from the release build.
- [ ] Set intentional product identity, version, icon, window behavior, and portrait presentation for the Windows build.
- [ ] Ensure the first-time player can understand mouse/touch interaction and the current objective without developer explanation.
- [ ] Perform one focused visual-consistency pass on loading, authentication, map, gameplay HUD, dialogs, win, and loss screens.
- [ ] Add a minimal licensed audio pass if the current build is silent: one music loop plus clear swap, match, skill, win, loss, and button feedback. Do not build a large audio feature set.
- [ ] Fix only highly visible animation, layering, text, or transition defects encountered during the showcase.

Exit condition: a reviewer can play the showcase without guidance and the recording does not look or sound obviously unfinished.

## Phase 3: Create Credible Engineering Proof

Goal: replace claims with a small amount of trustworthy evidence.

### Gameplay tests

- [ ] Remove or replace tests based on the deleted `Tile[,]` model and removed petal values.
- [ ] Test the current `BoardCell[,]` model directly without reflection-based reconstruction of removed APIs.
- [ ] Cover a representative set rather than every permutation: important match shapes, invalid versus committed swaps, adjacent obstacle damage, one skill chain, gravity/refill behavior, and a settled-turn decision.

### Backend tests

- [ ] Add focused tests around progression mutation without requiring live PlayFab or Azure infrastructure.
- [ ] Prove that a new winning attempt updates progression once.
- [ ] Prove that an identical retry does not apply the attempt twice.
- [ ] Prove that reuse of an attempt ID with different data is rejected.
- [ ] Prove that locked-level and invalid-value requests are rejected.

### Evidence capture

- [ ] Run the approved test suites and save the exact passing counts for the README.
- [ ] Capture one profiler session of the showcase path and document only measured findings; do not start a general optimization project.
- [ ] Fix a performance issue only if the capture reveals a visible or material problem.

Exit condition: the repository contains a small, current, understandable test suite and measured evidence instead of obsolete test files or unsupported quality claims.

## Phase 4: Package The Project For Humans

Goal: make the work visible without requiring a reviewer to explore 145 client scripts.

### Windows artifact

- [ ] Produce a clean Windows x64 development-candidate build.
- [ ] Smoke-test the complete showcase path in the standalone build, not only in the Unity Editor.
- [ ] Produce a final portfolio build with intentional versioning and no tester UI.
- [ ] Zip the build and publish it as a clearly labeled GitHub Release artifact.

### Demonstration media

- [ ] Capture four to six clean screenshots covering the level map, gameplay, a skill combination, an obstacle objective, and the result screen.
- [ ] Record a 90–150 second demonstration showing gameplay, progression unlocking, persistence after restart, and the PlayFab/Azure boundary.
- [ ] Keep the demonstration focused on visible proof. Do not narrate every class or roadmap item.

### Root README

- [ ] Add a concise root README with a strong screenshot or GIF at the top.
- [ ] Include a one-paragraph project pitch and a direct build/release link.
- [ ] List implemented player-facing features separately from engineering features.
- [ ] Include a compact architecture diagram showing Unity, PlayFab, Azure Functions, and progression storage.
- [ ] Explain three engineering stories: the turn-resolution pipeline, model-before-presentation updates, and idempotent/concurrent progression writes.
- [ ] Link those stories to a few representative source files instead of listing the entire codebase.
- [ ] Show current test evidence and the platform/build actually verified.
- [ ] Provide setup instructions that do not expose secrets.
- [ ] State known limitations honestly, including client-trusted gameplay results and any remaining network/offline limitations.

Exit condition: a reviewer can understand, watch, download, run, and inspect the project from the README alone.

## Phase 5: Clean The Public Evidence

Goal: remove avoidable reasons for an interviewer to doubt the work.

- [ ] Update `ARCHITECTURE.md` so it describes the implementation that is actually being released.
- [ ] Mark `ROADMAP.md` clearly as long-term work so unchecked production features do not make the portfolio appear unfinished.
- [ ] Review comments in representative gameplay, flow, authentication, and backend files; resolve, rewrite, or remove informal and stale TODO comments.
- [ ] Do not perform broad refactors merely to make code look different.
- [ ] Replace or remove the machine-specific launch script as a public setup mechanism.
- [ ] Audit tracked files for secrets, generated output, local IDE state, obsolete recovery files, and misleading documentation.
- [ ] Confirm the ignored backend settings file remains untracked and document required configuration keys without real values.
- [ ] Review public repository naming and metadata, including product identity and release version.
- [ ] Create one clean portfolio release/tag after the approved changes are reviewed.

Exit condition: the public repository is coherent, honest, portable enough for review, and free of knowingly broken evidence.

## Phase 6: Prepare The Interview Story And Apply

Goal: convert the project into interview performance instead of adding more code.

- [ ] Prepare a two-minute project introduction that does not call BloomKeeper merely a "simple match-3 game."
- [ ] Prepare a five-minute technical walkthrough of the turn state machine and its ownership boundaries.
- [ ] Prepare one debugging story, one architecture tradeoff, one backend reliability story, and one mistake that changed the design.
- [ ] Be able to explain why gameplay logic is separated from presentation and why the model mutates before animation.
- [ ] Review C# shallow versus deep copying, collection tradeoffs, Unity destroyed-object null behavior, Profiler/`GC.Alloc`, and Addressables handle ownership.
- [ ] Add concise BloomKeeper bullets to the résumé using only verified claims.
- [ ] Pin the repository and release, then begin applying to junior Unity and relevant C# roles.
- [ ] Continue long-term roadmap work only while applications are already running.

Exit condition: applications have started and every major portfolio claim can be explained without reading generated documentation.

## Explicitly Deferred Until After Applications Start

These features may deepen the project later, but they are not required to make the current work interview-worthy:

- Progression schema migration.
- Offline progression cache and durable result synchronization.
- Google or Apple authentication, account linking, and account merging.
- Currency, inventory, boosters, shop, advertising, and purchases.
- Remote content publishing and rollback infrastructure.
- Large level counts or additional match-3 mechanics.
- Full localization and accessibility systems.
- Android and iOS portfolio releases.
- Broad CI/CD infrastructure.
- Deterministic replay anti-cheat and leaderboards.
- A custom administration dashboard.

Deferred does not mean unimportant. It means lower hiring value per day than a stable build, current tests, a strong README, a short demonstration, and a clear technical explanation.

## Stop Rule

Do not postpone applications because BloomKeeper could support one more production feature.

Once the Definition Of Done is satisfied, publish the release and apply. Future work must earn its place by strengthening a specific interview story, fixing observed player friction, or responding to real employer feedback.
