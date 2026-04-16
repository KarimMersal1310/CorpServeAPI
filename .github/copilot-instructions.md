# Copilot Instructions

## Project Guidelines
- User prefers real AI-powered estimation for request estimates instead of rule-based/calculated heuristics.
- User prefers using Gemini free tier for AI estimation during development/testing.
- User prefers implementing the requirement for persisting generated estimate data in the `CreateRequest` flow, rather than only in the `GenerateEstimate` flow.
- User prefers centralized constants for notification titles/types/event keys.
- User prefers stricter transaction wrapping for multi-step business flows.
- For profile-related changes, prefer service-layer logic with simpler relation-based queries and avoid adding or modifying domain entities unless explicitly required.

## Code Organization
- User prefers separating client and vendor service methods into distinct #region blocks in both interfaces and implementing classes.