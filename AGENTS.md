# Collaboration Rules

## Architecture First

- The primary task is discussing and agreeing on architecture with the user.
- When the user asks to edit only Markdown files, apply the requested Markdown changes directly without asking for file-scope confirmation. Continue to require explicit scope confirmation before editing code. All other project restrictions remain in effect.
- When the user asks to create or edit code under `Assets/Editor`, apply the requested Editor-script changes directly without architecture or file-scope approval. All other project restrictions remain in effect.
- When the user explicitly requests a comment-only code edit, including adding, changing, or removing a TODO, apply it directly without architecture or file-scope approval.
- When the user explicitly requests region-only code organization, apply it directly without architecture or file-scope approval. Do not change behavior, APIs, names, or non-region formatting as part of that edit.
- When the user explicitly communicates that implementation should proceed without a scope checkpoint, treat natural-language equivalents such as `skip scope`, `don't need scope`, `no scope needed`, and similarly clear wording as approval to implement the requested change immediately without presenting an implementation scope or asking for confirmation. Do not require an exact phrase. All other project restrictions remain in effect.
- When the user has explicitly authorized an ongoing redesign or iterative improvement of a named prefab or serialized UI asset, treat subsequent requests in the same conversation to apply, continue, simplify, tune, or redo that design as approval to edit that same asset. Do not repeat architecture or file-scope checkpoints for each iteration. Stop for renewed approval only if the work expands to another file, code, scenes, behavior, a new type, or a materially different feature goal.
- Do not edit production code, tests, prefabs, scenes, packages, configuration, or other project files until the user explicitly accepts the proposed architecture.
- Before implementation, explain the proposed responsibilities, ownership, boundaries, and data flow, including meaningful alternatives and tradeoffs.
- Before editing, list every file that will be created, modified, or deleted and describe the exact class, method, API, and behavior changes planned for each file.
- Wait for the user to explicitly approve that exact implementation scope. Never infer approval from enthusiasm, general agreement, or approval of a broader concept.
- When an exact implementation scope has been presented and the user is directly answering its approval question, affirmative replies such as `ok`, `yes`, `approved`, `do it`, or `proceed` explicitly approve that scope. Do not ask the user to repeat the approval using different wording.
- Before making the first implementation edit, use read-only inspection to confirm that the approved scope is self-contained and can produce a complete, usable result. If a newly discovered dependency, blocker, assumption, or required change falls outside the approved scope, stop before editing any implementation file and request approval for a revised exact scope. Never create or leave a partial implementation that depends on unapproved follow-up work.
- Do not require the user to save or finish in-progress Unity Editor setup before implementing an approved code change that is independently safe and self-contained. Treat the reported Editor state as ongoing work, complete the code portion first, and mention any remaining manual Inspector assignment afterward. Only stop for the Editor state when proceeding would overwrite unsaved asset work or the code cannot be implemented safely without it.
- Never perform any implementation work outside the exact approved scope. Routine tooling, patch-context, encoding, command, or other mechanical failures inside the approved scope may be diagnosed and retried with a safe equivalent method without stopping for user approval or reporting each failed attempt. Stop and obtain explicit approval only when resolving the failure requires a revised or additional file, API, behavior, responsibility, dependency, assumption, design decision, or implementation approach outside the exact approved scope.
- Do not create a new class, interface, file, component, manager, handler, abstraction, or data type unless the user has seen and explicitly approved it.
- This applies to every new type, including private classes, nested classes, private interfaces, helper/adaptor types, structs, records, enums, delegates, and event/data DTOs.
- Do not add supporting changes, cleanup, formatting, refactoring, validation, defensive code, or architectural structure that was not explicitly included in the approved scope.
- If implementation reveals any unapproved requirement or design decision, stop immediately, explain it, and wait for new approval before editing further.
- Implement only the approved changes. Do not make adjacent or opportunistic changes.
- Discussion, inspection, and read-only analysis do not authorize implementation.
- A request to explore, review, or discuss an idea is not approval to edit code.
- Only begin editing after the user clearly approves the architecture and asks for implementation.
- If the architecture changes during implementation, stop editing and return to architecture discussion for renewed approval.
- Unity serialized assets, editor-authored setup files, project settings, package files, generated Unity files, and `.meta` files may be created, edited, moved, or deleted when they are inside the explicitly approved implementation scope.
- Never assign or reassign Inspector fields for the user, whether through Unity automation or by directly editing serialized references. When Inspector assignment is required, give the user the exact manual field-assignment steps.
- Never preserve a misleading name, weak responsibility boundary, or inferior architecture merely to avoid breaking serialized Unity references. Make the clean code change, allow the references to break, and give the user exact manual Editor steps to reassign the affected scripts, prefabs, or fields.
- Never use serialization-preservation mechanisms such as `[FormerlySerializedAs]`. When a rename breaks a serialized reference or field value, leave it broken and give the user the exact manual Inspector reassignment steps instead.

## User Review

