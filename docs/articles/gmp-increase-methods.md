# GMP Increase Methods in Payment

## Background

Once a pension is in payment, the GMP and excess components increase at different rates. How schemes handle this interaction is a key design variable for the cashflow model.

## Component Increase Rules

| Component | Scheme Obligation | Notes |
|-----------|------------------|-------|
| Pre-88 GMP | None | State paid via Additional State Pension (pre-2016). Post-2016 there is a gap — no mechanism for the state to pay. |
| Post-88 GMP | CPI capped at 3% pa | Scheme must pay this. The percentage is specified each year in a Guaranteed Minimum Pensions Increase Order. |
| Excess above GMP | Per scheme rules | LPI5, LPI3, fixed %, public sector rates, etc. Varies by scheme. |

## Two Methods for Applying Increases

### Separate Increase Method

Each component increases independently at its own rate:

- Pre-88 GMP stays flat (scheme pays nothing)
- Post-88 GMP increases at CPI max 3%
- Excess increases at scheme PIP rate

This is the simpler method. The total pension is the sum of the separately-increased components. Most common in private sector schemes.

### Overall Increase Method

The scheme applies its increase rate to the **total pension**, but ensures the statutory GMP minimum is met. The excess effectively **absorbs** some of the GMP increase cost.

As Gowling WLG observe: "giving somebody a bigger GMP could make them worse off, because the total pension is still the same; by making the GMP bigger, one simply makes a larger proportion of the pension subject to the less generous pension increases."

This method can result in the excess reducing over time as GMP increases eat into it.

## Anti-Franking

Anti-franking legislation prevents schemes from reducing the excess pension to offset GMP revaluation during deferment. At GMP payable age, an anti-franking test ensures the total pension is at least equal to the revalued GMP. This can cause a "step up" in pension at GMP age.

Key timing issue: the anti-franking test is carried out at GMP payable age, which is 60 for women and 65 for men — so the test happens five years apart, adding to the potential for inequality.

## State vs Scheme Responsibility

- **Pre-2016 State Pension age**: Where the scheme didn't provide the full increase on GMP, the state topped up via the Additional State Pension.
- **Post-2016 State Pension age**: This top-up mechanism no longer exists. The scheme's obligation remains, but any CPI increase above the 3% cap on post-88 GMP is not covered by anyone — a known gap in the legislation.

## CalcLib Implementation

Both methods are implemented in `CashFlowBuilder` and selected via `SchemeConfig.IncreaseMethod` (a `PensionIncreaseMethod` enum: `Separate` or `Overall`). Default is `Separate`.

The `CashFlowEntry` record tracks Pre88/Post88/Excess/Total separately per sex, supporting both methods with the same data structure:

- **Separate method**: Each component increases independently at its own rate. GMP components follow statutory rules; excess follows the scheme PIP rate.
- **Overall method**: The `OverallExcess()` helper applies the scheme PIP rate to the running total pension, then tests the GMP floor (pre-88 flat + post-88 at LPI3 statutory). Excess is derived as `max(0, total pension - total GMP)`. A sentinel check (`runningTotal == 0`) detects the first PIP year since the `enteredPip` flag is already set by the GMP computation block.

Key behaviours verified by tests (115 passing):

- GMP components (pre-88, post-88) are identical regardless of increase method
- Under overall with a low scheme rate, excess erodes year-over-year
- The GMP floor prevents total pension from falling below GMP entitlement
- Excess is never negative
- Under overall with the same rate as LPI3, excess does not erode (rate applied to larger base)
- Under separate, excess of zero stays zero; under overall, it can become positive

## Sources

- [Quilter — Guaranteed Minimum Pension Benefits](https://www.quilter.com/help-and-support/technical-insights/technical-insights-articles/guaranteed-minimum-pension-benefits/)
- [Gowling WLG — GMP Equalisation: What is the Problem with GMPs?](https://gowlingwlg.com/en/insights-resources/articles/2020/gmp-equalisation-what-is-the-problem-with-gmps/)
- [PASA — GMP Equalisation Working Group Guidance (Sept 2019)](https://www.pasa-uk.com/wp-content/uploads/2021/09/Equalising-for-the-Effects-of-GMPS-September-2019-FINAL.pdf)
- [PASA — GMPE Anti-Franking Guidance](https://www.pasa-uk.com/wp-content/uploads/2021/09/GMPE-Anti-Franking-Guidance-Final-.pdf)
- [House of Commons Library — GMP Increases (SN04956)](https://commonslibrary.parliament.uk/research-briefings/sn04956/)
- [Wikipedia — Guaranteed Minimum Pension](https://en.wikipedia.org/wiki/Guaranteed_Minimum_Pension)
