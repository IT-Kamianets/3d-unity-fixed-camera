# Unity Fixed Camera

Fixed cinematic shots for third-person games (the classic survival-horror look). Game-neutral: no story, puzzle or character code.

- `CameraShotVolume` (a box): while the subject is inside, the camera sits at its `Anchor` and looks at the subject. It has a `ShotId`, `Fov`, `Priority` and `Cut` (jump instead of blend).
- `FixedViewCamera` (on the camera): set `Subject`; it chooses the shot each frame. Leaving a shot needs the subject to be `Hysteresis` metres outside its box, unless another volume has a higher priority, so standing on a border never flickers.
- `PushOverride(anchor, look, fov)` / `PopOverride()` take a dedicated close-up shot (a puzzle, an inspection) and restore the previous one.
- `MovementYaw`: the yaw to move relative to. It is frozen while a movement key is held across a cut, so a cut never reverses held input (call `HoldBasis(true)` while input is held).

```csharp
var camera = Camera.main.gameObject.AddComponent<FixedViewCamera>();
camera.Subject = player.transform;
```

Selection is a pure function, `CameraShotVolume.Select(volumes, point, current, hysteresis)`, so it can be tested without physics.
