# AI Mistake Log

Running log of errors made by AI tooling during this project: what the AI
suggested, what actually happened, how it was caught, and what fixed it.
Maintained from the first step onward; feeds the AI-usage declaration in the
final submission. Entries are dated, newest last.

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
