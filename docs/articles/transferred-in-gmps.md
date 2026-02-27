# Transferred-In GMPs and Equalisation

## Overview

When a member transfers pension rights from one contracted-out scheme to another,
the receiving scheme takes on the obligation to pay the GMP originally accrued in
the transferring scheme. This **transferred-in GMP** has different characteristics
from scheme-accrued GMP that affect equalisation calculations.

This document summarises the legal position, practical complications, and our
current CalcLib approach.

## Legal Position

### Coloroll Principle

The **Coloroll** case (ECJ, 1994) established that where an individual transfers
pension rights between occupational schemes, the receiving scheme must increase
benefits to eliminate the effects of any inadequacy in the transfer value arising
from a breach of the right to equal pay — for service after 17 May 1990.

The receiving scheme's obligation only arises where it can be shown the original
transfer value was **inadequate**. Since receiving schemes generally do not hold
this information, the onus falls on the affected member to establish this.

### Lloyds 3 (November 2020)

The third Lloyds judgment specifically addressed historical transfers:

- Trustees owe a duty to ensure transfer payments correctly reflect equalised
  benefits.
- Where statutory CETVs were underpaid due to GMP inequality, the member can
  require a **top-up payment** to the receiving scheme.
- Claims are **not time-barred**.
- There is **no right to a residual benefit** in the transferring scheme — the
  remedy is a top-up to the receiving scheme.
- **Bulk transfers** (where legislation was followed) differ from **individual
  rule-based transfers** (where trustees may have breached duty).

## Complications for Equalisation

### 1. Different Revaluation Method

The transferred-in GMP may use a different revaluation basis from the scheme's own
GMP. Constraints:

- If the original termination date is on or after 6 April 1997, **Limited Rate is
  not available**.
- If a previous transfer used Fixed or Limited Rate, the new scheme must continue
  at the same rate or switch to Section 148.
- The **date of leaving** for revaluation is the *original* termination date from
  the scheme where rights first accrued, not the transfer date.

### 2. Separate Pre-88/Post-88 Split

The transferred-in GMP has its own pre-88/post-88 split that may span different
tax years from the receiving scheme's contracted-out period:

- Pre-88 GMP (6 April 1978 – 5 April 1988): no statutory obligation to increase
  in payment.
- Post-88 GMP (6 April 1988 – 5 April 1997): must increase annually at LPI
  capped at 3%.

### 3. Different Comparator Construction

Under C2, the opposite-sex comparator for transferred-in benefits relates to the
**transfer value received**, not pensionable service in the receiving scheme. The
comparator's GMP comes into payment at a different age (60F vs 65M).

### 4. Bulk vs Individual Transfers

| Aspect | Bulk Transfer | Individual Transfer |
|--------|---------------|---------------------|
| Data availability | Usually good (mirror-image terms) | Usually poor |
| Equalisation obligation | Clearer — detailed data transferred | Stalemate if data missing |
| Transfer type | Easier to classify | Often unclear (statutory vs rules-based) |

### 5. Missing Data

Receiving schemes rarely hold:

- The transferring scheme's benefit structure
- How the original transfer value was calculated
- What proportion related to post-17 May 1990 service
- The pre-88/post-88 GMP split

PASA acknowledges this creates genuine **stalemate** for individual transfers
where the adequacy of the original transfer cannot be determined.

### 6. Double Equalisation Risk

If a receiving scheme has already equalised transferred-in benefits and then
receives a top-up from the transferring scheme, a question arises whether it needs
to equalise again. PASA raises but does not definitively resolve this.

## CalcLib Approach

### Current State

CalcLib does **not** model transferred-in GMP as a separate tranche. The
`MemberData` record has a single set of contracted-out dates and earnings.

### Warning Flag

`MemberData` includes a `HasTransferredInGmp` boolean flag. When set to `true`,
`GmpCalculator.Calculate()` includes a warning on the `EqualisationResult`
indicating that the calculation does not account for transferred-in GMP
complications and results should be reviewed by an actuary.

This is a deliberate design decision: full transferred-in GMP modelling would
require separate GMP tranches with independent revaluation methods, contracted-out
periods, and comparator construction — significant complexity that is out of scope
for the current commercial use case (small schemes with minimal data).

### Future Extension

If transferred-in GMP modelling is needed, the data model changes would be:

- Separate transferred-in GMP fields on `MemberData` with own pre-88/post-88
  split
- Original date of leaving from the transferring scheme
- Revaluation method for the transferred-in tranche
- Bulk vs individual transfer indicator
- Whether a top-up has been received

The C2 cash flow would need to handle two GMP tranches with potentially different
revaluation and payment start dates.

## Sources

- [PASA Methodology Guidance (September 2019), Sections 4.1–4.3](https://www.pasa-uk.com/wp-content/uploads/2021/09/Equalising-for-the-Effects-of-GMPS-September-2019-FINAL.pdf)
- [PASA Transfers Guidance (September 2021)](https://www.pasa-uk.com/wp-content/uploads/2021/09/GMPE-Transfers-Guidance-FINAL.pdf)
- [PASA Data Guidance (July 2020)](https://www.pasa-uk.com/wp-content/uploads/2020/07/GMPE-Data-Guidance-vFINAL.pdf)
- [GOV.UK — Transfer contracted-out pension rights](https://www.gov.uk/guidance/transfer-your-scheme-members-contracted-out-pension-rights)
- [Sackers — PASA publishes GMP equalisation guidance on historical transfers](https://www.sackers.com/publication/pasa-publishes-gmp-equalisation-guidance-on-historical-transfers/)
- [Addleshaw Goddard — Lloyds 3 Judgment](https://www.addleshawgoddard.com/en/insights/insights-briefings/2020/pensions/lloyds-bank-gmp-equalisation-transfers-judgment/)
- [Barnett Waddingham — Lloyds 3 / PASA guidance on GMP equalisation](https://www.barnett-waddingham.co.uk/comment-insight/blog/lloyds-3-pasa-guidance-on-gmp-equalisation/)
- [WTW — PASA guidance on GMP equalisation transfers](https://www.wtwco.com/en-gb/insights/2021/08/pasa-guidance-on-gmp-equalisation-transfers)
