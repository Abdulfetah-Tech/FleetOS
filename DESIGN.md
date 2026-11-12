---
name: EthioFleet OS
description: Operational fleet readiness, mapped with clear wayfinding.
colors:
  wayfinding-yellow: "#ffd500"
  ink: "#111111"
  page: "#f3f4ef"
  surface: "#ffffff"
  line: "#c8cbc5"
  muted: "#454b48"
  focus: "#1d4ed8"
  good: "#176447"
  warn: "#874000"
  critical: "#a32020"
  dark-ink: "#f4f4ef"
  dark-page: "#171b1c"
  dark-surface: "#202627"
  dark-line: "#465052"
  dark-muted: "#bdc5c2"
  dark-focus: "#a9c7ff"
  dark-good: "#8de0b0"
  dark-warn: "#ffc078"
  dark-critical: "#ff9a93"
typography:
  display:
    fontFamily: "Barlow Semi Condensed, Arial Narrow, sans-serif"
    fontSize: "clamp(38px, 4.2vw, 62px)"
    fontWeight: 700
    lineHeight: 0.98
    letterSpacing: "-0.025em"
  headline:
    fontFamily: "Barlow Semi Condensed, Arial Narrow, sans-serif"
    fontSize: "clamp(26px, 2.4vw, 34px)"
    fontWeight: 700
    lineHeight: 1.1
    letterSpacing: "-0.025em"
  title:
    fontFamily: "Source Sans 3, Arial, sans-serif"
    fontSize: "30px"
    fontWeight: 700
    lineHeight: 1.15
    letterSpacing: "-0.02em"
  body:
    fontFamily: "Source Sans 3, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.45
  label:
    fontFamily: "Source Sans 3, Arial, sans-serif"
    fontSize: "13px"
    fontWeight: 650
rounded:
  sign-edge: "2px"
  control: "3px"
spacing:
  compact: "6px"
  standard: "12px"
  comfortable: "16px"
  section: "24px"
components:
  button-primary:
    backgroundColor: "{colors.wayfinding-yellow}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "10px 15px"
    height: "48px"
  button-rail:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "6px 9px"
    height: "36px"
  input:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.control}"
    padding: "9px 11px"
    height: "46px"
---

# Design System: EthioFleet OS

## Overview

**Creative North Star: "Fleet Wayfinding"**

Fleet Wayfinding makes the next operational decision easy to locate. The visual language borrows the direct hierarchy of international airport signs: a saturated yellow navigation rail, one action band for due work, compact white pictograms in black insets, and clear high-contrast type. The mood is precise, direct, and operational; controls feel tactile and confident without turning the dashboard into a stack of decorative cards.

The interface supports fleet managers and operations leads who need to scan readiness, overdue work, and cost context quickly. Yellow is reserved for wayfinding and the next action. Maintenance and document states use explicit text alongside restrained status color. The light and dark themes share the same structure and preserve the yellow sign system.

**Key Characteristics:**
- A single yellow navigation rail and one next-action band establish orientation.
- Fleet measures use a compact strip; operational work stays in readable rows.
- Borders and spacing provide hierarchy instead of shadows or nested cards.
- Every live operational value comes from the API; no simulated GPS or demo records are implied.

## Colors

The palette pairs one unmistakable wayfinding accent with cool neutral work surfaces; status colors remain secondary to labels.

### Primary
- **Wayfinding Yellow:** The navigation rail and the dashboard's single next-action band; it is not a general-purpose highlight.

### Secondary
- **Focus Blue:** Keyboard focus indication, independent from operational status.

### Tertiary
- **Confirmed Green:** Positive state where a status needs it.
- **Service Ochre:** Caution for upcoming document expiry.
- **Critical Red:** Overdue, failed, or blocking states.

### Neutral
- **Ink Black:** Primary text and sign-system foreground.
- **Cool Paper:** Light-theme page canvas.
- **Signal White:** Inputs and compact data surfaces.
- **Divider Gray:** Fine rules and row separators.
- **Operational Slate:** Secondary text in the light theme.
- **Night Shift, Dark Surface, Dark Divider, and Dark Muted Text:** The dark-theme canvas, data surface, separators, and supporting text.
- **Dark Theme Ink:** Primary dark-theme text.
- **Dark Focus Blue, Dark Confirmed Green, Dark Service Amber, and Dark Critical Red:** Dark-theme interactive and status colors.

