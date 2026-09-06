export const scenes = [
  {
    seconds: 15,
    title: "01  Secure participant access",
    caption:
      "Protected links require sign-in. Empty fields and incorrect credentials receive clear feedback.",
    code: "await screens.signIn.validation();",
  },
  {
    seconds: 15,
    title: "02  Explicit enrollment",
    caption:
      "A participant awaiting a cohort sees a clear enrollment notice, with no empty programme.",
    code: "await screens.signIn.awaiting();",
  },
  {
    seconds: 15,
    title: "03  Your cohort and curriculum",
    caption:
      "Twelve ordered modules, cohort dates, mentor and derived progress. Future modules stay locked.",
    code: "await screens.shell.signOut(); await screens.signIn.enter(); await screens.curriculum.overview();",
  },
  {
    seconds: 20,
    title: "04  Reading, practice and preparation",
    caption:
      "Resume at the first incomplete section. Reading, all assignment steps and prompts remain available.",
    code: "await screens.curriculum.continue(); await screens.module.read();",
  },
  {
    seconds: 20,
    title: "05  Progress that persists",
    caption:
      "Complete a section, reload and resume. Finishing a module unlocks the next; completed reading stays open.",
    code: "await screens.module.finish();",
  },
  {
    seconds: 20,
    title: "06  Write a module note",
    caption:
      "An empty note cannot be saved. A new reflection is stored against its module by the real API.",
    code: "await screens.module.addNote(); await screens.editor.create();",
  },
  {
    seconds: 20,
    title: "07  Revise without losing your draft",
    caption:
      "Decline the unsaved-change warning, keep the draft, save the revision and verify it after reload.",
    steps: [
      {
        code: "await screens.notes.edit(config.note); await screens.editor.beginRevision();",
        dialog: true,
      },
      { command: ["dialog-dismiss"] },
      { code: "await screens.editor.finishRevision();" },
    ],
  },
  {
    seconds: 15,
    title: "08  Every note remains reachable",
    caption:
      "The notes destination includes module and session attachments. More notes loads the next real page.",
    code: "await screens.notes.more();",
  },
  {
    seconds: 20,
    title: "09  Session history and availability",
    caption:
      "Read notes from earlier conversations, change the booking day and distinguish taken from open times.",
    code: "await screens.sessions.history();",
  },
  {
    seconds: 20,
    title: "10  Book one mentor conversation",
    caption:
      "Select an open slot and confirm. The held session and the curriculum show the same mentor and time.",
    code: "await screens.sessions.book(); await screens.curriculum.nextSession();",
  },
  {
    seconds: 20,
    title: "11  Prepare for the conversation",
    caption:
      "The current module supplies the prompts. Save an answer and see it beneath the original prompt.",
    code: "await screens.preparation.open(); await screens.preparation.answer(); await screens.editor.write(config.answer); await screens.editor.save(config.answer); await screens.preparation.checkAnswer();",
  },
  {
    seconds: 18,
    title: "12  Keep notes with the session",
    caption:
      "A session reflection is saved and retrieved in the conversation it belongs to.",
    code: "await screens.preparation.addNote(); await screens.editor.write(config.sessionNote); await screens.editor.save(config.sessionNote); await screens.preparation.checkNote();",
  },
  {
    seconds: 20,
    title: "13  Reschedule and cancel",
    caption:
      "Move to another open time, keep a booking from the dialog, then cancel and recover the session allowance.",
    code: "await screens.sessions.changeAndCancel();",
  },
  {
    seconds: 17,
    title: "14  The 24-hour change window",
    caption:
      "This prepared account has a session in twelve hours. Rescheduling and cancellation are disabled with a reason.",
    code: 'await screens.shell.signOut(); await screens.signIn.enter("cutoff@demo.invalid"); await screens.sessions.cutoff();',
  },
  {
    seconds: 15,
    title: "15  Completion and cohort end",
    caption:
      "A completed cohort keeps its reading and history. All twelve modules and six held sessions remain visible.",
    code: 'await screens.shell.signOut(); await screens.signIn.enter("graduate@demo.invalid"); await screens.curriculum.completedProgramme(); await screens.sessions.ended();',
  },
  {
    seconds: 21,
    title: "16  The same programme on mobile",
    caption:
      "At 390px, sign in by keyboard and use the menu to reach curriculum, notes and sessions without horizontal scrolling.",
    code: 'await screens.shell.signOut(); await page.setViewportSize({width:390,height:810}); page.__demo.mobileStart = Date.now(); await screens.signIn.enterKeyboard(); await screens.shell.navigate("Curriculum"); await screens.curriculum.ready(1); await screens.shell.fits(); await screens.shell.pause(2500); await screens.shell.navigate("Notes"); await screens.notes.visibleNote(config.revisedNote); await screens.shell.fits(); await screens.shell.pause(2500); await screens.shell.navigate("Sessions"); await screens.sessions.canBook(); await screens.shell.fits();',
  },
  {
    seconds: 9,
    title: "17  Deliberate sign-out",
    caption:
      "Sign-out revokes this device. Browser history and protected URLs return to sign-in; the API refuses access.",
    code: "await screens.signIn.protectedAfterSignOut();",
  },
];
