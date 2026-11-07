# Temporary quarantine for unsafe module ports

The #963 source defines `R4B_OctColumn` sockets 0 and 1 as full and permits modules, but offline numerical inspection found their corner samples approximately 40.305 mm outside the source collider. Until the source owner resolves the geometry/port specification, `R4BlockCatalog.CanMountModule` fails closed for those two specific sockets. Source metadata and assets are unchanged; other full ports remain available and existing partial/forbidden-module rules still apply. This is a temporary guard, not a modification or acceptance of the underlying source.

The staged catalog is **not wired to the garage, physical placement or restoration**. Therefore this guard does not yet change or protect player gameplay. `R4B_HexColumn`'s two mismatched partial sockets are already rejected by the existing module rule; structural-port behavior is not solved by this guard. See `R4_BLOCK_SOCKET_SURFACE_QA.md` for findings and method.

## Lightweight validation

- The actual `R4BlockCatalog.cs` compiled in a single-worker, offline .NET 8 smoke project using minimal stand-ins for Unity value types. Before the guard, the new octagonal-port assertion **FAIL**; after the guard, the smoke checks **PASS** for all 30 entries, valid cube/wedge full ports, wedge and octagonal partial ports, both quarantined octagonal end ports and invalid inputs. This is real C# logic on stubs, **not native Unity** or a gameplay test.
- The source-catalog parity verifier still reports **30/30 PASS**; the source socket baseline still reproduces the four known mismatches. The underlying issue has not been marked fixed.
- Native Unity EditMode/PlayMode, actual placement, repair/restoration, collider behavior and gameplay are **NOT RUN**. Corresponding Unity catalog assertions were added for the next native test window.
