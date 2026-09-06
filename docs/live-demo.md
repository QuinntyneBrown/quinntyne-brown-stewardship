# Five-minute live application walkthrough

The walkthrough exercises the production Angular bundle at
`https://localhost:7240`, using the real HTTP adapters, HTTPS API, and a dedicated
SQL Server database. Playwright CLI drives the browser and captures the footage.
The final MP4 adds chapter captions and offline synthetic narration.

The [September 6 verification record](verification/2026-09-06-live-demo.json)
records 17 passing live chapters, 157 HTTP responses without unexpected failures,
six privacy/CSRF checks, and a verified 300-second MP4 with 7,500 video frames.

The recording uses prepared demonstration accounts. Historical sessions, journal
entries, an approaching booking, and a completed cohort are fixtures; every action
shown in the browser uses the running application's real clock and persistence.
The ordinary Playwright acceptance suite continues to bind service tokens to mocks.

## Chapters

| Time | Demonstration                                                       |
| ---- | ------------------------------------------------------------------- |
| 0:00 | Protected links, required fields, and incorrect credentials         |
| 0:15 | The notice for a participant awaiting enrollment                    |
| 0:30 | Cohort, mentor, curriculum, and locked modules                      |
| 0:45 | Resume point, reading, assignment, and preparation prompts          |
| 1:05 | Section completion, persistence after reload, and module unlocking  |
| 1:25 | Create and save a module note                                       |
| 1:45 | Protect an unsaved draft, revise, save, and reload                  |
| 2:05 | Retrieve another page of notes and move keyboard focus              |
| 2:20 | Session history, historical notes, and published availability       |
| 2:40 | Book a slot and verify the same session on the curriculum           |
| 3:00 | Save an answer beneath a preparation prompt                         |
| 3:20 | Save a session note and retrieve it with the booking                |
| 3:38 | Reschedule, decline cancellation, then cancel and recover allowance |
| 3:58 | The 24-hour restriction on booking changes                          |
| 4:15 | A completed curriculum and an ended cohort's session history        |
| 4:30 | Keyboard sign-in and mobile curriculum, notes, and sessions         |
| 4:51 | Sign-out, browser history, protected links, and API refusal         |

This covers the participant feature families in [L1](specs/L1.md). Detailed
concurrency, security, accessibility, and performance checks belong to the
[acceptance and performance suites](testing.md); the video is a feature
walkthrough, not a replacement for those checks.

## Reproduce on Windows

Use the repository's normal .NET, Node, and LocalDB prerequisites, PowerShell 7,
Google Chrome, and Playwright CLI. The original run used `@playwright/cli` 0.1.0.
Install the CLI with `npm install --global @playwright/cli@0.1.0`, or set
`PLAYWRIGHT_CLI_PATH` to an existing `playwright-cli.js` entry point.

With the demo port available:

```powershell
npm run build
./scripts/prepare-live-demo.ps1
node e2e/demo/record.mjs --rehearse
node e2e/demo/render.mjs --narrate
node e2e/demo/record.mjs
node e2e/demo/verify-access.mjs
node e2e/demo/render.mjs
```

Preparation creates a fresh `StewardshipDemo_yyyyMMdd_HHmmss` database, provisions
accounts and programme data through the CLI, adds historical fixtures, and starts
a hidden API process. It does not clear the normal application database. The
database name, API process ID, generated password, and URL are stored in ignored
`.local/live-demo/run.json`.

The rehearsal and recording use separate participant accounts. Each is intended
to run once per prepared database because completing a module changes its resume
point. Before preparing again, stop the API process identified in `run.json`,
after confirming it is the demo process. A different port can be supplied with
`./scripts/prepare-live-demo.ps1 -Port 7340`.

The narrator runs Windows PowerShell's `System.Speech`, selecting Microsoft Zira
when installed. Rendering requires a full FFmpeg distribution with H.264, AAC,
and libass support. Set `FFMPEG_PATH` to its `ffmpeg.exe`, with `ffprobe.exe` beside
it, or extract it under `.local/live-demo/ffmpeg/ffmpeg-<version>/bin/`.
Playwright's bundled minimal encoder is insufficient for the final MP4.

To watch with clickable chapters, run `node e2e/demo/serve.mjs` and open
`http://127.0.0.1:4321`. The preview serves only the player, MP4, and sanitized
recording evidence. Demo credentials and setup files are not served.

## Artifacts and checks

All generated artifacts stay under ignored `.local/live-demo/`:

- `stewardship-live-demo-5min.mp4`: the five-minute H.264/AAC video, with burned-in
  captions, embedded chapter markers, and synthetic narration.
- `raw.webm`: the original browser capture, before captions or narration.
- `chapters.vtt`: chapter captions for a player that accepts WebVTT.
- `evidence.json`: actual scene times, browser exceptions, HTTP methods, paths and
  statuses, and persistence assertions. Credentials and response bodies are omitted.
- `media-verification.json`: FFprobe's duration, resolution, and codec inspection.
- `rehearsal.json`: the independent rehearsal results.
- `access-verification.json`: live checks that the note owner and assigned mentor
  can retrieve the saved revision, another participant receives 404, and mutating
  requests without a CSRF token receive 400.
- `index.html`: a local video player with clickable chapters.

The browser must remain open after recording for `verify-access.mjs` to retrieve
the identifier of the note created on camera. It authenticates each verification
account in an isolated browser context and revokes those sessions afterward.

The walkthrough follows these acceptance criteria:

- Given a healthy app with prepared accounts, when the browser performs each
  chapter, then all its visible state and persistence assertions finish within
  the allocated time, with no unexpected HTTP failures or browser exceptions.
- Given the note created and revised during recording, when its owner or assigned
  mentor requests it, then the API returns the saved revision; when another
  participant requests it, then the API returns 404.
- Given an authenticated verification account, when a mutating request omits the
  CSRF token, then the API rejects it with 400.
- Given all 17 recorded chapters pass, when the footage is rendered, then the
  result is a playable 300-second H.264/AAC MP4 with legible desktop and mobile
  footage, captions, and narration.

Each scene must finish its assertions within its allotted recording time. A
native unsaved-change dialog is handled through Playwright CLI's `dialog-dismiss`
command; an unexpected interruption fails the run. The renderer requires all 17
scenes to pass, rejects unexpected browser errors, and verifies an exact
300-second output. Mobile footage is centered without changing its scale.

## Issues corrected during the walkthrough

The live app requested a missing favicon. A shared SVG icon now comes from the
design system, is mirrored into the Angular assets, and returns HTTP 200. The
production build passed after this change.

The recording harness also needed explicit synchronization for native dialogs
and mobile menu closure. It now waits for the menu's collapsed state, verifies
that every scene finishes, and pauses at useful reading points. These were
automation timing issues; the corresponding application interactions passed
against the live API after correcting the harness.
