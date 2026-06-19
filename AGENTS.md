# Collaboration Rules

## Architecture First

- The primary task is discussing and agreeing on architecture with the user.
- Do not edit production code, tests, prefabs, scenes, packages, configuration, or other project files until the user explicitly accepts the proposed architecture.
- Before implementation, explain the proposed responsibilities, ownership, boundaries, and data flow, including meaningful alternatives and tradeoffs.
- Before editing, list every file that will be created, modified, or deleted and describe the exact class, method, API, and behavior changes planned for each file.
- Wait for the user to explicitly approve that exact implementation scope. Never infer approval from enthusiasm, general agreement, or approval of a broader concept.
- Do not create a new class, interface, file, component, manager, handler, abstraction, or data type unless the user has seen and explicitly approved it.
- Do not add supporting changes, cleanup, formatting, refactoring, validation, defensive code, or architectural structure that was not explicitly included in the approved scope.
- If implementation reveals any unapproved requirement or design decision, stop immediately, explain it, and wait for new approval before editing further.
- Implement only the approved changes. Do not make adjacent or opportunistic changes.
- Discussion, inspection, and read-only analysis do not authorize implementation.
- A request to explore, review, or discuss an idea is not approval to edit code.
- Only begin editing after the user clearly approves the architecture and asks for implementation.
- If the architecture changes during implementation, stop editing and return to architecture discussion for renewed approval.

## User Review

- The user must be able to review and approve every proposed change before it is made.
- Keep proposals concrete and concise to avoid wasting the user's time and token usage.
- Do not hide implementation decisions behind broad summaries such as "wiring," "supporting changes," or "affected files."
- After editing, report exactly what changed and do not claim verification that was not performed.

## Production-Grade Quality

- Do not implement hacks, throwaway code, temporary workarounds, quick fixes, cheap fixes, or knowingly brittle solutions.
- Do not hard-code feature-specific behavior inside generic managers, orchestrators, state machines, or shared systems.
- Do not introduce magic values or presentation tuning directly in orchestration or gameplay logic; place approved tuning in the appropriate configuration or owning view system.
- Require clear ownership, maintainable boundaries, explicit data flow, and an appropriate extension path for foreseeable production use.
- Prefer the simplest production-grade solution, not the shortest implementation and not speculative overengineering.
- Before implementation, identify any coupling, special-case dispatch, type checks, or volatility introduced by the design so the user can review it explicitly.
- If only a brittle or temporary solution is currently possible, stop and explain the limitation instead of coding it.

## Verification

- Do not build, run tests, launch Unity, or perform other verification unless the user explicitly asks for it.
- Do not run extra inspection, diff, status, formatting, or diagnostic commands after editing unless they are necessary for the specifically approved change or explicitly requested.

## Git

- Do not stage or commit changes without the user's explicit permission for that specific action.
