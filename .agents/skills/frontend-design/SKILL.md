---
name: frontend-design
description: Build production-grade Angular UI — components, pages, features, or full views — for this project's stack (Angular 21+, PrimeNG, Tailwind CSS, i18n). Use this skill whenever the user asks to create or update any frontend piece: a component, a page, a screen, a view, a form, a table, a modal, a dialog, a layout, a list, a dashboard, or any visual UI element. Always triggers when the user mentions "component", "page", "screen", "view", "form", "table", "modal", "layout", or "frontend".
---

This skill guides the construction of polished, maintainable Angular UI for this project. Every piece of UI must respect the project's non-negotiable constraints listed below **before** any design decision is made.

---

## Non-Negotiable Rules

### Rule 1 — No Direct CSS
- **Never** write raw CSS, inline `style=""` attributes, `styles`, or `styleUrls` in any component.
- All styling is done exclusively through **Tailwind utility classes** in the HTML template.
- The only permitted exception is in designated **custom/branded pages** (features under `features/custom/`, e.g. `example-agency`, `example-property-detail`). Even there, raw CSS is a **last resort** — try Tailwind first. Only reach for custom CSS when a branded effect (unique keyframe animation, very precise pixel value, complex clip-path, etc.) is truly impossible with Tailwind utilities.

### Rule 2 — PrimeNG First — Always Use the MCP
Before building any UI element from scratch, query the PrimeNG MCP tools to find the right component. Do this before writing a single line of code.

| Goal | MCP Tool to call |
|------|-----------------|
| "What component should I use for X?" | `mcp_primeng_suggest_component` |
| "Show me a working template for Y" | `mcp_primeng_generate_component_template` |
| "What props/events does Z have?" | `mcp_primeng_suggest_component` (includes API info) |
| "How do I theme/style this?" | `mcp_primeng_get_theming_guide` |
| "What CSS classes does this component expose?" | `mcp_primeng_get_component_styling` |

- Compose PrimeNG components with Tailwind layout utilities (flex, grid, spacing, sizing).
- Never override PrimeNG component internals with raw CSS.
- Use PrimeNG design tokens (CSS variables) for color and spacing instead of hardcoded Tailwind color values where possible.

### Rule 3 — All Text Through i18n — Zero Hardcoded Strings
Every user-visible string in any template or component **must** go through Angular's i18n system. This includes labels, placeholders, headings, button text, aria-labels, tooltips, error messages, empty-state messages, and any other copy.

**Use the `i18n` attribute** on static text elements:
```html
<!-- correct -->
<h1 i18n="@@properties.list.title">Properties</h1>
<p i18n="@@properties.empty.message">No properties found</p>
```

**Use `$localize` tagged templates** for dynamic values (in `.ts`):
```typescript
// correct — assign to a signal or readonly property
readonly searchPlaceholder = $localize`:@@search.placeholder:Search properties...`;
```

```html
<!-- wrong -->
<h1>Properties</h1>
<input placeholder="Search properties..." />
```

If a translation key doesn't exist yet, add it with a descriptive i18n ID and a sensible default value.

**i18n ID convention:** `@@<feature>.<element>.<purpose>`
- `@@properties.list.title`
- `@@auth.login.submitButton`
- `@@tenants.form.nameLabel`
- `@@common.empty.noResults`

---

## Workflow

### 1. Understand context
- Which feature module does this UI belong to? Where does it live in `src/app/features/`?
- What data does it display or collect?
- Is this a **standard page** (strict no-CSS) or a **custom/branded page** (`features/custom/` — Tailwind first, CSS only as last resort)?

### 2. Discover PrimeNG building blocks
Query the MCP tools now. Do not skip this step. Find the right PrimeNG components first, then plan the layout.

### 3. Design with intent (within constraints)
Even within PrimeNG + Tailwind there is room for purposeful visual direction:
- **Layout**: Use Tailwind grid/flex to create clear information hierarchy. Vary spacing rhythm instead of applying uniform padding everywhere.
- **Typography hierarchy**: Use PrimeNG severity/size variants combined with Tailwind typography utilities (`text-sm`, `font-semibold`, `tracking-wide`, etc.) to guide the eye.
- **Density**: Match the content — comfortable padding for detail views, tight/compact for data-heavy tables.
- **Color**: Lean on PrimeNG's design token system (surface, primary, text). Use Tailwind color utilities only for truly decorative, non-themed accents.

### 4. Implement (Angular 21+ standards)
Follow all Angular best practices from the project instructions:
- Standalone components, `ChangeDetectionStrategy.OnPush`
- `signal()` / `computed()` for state; `input()` / `output()` functions for component I/O
- `inject()` for dependency injection
- Native control flow: `@if`, `@for`, `@switch`
- Reactive forms for any form
- `NgOptimizedImage` for static images
- Logic in `.ts`, template in `.html`, no separate style file

### 5. Accessibility
- All interactive elements need meaningful `aria-label` values (also i18n'd).
- Ensure correct focus management and keyboard navigation.
- PrimeNG handles many a11y details automatically — leverage that and don't duplicate them.

