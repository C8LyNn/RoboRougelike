# Limitations

- Research / screenshot annotation utility, **not an integrated game feature** and not a replacement for a true human UX study.
- No real RoboRougelike screenshot bundled; initial canvas is a prominently labelled original **synthetic mock image**.
- Cannot automatically detect visual clarity, clipping, obstruction, contrast or usability failures. Human/independent QA reviewers manually annotate issues; labels are assertions by reviewers, not automated conclusions.
- PNG export includes original uploaded image pixels; export only source material you are permitted to share. JSON exports dimensions, filename, review labels and notes but not pixels.
- Saved annotation coordinates normalize to [0,1] and JSON import requires exact image dimensions, not image cryptographic identity. Operator must additionally associate report with screenshot SHA externally if acceptance depends on original pixels.
- Supports max 100 rectangular annotations, not arbitrary polygons or automated screenshot comparisons.
- Native Unity G1 and independent B01-60, B01-40 and G2 are pending; no game branch or release integration.
