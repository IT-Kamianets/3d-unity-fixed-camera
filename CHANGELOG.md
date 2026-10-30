# Changelog

## 0.1.0

- `CameraShotVolume`: a box that picks a fixed shot, with priority, cut or blend, field of view and a shot id.
- `FixedViewCamera`: follows a subject by choosing the shot of the volume it stands in, with hysteresis at the borders, and an inspection override for close-ups.
- `CameraShotVolume.Select`: the pure choice, usable in tests.
