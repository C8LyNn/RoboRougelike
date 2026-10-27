# B01-12 UX.U5 source-backed study requests to B01-11 (NOT executed on people)

Source: exact v2 HTML Git blob `bc17deba3c96307c8179a0ff564b26b3855287ce`. Reviewed original independent B01-11 v1 [comment](https://github.com/C8LyNn/RoboRougelike/issues/296#issuecomment-6440580983) and benchmark [#292](https://github.com/C8LyNn/RoboRougelike/issues/292).

## Five sequential cases
- **UX-01:** preset ready → hover missing wheel/free cell → Is it physically mount-ready? Check sidebar `MOUNT_AUTHORITY UNVERIFIED`. Click UNKNOWN without checkbox (must not insert); with `rekwizyt badawczy` checkbox (mock insertion counts independently, not approval).
- **UX-02:** armor 2×2 yaw90, unmeasured wheel, select `TYPED_SOCKET_UNVERIFIED` and `INCOMPLETE_EXTERNAL_BROADPHASE`; ask testers to distinguish logical occupancy, actual collider verification and typed physical socket. No guessed collider measurements for 11 unknown.
- **UX-03:** choose C, invalid overlap/unknown, locate working `Dlaczego?` by keyboard/mouse, determine cause and remedy.
- **UX-04:** CHL → begin → skip/reverse/wheel-graze, read **visible** rejected reason before inspecting log; select FALL/TIMEOUT/COLLISION fail fixture and name next action. CP events are buttons, not PhysX evidence.
- **UX-05:** Heavy attempt CP1–4, click mock Finish then attempt Light switch without Retry (blocked), retry then Light start, check `HEAVY-01` history remains correctly attributed and new `LIGHT-01` represents new attempt. Pause/Retry + modal keyboard Tab/Escape.

## 8-user exploratory design (B01-11 ownership)
8 target-player volunteers, counterbalance ABC/ACB/BAC/BCA/CAB/CBA+ABC/CBA; source-blind neutral tasks, observer captures `participant_id,source_html_blob,variant,order,case_id,success,seconds_to_first_correct_answer,wrong_clicks,said_physics_approved,said_socket_approved,failed_to_find_reason,wrong_attempt_attribution,participant_quote,observer_note`. Hypothesis gates: H1 >=90% source-safe classification/zero UNKNOWN interpreted approved, H2 >=25pt improvement cause attribution, H3 >=80% failure understanding and >=90% Retry found within 5s. **These are NOT measured outcomes; no human study performed.**

**Next action:** independent B01-11 SOURCE+CHROMIUM re-review current v2 with exact HTML blob and 5 scenarios, then actual humans only if accessible. Without genuine humans report `HUMAN_NOT_RUN`; never claim results from LLM agents.