- Before giving advice, recommendations, explanations, or proposed solutions about this project, inspect the relevant project files and base the response on the actual implementation. Do not give generic advice about a project-specific matter. Generic advice is allowed only when the user explicitly asks about a general problem that is not tied to this game.
- Except for the Markdown, `Assets/Editor`, and ongoing named-prefab/serialized-UI redesign exemptions above, the user must be able to review and approve every proposed change before it is made.
- Keep proposals concrete and concise to avoid wasting the user's time and token usage.
- When the user provides new context that corrects or refines an active question, immediately re-answer the original question using that context. Do not explain why the new context matters, restate it back to the user, or narrate the correction unless the user asks.
- When the user says they do not know how to use a tool, never replace the missing instructions with an outcome such as "find," "select," or "inspect" something. Guide each operation from the visible UI: state where the control is, exactly what to click or enter, what changes after that action, and why that result is useful before continuing to the next operation.
- When guiding Shader Graph work, always identify every referenced item by its complete Shader Graph category and name, such as `[Node] Negate`, `[Property] Border Width`, `[Port] UV`, `[Setting] Alpha Clipping`, or `[Concept] texel`. State whether each term is a node, property, port, setting, or concept before instructing the user to use it. Give the exact visible action for creating or locating nodes and properties, and do not use shortened names until the full labeled name has been introduced.
- Treat production-grade engineering architecture as the priority during feature planning. Focus discussion on ownership, responsibility boundaries, explicit data flow, server authority for valuable account state, transaction safety and idempotency, failure behavior, testability, maintainability, and appropriate extension paths.
- Do not overwhelm or block the user with low-impact game-design questions when the user has not expressed a design preference. Choose the normal genre convention and continue. Ask the user only when a game-design choice materially changes architecture, security, stored data, economy integrity, implementation scope, or another consequential system boundary.
- Evaluate proposed architecture from the perspective of a strong production code review or technical interview: UI remains behavior-free, application flow owns orchestration, domain/gameplay owners execute their own rules, infrastructure owns external communication, and no layer reaches across those boundaries for convenience.
- When the user asks for big steps, concepts, architecture pictures, strategy, or high-level planning, answer at that level only. Do not provide file lists, exact code, class names, method names, implementation steps, or other low-level details unless the user explicitly asks for implementation detail.
- Preserve the user's sense of ownership and learning. Do not hand over large prebuilt implementations or hide design choices inside generated code; explain the next concept, let the user make the design decision, challenge tradeoffs, and implement only the smallest approved step.
- Whenever describing current code behavior, always link every behavioral claim to the exact code that implements it. Do not describe behavior without a direct file-and-line link to its implementation.
- Every code link must point to the exact operative line that performs the behavior being described. Never link merely to a method signature, class declaration, broad code region, or nearby line when a more precise implementation line exists. If one behavioral claim depends on multiple operative lines, link each relevant line at the corresponding part of the explanation.
- Explain code mechanically and concretely. Name the exact fields read or written, the exact object or component affected, who calls each relevant method and when, whether the operation mutates state immediately or only stores/schedules work, and the exact math or transformation performed. Do not substitute vague intention-level phrases such as "handles layout" for these details. Assume the user already has two years of Unity experience and omit basic Unity explanations unless requested.
- Always distinguish compiler-enforced or framework-enforced behavior from documentation, naming, and behavioral convention. For callbacks, abstract methods, interfaces, and overrides, state exactly what the signature enforces (arguments, return value, required implementation), what the caller actually does, and what side effects are merely expected from the implementation. Never describe an expected convention as though the type system or framework guarantees that fields or objects are mutated.
- When providing code snippets, always state the exact owning class and function or method they belong to; do not give loose snippets detached from implementation context.
- Do not hide implementation decisions behind broad summaries such as "wiring," "supporting changes," or "affected files."
- After editing code, check whether any touched class, method, function, field, file, or serialized API name no longer matches its current responsibility or behavior. If a name has become stale or misleading, explicitly call it out and propose the rename before continuing.
- After editing, report exactly what changed and do not claim verification that was not performed.

## Production-Grade Quality

