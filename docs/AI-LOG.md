# AI Mistake Log

**Declaration of AI tool usage:** this project was built pair-programming
with Claude (Anthropic). The AI drafted code, editor tooling and
documentation and ran the headless verification pipeline; every step was
human-reviewed, play-tested and explicitly approved before each commit.
Below is the candid, dated log of what the AI got wrong along the way: what
it suggested, what actually happened, how it was caught, and what fixed it.
Entries run oldest to newest. Its value depends on being complete rather
than flattering — nothing that cost a cycle has been left out.

---

## 2026-08-11 — Planning: two Unity 6.3 facts wrong in the initial AI plan

Caught before implementation by verifying the draft plan against current Unity
6000.3 documentation and forum reports:

- **Suggested:** fix pink Mixamo materials via `Edit > Rendering > Materials >
  Convert Selected Built-in Materials to URP`.
  **Actual:** that quick-convert menu was removed in Unity 6.3; only
  `Window > Rendering > Render Pipeline Converter` remains.
  **Caught by:** web check against Unity discussions ("Unity 6.3: URP Converter
  Removed?"). **Fix:** plan now references the converter window, with a manual
  URP Lit material as the lighter alternative for a single character.

- **Suggested:** treating "Asset Serialization = Force Text" and "Visible Meta
  Files" as settings that must be changed, with Visible Meta Files placed under
  Editor settings.
  **Actual:** both are already the defaults in Unity 6.3, and Visible Meta Files
  lives in its own category: `Project Settings > Version Control`.
  **Caught by:** Unity 6000.3 manual pages for Editor and Version Control
  settings. **Fix:** step 1 verifies these instead of changing them.

## 2026-08-11 — Step 1: headless Unity Hub install appeared to run for 4.5 hours

- **Suggested:** running the Unity Hub headless module install as a background
  command with its output piped through `tail`.
  **Actual:** the install finished long before, but Hub's orphaned
  `chrome_crashpad_handler` process inherited the output pipe and never closed
  it, so the command sat "running" for ~4.5 hours with nothing left to do.
  **Caught by:** Pedram questioning the runtime; process inspection then showed
  0% CPU, no file writes in 10+ minutes, and complete module sizes (OpenJDK
  235 MB, SDK 865 MB, NDK 2.4 GB).
  **Fix:** killed the orphaned handler (pipe closed, command exited 0) and
  verified the toolchain directly — java, adb and the NDK all run. Lesson:
  redirect headless-Hub output to a file instead of holding a pipe on it.

## 2026-08-11 — Step 1: first headless editor run failed — no Unity license

- **Suggested:** running the first batchmode import/configuration on the
  assumption that a Unity Personal license was active, because two Hub-managed
  editors were already installed; the run was even reported as "finished
  cleanly (exit 0)".
  **Actual:** the wrapper shell exited 0, but Unity itself exited 198 — "No
  valid Unity Editor license found". No license store exists on this machine;
  installing editors via Hub does not activate anything. None of the project
  settings were applied.
  **Caught by:** post-run verification — ProjectVersion.txt missing and
  ProjectSettings.asset still holding template defaults — then reading the
  editor log instead of trusting the exit code.
  **Fix:** license activation requires a Unity Hub sign-in (account
  credentials), which only Pedram can do; re-run the setup afterwards. Lessons:
  verify license state before the first headless run, and never report an
  aggregate shell exit code as the tool's own result.

## 2026-08-11 — Step 1/2: "Packages with Errors" dialog on first interactive open

- **Suggested:** creating the project by manually unpacking the Hub template
  tgz, described as producing an "identical result to Hub's Universal 3D
  template", then committing the manifest as "100% stock".
  **Actual:** Unity Hub also writes `ProjectVersion.txt` at creation time; the
  manual unpack skipped that. Opening a project with no version stamp made
  Unity run its ancient-project migration, silently injecting legacy packages
  (deprecated `com.unity.purchasing` IAP, `com.unity.analytics`, 2D tooling,
  `com.unity.multiplayer.center`, `com.unity.xr.legacyinputhelpers` — the last
  against the brief's "no XR packages"). Commit 1 shipped that manifest, and
  the first interactive open greeted Pedram with a "Packages with Errors"
  dialog caused by the deprecated IAP package.
  **Caught by:** Pedram screenshotting the dialog; the editor log then showed
  the deprecation notice, and all legacy-service flags being disabled ruled
  out the first theory (services migration) in favour of the missing version
  stamp.
  **Fix:** removed the six auto-injected packages from `Packages/manifest.json`,
  verified a clean headless re-resolve/recompile, committed as its own chore
  fix. Lesson: after reproducing a Hub behaviour by hand, diff the result
  against what Hub actually produces — including generated files, not just the
  template payload.

## 2026-08-11 — Step 4: three wrong theories before the greybox scene had light

The first framing render came out near-black. It took two wrong AI theories to
reach the real cause; each was falsified by rendering an image rather than
by argument:

- **Theory 1 (partly wrong):** "the template's default light points away from
  the camera-facing surfaces" — rotating the sun changed shadows (proving
  rotation worked) but the scene stayed dark; a first rotation guess (yaw 205°)
  even put the whole play area inside the back wall's shadow.
- **Theory 2 (wrong):** "`Camera.Render()` is a legacy path URP doesn't support,
  so the screenshot itself is lying." Switching to the supported
  `RenderPipeline.SubmitRenderRequest` produced a pixel-identical image,
  falsifying the theory. The supported API was kept anyway.
- **Actual cause, found by an A/B diagnostic (shadows off / intensity ×5 /
  default rotation):** with shadows disabled the scene rendered perfectly —
  the URP assets' default ~50 m shadow distance stretched over a ~4 m room
  degraded shadow-map precision until everything self-shadowed to black,
  amplified by the room-sized floor/wall cubes acting as shadow casters.
  **Fix:** shadow distance 12 m on both RP assets; floor and wall set to not
  cast shadows (they cannot shadow anything the fixed camera sees). Lesson:
  when a render looks wrong, bisect with A/B images instead of stacking
  plausible API theories.

## 2026-08-11 — Step 9: speech balloon rendered as an empty strip, three stacked causes

- **Suggested:** creating the balloon's TextMeshProUGUI in an editor script and
  relying on TMP's default font to apply, with essentials imported via
  `AssetDatabase.ImportPackage` inside the same batch run.
  **Actual:** three compounding failures. (1) A script-created TMP component
  serializes `font = null` and renders no text at all — in the editor and in a
  player build alike. (2) `ImportPackage` in a `-quit` batch run only queues
  the import; nothing persists, so the "imported" essentials vanished between
  runs. (3) The defensive rewrite `TMP_Settings.defaultFontAsset != null ? …`
  still crashed, because that property's *getter* itself throws
  NullReferenceException before TMP settings load.
  **Caught by:** the step's screenshot verification — an empty white strip
  where the sentence should be — then reading the editor log for the NRE.
  **Fix:** essentials imported once via Unity's synchronous `-importPackage`
  command-line argument; the font asset loaded directly by asset path and
  assigned explicitly; the wiring refuses to build the balloon (with a clear
  error) if essentials are absent. Lesson: render-verify UI built by scripts —
  each of these three failures was invisible in code review and obvious in a
  screenshot.

## 2026-08-11 — Step 11: NullReferenceException on Play — Awake-order assumption

- **Suggested:** TherapistPanel applying its default control values in
  `Awake()`, calling into `AmbientNoise.SetLevel()`, which used the
  AudioSource its own `Awake()` creates.
  **Actual:** Unity guarantees no ordering between different components'
  Awake calls; on the real machine the panel's Awake ran first and SetLevel
  dereferenced a null AudioSource. The static screenshot verification could
  never catch this — it is a Play-mode-only failure.
  **Caught by:** Pedram pressing Play and screenshotting the Console.
  **Fix:** defaults now applied in `Start()` (guaranteed to run after all
  Awakes), and AmbientNoise initializes lazily so early calls are safe
  regardless. Lesson: cross-component initialization belongs in Start, and
  editor-side render checks do not exercise runtime lifecycle order.

## 2026-08-11 — Extension: touch input relied on mouse emulation — stuck highlights and every-other-tap misses

- **Suggested:** letting Android input flow through Unity's mouse emulation
  (`Input.mousePosition` / `GetMouseButtonDown`), as the click router did on
  desktop.
  **Actual:** two device-only failures. The emulated pointer keeps reporting
  the last touch position forever, so the hover highlight froze ON after
  every tap (a red apple stayed "orange" until the next tap elsewhere); and
  the emulated position lags a frame on device, so tap N was processed at
  tap N−1's position — hits landed roughly every other tap. Several items
  were also genuinely tiny targets (the 3 cm chocolate bar at 4 m).
  **Caught by:** Pedram play-testing on his phone and describing the exact
  cadence ("almost every other click"), which pointed straight at the
  stale-position mechanism.
  **Fix:** the router reads touches directly (position from the touch,
  click on touch-began, hover only while a finger is down — as press
  feedback), and item prefabs carry padded box colliders guaranteeing ~10 cm
  of tappable extent per axis. Lesson: mouse emulation is a compatibility
  shim, not an input path — and touch targets need physical minimum sizes.

## 2026-08-12 — Environment builder repainted the groceries, and the AI explained away the first report

- **Suggested:** EnvironmentArtBuilder dressing the shelf via
  `GetComponentsInChildren<Renderer>()`, documented as "idempotent — safe to
  re-run" and exposed as a menu item.
  **Actual:** the item displays live under the shelf's anchors, so running
  the menu item standalone painted every grocery wood-brown and saved the
  scene. The automated pipeline always re-ran the item stage afterwards,
  masking the bug in every scripted verification. Worse: when Pedram first
  reported sepia items in a phone screenshot, the AI produced a plausible
  wrong explanation (a night-mode display filter) and dismissed a correct
  bug report — the later editor screenshot with the builder's own log line
  visible proved the real cause.
  **Caught by:** Pedram, twice.
  **Fix:** the builder now paints only the shelf's direct structural
  children; the pipeline re-run restores the scene. Lessons: a builder
  advertised as safe-to-re-run must be verified standalone, not only inside
  the pipeline that happens to repair its damage — and a user's bug report
  outranks a convenient theory.

## 2026-08-12 — Art pass: a material helper that destroyed what it returned, and mis-scaled items

- **Suggested:** a `SaveMaterial` helper whose reuse path copied properties
  into the existing asset and then destroyed the fresh instance — and one
  call site assigned that destroyed instance to the register screen.
  **Actual:** first run fine, every re-run a magenta screen (destroyed
  material). The render-verification screenshot caught it immediately; the
  fix is assigning the returned asset. A second art miss in the same round:
  the modeled groceries used real-world dimensions, which read toy-sized
  from a camera four metres away — stage presence needed a 1.35× scale-up,
  judged from the render, not the tape measure.
  **Caught by:** the per-stage screenshot verification both times.
  **Lesson:** helpers that consume their arguments must be the only path to
  the object, and art scale is a camera-relative judgement.

## 2026-08-11 — Step 11/13: two of three bystanders stood outside the camera frame

- **Suggested:** queue anchor positions receding from the counter toward the
  camera; the step-11 verification render showed "a bystander at the frame
  edge" and was accepted.
  **Actual:** the frustum narrows toward the camera — anchor 2 stood just
  outside the right frame edge and anchor 3 far outside. The patient saw
  shadows creeping into frame but not the figures; a social-presence stimulus
  the patient cannot see. The AI's own render check had shown exactly one
  figure and it concluded success without counting.
  **Caught by:** Pedram setting bystanders to 3 and screenshotting a lone
  shadow.
  **Fix:** three attempts. The first two repositionings were derived from
  hand-computed frustum estimates that ignored the camera's downward pitch —
  one put a figure inside the counter, the next made a near figure loom
  gigantically. The placement that shipped came from inverse-projecting
  target screen positions through the actual camera onto the floor (a
  measurement grid), which revealed the visible floor wedge is far smaller
  than intuition suggested and only its far band renders figures at person
  scale. Verification now projects every bystander's head to viewport
  coordinates and asserts all three are in frame, and the APK was rebuilt so
  the delivered build contains the fix. Along the way one automated patch
  silently failed to apply (wrong working directory) and was only caught
  because the re-run's numbers were identical to the previous run's —
  subsequent runs verify the edit is present before building. Lessons: when
  the claim is "N things are visible", the check must count to N; measure
  through the camera instead of estimating its frustum; and a verification
  that produces identical numbers after a change is itself a red flag.
