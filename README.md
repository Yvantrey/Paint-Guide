# RUGA

RUGA is an offline Android app that helps beginner and hobby painters (14+) paint on a real canvas. The phone camera shows the canvas on a table, and RUGA overlays a transparent drawing guide at the canvas's real size. The painter follows the guide stage by stage and sees how long each stage took.

Repository: Paint-Guide. App name: RUGA (formerly PaintGuide AR).

## Features
- Horizontal surface detection and tap-to-place, held in place with an AR anchor
- Guide sized from the canvas width and height in cm, drag, two-finger rotate, lock and unlock
- Guide opacity (10-100%), colour (cyan, yellow, white), outline and grid toggles
- Three paintings (Mountain Sunrise, Ocean Calm, Little Sunflower) and three beginner warm-ups
- Stage-by-stage painting with per-stage and total time
- Local saving of the latest 20 sessions and remembered preferences
- Camera permission, unsupported-device and tracking-lost messages
- Fully offline: the only permission is the camera, and the camera feed is never saved

## Built with
- Unity 6000.4.7f1, Universal Render Pipeline, C#
- AR Foundation 6.4.3 and Google ARCore XR Plugin 6.4.3
- uGUI and TextMeshPro, Input System

## Getting started
1. Open the project in Unity 6000.4.7f1 with Android Build Support installed.
2. Run **RUGA > Setup > Full App** once to create the scene wiring, fonts, icon and splash screen.
3. Press Play to try it in XR Simulation, or use **RUGA > Build > Android APK** to build `Builds/RUGA.apk`.
4. Real AR needs an ARCore-supported Android phone (Android 10 or newer).

## Project structure
- `Assets/Scripts/Core` - app controller and state machine (Home, Catalog, Scanning, Placing, Aligning, Painting, Summary)
- `Assets/Scripts/AR` - AR session, surfaces, anchors, tracking and camera permission
- `Assets/Scripts/Guide` - the guide overlay and touch gestures
- `Assets/Scripts/Session` - stages and timing
- `Assets/Scripts/Data` - paintings (factory), session and preference repositories
- `Assets/Scripts/UI` - screens built in the RUGA theme
- `Assets/Resources/Paintings` - painting and warm-up definitions (JSON) and their images

See `updates.md` for decisions, the artwork spec and progress notes.

## Credits
- Copperplate CC font by Owen Earl (indestructible type*) for The Cowboy Collective, SIL Open Font License 1.1
- Phosphor Icons, MIT License
- Liberation Sans (TextMeshPro default font), SIL Open Font License 1.1
- All paintings and warm-up art are original
