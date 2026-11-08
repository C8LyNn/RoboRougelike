# Garage feedback while rebuilding

When the vehicle could not deploy, both simple garage layouts replaced the last action message with the readiness warning. This hid messages about placement, purchase or selection precisely while a player was rebuilding an incomplete vehicle.

The landscape and portrait simple garage now share feedback formatting: the latest action remains visible, followed by the deployment warning when applicable. This does not alter inventory, construction or deployment rules.

Unity 6000.3.14f1 targeted EditMode runner: **3/3 PASS**, exit 0, `Artifacts/Local/GarageFeedback-Focused-20261108/results.xml` and `Editor.log`. The new test enters Play Mode, opens the actual garage, removes all wheels and checks that action feedback and the missing-drive warning coexist. The selected regression tests also exercise rebuilding and category coverage. The initial red run failed because the shared formatter was not present; it is not a visual reproduction of the old UI.

Windows Player and visual clipping checks at multiple resolutions: **NOT RUN**. Local XML/log evidence is not part of this source commit.
