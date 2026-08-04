# Collaboration Rules

## Architecture First

- The primary task is discussing and agreeing on architecture with the user.
- When the user asks to edit only Markdown files, apply the requested Markdown changes directly without asking for file-scope confirmation. Continue to require explicit scope confirmation before editing code. All other project restrictions remain in effect.
- When the user asks to create or edit code under `Assets/Editor`, apply the requested Editor-script changes directly without architecture or file-scope approval. All other project restrictions remain in effect.
- When the user explicitly requests a comment-only code edit, including adding, changing, or removing a TODO, apply it directly without architecture or file-scope approval.
- When the user explicitly says `skip scope`, treat it as approval to implement the requested change immediately without presenting an implementation scope or asking for confirmation. All other project restrictions remain in effect.
- When the user has explicitly authorized an ongoing redesign or iterative improvement of a named prefab or serialized UI asset, treat subsequent requests in the same conversation to apply, continue, simplify, tune, or redo that design as approval to edit that same asset. Do not repeat architecture or file-scope checkpoints for each iteration. Stop for renewed approval only if the work expands to another file, code, scenes, behavior, a new type, or a materially different feature goal.
- Do not edit production code, tests, prefabs, scenes, packages, configuration, or other project files until the user explicitly accepts the proposed architecture.
- Before implementation, explain the proposed responsibilities, ownership, boundaries, and data flow, including meaningful alternatives and tradeoffs.
- Before editing, list every file that will be created, modified, or deleted and describe the exact class, method, API, and behavior changes planned for each file.
- Wait for the user to explicitly approve that exact implementation scope. Never infer approval from enthusiasm, general agreement, or approval of a broader concept.
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

## User Review

- Except for the Markdown, `Assets/Editor`, and ongoing named-prefab/serialized-UI redesign exemptions above, the user must be able to review and approve every proposed change before it is made.
- Keep proposals concrete and concise to avoid wasting the user's time and token usage.
- When the user asks for big steps, concepts, architecture pictures, strategy, or high-level planning, answer at that level only. Do not provide file lists, exact code, class names, method names, implementation steps, or other low-level details unless the user explicitly asks for implementation detail.
- Preserve the user's sense of ownership and learning. Do not hand over large prebuilt implementations or hide design choices inside generated code; explain the next concept, let the user make the design decision, challenge tradeoffs, and implement only the smallest approved step.
- Whenever describing current code behavior, always link every behavioral claim to the exact code that implements it. Do not describe behavior without a direct file-and-line link to its implementation.
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
- For responsive UI, do not hard-code screen/layout-dependent numbers such as pixel offsets, margins, breakpoints, widths, heights, or scale constants in code.
- Responsive UI sizing and positioning must come from Unity layout, anchors, RectTransform bounds, aspect tools, or explicitly serialized/configured values owned by the relevant view.
- Do not add speculative layout compensation values such as hidden margins, overlap pixels, or seam fixes before the visual problem is observed and approved.
- Never silently ignore invalid runtime state, failed assumptions, or unexpected null references.
- Never soften an error or missing required data by silently providing a fallback, default, placeholder, or alternate path.
- Error states must be loud and visible. If a fallback is being considered, stop and ask the user for explicit approval, making the fallback question highly visible.
- Serialized fields and scene/prefab setup references are non-null by contract. Do not write runtime null checks, `?.` guards, fallback lookup, custom `Debug.LogError`, or recovery paths for missing serialized setup.
- Component dependencies must be explicit serialized fields unless the exact component type is enforced on the same GameObject by `[RequireComponent(typeof(T))]`; in that case, assigning it with `GetComponent<T>()` is allowed. Never use `GetComponentInChildren`, `GetComponentInParent`, or an unenforced `GetComponent` lookup for required dependencies.
- Use serialized fields directly. If setup is broken, let Unity/C# surface the failure naturally.
- Null checks are allowed only for real nullable runtime state or data, such as optional method arguments, cached instances, active tweens, lookup results, or intentionally absent content.
- Require clear ownership, maintainable boundaries, explicit data flow, and an appropriate extension path for foreseeable production use.
- Prefer the simplest production-grade solution, not the shortest implementation and not speculative overengineering.
- Before implementation, identify any coupling, special-case dispatch, type checks, or volatility introduced by the design so the user can review it explicitly.
- If only a brittle or temporary solution is currently possible, stop and explain the limitation instead of coding it.

## Code Formatting

- Keep function calls and signatures compact. Never format parameters or arguments one per line.
- Library calls, such as DOTween APIs, are exempt when the library's conventional formatting places each parameter or argument on its own line for readability.

## Code Organization

- Declare every project enum in `Assets/Scripts/Shared/Enum.cs`. Do not create standalone enum files or declare enums inside other project types.
- Do not explicitly use the `internal` keyword in project-owned code until the project defines its own assembly boundaries with `.asmdef` files. Leave third-party and vendor code unchanged.
- `GameFlowController` is always an application-level orchestrator. It may order flow transitions and make semantic calls to the systems that own work, but it must not own feature configuration, presentation assets or tuning, domain logic, or subsystem implementation. Put those responsibilities in their dedicated owning flow, service, director, or presentation component.
- Follow the established `UIManager` panel convention: serialize a prefab field named `<panel>Prefab`, keep the spawned runtime component in a separate non-serialized field named `<panel>Instance`, instantiate it once under `uiRoot` (or `overlayRoot` for overlays) when first shown, and perform event binding, display, hiding, and cleanup against the runtime instance only. Never treat a serialized prefab asset reference as the live UI instance or parent runtime objects beneath it.

## Verification

- Do not build, run tests, launch Unity, or perform other verification unless the user explicitly asks for it.
- Do not run extra inspection, diff, status, formatting, or diagnostic commands after editing unless they are necessary for the specifically approved change or explicitly requested.

## Git

- Do not stage or commit changes without the user's explicit permission for that specific action.
