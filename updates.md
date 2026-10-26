# RUGA

Offline Android AR painting-guide app (formerly PaintGuide AR). The phone camera shows a canvas on a table and a transparent, tintable guide is overlaid on it. The user paints the real canvas in four stages.

## Fixed decisions
- Unity 6000.4.7f1, URP template, Android only (ARCore phones), AR Foundation + Google ARCore XR Plugin.
- Fully offline: no network calls, no analytics, and the camera feed is never saved. Avoid INTERNET and other unneeded permissions.
- Portrait. uGUI + TextMeshPro for UI. Input System package for touch (EnhancedTouch).
- Target: 30+ FPS on a mid-range phone.

## Architecture
- Managers: UIManager, ARTrackingManager, GuideManager, SessionManager, DataManager.
- Patterns: State (Home, Catalog, Scanning, Placing, Aligning, Locked/Painting, Summary), Observer (C# events: OnStageChanged, OnTrackingLost, OnTrackingRestored, OnGuideLocked, OnSessionCompleted; managers don't reference each other), Factory (PaintingFactory builds Painting/Stage from JSON), Repository (ISessionRepository, IPreferencesRepository).
- Data: Painting(id, name, label, stages), Stage(index, title, instruction, guide image names), SessionRecord(paintingId, startTime, StageRecords, totalDuration), Preferences(guideColour, defaultOpacity).
- Paintings load from Assets/Resources/Paintings (TextAsset + sprites). Sessions go to Application.persistentDataPath as JSON (keep the latest 20).
- Scripts live in Assets/Scripts/{Core, AR, Guide, Session, Data, UI}.

## v1 scope
Horizontal planes, tap-to-place with anchor, drag/rotate, size typed in cm (cm / 100 = Unity metres), lock/unlock, opacity 10-100%, outline and grid toggles, guide colours cyan #67e8f9 / yellow #ffe07a / white, 3 paintings x 4 stages, timing + summary, camera-permission and ARCore-availability handling, tracking-lost warning after 2 s.
Paintings: Mountain Sunrise (Outline, Sky, Mountains, Details), Ocean Calm (Outline, Sky, Ocean, Details), Little Sunflower (Outline, Background, Petals, Details).

## Theme
bg #141918, card #242b28, text #f0f1e9, muted #a7b1aa, lines #3a443e, accent #cfdfa1, deep green #1b2619. Serif titles, sans-serif body, large touch targets.

## Assets rule
The Shutterstock-watermarked line drawing is a private style reference only and must never ship. Keep a credits list for every third-party asset.

## Milestones
1 Setup + camera feed on phone, 2 Plane + tap-to-place with anchor, 3 Guide controls, 4 Data layer, 5 State machine/flow/timing, 6 Saving, 7 Messages, 8 UI polish + device perf pass.

## Progress log
- 2026-10-23 M1: AR Foundation + ARCore XR Plugin 6.4.3, ARCore loader on Android (Required, Depth Optional), AR Background Renderer Feature on Mobile_Renderer, Portrait, package com.rugambanzizayvan.ruga, ARWorkspace scene. Setup automated in Assets/Scripts/Editor/RugaProjectSetup.cs (menu RUGA > Setup). Phone build test still pending.
- 2026-10-24 XR Simulation enabled on the Desktop tab for in-editor testing.
- 2026-10-24 M2 code: ARTrackingManager (planes, raycast, anchors, OnSurfaceFound), GuidePlacement (tap to place/move GuideTest prefab on an anchor). Run RUGA > Setup > Milestone 2.
- 2026-10-24 Decision: all artwork is our own and hand-made by the user (no third-party or generated images; only temporary test patterns until the real art is added). Confirmed addition: a "Warm-up" section of follow-the-line guides for total beginners (lines/curves, basic shapes, one-line drawings), same Painting/Stage model plus a category field (warmup | painting), 1-4 stages each. Add in Milestone 4.
- 2026-10-24 Test phone: user's Xperia (Android 12) is NOT ARCore-compatible (Play Store says incompatible). Keep developing in XR Simulation; need a borrowed ARCore-supported phone for real-device tests. The Xperia is useful for testing the 'AR not supported' message (Milestone 7).

## Artwork spec (agreed 2026-10-24)
- Files: Assets/Resources/Paintings/<id>.json plus folder <id>/ with <id>_stage1.png ... and <id>_final.png (names as in the JSON, no .png there).
- Guide layers: transparent PNG, pure white lines/shapes only (the app tints them), 1536 x 2048 px portrait (3:4), all layers of one painting aligned, lines ~6-10 px.
- Final image: full colour, same 3:4 portrait shape.
- Missing images show a generated test pattern (numbered grid + "top" arrow), never real art.
- Do not put any other files (README etc.) in Resources/Paintings: every text file there is read as a painting.
- 2026-10-24 Built Milestones 3-7 + warm-ups (code compiles for editor and Android with 0 errors/warnings; not yet run in Unity). Setup: RUGA > Setup > Full App (replaces the Milestone 2 menu; GuidePlacement.cs removed).

## Code map (as built 2026-10-24)
- Core/AppController.cs: composition root. Loads data, builds UI, creates states, routes UI + gesture events to the current state, Android back = Escape.
- Core/AppStateMachine.cs: AppState enum, AppContext (shared refs), AppStateMachine.
- Core/AppStates.cs: Home, Catalog, Scanning, Placing, Aligning, Painting, Summary. States are the only place that uses several managers; managers never reference each other.
- AR/ARTrackingManager.cs: StartAR (availability, install, camera permission), ResetAR/StopAR, plane raycast, anchors (AttachAnchor, free-anchor fallback), tracking-lost after 2 s. AR/CameraPermission.cs: Android permission.
- Guide/GuideManager.cs: guide prefab (Art + Grid quads), size cm->m, move/rotate, lock, opacity, colour, outline/grid/hide. GuideGestures.cs: tap / drag / two-finger twist, ignores touches on UI. GuideTextures.cs: grid + test-pattern placeholders.
- Session/SessionManager.cs: stages, timer (paused when unlocked or app in background), raises OnStageCompleted (saved every stage) and OnSessionCompleted.
- Data/: PaintingFactory (JSON + Resources images), Repositories (ISessionRepository -> JSON file in persistentDataPath, latest 20; IPreferencesRepository -> PlayerPrefs), DataManager.
- UI/: UIManager builds all screens from code (uGUI + TMP) in the prototype's style; UITheme holds colours/sizes; UIFactory builds controls. Fonts: assign TMP font assets on UIManager (titleFont, bodyFont).

## Still to do
- Run in XR Simulation and fix anything found.
- Real-device test on a borrowed ARCore phone: camera, ruler scale test, FPS, permission flow, tracking-lost.
- Milestone 8: user picks fonts (Google Fonts, check licence) and icons; check the final Android manifest has no INTERNET permission; performance pass.
- Artwork from the user (3 paintings x 4 stages + 3 warm-ups).

## Fonts and icons (decided 2026-10-24)
- User wanted Copperplate Gothic. The Windows copy (C:\Windows\Fonts\COPRGTB.TTF, (c) URW/Microsoft) may NOT be bundled in an app; Monotype sells app licences. Used instead: Copperplate CC, an open-source Copperplate Gothic revival by Owen Earl / The Cowboy Collective, SIL OFL 1.1 (verified in github.com/CowboyCollective/CopperplateCC OFL.txt). Used for titles and small labels; body text stays Liberation Sans (TMP default) for readability. Copperplate CC has no arrow glyphs; arrows come from the icon font.
- Icons: Phosphor Icons regular, MIT licence (verified, @phosphor-icons/web 2.1.2), used as a font (Assets/UI/Icons/Phosphor.ttf) through TMP. Codes in Assets/Scripts/UI/Icons.cs.
- Setup (RUGA > Setup > Full App) creates the TMP SDF font assets and assigns titleFont/iconFont on UIManager.

## Credits list
| Asset | Source | Licence | Licence file |
|---|---|---|---|
| Copperplate CC font | github.com/CowboyCollective/CopperplateCC | SIL OFL 1.1 | Assets/Resources/Licences/CopperplateCC-OFL.txt |
| Phosphor Icons font | unpkg.com/@phosphor-icons/web@2.1.2 | MIT | Assets/Resources/Licences/Phosphor-LICENSE.txt |
| Liberation Sans (TMP default) | TextMeshPro Essential Resources | SIL OFL 1.1 (confirm the licence file in Assets/TextMesh Pro after import) | - |
| Paintings and warm-up art | Original, by the user | - | - |
The app shows this list under Settings > Credits & licences.
- 2026-10-24 First APK built by batch mode (Assets/Scripts/Editor/RugaBuild.cs, menu RUGA > Build > Android APK): Builds/RUGA.apk, 41 MB, arm64, minSdk 29 (Android 10+), targetSdk 36. Unity added android.permission.INTERNET automatically ("Internet Access: Auto"); removed via Assets/Plugins/Android/LauncherManifest.xml (tools:node="remove", Custom Launcher Manifest enabled). Verified with aapt: only CAMERA (+ AndroidX's internal DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION, app-private). Not yet run on a device or in Play mode.
- 2026-10-25 App icon added: user-supplied image (original kept at Design/ruga_icon_source.jpg). Cropped to the tile interior, art scaled to fit the round-mask safe zone, background gradient extended: Assets/UI/AppIcon/ruga_icon.png + ruga_icon_background.png (1024 px, uncompressed). Set as the Android adaptive icon by RUGA > Setup > Full App. Unity "Engine Diagnostics" data collection turned off (ProjectSettings/UnityConnectSettings.asset). Rebuilt APK: 40.9 MB, 0 errors, permissions CAMERA only.
- 2026-10-25 Branding: Home badge now shows the RUGA logo (UIManager.logo = ruga_icon.png). Startup splash: Unity logo off, RUGA logo (Assets/UI/AppIcon/ruga_splash.png, Sprite/Single) for 2 s on #141918. APK rebuilt: 41.8 MB, permissions CAMERA only.

## Theme v2: botanical parchment (2026-10-26)
User supplied four mood mockups (dark-green torn-paper cards; cream paint palette; cream step-by-step guide; cream "Discover your palette"). Followed the light parchment direction (3 of 4, and the step-guide screen matches RUGA's paint panel).
- Palette (UITheme.cs): parchment #f3ece0, paper #fbf7f0, ink #3a2a20, muted #7d6b5d, line #ddd0bf, button outline #8c6b57, terracotta #c4643f (primary), sage #93a888, ochre #d49a3a.
- Paper-grain backdrop (generated, tiled), paper cards with soft shadows, outlined brown secondary buttons, terracotta primary buttons.
- Stage progress drawn as leaves on a stem (done terracotta, current sage, upcoming pale); leaf icon on "Next stage"; sprig next to the RUGA title.
- Fonts: titles Copperplate CC (kept), body Alegreya (SIL OFL 1.1, google/fonts ofl/alegreya, variable font used at its default Regular weight).
- AR surface highlight now terracotta; splash background parchment.
- Kept: AR guide colours cyan / yellow / white (they must stand out on a real canvas).
- Not adopted from the mockups: bottom tab bar, digital paint palette, colour-adjust sliders (RUGA is a guide for real painting). Possible later addition: per-stage paint swatches ("Gouache swatches") in the JSON.

## Content and Reference mode (2026-10-26)
- Paintings now have real images: the three prototype artworks (Mountain Sunrise, Ocean Calm, Little Sunflower) rendered from the user's clickable prototype SVG code with Edge headless at 1536 x 2048 (finals in colour; stage guides white on transparent, current stage area filled at 25%). Replace with final hand-made art any time, same file names. Warm-ups still use test patterns until drawn.
- Per-stage "Paint swatches" (JSON field "swatches": [{name, hex}]), colours taken from the prototype artwork. Shown in the AR paint panel and Reference mode (StageView.cs).
- Reference mode (AppState.Reference): paint without AR. Stage guide in brown ink over a faint finished painting on a paper "canvas", toggle to the finished painting, swatches, stages, timer, saving and summary as in AR. Entry: catalog "Paint without AR", and the "This phone can't run AR" message.
- AR paint panel: "Reference" link opens the finished painting full screen.
- Editor tool: RUGA > Preview Screens renders Home, Catalog, Reference, AR paint panel, Summary and Settings to Builds/Preview.
