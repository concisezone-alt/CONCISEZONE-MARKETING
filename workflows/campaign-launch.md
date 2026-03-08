# Workflow: Campaign Launch

This workflow describes the standard sequence for launching a new marketing campaign using the ConciseZone AI work team.

---

## Trigger

A new campaign goal is identified by a human stakeholder or the Campaign Manager agent.

---

## Steps

### Step 1 — Brief Creation (Campaign Manager)

1. Receive campaign goal and any constraints (dates, budget, audience).
2. Fill in `templates/campaign-brief.md` and save to `campaigns/<campaign-name>/brief.md`.
3. Identify which channels and agents are needed.
4. Assign deliverables and due dates in the brief.

### Step 2 — Keyword Research (SEO Specialist)

1. Read the campaign brief.
2. Identify primary and secondary keywords.
3. Save keyword research to `content/blog/<campaign-name>-keywords.md`.
4. Share findings with the Content Strategist and Copywriter.

### Step 3 — Content Planning (Content Strategist)

1. Read the campaign brief and keyword research.
2. Outline blog post(s) for the campaign.
3. Update `templates/content-calendar.md` with planned content.
4. Save outlines to `content/blog/`.

### Step 4 — Copy Creation (Copywriter + Email Marketer + Social Media Manager)

_These steps can run in parallel:_

- **Copywriter:** Write ad copy and landing page copy → `content/ads/`
- **Email Marketer:** Write email sequence → `content/email/`
- **Social Media Manager:** Write social posts → `content/social/`

### Step 5 — Review (Campaign Manager)

1. Review all draft content against the campaign brief.
2. Check alignment with `brand/guidelines.md` and `brand/voice-and-tone.md`.
3. Return feedback to the relevant agent or mark as approved.

### Step 6 — Finalize & Handoff

1. Move approved deliverables to `outputs/<campaign-name>/`.
2. Update status in `templates/content-calendar.md` to "Approved".
3. Notify the human stakeholder that content is ready for publishing.

---

## Roles Summary

| Step | Agent |
|------|-------|
| Brief | Campaign Manager |
| SEO | SEO Specialist |
| Blog planning | Content Strategist |
| Ad & page copy | Copywriter |
| Email | Email Marketer |
| Social | Social Media Manager |
| Review | Campaign Manager |
