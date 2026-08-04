# BloomKeeper Production-Grade Portfolio Roadmap

## Goal

Build BloomKeeper into a production-grade portfolio project that demonstrates distinct engineering skills across Unity gameplay architecture, backend services, authentication, account continuity, economy, monetization, live operations, reliability, testing, deployment, and security.

This roadmap deliberately prioritizes engineering depth over adding many similar match-3 mechanics.

## Existing Foundation

- [x] Model-driven match-3 board with an explicit turn state machine.
- [x] Swapping, match detection, cascades, gravity, refill, deadlock detection, and shuffling.
- [x] Special petals, skill activation, and skill combinations.
- [x] Coordinated Striped skill presentation with dedicated rainbow VFX and matching one-shot SFX.
- [x] Match and clear-spider-web objectives.
- [x] Move and timer constraints.
- [x] Data-driven score rules, star thresholds, and level results.
- [x] Single-scene application flow with authentication, account loading, home, level session, completion, and result flows.
- [x] Virtualized level map and Addressable background/art loading.
- [x] PlayFab guest authentication.
- [x] Azure Functions progression loading and completion persistence through PlayFab entity files.
- [x] Initial Azure Monitor/OpenTelemetry backend configuration.
- [x] JSON-authored level and scoring content.

## P0: Trustworthy Baseline

- [x] Repair the content catalog: correct the duplicate level ID, complete or remove placeholder levels, and ensure every advertised map level has valid content.
- [x] Structure the initial ten-level progression so objectives and constraints are introduced deliberately: petal collection, move limits, timer limits, spider-web clearing, layered webs, and combined constraints.
- [x] Enforce `highestUnlockedLevel` in the level map and show an appropriate locked state.
- [x] Connect the win-screen Next action and define behavior for the final available level.
- [x] Verify home, retry, next, loss, and repeated-level paths cannot create duplicate subscriptions or input during transitions.
- [x] Fix Butterfly targeting so multiple Butterfly effects spread across eligible web tiles instead of repeatedly targeting one web tile, while preserving normal random-tile targeting behavior.
- [ ] Replace obsolete tests based on `Tile[,]` and removed petal values with tests for the current `BoardCell[,]` model.
- [ ] Add regression coverage for important match shapes, skill combinations, chained skills, obstacles, gravity, deadlocks, scoring, and end-of-turn decisions.

### P0 Exit Condition

Every advertised level loads valid data, the complete navigation loop has no dead action, progression locking is visible and enforced, and automated tests represent the current gameplay model.

## P1: Complete And Reliable Player Loop

- [x] Convert account-load and level-completion failures into explicit recoverable flow states.
- [x] Distinguish retryable completion failures from authoritative rejection: retain and retry valid results after connectivity or server-availability failures, but do not update progression when the backend rejects an invalid or cheating attempt, and show the player an honest outcome for each case.
- [x] Show useful retry and recovery UI for authentication, progression loading, and result submission failures.
- [x] Prevent unobserved exceptions from asynchronous flow event handlers.
- [x] Make level-completion writes idempotent so retrying cannot grant or record a result twice.
- [x] Handle PlayFab entity profile-version conflicts with an explicit concurrency policy.
- [ ] Add progression schema migration instead of only rejecting unsupported data.
- [ ] Cache the last successfully loaded progression for online-first startup and recovery.
- [ ] Allow an active level to finish if connectivity drops.
- [ ] Persist unsent completion attempts locally and synchronize them after reconnecting.
- [ ] Define conflict rules between cached progression and newer server progression.
- [ ] Handle pause, backgrounding, termination, and reconnect without losing or duplicating a result.

### P1 Exit Condition

A guest can launch, play, win or lose, unlock the next level, restart the application, and recover correct progression under normal networking, interrupted networking, and retried requests.

## Application-Ready Gate A

- [ ] Produce a tested Windows build.
- [ ] Produce sideloadable iPhone 11 and iPad Air 5 builds through CodeMagic/Sideloadly.
- [ ] Record a concise demonstration showing gameplay, progression, PlayFab, Azure Functions, and failure recovery.
- [ ] Add a portfolio-quality README with screenshots, architecture diagrams, setup instructions, technical decisions, and known limitations.
- [ ] Document how the Azure Functions and PlayFab custom functions are configured and deployed without publishing secrets.
- [ ] Audit the public repository for secrets, generated files, obsolete documentation, and misleading claims.

Start applying for jobs after Gate A while continuing the rest of the roadmap. The project does not need every later production feature before it can demonstrate professional engineering ability.

## P2: Account Continuity

