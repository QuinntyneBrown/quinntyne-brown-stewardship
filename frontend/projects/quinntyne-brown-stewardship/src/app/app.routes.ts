import { Routes } from "@angular/router";
import { authGuard } from "./auth.guard";
export const routes: Routes = [
  {
    path: "sign-in",
    loadComponent: () =>
      import("./sign-in/sign-in-page.component").then(
        (m) => m.SignInPageComponent,
      ),
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () => import('./programme-shell/programme-shell.component').then(m => m.ProgrammeShellComponent),
    children: [
      { path: 'curriculum', loadComponent: () => import('./curriculum/curriculum-page.component').then(m => m.CurriculumPageComponent) },
      { path: 'modules/:id', loadComponent: () => import('./module/module-page.component').then(m => m.ModulePageComponent) },
      { path: 'sessions', loadComponent: () => import('./sessions/sessions-page.component').then(m => m.SessionsPageComponent) },
      { path: 'sessions/:id', loadComponent: () => import('./session-detail/session-detail-page.component').then(m => m.SessionDetailPageComponent) },
      { path: 'notes', loadComponent: () => import('./notes/notes-page.component').then(m => m.NotesPageComponent) },
      { path: 'notes/new', canDeactivate: [(component: { canLeave(): boolean }) => component.canLeave()], loadComponent: () => import('./note-editor/note-editor-page.component').then(m => m.NoteEditorPageComponent) },
      { path: 'notes/:id', canDeactivate: [(component: { canLeave(): boolean }) => component.canLeave()], loadComponent: () => import('./note-editor/note-editor-page.component').then(m => m.NoteEditorPageComponent) },
    ],
  },
  ...["sessions", "notes"].map((path) => ({
    path,
    canActivate: [authGuard],
    runGuardsAndResolvers: "always" as const,
    loadComponent: () =>
      import("./enrollment/enrollment-page.component").then(
        (m) => m.EnrollmentPageComponent,
      ),
  })),
  { path: "", pathMatch: "full", redirectTo: "curriculum" },
  { path: "**", redirectTo: "curriculum" },
];
