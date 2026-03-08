# CLAUDE.md — ConciseZone Marketing AI Work Team

This file provides guidance for AI assistants (Claude and others) working in this repository.

---

## Project Overview

**Repository:** `concisezone-alt/CONCISEZONE-MARKETING`
**Purpose:** AI-powered marketing work team for the ConciseZone platform. This workspace coordinates a team of specialized AI agents that collaborate on marketing campaigns, content creation, brand management, and channel distribution.

**Initialized:** 2026-03-08

---

## What This Project Is

ConciseZone Marketing is an AI work team — a structured set of AI agents, workflows, templates, and brand assets that together function as a full marketing team. Each agent has a defined role and works within shared brand guidelines and campaign workflows to produce consistent, high-quality marketing output.

### AI Agent Roles

| Agent | Folder | Responsibility |
|-------|--------|----------------|
| Campaign Manager | `agents/campaign-manager/` | Plans campaigns, sets goals, coordinates other agents |
| Copywriter | `agents/copywriter/` | Writes copy for ads, emails, landing pages, and social posts |
| SEO Specialist | `agents/seo-specialist/` | Keyword research, on-page SEO, content optimization |
| Social Media Manager | `agents/social-media/` | Social content, scheduling strategy, platform tone |
| Email Marketer | `agents/email-marketer/` | Email sequences, newsletters, nurture campaigns |
| Content Strategist | `agents/content-strategist/` | Blog posts, long-form content, editorial calendar |

---

## Directory Layout

```
CONCISEZONE-MARKETING/
├── CLAUDE.md                   # This file — project guidance for AI assistants
├── agents/                     # AI agent definitions, personas, and system prompts
│   ├── campaign-manager/
│   ├── copywriter/
│   ├── seo-specialist/
│   ├── social-media/
│   ├── email-marketer/
│   └── content-strategist/
├── brand/                      # Brand guidelines, voice, tone, and visual references
│   ├── guidelines.md
│   └── voice-and-tone.md
├── campaigns/                  # One folder per campaign; contains briefs and outputs
├── content/                    # Content organized by channel
│   ├── blog/
│   ├── email/
│   ├── social/
│   └── ads/
├── templates/                  # Reusable templates for briefs, copy, and calendars
├── workflows/                  # Multi-agent workflow definitions and orchestration specs
└── outputs/                    # Final deliverables ready for publishing or handoff
```

---

## Git Workflow

### Branch Naming

| Type | Pattern | Example |
|------|---------|---------|
| Feature | `feature/<short-description>` | `feature/add-landing-page` |
| Bug fix | `fix/<short-description>` | `fix/broken-cta-link` |
| AI-generated | `claude/<task-id>` | `claude/setup-marketing-team-project-6mRQ3` |
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
feat(campaigns): add Q2 product launch campaign brief
feat(agents): add copywriter agent system prompt
fix(email): correct unsubscribe link in newsletter template
docs: update CLAUDE.md with directory layout
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
4. Review brand guidelines (`brand/guidelines.md`) before generating content.

### Making Changes

1. Keep changes focused and minimal — do not refactor unrelated files.
2. Prefer editing existing files over creating new ones.
3. All agent output goes into the appropriate `content/` subfolder or `campaigns/<campaign-name>/`.
4. Do not commit draft content as final — use a `drafts/` subfolder within campaigns.

### After Making Changes

1. Commit with a descriptive message following the convention above.
2. Push to the feature branch.
3. Open a pull request with a clear summary of the changes.

---

## Code & Content Conventions

### Secrets

- Never commit API keys, credentials, or `.env` files.
- Use `.env.example` to document required environment variables.

### Security

- Sanitize all user-supplied input at system boundaries.
- Validate external API responses before using their data.

### Content

- All copy must align with the brand voice defined in `brand/voice-and-tone.md`.
- Campaign briefs live in `campaigns/<campaign-name>/brief.md` before any content is created.
- Completed deliverables move to `outputs/` when approved.

---

## AI Assistant Instructions

### What You Should Do

- Read `brand/guidelines.md` and `brand/voice-and-tone.md` before writing any copy.
- Check the relevant campaign brief before creating campaign-specific content.
- Follow the agent role descriptions when acting as a specific agent.
- Break large tasks into a todo list and complete them sequentially.
- Confirm with the user before taking irreversible actions.

### What You Should Not Do

- Do not push to `main`/`master` without explicit permission.
- Do not create content outside the defined folder structure.
- Do not invent brand guidelines — always reference what is in `brand/`.
- Do not use `--no-verify`, `--force`, or other safety bypasses without explicit instruction.
- Do not commit `.env` files or secrets.
- Do not guess URLs or fabricate external links.

### Risky Actions — Always Confirm First

- `git push --force`
- `git reset --hard`
- Deleting files or directories
- Publishing or distributing content to external channels
- Modifying CI/CD pipelines

---

## Environment Variables

> Update this section as integrations are added.

| Variable | Purpose |
|----------|---------|
| `ANTHROPIC_API_KEY` | Claude API key for running AI agents |

---

## Sections to Add as the Project Grows

- **Tech Stack** — languages, frameworks, runtime versions (e.g., if agents are automated via scripts)
- **Build & Run** — commands to install dependencies and run agent pipelines
- **Testing** — how to validate agent outputs and workflow correctness
- **Deployment** — publishing pipelines and handoff processes
- **Integrations** — CMS, email platform, social scheduler, analytics tools
