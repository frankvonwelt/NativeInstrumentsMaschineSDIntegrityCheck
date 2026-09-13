# Maschine 3 File Format Reverse-Engineering Notes

> ⚠️ **CORRECTION NOTICE (added 2026-08-30 after empirical verification)**
>
> This document is a second-hand research snapshot consolidated from public
> web sources. It has since been checked against **323 real preset files**
> in `examples/`. Its central technical claim did **not** survive that check:
>
> - ❌ **"Sample paths are UTF-16LE" (§3.1, §9 — rated "High") is REFUTED.**
>   Across all 88 `.mxgrp` files: **2064 ASCII `.wav` refs, 0 UTF-16LE refs.**
>   Strings are varint-length-prefixed **ASCII**.
> - ✅ Several items listed as open "research gaps" in §14 are now **closed**
>   (file header, magic, version field, string encoding, `.mxsnd` container).
>
> **Read `docs/mxgrp-format-findings.md` first** — it is based on direct
> measurement of real files and takes precedence over this document wherever
> the two disagree. The body below is left unedited as a record of the
> original sources and for its still-useful leads (NITools, `ni-file`,
> the experiment matrices in §10).

> Research snapshot: 2026-08-30
>
> Scope: Native Instruments Maschine 2/3-era files, especially `.mxgrp` (Groups/Kits) and `.mxsnd` (Sounds).
>
> Status: This is a research notebook, **not an official specification**. The formats are proprietary. Findings below distinguish between confirmed facts, community reverse engineering, and hypotheses.

## 1. Executive summary

Native Instruments does not publish a public binary specification for Maschine's `.mxgrp` and `.mxsnd` files.

There is, however, useful community reverse engineering:

- `.mxgrp` has been partially reverse engineered well enough to recover sample assignments/pad mappings.
- A recent 2026 write-up by `embercore` describes `.mxgrp` as a binary format and reports that sample paths are visible as UTF-16LE strings. The author uses `.wav` byte sequences as markers and reads surrounding data to determine which sample is assigned to which pad.
- `joanroig/nitools` is an open-source implementation that scans `.mxgrp` files and extracts/processes sample data. Its README says the project intentionally focuses on extracting sample paths rather than fully decompiling the format.
- `git-moss/ConvertWithMoss` has reverse-engineered Native Instruments formats and identifies `.mxsnd` as the Maschine Sound v2/3 format for Maschine 2/3, described as an NI container format. Its documentation/code are worth inspecting for format detection and parsing architecture.
- A broader `ni-file` reverse-engineering project exists for Native Instruments formats, although it is not a complete Maschine 3 `.mxgrp`/`.mxsnd` specification.
- I did not find a complete public field-by-field `.mxsnd` specification.

## 2. Official file types

Native Instruments' Maschine documentation identifies these file types:

| File | Meaning |
|---|---|
| `.mxprj` | Maschine Project |
| `.mxgrp` | Maschine Group |
| `.mxsnd` | Maschine Sound |
| `.mxinst` | Instrument plug-in preset |
| `.mxfx` | Effect plug-in preset |
| `.wav`, `.aiff`, etc. | Audio |

Official documentation confirms that Groups can be saved as individual `.mxgrp` files and Sounds as individual `.mxsnd` files.

Sources:
- Native Instruments Maschine Software Manual:
  https://docs.native-instruments.com/ni-tech-manuals/maschine-software-manual/en/managing-sounds%2C-groups%2C-and-your-project
- Native Instruments Maschine browser documentation:
  https://docs.native-instruments.com/ni-tech-manuals/maschine-mikro-mk3-manual/en/browser

## 3. `.mxgrp` — known reverse engineering

### 3.1 Recent direct reverse engineering

Article:

**How I made all Maschine drum kits usable in Ableton — Reverse engineering the .mxgrp format**

Author: embercore
Date: 2026-04-04

https://note.com/embercore/n/n9e9404c69247

Important findings:

- `.mxgrp` is a binary format.
- There is no official public documentation.
- Sample paths can be observed inside the binary.
- Sample paths are encoded as UTF-16LE. — ❌ **REFUTED** on our 88-file corpus (0 UTF-16LE refs vs 2064 ASCII); see `docs/mxgrp-format-findings.md` §3.
- The `.wav` byte sequence can be used as a useful marker.
- By examining data around the path, it is possible to recover which sample belongs to which pad.
- The author's parser intentionally does **not** attempt to fully decode the format. It extracts only the pad → sample relationship needed for Ableton conversion.

This is currently one of the clearest public descriptions of practical `.mxgrp` parsing.

### 3.2 Open-source implementation: NITools

Repository:

https://github.com/joanroig/nitools

