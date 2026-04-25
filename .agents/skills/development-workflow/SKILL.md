---
name: development-workflow
description: Core development workflow guidelines for ConsultoresAPP. ALWAYS use this skill when starting any new development task, bug fix, feature implementation, or code change. This skill ensures you leverage all available MCP servers and project skills for optimal productivity, code quality, and architectural consistency. Use this whenever you're about to write or modify code, before creating components, implementing APIs, or working with the codebase.
---
# Development Workflow

This skill defines the core development workflow for ConsultoresAPP, ensuring you leverage all available tools, follow best practices, and maintain architectural consistency across the full-stack application.

## Why This Workflow Matters

ConsultoresAPP is a sophisticated multi-tenant SaaS platform with:

- **Backend**: .NET 10 Clean Architecture + CQRS + DDD + Event-driven design
- **Frontend**: Angular 21 + PrimeNG + Tailwind CSS + i18n
- **Infrastructure**: Docker, Kubernetes, RabbitMQ, Redis, PostgreSQL, Stripe

The project has invested in powerful tools (MCP servers) and documented patterns (skills) to accelerate development, maintain consistency, and reduce cognitive load. **Using these tools isn't optional—it's how this project works efficiently.**

## Available MCP Servers

The project has `.cursor/mcp.json` configured with these MCP servers. **Always check which MCP server can help with your task BEFORE implementing from scratch:**

### 1. Context7 (upstash/context7)

- **Purpose**: Retrieve up-to-date documentation for ANY library
- **When to use**: Before using unfamiliar libraries, checking API changes, finding code examples
- **Tools**: `query-docs`, `resolve-library-id`
- **Example**: "Get latest PrimeNG Table API documentation" or "Show Stripe webhook examples"
- **Why**: Prevents outdated patterns, provides authoritative examples, saves research time

### 2. Serena (oraios/serena)

- **Purpose**: Semantic code understanding and navigation
- **When to use**: Understanding codebase structure, finding cross-references, symbol-level editing
- **Tools**: `find_symbol`, `find_referencing_symbols`, `search_for_pattern`, `replace_symbol_body`, `insert_after_symbol`, `get_symbols_overview`
- **Example**: "Find all IRequestHandler implementations" or "Show me all references to ITenantContext"
- **Why**: Avoids reading entire files, understands relationships, enables precise edits, discovers patterns

### 3. Redis MCP Server

- **Purpose**: Direct Redis operations for caching layer
- **When to use**: Debugging cache issues, inspecting stored data, managing keys
- **Tools**: `get`, `set`, `delete`, `scan_keys`, `hgetall`, `json_get`
- **Example**: "Check what's cached for tenant:acme" or "Clear cache for subscription data"
- **Why**: Direct access to caching layer, faster debugging, no need for Redis CLI

### 4. Angular CLI MCP Server

- **Purpose**: Angular project management and best practices
- **When to use**: Generating components, checking Angular standards, understanding project structure
- **Tools**: `list_projects`, `get_best_practices`, `search_documentation`, `find_examples`
- **Example**: "Get Angular 21 signal-based component best practices" or "Generate standalone component pattern"
- **Why**: Version-specific guidance, consistent code generation, Angular official patterns

### 5. PrimeNG MCP Server

- **Purpose**: PrimeNG component documentation and examples
- **When to use**: Implementing UI components, checking API, finding usage examples
- **Tools**: `get_component`, `get_component_props`, `get_usage_example`, `find_components_with_feature`, `suggest_component`
- **Example**: "Get PrimeNG Table with filtering example" or "Show DatePicker props"
- **Why**: Component-specific guidance, props discovery, working examples, migration paths

### 6. Chrome DevTools MCP Server

- **Purpose**: Browser automation and testing
- **When to use**: E2E testing, debugging UI, performance analysis
- **Tools**: `navigate_page`, `click`, `fill`, `take_screenshot`, `evaluate_script`, `lighthouse_audit`
- **Example**: "Test login flow" or "Audit performance of dashboard page"
- **Why**: Automates manual testing, captures visual regressions, performance insights

### 7. Stripe MCP Server

- **Purpose**: Stripe payment integration and management
- **When to use**: Working with payments, subscriptions, webhooks, billing
- **Tools**: `search_stripe_documentation`, `list_subscriptions`, `create_payment_link`, `retrieve_balance`
- **Example**: "Get webhook signature verification docs" or "List active subscriptions"
- **Why**: Official Stripe patterns, reduces API errors, webhook testing

## Available Project Skills

The project has documented skills in `.claude/skills/`. **Always check if an existing skill covers your task BEFORE reinventing the wheel:**

### 1. frontend-design

