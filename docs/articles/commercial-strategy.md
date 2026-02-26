# Commercial Strategy: Universal GMP Compensator

## Core Insight

The GMP calculation is **universal** — given a member's DOB, sex, contracted-out dates, and earnings history, the result is entirely deterministic. HMRC-published factors (S148, fixed rates) are the same for everyone. The scheme-specific variables that drive compensation are relatively few.

## Scheme-Specific Variables

The only things that vary between schemes:

- **NRA** (pre and post equalisation)
- **Accrual rate** (60ths, 80ths, etc.)
- **PIP increase method** (LPI3, LPI5, public sector)
- **Excess pension above GMP**
- **Discount rate assumptions**

## Scenario-Based Approach

Rather than requiring exact scheme rules (high onboarding friction), define 3-4 archetype schemes:

| Scenario | Accrual | NRA | PIP Method | Character |
|----------|---------|-----|------------|-----------|
| Generous | 60ths | 60 | LPI5 | Final salary, older scheme |
| Standard | 80ths | 65 | LPI3 | Typical private sector |
| Basic | 80ths | 65 | Fixed | Minimum compliance |
| Public Sector | 80ths | 60 | Public Sector rates | NHS, teachers, etc. |

Run each member through all scenarios, produce a compensation range. Trustees/actuaries can identify which archetype is closest to their scheme.

## Commercial Proposition

**Value proposition is not "look how much compensation we found"** — it's **"discharge your legal obligation at a fraction of the cost."**

Since the Lloyds Banking Group ruling (2018), schemes **must** equalise GMP. But:

- Per-member compensation is often modest (hundreds, not thousands, for most members)
- Actuarial/admin cost to calculate bespoke is high
- Many schemes are still dragging their feet because of this cost/benefit mismatch

A tool that's significantly cheaper than hiring an actuary flips the economics. The scenario-based approach removes onboarding friction.

## Realistic Compensation Bounds

- Case 4 (long service, S148 revaluation): ~£14,324 — this is the higher end
- Most members with shorter service or fixed-rate revaluation: likely £100–£2,000
- The compensation amount is often small relative to traditional admin costs
- This supports the "low-cost universal tool" positioning rather than a bespoke consultancy model

## Architecture Alignment

The CalcLib is already well-positioned:

- Pure functions, no scheme dependency in the GMP layer
- Factor data injected via `IFactorProvider`
- `SchemeConfig` record maps directly to scenario archetypes
- CashFlowBuilder + CompensationCalculator take `SchemeConfig` as input
- Excel export provides the audit trail trustees need