NITools is an unofficial open-source suite for extracting and converting Native Instruments resources.

Relevant module:

**Maschine Groups Exporter**

According to its README, it:

- scans folders for `.mxgrp` files;
- parses sample data;
- extracts/processes samples;
- supports pad reorder matrices;
- supports filtering;
- can fill blank pads;
- can include group preview samples;
- can build a JSON index containing raw group/kit metadata.

The project explicitly says:

> The tools development was focused on extracting sample paths from binary files instead of doing proper decompiling.

This is important: NITools demonstrates practical parsing, but it should not be treated as a complete file-format specification.

Repository structure mentioned by the project:

- `src/processors/groups/build_groups_json.py`
- `src/processors/groups/process_groups_json.py`

The `build_groups_json.py` path is especially worth examining when continuing the research.

## 4. What `.mxgrp` appears to contain

Confirmed or strongly supported:

- Group/kit identity/metadata
- Pad/sample assignments
- References to audio files
- Sample paths
- Group preview/sample information
- Other Group/Sound state that the current public parsers generally do not completely decode

The exact binary layout, offsets, field types, versioning, compression/container structure, and all parameter semantics are **not yet documented here**.

### Important caution

Do not assume every occurrence of a UTF-16LE path corresponds to a pad sample. A parser should establish context around each occurrence and correlate it with the surrounding structure.

## 5. `.mxsnd` — current state of public research

`.mxsnd` is the native Maschine Sound format.

Native Instruments confirms that Sounds can be saved individually with the `.mxsnd` extension.

The strongest additional lead found is:

**ConvertWithMoss / git-moss**

https://github.com/git-moss/ConvertWithMoss

Its format documentation identifies:

- Maschine Sound v1: `.msnd` — Maschine 1.x
- Maschine Sound v2/3: `.mxsnd` — Maschine 2.x/3.x

The project describes `.mxsnd` as an **NI Container format** and says its support is based on reverse engineering/community analysis.

DeepWiki summary:

https://deepwiki.com/git-moss/ConvertWithMoss/3.1-native-instruments-formats

This project should be investigated further because it appears to contain actual Java implementation details rather than only descriptive prose.

## 6. NI generic reverse engineering

Another relevant project:

**Ma5onic/ni-file**

https://github.com/Ma5onic/ni-file

This is broader Native Instruments format reverse engineering.

It is useful for:

- understanding NI container conventions;
- binary structures;
- Hex Fiend templates;
- file schematics;
- comparing formats used by different NI products.

It is not, by itself, a complete Maschine 3 `.mxgrp`/`.mxsnd` specification.

## 7. Maschine 1 vs Maschine 2/3

Do not confuse the older `.msnd`/`.mgrp`/related Maschine 1 formats with `.mxsnd`/`.mxgrp`.

Community discussions indicate that old Maschine 1 Groups used an older format and that compatibility/import behavior changed between product generations.

For current work, focus on:

- `.mxgrp`
- `.mxsnd`
- `.mxprj`

and treat old `.mgrp` / `.msnd` formats separately.

## 8. Native Instruments official documentation

Official Maschine documentation confirms:

### Groups

Groups can be saved as individual `.mxgrp` files.

https://docs.native-instruments.com/ni-tech-manuals/maschine-software-manual/en/managing-sounds%2C-groups%2C-and-your-project

### Sounds

Sounds can be saved as individual `.mxsnd` files.

https://docs.native-instruments.com/ni-tech-manuals/maschine-plus-manual/en/managing-sounds%2C-groups%2C-and-your-project

### Browser file types

The official browser documentation lists:

- Project: `.mxprj`
- Groups: `.mxgrp`
- Sounds: `.mxsnd`
- Instrument plug-in presets: `.mxinst`
- Effect plug-in presets: `.mxfx`

https://docs.native-instruments.com/ni-tech-manuals/maschine-mikro-mk3-manual/en/browser

## 9. Current knowledge table

| Area | Status | Confidence |
|---|---|---:|
| `.mxgrp` exists as Group format | Officially confirmed | High |
| `.mxsnd` exists as Sound format | Officially confirmed | High |
| `.mxgrp` is binary | Community RE | High |
| ~~`.mxgrp` contains UTF-16LE sample paths~~ ❌ **REFUTED** — paths are varint-prefixed ASCII | Measured on 88 files | High (refutation) |
| `.mxgrp` sample paths can identify pad assignments | Working community parser | High |
| `.mxgrp` fully documented | Not found | None |
| `.mxsnd` is Maschine 2/3 Sound format | ConvertWithMoss | High |
| `.mxsnd` is an NI container | ConvertWithMoss classification | Medium/High |
| `.mxsnd` fully documented | Not found | None |
| NI generic container research exists | Yes | High |
| NCW may occur in NI ecosystems | Known NI format; not proof of embedded NCW in every `.mxgrp`/`.mxsnd` | Medium |