- Do not implement hacks, throwaway code, temporary workarounds, quick fixes, cheap fixes, or knowingly brittle solutions.
- UI view components must keep buttons behavior-free: they translate Unity `Button` clicks into semantic C# events, and the caller subscribes to those events and owns the resulting action or flow decision.
- Do not expose UI `Button` components so callers can attach behavior directly. Do not use delegate properties or callback parameters instead of events unless a concrete ownership, lifetime, return-value, or composition requirement makes a delegate materially better; explain that advantage before proposing it.
- A parent or composite UI component owns the creation, parenting, switching, visibility, and cleanup of UI children displayed inside its hierarchy. Application flows and non-UI callers must never instantiate a UI view and pass its `GameObject`, `Component`, `Transform`, or `RectTransform` into another UI component for display.
- Callers interact with composite UI through semantic methods using data, identifiers, or asset addresses, and subscribe to semantic events. Do not expose child slots or accept externally created UI instances merely to let callers compose the UI hierarchy.
- Do not hard-code feature-specific behavior inside generic managers, orchestrators, state machines, or shared systems.
- Do not introduce magic values or presentation tuning directly in orchestration or gameplay logic; place approved tuning in the appropriate configuration or owning view system.
- When an integer needs to represent an unassigned or absent state, use `int?`. Never reserve a magic integer such as `-1` as an unassigned-state sentinel.
- For responsive UI, do not hard-code screen/layout-dependent numbers such as pixel offsets, margins, breakpoints, widths, heights, or scale constants in code.
- Responsive UI sizing and positioning must come from Unity layout, anchors, RectTransform bounds, aspect tools, or explicitly serialized/configured values owned by the relevant view.
- Do not add speculative layout compensation values such as hidden margins, overlap pixels, or seam fixes before the visual problem is observed and approved.
- Never silently ignore invalid runtime state, failed assumptions, or unexpected null references.
- Never soften an error or missing required data by silently providing a fallback, default, placeholder, or alternate path.
- Error states must be loud and visible. If a fallback is being considered, stop and ask the user for explicit approval, making the fallback question highly visible.
- Serialized fields and scene/prefab setup references are non-null by contract. Do not write runtime null checks, `?.` guards, fallback lookup, custom `Debug.LogError`, or recovery paths for missing serialized setup.
- Choose dependency construction according to ownership. Runtime-owned plain C# collaborators must be constructed by their owner, while Unity component dependencies authored as scene or prefab setup must use serialized fields. When the exact component type is enforced on the same GameObject by `[RequireComponent(typeof(T))]`, assigning it with `GetComponent<T>()` is allowed. Never use `GetComponentInChildren`, `GetComponentInParent`, or an unenforced `GetComponent` lookup for required dependencies.
- Use serialized component references directly. If setup is broken, let Unity/C# surface the failure naturally.
- Null checks are allowed only for real nullable runtime state or data, such as optional method arguments, cached instances, active tweens, lookup results, or intentionally absent content.
- Require clear ownership, maintainable boundaries, explicit data flow, and an appropriate extension path for foreseeable production use.
- Never use the project's current small scale to justify a short-term architecture. Always prioritize scalable data structures, algorithms, ownership boundaries, and extension paths over solutions that only remain acceptable while content counts are low.
- Prefer the simplest production-grade solution, not the shortest implementation and not speculative overengineering.
- Before implementation, identify any coupling, special-case dispatch, type checks, or volatility introduced by the design so the user can review it explicitly.
- If only a brittle or temporary solution is currently possible, stop and explain the limitation instead of coding it.

## Code Formatting

- Keep function calls and signatures compact. Never format parameters or arguments one per line.
- Library calls, such as DOTween APIs, are exempt when the library's conventional formatting places each parameter or argument on its own line for readability.
- Write inline code comments as short, plain action labels, such as `// Hide all booster buttons.` Place each comment directly above the code chunk it describes, keep each comment to one action or concept, and split comments that describe multiple actions. Do not use long explanatory sentences when a brief label is enough.
- XML documentation summaries must state the member's concise caller-visible contract and any important resulting lifecycle or state consequence. They must sound natural, relaxed, and concise rather than stiff, formal, or robotic. Do not merely repeat the member name, enumerate implementation steps, or include details callers do not need.
- When the user asks for an opinion or review of something they wrote, evaluate their version first and identify its concrete problems before offering a revised version. Do not silently replace their work with a suggestion; if it has no meaningful problem, say so explicitly.

## Code Organization

- For non-trivial `MonoBehaviour` classes, keep serialized fields and runtime state at the top, then organize members with `#region Unity Lifecycle`, `#region Public API`, and `#region Private Methods` in that order. Unity callbacks such as `Awake`, `OnEnable`, and `OnDestroy` belong in `Unity Lifecycle`; public events, properties, and callable methods belong in `Public API`; handlers and implementation helpers belong in `Private Methods`. Omit empty regions and do not add regions to short classes where they would create more noise than navigation value.
- Declare every project enum in `Assets/Scripts/Shared/Enum.cs`. Do not create standalone enum files or declare enums inside other project types.
- Do not explicitly use the `internal` keyword in project-owned code until the project defines its own assembly boundaries with `.asmdef` files. Leave third-party and vendor code unchanged.
- `GameFlowController` is always an application-level orchestrator. It may order flow transitions and make semantic calls to the systems that own work, but it must not own feature configuration, presentation assets or tuning, domain logic, or subsystem implementation. Put those responsibilities in their dedicated owning flow, service, director, or presentation component.
- Follow the established `UIManager` panel convention: serialize a prefab field named `<panel>Prefab`, keep the spawned runtime component in a separate non-serialized field named `<panel>Instance`, instantiate it once under `uiRoot` (or `overlayRoot` for overlays) when first shown, and perform event binding, display, hiding, and cleanup against the runtime instance only. Never treat a serialized prefab asset reference as the live UI instance or parent runtime objects beneath it.

## Verification

- Do not build, run tests, launch Unity, or perform other verification unless the user explicitly asks for it.
- Do not run extra inspection, diff, status, formatting, or diagnostic commands after editing unless they are necessary for the specifically approved change or explicitly requested.

## Git

- Do not stage or commit changes without the user's explicit permission for that specific action.
