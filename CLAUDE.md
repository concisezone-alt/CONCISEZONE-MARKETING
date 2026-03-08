# CLAUDE.md — ConciseZone Marketing Repository

This file provides guidance for AI assistants (Claude and others) working in this repository.

---

## Project Overview

**Repository:** `concisezone-alt/CONCISEZONE-MARKETING`
**Purpose:** Marketing assets, campaigns, and related code for the ConciseZone platform.

> **Note:** This repository was initialized on 2026-03-08 and currently contains no source files. Update this document as the project takes shape.

---

## Repository Status

This repository is in initial setup. As files are added, update the sections below to reflect:
- The chosen tech stack and framework
- Directory layout conventions
- Build and deployment instructions
- Testing approach

---

## Git Workflow

### Branch Naming

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/<short-description>` | `feature/add-landing-page` |
| Bug fix | `fix/<short-description>` | `fix/broken-cta-link` |
| AI-generated | `claude/<task-id>` | `claude/claude-md-mmhm5wiig3byyml3-XTcy2` |
| Release | `release/<version>` | `release/v1.2.0` |

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short summary>

[optional body]
```

Common types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Examples:
```
feat(landing): add hero section with CTA button
fix(email): correct unsubscribe link in newsletter template
docs: update CLAUDE.md with project structure
chore: add .gitignore for Node.js project
```

### Push Instructions

Always push to the correct branch:
```bash
git push -u origin <branch-name>
```

AI assistant branches must start with `claude/` and end with the matching session ID.

---

## Development Workflow

### Before Starting Work

1. Pull the latest changes from the target branch.
2. Create a dedicated feature branch.
3. Confirm the task scope before making changes.

### Making Changes

1. Keep changes focused and minimal — do not refactor unrelated code.
2. Prefer editing existing files over creating new ones.
3. Do not add comments, docstrings, or type annotations to code you did not change.
4. Avoid over-engineering; build only what is needed now.

### After Making Changes

1. Run the project's lint and test commands (update this section once the stack is defined).
2. Commit with a descriptive message following the convention above.
3. Push to the feature branch.
4. Open a pull request with a clear summary of the changes.

---

## Code Conventions

> Update this section once a tech stack and linter configuration are established.

### General Rules

- **Formatting:** Use the project's formatter (Prettier, Black, etc.) — never manually reformat unrelated code.
- **Naming:** Follow the conventions already present in the file being edited.
- **Secrets:** Never commit API keys, credentials, or `.env` files. Use `.env.example` for documenting required variables.
- **Dependencies:** Add new dependencies only when clearly necessary; prefer existing packages.

### Security

- Sanitize all user-supplied input at system boundaries.
- Do not introduce SQL injection, XSS, command injection, or other OWASP Top 10 vulnerabilities.
- Validate external API responses before using their data.

---

## AI Assistant Instructions

### What You Should Do

- Read existing code thoroughly before suggesting or making changes.
- Match the style, patterns, and conventions already in the file.
- Break large tasks into a todo list and complete them sequentially.
- Confirm with the user before taking irreversible actions (deleting files, force-pushing, dropping data).
- Prefer simple, direct solutions over abstractions.

### What You Should Not Do

- Do not push to `main`/`master` without explicit permission.
- Do not create files that aren't strictly necessary.
- Do not add features, error handling, or configurability beyond what was requested.
- Do not use `--no-verify`, `--force`, or other safety bypasses without explicit instruction.
- Do not commit `.env` files or secrets.
- Do not guess URLs or fabricate external links.

### Risky Actions — Always Confirm First

The following require explicit user approval before proceeding:
- `git push --force`
- `git reset --hard`
- Deleting files or directories
- Modifying CI/CD pipelines
- Posting to external services (Slack, email, GitHub PRs/issues)
- Dropping or truncating database tables

---

## Adding to This Document

When the project gains structure, add the following sections:

- **Tech Stack** — languages, frameworks, runtime versions
- **Directory Layout** — annotated tree of the project
- **Environment Variables** — list of required variables and their purpose
- **Build & Run** — commands to install dependencies, start dev server, and build for production
- **Testing** — how to run unit, integration, and end-to-end tests
- **Deployment** — where and how the project is deployed
- **Design System / Assets** — brand guidelines, color palette, fonts, logo usage
