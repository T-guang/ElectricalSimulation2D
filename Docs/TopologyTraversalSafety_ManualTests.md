# Topology traversal safety manual tests

Purpose: verify that extreme free-wiring cases do not freeze Unity, recurse forever, or block local inspection reports. These cases are stability guards, not electrical-meaning acceptance tests.

Expected result for every case:

- Unity remains responsive.
- Running simulation does not throw Console errors.
- "检查当前电路" and "当前电路解释" complete without text overlap or hanging.
- If a traversal budget is exceeded, the report may show `COMPLEX_LOOP_OR_UNSUPPORTED_TOPOLOGY`.
- Existing precise rules may still report their own errors or warnings.

## Extreme wiring cases

1. KM self-loop through auxiliary contact
   - Wire KM 13/14 back toward its own A1/A2 control path.
   - Goal: self-holding eligibility search stops safely.

2. Two contactors mutually self-holding
   - KM1 13/14 feeds KM2 coil path, KM2 13/14 feeds KM1 coil path.
   - Goal: dynamic contactor stabilization stays bounded.

3. Start, stop, and KM auxiliary contact closed loop
   - Connect start 23/24, stop 11/12, and KM 13/14 into a ring.
   - Goal: STOP_BUTTON_BYPASSED and SELF_HOLDING_BRANCH_INCOMPLETE checks do not loop.

4. SQ contact and KM self-holding branch cycle
   - Put SQ 23/24 in a loop with KM 13/14 and KM A1.
   - Goal: automatic reciprocating related SQ/KM topology is bounded.

5. Direct L1-L2 short
   - Connect L1 and L2 directly or through one wire-only branch.
   - Goal: short/conflict handling completes without freeze.

6. Indirect L1-to-L2 short through many closed contacts
   - Route L1 through multiple closed buttons/contactors/limit switches back to L2.
   - Goal: phase reachability and coil energized判断 remain bounded.

7. Star-delta secondary terminal abnormal short
   - Short U2/V2/W2 in a non-standard way and also add delta cross links.
   - Goal: star-delta invariant checks finish and report existing star-delta issues where applicable.

8. Forward/reverse contactors cross-wired
   - Cross KM forward and reverse outputs and interlock terminals.
   - Goal: REVERSING_CONTACTOR_CONFLICT and ReversingPairScopeHelper stay bounded.

9. Thermal relay 95/96 bypass plus main-circuit break
   - Open/bypass FR 95/96 while also breaking one main phase path.
   - Goal: THERMAL_RELAY_CONTROL_BYPASSED and motor phase checks finish safely.

10. Nested multi-contactor self-holding
    - Create three or more contactors with 13/14 branches feeding each other in a ring.
    - Goal: traversal budgets prevent runaway checks and produce a controlled warning if needed.

## Normal regression templates

After the extreme cases, re-open or reload these normal templates and verify no traversal warning appears in normal operation:

- Household 8 templates.
- Point control.
- Continuous self-holding control.
- Thermal relay protection.
- Forward/reverse control.
- Automatic reciprocating control.
- Two-motor time relay sequence start.
- Star-delta reduced-voltage start.
- V2.3.6.4 negative perturbation cases.