## 10. Recommended next reverse-engineering step

If the goal is to actually reconstruct the format, the most productive approach is differential binary analysis.

Create controlled files in Maschine and compare them byte-for-byte.

### `.mxgrp` experiment matrix

Create:

1. Empty Group
2. Group with one sample on pad 1
3. Same Group with a different sample
4. Same Group with sample on pad 2
5. Same Group with two samples
6. Rename Group
7. Change Group color
8. Change pad/Sound name
9. Change volume
10. Change pan
11. Change pitch
12. Change sample start
13. Change sample end
14. Change choke/group settings
15. Change velocity parameters
16. Add/remove plug-in
17. Save under different Maschine versions

For every file:

- record file size;
- calculate hashes;
- run a binary diff;
- locate UTF-16LE strings;
- locate `.wav` occurrences;
- locate `.aif`/`.aiff`/`.flac` occurrences;
- compare changed regions;
- test whether values are little-endian integers, floats, doubles, or strings.

### `.mxsnd` experiment matrix

Start with:

1. Empty Sound
2. Sound with Sampler and one WAV
3. Change only WAV
4. Change sample start
5. Change sample end
6. Change tuning
7. Change volume
8. Change pan
9. Change filter
10. Change envelope
11. Change velocity response
12. Change Sound name
13. Change Sound color
14. Add an internal Maschine effect
15. Change one effect parameter
16. Remove effect
17. Add a Native Instruments plug-in
18. Change one plug-in parameter
19. Save identical Sound under different versions

This should reveal which sections are stable and which fields correspond to specific parameters.

## 11. Useful tooling

Recommended tools for further analysis:

- `xxd`
- Hex Fiend
- ImHex
- `radare2`
- Python `struct`
- Python `difflib`
- Kaitai Struct
- Binary Ninja / Ghidra if analyzing Maschine itself
- `strings` with UTF-16 support

For the initial work, a Python script that produces:

- offset
- byte length
- UTF-16LE decoded string
- surrounding hex
- ASCII interpretation
- nearby floating-point candidates

would be extremely useful.

## 12. Important legal/interoperability note

The projects found publicly describe themselves as independent reverse-engineering/interoperability tools. They do not provide Native Instruments proprietary source code.

Reverse engineering and interoperability can have legal constraints depending on jurisdiction and intended use. This document is intended as technical research notes, not legal advice.

## 13. Primary references

### Direct `.mxgrp` reverse engineering

embercore — “How I made all Maschine drum kits usable in Ableton — Reverse engineering the .mxgrp format”

https://note.com/embercore/n/n9e9404c69247

### Open-source `.mxgrp` extraction

joanroig / NITools

https://github.com/joanroig/nitools

### Native Instruments format reverse engineering

git-moss / ConvertWithMoss

https://github.com/git-moss/ConvertWithMoss

### Native Instruments generic format research

Ma5onic / ni-file

https://github.com/Ma5onic/ni-file

### Official Maschine documentation

https://docs.native-instruments.com/ni-tech-manuals/maschine-software-manual/en/managing-sounds%2C-groups%2C-and-your-project

https://docs.native-instruments.com/ni-tech-manuals/maschine-mikro-mk3-manual/en/browser

## 14. Research gaps

The following remain open:

- Exact `.mxgrp` header/magic
- `.mxgrp` version fields
- Complete object hierarchy
- Pad object structure
- Sound object structure inside a Group
- Exact encoding/length representation of strings
- Sample-path reference structure
- All numeric parameter types
- Group metadata fields
- Preview metadata
- Plug-in state representation
- Exact `.mxsnd` container structure
- `.mxsnd` header/version details
- `.mxsnd` sample reference structure
- `.mxsnd` internal Sampler state
- `.mxsnd` plug-in state
- Compression/encryption details, if any
- Maschine 3-specific changes relative to Maschine 2
- Maschine+ differences
- Compatibility/version negotiation

## 15. Bottom line

There is enough public information to start building a real `.mxgrp` parser today.

The strongest starting points are:

1. embercore's direct `.mxgrp` reverse engineering;
2. `joanroig/nitools` for working extraction code;
3. `git-moss/ConvertWithMoss` for broader NI/Maschine format reverse engineering;
4. `Ma5onic/ni-file` for generic NI container knowledge.

There does **not** appear to be a complete public `.mxsnd` specification yet.

The next useful step is therefore not more speculation about the format, but a controlled corpus of Maschine 3 `.mxgrp` and `.mxsnd` files followed by differential binary analysis. That could turn these scattered observations into an actual field-level specification.
