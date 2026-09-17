---
name: Bug report
about: Something is broken, or behaves differently than it should
title: ''
labels: bug
assignees: ''
---

## What happened

A clear description of what went wrong.

## What you expected instead

## Steps to reproduce

1.
2.
3.

## Screen recording

Anything to do with the rope, the charm or the animation is far easier to show
than to describe. A few seconds is plenty.

## Your setup

- **macOS version:**
- **Mac model:** (Apple menu → About This Mac — e.g. MacBook Air M2)
- **Dangly version:** (Settings → About)
- **Where the build came from:** downloaded release / built from source
- **Display:** built-in / external, and its refresh rate if you know it

## Console output

If the app misbehaved rather than just looking wrong, this often has the answer:

```sh
log show --predicate 'subsystem == "com.dangly.Dangly"' --last 10m --style compact
```

Note that release builds log only warnings and errors by design.

## Anything else

Other menu bar apps involved, an unusual display arrangement, Reduce Motion on,
something you were doing at the time.
