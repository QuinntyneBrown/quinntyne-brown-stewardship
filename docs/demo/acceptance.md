# Participant demo acceptance

- **DEMO-01** Given a fresh, isolated database, when the CLI imports and the demo publishes the starter curriculum, then the authenticated API returns Prepare and the four Ds in order, and Develop contains Request, Receive, Review, Render, and Rejoice.
- **DEMO-02** Given a participant with two prepared section completions, when the recorded UI completes the remaining sections and reloads, then progress persists and Discover unlocks.
- **DEMO-03** Given an enrolled participant, when the UI saves a reflection, books a session, and answers its preparation prompt, then the complete note, same booking, and answer remain visible after reload.
- **DEMO-04** Given a separately prepared advanced account, when it opens Develop, then all five R movements are readable. The recording identifies the account switch and its prerequisite fixtures.
- **DEMO-05** Given the recorded account, when it navigates at 390 pixels and signs out, then curriculum, notes, and sessions fit, and protected requests return to sign-in or receive HTTP 401.
- **DEMO-06** Given a successful continuous take, when encoded, then the WebM decodes fully at 1280 × 720 with an audible Microsoft David track, readable captions, and chapter times aligned to the recorded assertions. No narration is truncated or accelerated.
- **DEMO-07** Given setup or recording failure, when the runner exits, then it stops only owned processes and drops only its verified disposable database. Existing data and published videos remain intact.

The separate demo runner checks DEMO-01 through DEMO-05 against the real API and UI using page objects. The media verifier and normal-speed playback review check DEMO-06. The orchestration cleanup reports DEMO-07 failures explicitly. Ordinary mocked acceptance suites are unchanged.

Regression evidence: the media check initially failed because no successful take
existed. The Windows manuscript check reproduces missing preparation prompts with
the original generator and passes after line-ending normalization. An occupied-port
run was rejected before database creation; its browser cleanup succeeded and the
active recording continued through all chapters.
