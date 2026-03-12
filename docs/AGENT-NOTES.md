# Agent Notes

## Purpose

This file keeps a compact repository-side summary of planning discussions with the coding agent. It exists so the reasoning is preserved outside chat history.

## Bootstrap summary

Initial planning centered on a few explicit choices:

- treat the assignment as backend-first
- keep the mandatory scope focused on correctness, persistence, concurrency, tests, and minimal UI
- use ASP.NET Core, Blazor, PostgreSQL, and Docker Compose
- avoid full authentication and use a self-declared participant identifier instead
- enforce critical rules with database constraints plus transactions

## Backlog shaping

The implementation plan was kept intentionally small:

- bootstrap and local run path
- persistence and initialization
- transactional scratch flow and audit trail
- minimal UI
- database-backed tests
- final documentation

Stretch work was split into separate issues only after the mandatory scope was in place.

## Why notes are split

- `README.md` covers setup and usage
- `docs/DESIGN.md` covers architecture and trade-offs
- `docs/AI-NOTES.md` is reviewer-facing and concise
- this file keeps the more process-oriented planning summary