- **Purpose**: Build production-grade Angular UI components, pages, features
- **When to use**: ANY frontend work—components, pages, screens, views, forms, tables, modals, layouts
- **Triggers**: "component", "page", "screen", "view", "form", "table", "modal", "layout", "frontend"
- **Stack**: Angular 21+, PrimeNG, Tailwind CSS, i18n, signals, standalone components
- **Why**: Ensures consistency, follows project patterns, handles i18n, uses established design system

### 2. cqrs-endpoint

- **Purpose**: Implement Clean Architecture CQRS endpoints
- **When to use**: Creating new API endpoints, commands, queries, handlers
- **Triggers**: "endpoint", "API", "command", "query", "handler", "use case"
- **Why**: Maintains Clean Architecture, ensures proper separation of concerns, consistent error handling

### 3. test-driven-development

- **Purpose**: TDD workflow for features and bug fixes
- **When to use**: Before implementing ANY feature or bugfix
- **Triggers**: "implement feature", "fix bug", "add functionality"
- **Why**: Prevents regressions, documents behavior, enables refactoring with confidence

### 4. advanced-typescript

- **Purpose**: Advanced TypeScript patterns and best practices
- **When to use**: Complex type definitions, generic types, type guards, utility types
- **Triggers**: "type definition", "generic", "type safety", "TypeScript patterns"
- **Why**: Maintains type safety, prevents runtime errors, leverages TypeScript power

## The Development Plan Workflow

Follow this workflow for EVERY development task:

### Phase 1: Understand (Leverage Serena + Context7)

1. **Use Serena to explore the codebase:**

   - `find_symbol` to locate relevant classes/interfaces
   - `find_referencing_symbols` to understand dependencies
   - `search_for_pattern` to discover existing patterns
   - `get_symbols_overview` to understand file structure
2. **Use Context7 to get up-to-date documentation:**

   - Query docs for any libraries you'll be using
   - Get examples of patterns you need to implement
   - Check for breaking changes or new APIs

**Why**: Building on existing patterns is 10x faster than creating from scratch. Serena finds those patterns without reading entire files. Context7 ensures you're using current best practices.

### Phase 2: Plan (Use Available Skills)

3. **Check which skill applies to your task:**

   - Frontend work? → **Use `frontend-design` skill**
   - Backend API? → **Use `cqrs-endpoint` skill**
   - Implementing feature? → **Use `test-driven-development` skill first**
   - Complex types? → **Use `advanced-typescript` skill**
4. **Break down the task into steps:**

   - What needs to be created? (entities, handlers, components)
   - What needs to be modified? (existing files, configurations)
   - What tests need to be written? (unit tests, integration tests)

**Why**: Skills encode project-specific patterns that took months to establish. Following them ensures consistency, reduces review time, and prevents architectural drift.

### Phase 3: Implement (Follow Skill Instructions)

5. **Follow the relevant skill's instructions:**

   - Read the skill's SKILL.md completely before starting
   - Follow the documented patterns and structure
   - Use the skill's recommended tools and approaches
6. **Use MCP servers during implementation:**

   - **Serena**: For precise symbol-level edits (`replace_symbol_body`, `insert_after_symbol`)
   - **Angular CLI**: For component generation and best practices
   - **PrimeNG**: For UI component implementation
   - **Context7**: For API documentation lookups

**Why**: Skills prevent common mistakes and ensure you're implementing features the way the project expects. MCP servers accelerate implementation by providing instant access to docs and semantic code operations.

### Phase 4: Verify (Use Testing Tools)

7. **Run tests to verify your changes:**

   - Unit tests for business logic
   - Integration tests for API endpoints
   - E2E tests for UI flows (use Chrome DevTools MCP)
8. **Check for errors:**

   - Use `get_errors` tool to validate compilation
   - Check Redis cache behavior (use Redis MCP)
   - Verify Stripe integration (use Stripe MCP)

**Why**: Automated verification catches issues before code review. MCP servers enable quick checks without switching contexts.

### Phase 5: Document (If Creating New Patterns)

9. **If you've created a reusable pattern:**
   - Consider creating a new skill (use `skill-creator` skill)
   - Document in the relevant skill's references folder
   - Update project documentation

**Why**: The next developer (or future you) should be able to leverage your work as easily as you leveraged existing patterns.

## Critical Principles

### 1. Tool-First Mindset

Before writing ANY code:

- "Can Serena find existing similar code?"
- "Does Context7 have docs/examples for this?"
- "Which skill covers this type of work?"

**Why**: The tools exist to save time. Using them is faster than reinventing solutions.

### 2. Pattern Reuse Over Creation

If Serena finds 5 similar handlers:

- Study them with `find_symbol` + `find_referencing_symbols`
- Follow their structure exactly
- Don't create new patterns without justification

**Why**: Consistency reduces cognitive load. Every new pattern increases maintenance cost.