- [ ] Add Google authentication where supported.
- [ ] Add Sign in with Apple on iOS.
- [ ] Preserve guest progression by linking a previously unused provider identity to the current PlayFab account.
- [ ] Detect when a provider identity is already linked to another PlayFab account.
- [ ] Build an explicit server-side account-conflict workflow instead of silently forcing a link.
- [ ] Define deterministic merge rules for level progression, currency, inventory, purchase entitlements, and account metadata.
- [ ] Show both account states before a destructive merge or replacement decision.
- [ ] Add token refresh or re-authentication before authenticated operations expire.
- [ ] Add logout, account switching, and account deletion behavior.
- [ ] Test new guest, returning guest, new provider, returning provider, guest linking, conflict merging, reinstall, logout, and interrupted-link scenarios.

### P2 Exit Condition

A player can safely begin as a guest, later attach Google or Apple identity, recover the account on another installation, and resolve a two-account conflict without silent progression loss.

## P3: Server-Authoritative Economy And Monetization

- [ ] Add server-authoritative soft currency.
- [ ] Add lives or energy using trusted server time.
- [ ] Add inventory with explicit item definitions and balances.
- [ ] Add an auditable transaction ledger or equivalent transaction history.
- [ ] Route reward grants and spending through idempotent Azure operations.
- [ ] Implement representative pre-level and in-level boosters that consume real inventory.
- [ ] Keep booster behavior extensible without adding many nearly identical booster mechanics.
- [ ] Add remotely configured shop offers and price definitions.
- [ ] Add one sandbox consumable in-app purchase.
- [ ] Validate purchase receipts before granting server-side value.
- [ ] Make purchase fulfilment idempotent and recoverable after interruption.
- [ ] Restore durable purchases when any non-consumable entitlement is introduced.
- [ ] Optionally add one rewarded-ad placement after the purchase path is reliable.

### P3 Exit Condition

Currency, rewards, spending, boosters, inventory, and one real-money purchase form a complete server-authoritative transaction loop that remains correct across retries and reconnects.

## P4: Live Content And Operations

- [ ] Extract live-content authoring and deployment into a dedicated project separate from the player client. It must author chapter, level, and index JSON; manage Addressable chapter assets and labels; build platform-specific catalogs and bundles; validate the complete staged content set; and publish the resulting `configs/` and `addressables/` trees to R2 through one versioned release workflow.
- [ ] Move level and other live configuration ownership to a versioned backend content catalog managed through an admin-facing content workflow; remove bundled local configuration as the runtime source of truth.
- [ ] Build a focused admin level-authoring and publishing tool for board dimensions, tile and void layouts, preplaced petals and skills, objectives, constraints, score thresholds, and export to the backend-owned level format.
- [ ] Make the authoring tool use shared automated catalog validation for JSON/schema parsing, IDs, advertised content, dimensions, tile counts, objectives, constraints, score thresholds, and referenced assets; enforce the same validation during publishing and in CI.
- [ ] Require each newly introduced objective, constraint, or board mechanic to have a deliberate onboarding level before it appears in mixed or advanced levels.
- [ ] Define schema and content versions for levels, scoring, economy, offers, and feature configuration.
- [ ] Add a remote content manifest that identifies compatible content versions and files.
- [ ] Fetch published level and configuration content from the backend, synchronize and verify it before level selection or when a newer compatible version is available, and start levels only from the resulting verified client cache rather than bundled local configuration.
- [ ] Make level, scoring, and manifest loading platform-safe, including Android StreamingAssets behavior.
- [ ] Verify downloaded content with hashes or equivalent integrity metadata.
- [ ] Cache the last-known-good content set locally.
- [ ] Reject incompatible or incomplete content atomically instead of partially applying it.
- [ ] Support rollback to a previous content version.
- [ ] Add remote feature flags and event/offer scheduling where they provide real operational value.
- [ ] Keep PlayFab Game Manager, Azure tooling, and Unity tooling as the operational interface; do not build a custom web dashboard for this portfolio.
- [ ] Treat Lua or HybridCLR runtime code hotfixing as a later experiment after content hotfixing is reliable.

### P4 Exit Condition

Levels and important tuning can be changed without rebuilding the client, while incompatible or broken remote content cannot strand the player.

## P5: Product And Platform Quality

- [ ] Lock the intended portrait orientation and disable unsupported rotation modes.
- [ ] Verify safe areas and layout on iPhone 11 and iPad Air 5.
- [ ] Define and verify constrained or letterboxed portrait behavior on Windows.
- [ ] Produce an Android build and perform emulator smoke testing; do not claim physical-device QA without a device.
- [x] Add music, gameplay SFX, UI audio, mixer groups, and persistent volume controls.
- [ ] Add haptics with a persistent enable/disable setting.
- [ ] Localize the complete player-facing UI into English and Vietnamese.
- [ ] Verify translated text expansion and font coverage without layout overlap.
- [ ] Distinguish petals through shape or iconography in addition to color.
- [ ] Add reduced-motion and appropriate contrast/accessibility settings.
- [ ] Verify touch and mouse interactions across supported layouts.
- [ ] Before release, inspect the Addressables Build Layout Report for duplicated dependencies across bundles; move genuinely shared heavy sprites, atlases, fonts, materials, audio, and other assets into shared Addressables groups, while avoiding bundle fragmentation for trivial duplicate assets.
- [ ] Profile loading time, memory, garbage collection, and frame pacing.
- [ ] Hold a measured 60 FPS target on the owned iPhone, iPad, and representative Windows hardware.

