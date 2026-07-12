# Collaboration Rules

## Architecture First

- The primary task is discussing and agreeing on architecture with the user.
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
- Do not edit Unity serialized assets or editor-authored setup files, including `.unity`, `.prefab`, `.asset`, `.mat`, `.controller`, `.anim`, `.meta`, project settings, package files, or generated Unity files.
- Implementation work is code-only unless the user explicitly changes this rule. When Unity setup is required, explain the exact manual Editor steps instead of modifying serialized Unity files.

## User Review

- The user must be able to review and approve every proposed change before it is made.
- Keep proposals concrete and concise to avoid wasting the user's time and token usage.
- When the user asks for big steps, concepts, architecture pictures, strategy, or high-level planning, answer at that level only. Do not provide file lists, exact code, class names, method names, implementation steps, or other low-level details unless the user explicitly asks for implementation detail.
- Preserve the user's sense of ownership and learning. Do not hand over large prebuilt implementations or hide design choices inside generated code; explain the next concept, let the user make the design decision, challenge tradeoffs, and implement only the smallest approved step.
- When providing code snippets, always state the exact owning class and function or method they belong to; do not give loose snippets detached from implementation context.
- Do not hide implementation decisions behind broad summaries such as "wiring," "supporting changes," or "affected files."
- After editing code, check whether any touched class, method, function, field, file, or serialized API name no longer matches its current responsibility or behavior. If a name has become stale or misleading, explicitly call it out and propose the rename before continuing.
- After editing, report exactly what changed and do not claim verification that was not performed.

## Production-Grade Quality

- Do not implement hacks, throwaway code, temporary workarounds, quick fixes, cheap fixes, or knowingly brittle solutions.
- Do not hard-code feature-specific behavior inside generic managers, orchestrators, state machines, or shared systems.
- Do not introduce magic values or presentation tuning directly in orchestration or gameplay logic; place approved tuning in the appropriate configuration or owning view system.
- For responsive UI, do not hard-code screen/layout-dependent numbers such as pixel offsets, margins, breakpoints, widths, heights, or scale constants in code.
- Responsive UI sizing and positioning must come from Unity layout, anchors, RectTransform bounds, aspect tools, or explicitly serialized/configured values owned by the relevant view.
- Do not add speculative layout compensation values such as hidden margins, overlap pixels, or seam fixes before the visual problem is observed and approved.
- Never silently ignore invalid runtime state, failed assumptions, or unexpected null references.
- Never soften an error or missing required data by silently providing a fallback, default, placeholder, or alternate path.
- Error states must be loud and visible. If a fallback is being considered, stop and ask the user for explicit approval, making the fallback question highly visible.
- Serialized fields and scene/prefab setup references are non-null by contract. Do not write runtime null checks, `?.` guards, fallback lookup, custom `Debug.LogError`, or recovery paths for missing serialized setup.
- Use serialized fields directly. If setup is broken, let Unity/C# surface the failure naturally.
- Null checks are allowed only for real nullable runtime state or data, such as optional method arguments, cached instances, active tweens, lookup results, or intentionally absent content.
- Require clear ownership, maintainable boundaries, explicit data flow, and an appropriate extension path for foreseeable production use.
- Prefer the simplest production-grade solution, not the shortest implementation and not speculative overengineering.
- Before implementation, identify any coupling, special-case dispatch, type checks, or volatility introduced by the design so the user can review it explicitly.
- If only a brittle or temporary solution is currently possible, stop and explain the limitation instead of coding it.

## Code Formatting

- Keep function calls and signatures compact. Never format parameters or arguments one per line.
- Library calls, such as DOTween APIs, are exempt when the library's conventional formatting places each parameter or argument on its own line for readability.

## Verification

- Do not build, run tests, launch Unity, or perform other verification unless the user explicitly asks for it.
- Do not run extra inspection, diff, status, formatting, or diagnostic commands after editing unless they are necessary for the specifically approved change or explicitly requested.

## Git

- Do not stage or commit changes without the user's explicit permission for that specific action.