### 3. Skills Are Your Playbook

Skills aren't suggestions—they're the project's documented way of working:

- **frontend-design** dictates UI component structure
- **cqrs-endpoint** dictates API architecture
- **test-driven-development** dictates testing approach

**Why**: Skills encode lessons learned and prevent repeated mistakes. Ignoring them causes rework.

### 4. MCPs Are Your Research Team

Don't guess about APIs or patterns:

- Context7 has current documentation for ANY library
- Serena has deep understanding of THIS codebase
- Redis MCP shows actual cached data
- Stripe MCP shows real payment state

**Why**: Guessing causes bugs. MCPs provide authoritative answers instantly.

## Common Workflows

### Creating a New Feature

1. **Check test-driven-development skill** → Write tests first
2. **Use Serena** → Find similar features (`search_for_pattern` for handlers/components)
3. **Check relevant skill**:
   - Frontend? → **frontend-design** skill
   - Backend? → **cqrs-endpoint** skill
4. **Use MCP servers**:
   - Context7 for library docs
   - Angular CLI for Angular patterns
   - PrimeNG for UI components
5. **Implement following skill instructions**
6. **Verify with tests**

### Fixing a Bug

1. **Use Serena** → Find the buggy code (`find_symbol`, `find_referencing_symbols`)
2. **Check test-driven-development skill** → Write failing test first
3. **Use MCP servers for context**:
   - Redis MCP for cache issues
   - Stripe MCP for payment issues
   - Context7 for library behavior questions
4. **Fix with Serena** → Use `replace_symbol_body` for precise edits
5. **Verify tests pass**

### Adding a UI Component

1. **Use frontend-design skill** → This is the authority for UI work
2. **Use PrimeNG MCP** → Get component docs (`get_component`, `get_usage_example`)
3. **Use Angular CLI MCP** → Get Angular 21 best practices
4. **Use Context7** → Get examples for any other libraries needed
5. **Implement following frontend-design skill pattern**
6. **Test with Chrome DevTools MCP if needed**

### Implementing an API Endpoint

1. **Use cqrs-endpoint skill** → This is the authority for backend APIs
2. **Use Serena** → Find similar endpoints (`search_for_pattern` for "IRequestHandler")
3. **Check test-driven-development skill** → Write tests first
4. **Use Context7** → Get docs for any libraries (Stripe, MediatR, etc.)
5. **Implement following cqrs-endpoint skill pattern**
6. **Verify with tests**

### Working with Payments

1. **Use Stripe MCP** → Get official Stripe docs and examples
2. **Use Serena** → Find existing payment handlers
3. **Use cqrs-endpoint skill** → Follow API structure
4. **Use test-driven-development skill** → Write tests with mocks
5. **Test with Stripe CLI** + **Stripe MCP** → Verify webhook handling

## Anti-Patterns to Avoid

### ❌ Don't: Write code without checking existing patterns

**Why**: You'll create inconsistent code that doesn't match project conventions, causing review delays and rework.

### ❌ Don't: Ignore available skills

**Why**: Skills encode hard-learned lessons. Ignoring them means repeating mistakes others already solved.

### ❌ Don't: Implement without understanding the context

**Why**: You might duplicate existing functionality or break dependencies. Serena shows the full context.

### ❌ Don't: Guess at library APIs

**Why**: Context7 has up-to-date docs. Guessing causes bugs and outdated patterns.

### ❌ Don't: Read entire files to understand structure

**Why**: Serena's semantic tools show structure and relationships efficiently. Reading full files wastes time and context window.

### ❌ Don't: Skip test-driven-development skill

**Why**: Writing tests after implementation catches fewer bugs and creates less confidence in changes.

### ❌ Don't: Create new architectural patterns without team discussion

**Why**: Consistency matters more than cleverness. Stick to established patterns unless there's a compelling reason.

## Workflow Summary

```
1. UNDERSTAND (Serena + Context7)
   ↓
2. PLAN (Check Skills)
   ↓
3. IMPLEMENT (Follow Skill + Use MCPs)
   ↓
4. VERIFY (Run Tests + Check Errors)
   ↓
5. DOCUMENT (If New Pattern)
```

## Remember

You have a powerful toolkit:

- **7 MCP servers** providing instant access to docs, cache, payments, code structure, UI components, and more
- **4+ project skills** encoding the team's established patterns and workflows
- **Semantic code tools** (Serena) that understand your codebase deeply

**Using these tools isn't extra work—it IS the work.** They exist to make you faster, more consistent, and more effective.

Every time you're about to implement something:

1. Can an MCP server help? (Usually yes)
2. Does a skill cover this? (Check first)
3. Are you following the plan? (Don't skip steps)

This workflow exists because it works. Follow it, and you'll ship features faster with better quality.