### P5 Exit Condition

The same build behaves intentionally across the owned target devices, has complete audio and settings, supports English and Vietnamese, and meets documented performance and accessibility targets.

## P6: Engineering Operations And Automated Quality

- [ ] Add focused unit tests for deterministic gameplay rules and scoring.
- [ ] Add PlayMode tests for application flows and critical UI-to-gameplay paths.
- [ ] Add backend unit tests for progression, account merging, economy, rewards, and purchase fulfilment.
- [ ] Add client/backend contract tests for every function request and response.
- [ ] Add integration tests against a dedicated development PlayFab title or equivalent isolated environment.
- [ ] Run content validation automatically in CI.
- [ ] Run Unity and backend tests automatically in CI.
- [ ] Produce versioned Windows and Android build artifacts in CI.
- [ ] Keep CodeMagic as the iOS build path and record its exact configuration.
- [ ] Separate development and production configuration.
- [ ] Protect PlayFab, Azure, Apple, Google, store, and signing secrets.
- [ ] Add client analytics for progression, session flow, economy, purchases, content versions, and failures.
- [ ] Add client crash diagnostics.
- [ ] Attach correlation identifiers so client failures can be traced into Azure telemetry.
- [ ] Document deployment, migration, rollback, and incident-recovery procedures.

### P6 Exit Condition

A clean checkout can validate content, run meaningful tests, build supported artifacts, and expose client/backend failures without relying on undocumented manual knowledge.

## P7: Deterministic Replay Anti-Cheat

This phase starts only after the complete player loop is stable.

- [ ] Extract gameplay simulation into a pure C# boundary that does not depend on Unity presentation, scene objects, animations, or frame timing.
- [ ] Share the deterministic simulation contract between the Unity client and Azure validation code.
- [ ] Replace unseeded randomness with explicit seeded random-number generation.
- [ ] Record the initial seed, ordered player commands, level/content version, rules version, and client build version.
- [ ] Define a versioned replay payload and compatibility policy.
- [ ] Replay submitted attempts on the server.
- [ ] Validate objectives, score, stars, move count, resulting board state, and legal command order.
- [ ] Reject impossible or modified attempts before progression or leaderboard submission.
- [ ] Add replay fixtures and tampering tests.
- [ ] Add rate limits and suspicious-attempt telemetry.
- [ ] Retain enough diagnostic information to explain rejected attempts without storing unnecessary player data.

### P7 Exit Condition

Azure can reproduce a supported attempt from its seed and command stream, independently derive the accepted result, and reject altered results deterministically.

## P8: Verified Social Competition

- [ ] Add per-level leaderboards that accept only server-validated scores.
- [ ] Add seasonal leaderboard definitions and controlled reset behavior.
- [ ] Define deterministic tie-breaking rules.
- [ ] Support top ranks, pagination, and ranks around the current player.
- [ ] Display validation or synchronization state honestly when a result is still pending.
- [ ] Add leaderboard abuse monitoring and operational controls.

Realtime multiplayer, chat, clans, teams, and friends are outside the intended scope.

## Final Portfolio Release

- [ ] Produce final Windows, iOS, and Android portfolio artifacts at the level actually tested.
- [ ] Publish a concise technical case study covering architecture, tradeoffs, failures, migrations, testing, backend operations, and anti-cheat design.
- [ ] Include diagrams for application flow, gameplay simulation, account linking/merging, economy transactions, live content, and replay validation.
- [ ] Include test and CI evidence rather than relying only on written quality claims.
- [ ] Record a polished demonstration showing the player experience and the supporting PlayFab/Azure systems.
- [ ] Clearly label implemented features, mocked integrations, sandbox integrations, untested platforms, and remaining limitations.

## Explicitly Outside The Critical Path

- Large numbers of additional match-3 mechanics.
- A large level count before the content pipeline is reliable.
- A broad general-purpose visual level editor beyond the focused JSON level-authoring tool planned for P4.
- A custom web administration dashboard.
- Landscape rotation or freely resizable mobile UI.
- Realtime multiplayer, chat, clans, teams, or friends.
- Subscriptions, battle passes, and multiple advertising placements.
- Runtime code hotfixing before content hotfixing is complete.
- Public store publication as a requirement for the portfolio.
- Full screen-reader support before the higher-impact accessibility work is complete.

These items can be reconsidered only when they demonstrate a genuinely new engineering field or solve a verified production need.
