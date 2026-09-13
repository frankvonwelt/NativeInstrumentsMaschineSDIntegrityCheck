# Maschine file format notes

Consolidated from two sources:

- **`.mxprj` (Project) findings below** — first-hand, verified against the real files in
  `ExampleProjects/`. This is what `src/MxprjReader` actually parses today, and every claim here
  has been checked against real bytes (see each section).
- **`.mxgrp`/`.mxsnd` (Group/Sound) notes at the end** — secondhand research pulled together from
  public web sources in an earlier, separate session (originally `docs/MaschineDocs.md`, folded in
  here). These describe *different* file types that this tool does not currently read. Treat them
  as leads for future work, not verified fact — one of that document's own central claims was
  already self-corrected, and it references a follow-up file
  (`docs/mxgrp-format-findings.md`, said to be based on 323 real preset files) that **does not
  exist in this repo** — if you have it, it's worth adding.

## Part 1 — `.mxprj` (Project files)

### Container

- Binary file, not text/XML/JSON.
- Contains the string `serialization::archive` plus C++ class names such as
  `NI::MASCHINE::DATA::AudioModule`, `NI::MASCHINE::DATA::AudioEvent`, `NI::MASCHINE::DATA::Sampler`,
  etc. — this is a Boost-serialization-style binary archive of C++ objects.
- Near the top of the file there's a tag/length structure using 4-byte ASCII tags, e.g. `hsin`,
  `DSIN`, and a combined `2SAM` + `JORP` pair. Read backwards these spell `NISH`, `NISD`, `MAS2`,
  `PROJ` — i.e. the tags look like FourCC codes stored byte-reversed. We have **not** decoded the
  full chunk grammar (nesting rules, what each chunk contains) — that's out of scope until it's
  actually needed.
- Every example file we've checked carries the identical `2SAM`/`MAS2` signature near the top. See
  "Maschine 2 vs 3" below for what this does and doesn't tell us.

### Strings are Pascal-style (1-byte length prefix)

Every embedded string found so far is stored as a single length byte immediately followed by
that many printable ASCII (0x20–0x7E) bytes — no null terminator, no 4-byte length field. This
matches the "varint-length-prefixed ASCII" pattern also reported for `.mxgrp` in Part 2 below —
consistent with a shared string-encoding convention across NI's serialization format, for lengths
that fit in one byte (< 128).

Verified example (`AcidJogger.mxprj`, offset ~0x1783):
```
26 41 63 69 64 4a 6f 67 67 65 72 20 53 61 6d 70 6c 65 73 2f ...
^^ length = 0x26 = 38
   "AcidJogger Samples/AcidJogger Kick.wav"  (38 characters)
```

### Sample references: two storage shapes

Two different shapes have been observed for how a project stores a reference to a sample file —
confirmed by comparing multiple real projects, not just inferred from one:

**Shape A — relative path + separate stale absolute base** (e.g. `AcidJogger.mxprj`,
`ADaftPlayBox.mxprj`, `Adapter2nd.mxprj`, `AlgoRythm.mxprj`, most of `A320.mxprj`):

```
26 "AcidJogger Samples/AcidJogger Kick.wav"      <- relative to wherever the project file lives
00 00 01
3a "/Users/frankvonwelt/Producing/Maschine/PerformanceProjects"   <- stale absolute base (58 chars)
```

The relative path is what actually matters for resolving the sample on disk; the absolute base
is a leftover from the machine the project was authored on and is expected to be stale once the
project moves.

**Shape B — single, already-absolute path, no relative field at all** (e.g. `Adler.mxprj`,
`AFF8.mxprj`, some samples in `A320.mxprj`):

```
57 "/Users/frankvonwelt/Producing/Maschine/PerformanceProjects/Adler Samples/Adler Kick.wav"
```

There is no separate relative-only string for these — the full original absolute path is stored
as one field. We don't know how (or whether) Maschine falls back to a same-named local file for
these, so the reader tool reports this shape as its own "unverifiable" category rather than
guessing it's broken or fine. In practice this looks like it happens for samples that weren't
added from a location Maschine recognized as a project-relative library root (e.g. dragged in
from an arbitrary folder) — but that's an inference, not confirmed.

