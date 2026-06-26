# TDD-KITT gated project

This project is gated by the **TDD-KITT harness**: it enforces red -> green -> refactor by checking your edits
and commands through a preToolUse hook. **When it blocks something, the message is coaching, not an error** -
read it and adjust; don't try to work around it.

## What we're really after
Excellent, simple, well-designed code - minimal, clear, no more than the problem needs. TDD and the quality
checks are the means. Test code is held to the same bar as production code.

## Design expectations - domain-driven, test-driven
- **Build the DOMAIN model FIRST.** Evolve the domain - value objects, enums, the rules/invariants, the
  behavior - driven by **fast tests**, BEFORE you add the UI or the persistence/DB layer. The REST endpoints,
  the HTML form, and file/DB storage are *adapters around* the domain: write them AFTER the domain exists, not
  first. If your first test needs the web host or the database to run, you started at the wrong layer.
- **Model business concepts as domain types / value objects.** Don't carry domain meaning in bare primitives:
  a closed set (a fixed list of allowed values) wants a type, not a `string`; a constrained number wants a
  type that enforces its rule, not a bare `decimal`.
- **Put the rules in the domain**, where a fast test can reach them - not only at the HTTP/UI boundary. If you
  can't write a **fast** test for a rule without spinning up the web host or the database, the rule is in the
  wrong place. The harness asks for fast domain microtests of **real behavior** - you can't write one against
  an anemic bag of primitives (a getter/round-trip of a primitive doesn't count), so this pushes you to model
  a real domain.

## How to work
- One story at a time. Drive the current story to done, then stop. Don't build ahead.
- Red -> green -> refactor, in small steps. Write one small failing test; make it pass with the least code that
  works; then refactor under green.
- Don't design ahead - write only enough to pass the current failing test. No speculative classes, fields,
  methods, or abstractions. If nothing uses it yet, it shouldn't exist yet.

## Running tests - ALWAYS via the harness shim
Run the suite through the shim - this exact command - never the test runner directly (it is blocked):

    node "C:\agentic-engineering\AgenticTDDOrderProcessor-Problem-CSharp\node_modules\tdd-kitt\src\harness\hooks\cli.mjs" run test

The shim runs the tests, records the result, and tells you what to do next.

## Refactoring is behavior-preserving
A refactoring is a behavior-preserving transformation - it never adds or changes behavior. If an idea would,
it is NOT a refactoring: make it your next failing test instead. A large refactoring (e.g. introducing a
pattern) is done as a SEQUENCE of small, safe, behavior-preserving steps, each re-greened - not deferred.

## The refactor-review checkpoint
After tests go green, ask: what would you improve about the design of what you just changed? Of your
behavior-preserving refactoring ideas, do the one you judge most valuable now (in small safe steps, re-run the
shim green after each), then acknowledge what you improved:

    node "C:\agentic-engineering\AgenticTDDOrderProcessor-Problem-CSharp\node_modules\tdd-kitt\src\harness\hooks\cli.mjs" refactor-reviewed "extracted the shared status guard"

If nothing's worth improving, say why in the headline (e.g. "Order is minimal; no change needed"). This
unblocks the next test.

## Do the work in this session
Don't delegate to subagents - they run outside the harness's checks, so their edits wouldn't be gated.

## Design Suspects
When the harness escalates (you've had several green cycles without addressing a design problem), you must
name one of these suspects and say what you did about it. Format: `<id>: <what I did>`

- **data-clump**: the same fields always travel together — they're a concept waiting to be named → extract into a value object
- **subtle-duplication**: two pieces of code express the same thing but look different → find the common concept and unify them
- **duplicated-shape**: the same field-set reappears under different names (Dto/Request/Response) → unify on one type
- **primitive-obsession**: a value with rules (status, color, money) is a bare string/int, rules live at the boundary → make it a type that owns its rules
- **anemic-domain**: types are data bags; behavior lives in services/handlers → move it onto the type that owns the data
- **scattered-conditional**: the same field branched on across many methods (a creeping state machine) → centralize with State/Strategy/guard
- **conditional-complexity**: a method has too many branches making it hard to follow → extract predicates or replace with polymorphism
- **persistence-in-domain**: a domain type owns state AND touches File/DB/HTTP/clock → push I/O behind a port
- **untested-invariants**: value-object guards have no test while boundaries get re-tested → add fast domain microtests for the rules
- **all-boundary-suite**: behavior verified only through slow HTTP; no fast domain microtests → drive the domain directly with a test double
- **overcrowded**: a method, class, or file has too many responsibilities crammed in → extract and separate concerns
- **implementation-coupled-tests**: tests assert on structure; a behavior-preserving refactor breaks them → test what the code does, not how
- **missing-test-helper**: the same test setup or assertion is repeated across tests → extract a shared builder or helper
- **misplaced-responsibility**: a method works mostly with another object's data → move it to the object it envies
