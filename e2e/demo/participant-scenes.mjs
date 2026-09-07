// DEMO-01..05: actions delegate selectors and interactions to page objects.
// Narration starts only after each chapter's assertions complete.
export const scenes = [
  {
    title: "Welcome to Stewardship",
    caption:
      "A guided path from learning to practice and one-on-one mentorship.",
    code: "await screens.signIn.enter(); await screens.curriculum.ready();",
    narration:
      "Welcome to Stewardship. This is a place to learn how to build redemptive technology, practise what you learn, and bring your questions to a mentor. We are following a demonstration participant through a real learning programme.",
  },
  {
    title: "FaithTech: the four Ds",
    caption: "Prepare → Discover → Discern → Develop → Demonstrate",
    code: "await screens.curriculum.faithtech();",
    narration:
      "The curriculum adapts FaithTech’s four D cycle. Discover begins with the people carrying the problem. Discern invites God’s wisdom into the response. Develop turns discernment into faithful work. Demonstrate shares the impact and what the team has learned. A preparation module opens the path.",
  },
  {
    title: "Read, practise, prepare",
    caption:
      "Resume your reading; carry an assignment and questions into practice.",
    code: "await screens.curriculum.continue(); await screens.module.read();",
    narration:
      "This participant has two sections completed as starting fixtures. Continue resumes the next reading. Each module includes an assignment and questions for a mentor conversation. Learning here connects what you read with what you do and the questions you still carry.",
  },
  {
    title: "Progress that stays with you",
    caption:
      "Section completion survives reload; finishing Prepare unlocks Discover.",
    code: "await screens.module.finish(); await screens.curriculum.faithtech();",
    narration:
      "The completed sections have been saved, and reloading preserved our place. Finishing Prepare unlocked Discover. The path reflects the work this participant has actually completed. Earlier reading stays available when you need to return to it.",
  },
  {
    title: "Save a reflection",
    caption:
      "Write a module note and retrieve the complete text after reloading.",
    code: 'await screens.module.go("/modules/1"); await screens.module.addNote(); await screens.editor.create(); await screens.notes.persisted(config.note);',
    narration:
      "Before moving on, we have saved a reflection about listening to the people affected by a problem. The complete note remains here after reloading. It belongs to its module, so the context can travel with us into our next conversation.",
  },
  {
    title: "Make time with your mentor",
    caption:
      "Choose published availability and confirm one mentor conversation.",
    code: "await screens.sessions.history(); await screens.sessions.book(); await screens.curriculum.nextSession();",
    narration:
      "Our mentor’s published availability gives us a choice of times. Taken slots cannot be selected. We have booked an open time, and the learning path shows the same conversation. The mentor, time zone, and remaining session allowance help us plan.",
  },
  {
    title: "Bring a thoughtful question",
    caption: "Save an answer beneath the module’s preparation prompt.",
    code: "await screens.preparation.open(); await screens.preparation.answer(); await screens.editor.write(config.answer); await screens.editor.save(config.answer); await screens.preparation.checkAnswer();",
    narration:
      "Preparation begins before the meeting. We have answered a prompt from the current module, and the saved answer appears beneath the question. A specific reflection gives the conversation somewhere useful to begin, while leaving room for what we have not resolved.",
  },
  {
    title: "Develop: the five Rs",
    caption:
      "Prepared advanced participant · earlier learning was seeded before recording.",
    code: 'await screens.shell.signOut(); await screens.signIn.enter("advanced@demo.invalid"); await screens.module.develop();',
    narration:
      "We have switched to a separately prepared advanced participant to review Develop. Their earlier learning was seeded before this recording. Inside Develop, FaithTech’s co-creation cycle is Request, Receive, Review, Render, and Rejoice. These movements repeat around the work.",
  },
  {
    title: "Request and Receive",
    caption: "Invite the Holy Spirit into specific work; make room to listen.",
    code: 'await screens.module.choose("Request and Receive");',
    narration:
      "Request names the actual work in prayer and invites the Holy Spirit into it. Receive makes room to listen. The team records what comes without rushing to evaluate it. Listening is given time before a solution is chosen.",
  },
  {
    title: "Review and Render",
    caption:
      "Weigh what was received together, then build the next useful step.",
    code: 'await screens.module.choose("Review and Render");',
    narration:
      "Review brings those observations together and weighs them as a team. Render puts the supported next steps into practice. Careful design, testing, and honest engineering still matter. The aim is to build from shared discernment, with a clear connection between what was heard and what is done.",
  },
  {
    title: "Rejoice, then begin again",
    caption:
      "Give thanks for something specific; carry that gratitude into the next iteration.",
    code: 'await screens.module.choose("Rejoice");',
    narration:
      "Rejoice closes the iteration with specific thanks. It gives the team time to name what was received and learned, before the next task takes over. Then the cycle begins again. This curriculum draws on FaithTech’s Playbook and Workbook; it is an adaptation, without implying endorsement.",
  },
  {
    title: "Carry the programme with you",
    caption:
      "Back to the original participant · curriculum, reflections, and sessions on mobile.",
    code: 'await screens.shell.mobile(); await screens.signIn.enterKeyboard(); await screens.shell.navigate("Curriculum"); await screens.curriculum.ready(1); await screens.shell.fits(); await screens.shell.navigate("Notes"); await screens.notes.visibleNote(config.note); await screens.shell.fits(); await screens.shell.navigate("Sessions"); await screens.sessions.ready(); await screens.shell.fits();',
    narration:
      "Back with our original participant, the same programme works on a narrow screen. The menu keeps the learning path, reflections, and mentor sessions within reach. Your preparation can continue wherever you have a few quiet minutes.",
  },
  {
    title: "Continue with intention",
    caption:
      "Sign out securely. Return ready to learn, practise, and meet with your mentor.",
    code: "await screens.signIn.protectedAfterSignOut();",
    narration:
      "We have signed out, and protected pages now require a new sign-in. Stewardship brings reading, practice, reflection, and mentorship into one place, helping participants take their next faithful step in building redemptive technology.",
  },
];