The project's actual samples live next to the `.mxprj` file, typically in a `<ProjectName>
Samples` sibling folder — some projects are "standalone" and have no such folder at all (e.g.
`STROM.mxprj`), presumably referencing NI factory library content instead (its stale absolute
bases point at paths like `/ni/content/Native Instruments/Maschine 2 Factory Library/` and
`/run/media/mmcblk0p1/Native Instruments/<expansion name>/` — the latter being an SD-card mount
path from a Linux-based device, consistent with Maschine+ hardware).

This is the basis for the heuristic sample-reference scanner in `src/MxprjReader` — it scans for
Pascal strings generically and classifies each sample-looking one (Shape A vs. B) rather than
fully parsing the surrounding chunk structure.

### Maschine 2 vs. Maschine 3

Two independent approaches were tried:

1. **Byte-level detection (inconclusive).** All example files checked share the exact same
   `2SAM`/`MAS2` container signature, and other embedded version-looking strings (e.g. `2.17.5.0`,
   `1.7.14`) appear identically across files regardless of Shape A/B usage — most likely a fixed
   library/build version, not the Maschine app version. No byte-level marker found maps cleanly to
   "M2" vs "M3". Since there's no confirmed-M2-only or confirmed-M3-only file to diff against, the
   reader does not attempt to guess a second container format from bytes. It records the
   signature it detects and only parses files matching the one known-good signature; anything
   else is reported as `Unrecognized format` rather than risking a silent mis-parse.
2. **Folder-based detection (what the app actually uses).** On a real Maschine+ SD card, project
   files live under a fixed, version-specific layout:
   `Native Instruments\Maschine 2\Projects\...` and `Native Instruments\Maschine 3\Projects\...`.
   `ProjectFinder` in `src/MxprjReader` uses **this** to tag each scanned project's version — it's
   reliable and doesn't depend on decoding file bytes at all. This is the answer to the earlier
   open question, once the real folder layout was known.

### Known extensions considered "sample" references

`.wav`, `.aif`, `.aiff`, `.flac`, `.ogg`

## Part 2 — `.mxgrp` / `.mxsnd` (Groups / Sounds) — secondhand research notes

**Not currently parsed by this tool.** These are different file types than `.mxprj` (a Group is a
kit/pad layout, a Sound is a single instrument patch — both can be saved standalone from within a
project). The notes below come from an earlier session's web research, condensed here; treat them
as leads only, not verified findings, and re-verify against real files before relying on any of
it the way Part 1 has been verified.

### File types (officially confirmed by NI's own docs)

| File | Meaning |
|---|---|
| `.mxprj` | Maschine Project |
| `.mxgrp` | Maschine Group |
| `.mxsnd` | Maschine Sound |
| `.mxinst` | Instrument plug-in preset |
| `.mxfx` | Effect plug-in preset |

Older Maschine 1 formats (`.msnd`, `.mgrp`) are a different, older lineage — not the same as
`.mxsnd`/`.mxgrp` and not compatible without conversion.

### What's known about `.mxgrp`

- Binary format, no official public spec.
- Contains sample paths and pad/sample assignments; a `.wav`-byte-sequence search is a workable
  way to locate sample references and correlate them with pad data (per a public write-up by
  "embercore", and the `joanroig/nitools` "Maschine Groups Exporter" tool, both of which
  deliberately extract sample references without fully decompiling the format).
- **String encoding**: originally reported as UTF-16LE; a later correction in that same research
  (`docs/mxgrp-format-findings.md`, not present in this repo) says that claim didn't hold up
  against 88 real `.mxgrp` files — 2064 ASCII `.wav` references were found and zero UTF-16LE ones,
  with strings instead being **varint-length-prefixed ASCII**. That matches what Part 1 above
  found independently for `.mxprj` (1-byte-length-prefixed ASCII), which is a useful cross-check
  that this is a shared NI string-encoding convention rather than a coincidence.
- Full header/magic, version fields, object hierarchy, and most parameter semantics are
  undocumented publicly.

### What's known about `.mxsnd`

- Confirmed as the Maschine 2/3 Sound format (Maschine 1 used `.msnd` instead).
- Described by the `git-moss/ConvertWithMoss` project as an "NI container format"; no complete
  public field-level spec was found.

### Useful external references (unverified, for future investigation)

- `joanroig/nitools` — https://github.com/joanroig/nitools — working `.mxgrp` sample-path
  extraction (Python), intentionally not a full decompiler.
- `git-moss/ConvertWithMoss` — https://github.com/git-moss/ConvertWithMoss — broader NI format
  reverse engineering including `.mxsnd`, with actual parsing code (Java) to inspect.
- `Ma5onic/ni-file` — https://github.com/Ma5onic/ni-file — generic NI container conventions, Hex
  Fiend templates.
- embercore, "How I made all Maschine drum kits usable in Ableton — Reverse engineering the
  .mxgrp format" — https://note.com/embercore/n/n9e9404c69247

### If `.mxgrp`/`.mxsnd` parsing is ever needed here

The original document's suggested method — differential binary analysis (save a controlled matrix
of Groups/Sounds changing one thing at a time, diff the bytes) — is the same general approach that
worked for `.mxprj` in Part 1, and is the recommended starting point if this tool ever needs to
read Group/Sound files directly instead of just Project files.
