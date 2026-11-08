# Garage dismantle confirmation lifetime

A pending inventory dismantle confirmation could survive leaving the catalog or changing the selected group. Returning to the same instance could then dismantle it on the next click without a fresh confirmation.

Both catalog layouts now clear the pending confirmation on catalog dismissal, transition out of the garage, group/item selection, purchase, repair and fusion actions. Selecting a destroyed item retains the catalog behavior from the preceding work. The actual `R3VehicleModel.Dismantle` transaction, two-click confirmation, scrap calculation and reward-screen workflow are unchanged.

The new `UI943PortraitCatalogActionsTests.DismantleConfirmationResetsAfterGroupChangeAndCatalogClose` prepares a real-controller scenario including group change, closure/reopen, repair, no premature removal and one final award. It is **NOT RUN**. Unity test execution and actual UI clicks are **NOT RUN**.

Local verification of working-tree changes over base `b819748094d05aec356b1e22bf36b578a85bd742`: Roslyn C# semantic compile of the generated `Assembly-CSharp` source list **PASS**, exit 0, 199 files, 12 warnings; editor/test source list against the newly compiled game assembly **PASS**, exit 0, 201 files, 12 warnings. Artifacts: `Artifacts/Local/CSharpCompile-DismantleConfirm-20261108/` and `Artifacts/Local/CSharpEditorCompile-DismantleConfirm-20261108/`. These use previously built dependency DLLs and do not constitute a Unity Editor compile, a Player build or a test execution.
