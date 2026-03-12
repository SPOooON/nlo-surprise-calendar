# AGENTS.md

## Purpose

This repository is worked on with coding agents.
The goal is to keep all work transparent, reviewable, and easy to continue by a human or another agent.

`AGENTS.md` defines workflow and repository process rules only.
Product requirements, architecture, and assignment-specific details must live in `README.md` and `docs/*`.

## Working Principles

- Prefer clear, reviewable progress over large opaque changes.
- Keep changes small, scoped, and easy to reason about.
- Favor pragmatic solutions over unnecessary complexity.
- Do not introduce complexity unless it clearly supports a documented requirement.
- Make trade-offs explicit in repository documentation.
- Keep important knowledge in the repo, not in chat memory.

## Agent Permissions

Agent may freely:
- edit files
- add new files
- run local tests
- create and switch branches
- commit to feature branches
- push to feature branches
- open or update pull requests
- comment on issues and PRs

Agent must ask before:
- deleting files
- changing CI/CD workflows
- adding or upgrading dependencies
- touching secrets or credentials
- enabling network access
- making destructive data or schema changes
- modifying infrastructure in a way that creates external cost or risk

## Mandatory Git Workflow

- Never commit or push directly to `main`.
- Always branch from the latest `main`.
- Use branch names like:
  - `chore/<short-name>`
  - `feature/<short-name>`
  - `fix/<short-name>`
- Rebase feature branches onto the latest `origin/main` before opening or updating a PR.
- Keep PRs focused on one issue or one coherent change.
- Do not merge your own PR unless explicitly instructed.

## Commit Rules

- Use small, descriptive commits.
- Prefer conventional commit prefixes when applicable:
  - `chore:`
  - `feat:`
  - `fix:`
  - `test:`
  - `docs:`
  - `ci:`
- Commit messages should explain both the change and the reason when useful.

## Issue-Driven Work

Work should be tracked through GitHub issues whenever possible.

When starting work:
1. Read the issue fully.
2. If requirements are unclear, incomplete, or conflicting, ask clarification questions in the issue comments.
3. Wait for clarification before proceeding when missing information could affect implementation.
4. Create a feature branch.
5. Implement the task within scope.
6. Open or update a PR referencing the issue.

## Scope Discipline

- Keep changes strictly within the scope of the current task.
- Do not include unrelated refactors, dependency upgrades, naming sweeps, folder reorganizations, formatting sweeps, or architectural changes in the same branch/PR.
- If unrelated improvements are identified, document them and create a separate issue instead of folding them into the current work.

## Documentation Workflow

- `AGENTS.md` contains workflow/process rules only.
- Product documentation lives in `README.md` and `docs/*`.
- When a change affects functionality, UX behavior, requirements, technical constraints, architecture, operations, or security assumptions, update the relevant documentation in the same branch/PR.
- Keep documentation aligned with implementation.
- Keep `README.md` links and references to `docs/*` accurate.

When documenting a meaningful decision, include where relevant:
- the decision
- the reason
- the impact
- important trade-offs
- rejected alternatives if relevant

## Task Memory And Source Of Truth

- Agent working memory is scoped to the current issue/task.
- Assume chat/session memory is temporary and may be lost.
- Write important discoveries back to repository sources of truth:
  - `README.md`
  - `docs/*`
  - code comments where useful
  - issue comments
  - PR descriptions or comments
- The repository, issue history, and PR history are the source of truth, not chat/session memory.

## Code Change Expectations

- Prefer straightforward, boring, maintainable code.
- Keep naming and structure predictable.
- Avoid speculative abstractions.
- Keep the application runnable locally unless the task explicitly says otherwise.
- If something is intentionally omitted or simplified, document it.

## Testing Expectations

- Add automated tests for new or changed logic where practical.
- Cover both success and failure paths.
- Prefer fast, deterministic tests.
- Keep test data close to the tests that use it.
- If behavior is not tested, state why.

## Required Local Validation Before Push

Run relevant validation from the repository root before pushing:

- restore dependencies
- build in Release
- run automated tests

For .NET repositories, use:

- `dotnet restore`
- `dotnet build -c Release`
- `dotnet test -c Release`

If the repository uses other tooling, run the equivalent project-standard checks as well.

## Self-Review Before Push Or PR

Before any `git push` or PR creation/update:

1. Review the working tree and staged diff.
2. Check for:
   - bugs
   - edge cases
   - unnecessary complexity
   - naming inconsistencies
   - missing or weak tests
   - documentation drift
   - deviations from repository conventions
3. Apply safe fixes found during self-review.
4. Re-run required validation after fixes.

Expected workflow order:
1. understand task
2. implement change
3. run validation
4. self-review
5. apply safe fixes
6. run validation again
7. push branch
8. open or update PR

## Pull Request Expectations

PRs should be easy to review and explain both what changed and why.

PR description should include:
- what changed
- how it was validated
- any known limitations
- follow-up work if relevant
- issue reference such as `Fixes #<issue-number>` when applicable

## PR Review Responses

- Respond directly to each review thread with a specific resolution update.
- Do not replace threaded replies with a single generic PR-level comment.

## Diagrams

- Keep diagram source files in `diagrams/` when the repository uses diagrams.
- Keep diagrams aligned with implementation and documentation.
- Prefer Mermaid unless there is a strong reason not to.

## Definition Of Done

A task is only done when, where relevant:
- code is updated
- tests are added or updated
- documentation is updated
- diagrams are updated if architecture or deployment changed
- deferred work is captured explicitly
- the branch is validated and ready for review