### Named Rules
**The One Yellow Rule.** Yellow marks wayfinding and the next action; do not spread it across unrelated metrics or status labels.

## Typography

**Display Font:** Barlow Semi Condensed, with Arial Narrow and sans-serif fallbacks

**Body Font:** Source Sans 3, with Arial and sans-serif fallbacks

**Label Font:** Source Sans 3

**Character:** Barlow Semi Condensed gives sign-like headings compact authority. Source Sans 3 keeps operational details readable at dense dashboard sizes.

### Hierarchy
- **Display:** Bold, tightly led, and reserved for the authentication promise.
- **Headline:** A compact, responsive display face for the page title and major totals.
- **Title:** Source Sans 3 with a clear weight step for section and form headings.
- **Body:** A calm 16px working size with generous line height; keep explanatory copy near 65–75 characters per line where layout permits.
- **Label:** Compact, medium-to-bold labels for navigation, field names, statuses, and metric captions.

### Named Rules
**The Sign-First Rule.** Headings carry the hierarchy; do not add decorative eyebrow labels or uppercase section numbering.

## Layout

The desktop dashboard uses a fixed 224px navigation rail beside a flexible content area. A five-measure status strip sits above a two-column work-and-context region. At 900px the rail becomes a top navigation row; at 640px status measures become two columns and work details stack. Authentication changes from a two-column sign-and-form composition to a single column at 760px. Keep content fluid and verify it at narrow widths; no horizontal scrolling is acceptable.

Spacing is compact around related labels and values, with larger separation between operational sections. Use the existing rhythm rather than adding equal card gutters everywhere.

## Elevation & Depth

The system is flat and border-led. Thin rules separate work rows and data groups; tonal surfaces distinguish the page from inputs without floating-card shadows. Whitespace, typography, and the sign-yellow action band provide the depth hierarchy.

### Named Rules
**The Flat-By-Default Rule.** Do not add shadows to dashboard containers; use the existing surface and divider vocabulary.

## Shapes

Controls and navigation use small, square-cornered geometry. Keep edges crisp and borders thin; do not introduce pill-shaped controls or rounded card stacks.

## Components

### Buttons
- **Character:** Tactile and confident, with a direct action label.
- **Primary:** Full-width yellow submit action with ink text and compact padding.
- **Rail actions:** Transparent, outlined controls; hover reverses to an ink surface with light text.
- **Focus and disabled:** Preserve the visible blue focus ring; disabled actions remain legible and show their unavailable state.

### Inputs
- **Shape:** White or theme surface, thin neutral border, and compact square-cornered geometry.
- **Focus:** Change the border to the focus color and retain the visible keyboard outline.
- **Errors:** Place concise recovery-oriented text beside the affected field or form.

### Navigation
- **Rail:** Yellow, persistent, and limited to functional routes. The active destination receives a white inset rather than another accent color.
- **Mobile:** Reflow to a compact top bar; keep account actions reachable and avoid horizontal overflow.

### Attention Band
- **Role:** One yellow band reports the count and kinds of due work and links directly to the work list.
- **Content:** Keep the action on the trailing edge at wide widths and stack it cleanly on narrow screens.

### Fleet Status and Work Rows
- **Status:** Use a continuous, compact strip for fleet totals rather than separate metric cards.
- **Work:** Show maintenance and document details as rows with explicit status text; never rely on status color alone.

## Do's and Don'ts

### Do
- Keep the yellow sign system singular and purposeful.
- Use real API values, explicit currency codes, and visible data dates.
- Preserve semantic headings, visible focus, and text status labels.
- Keep loading, error, empty, and disabled states explicit.

### Don't
- Do not imply live GPS, integrations, or sample data are operational facts.
- Do not turn the page into a grid of interchangeable KPI cards.
- Do not use color alone to communicate urgency.
- Do not add ornamental gradients, heavy shadows, or decorative badges.
