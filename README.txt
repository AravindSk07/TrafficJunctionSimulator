# Junction Lab

A small Unity traffic-junction simulator with a low-poly downtown scene, protected turns, queueing, adjustable arrivals, and fixed/adaptive signals.

![Running scene preview](Evidence/scene-preview.png)

The preview is rendered from the running simulation's camera; the interactive dashboard is outside this scene-only capture.

## Open and play

1. In Unity Hub, choose **Add > Add project from disk** and select this directory.
2. Open with **Unity 6000.5.6f1**, the installed version used for this project.
3. Open `Assets/Traffic/Scenes/TrafficJunction.unity` and press Play.
4. Use the dashboard to change arrival demand and green duration, pause, reset, switch fixed/adaptive timing, or change simulation speed. The bottom-right controls show routes, rotate the camera, and toggle top view.

The source project uses the Built-in Render Pipeline with Standard materials, not URP. No Asset Store purchases or external art downloads are required. The Windows build, when supplied alongside the source, can be run directly without the editor.

## Features

- Four approaches, one incoming and one outgoing lane per road; right-hand traffic.
- Twelve distance-based routes: straight, left, and right from each approach.
- Seeded traffic demand with capacity and spawn-spacing limits.
- Acceleration, braking, following gaps, and FIFO approach queues.
- Green, yellow, and all-red phases; existing reservations drain before the next green.
- Conservative exclusive conflict-zone reservation for protected turns.
- Optional demand/age-weighted signal selection with a minimum green duration.
- Reused vehicle GameObjects, three original car prefabs, and reusable scenery.
- Dashboard for completed trips, active vehicles, completed-trip mean stopped time, queues, and simulation time.
- Deterministic simulation checks and a standalone runtime smoke-test mode.

## Configuration

Edit `Assets/Traffic/Settings/DefaultTraffic.asset` to change the seed, speed, acceleration, vehicle cap, demand, and signal durations. Runtime dashboard changes affect only the current run and are preserved by Reset; they do not alter the saved asset.

The demand slider reports requested arrivals over all four approaches. Arrivals are rejected when the spawn area or capacity is full. **Deferred arrivals** counts these rejected attempts; they are not placed into an off-screen queue.

## Verification

Use **Junction Lab > Run Simulation Checks**. Checks cover six deterministic scenarios, admission rules, intersection exclusivity, vehicle separation, throughput, signal fairness, blocked spawns, and final drain. Results are written to `Evidence/simulation-checks.txt`.

Use `JunctionLab.Editor.ProjectBuilder.BuildWindows` as a Unity `-executeMethod` to build the Windows player. Run the player with `-trafficSmoke` to exercise runtime controls, save a screenshot, and exit. See `docs/TESTING.txt` for exact commands.

## Scope and tradeoffs

This is an educational prototype, not a calibrated traffic-engineering tool. One vehicle owns the junction conflict zone at a time, including during protected turns. That keeps the rules easy to inspect but deliberately sacrifices throughput. No pedestrians, lane changing, emergency vehicles, network multiplayer, or vehicle dynamics are modeled. Crosswalks are decorative. Scene road geometry and route anchors are coupled; editing route geometry requires matching scene changes.

Vehicle following currently scans active vehicles, so it is O(n²), with a small configurable cap. It is not a Jobs/Burst implementation or evidence of mobile performance. High demand can saturate the road and raise rejected-arrival counts.

## Attribution and portfolio use

Created from scratch with AI assistance in this chat. No public repository was copied. Geometry is assembled from Unity primitives; no third-party art pack is included. See `LICENSE` and `ASSET_CREDITS.txt`. Review and understand the code before describing your contributions in interviews. Add your subsequent changes to `docs/PORTFOLIO.txt`.

The project is intentionally left uncommitted. `COMMIT_PLAN.txt` describes 20 logically separate commits you can make after reviewing the finished files, with actual commit timestamps.
