import { Routes } from "@angular/router";
import { authGuard } from "./auth.guard";
import { ProgrammeShellComponent } from './programme-shell/programme-shell.component';
import { CurriculumPageComponent } from './curriculum/curriculum-page.component';
export const routes: Routes = [
  { path: "", pathMatch: "full", redirectTo: "curriculum" },
  {
    path: "sign-in",
    loadComponent: () =>
      import("./sign-in/sign-in-page.component").then(
        (m) => m.SignInPageComponent,
      ),
  },
  {
    path: '',
    canActivateChild: [authGuard],
    component: ProgrammeShellComponent,
    children: [
      { path: 'curriculum', component: CurriculumPageComponent },
      { path: 'modules/:id', loadComponent: () => import('./module/module-page.component').then(m => m.ModulePageComponent) },
      { path: 'sessions', loadComponent: () => import('./sessions/sessions-page.component').then(m => m.SessionsPageComponent) },
      { path: 'sessions/:id', loadComponent: () => import('./session-detail/session-detail-page.component').then(m => m.SessionDetailPageComponent) },
      { path: 'notes', loadComponent: () => import('./notes/notes-page.component').then(m => m.NotesPageComponent) },
      { path: 'notes/new', canDeactivate: [(component: { canLeave(): boolean }) => component.canLeave()], loadComponent: () => import('./note-editor/note-editor-page.component').then(m => m.NoteEditorPageComponent) },
      { path: 'notes/:id', canDeactivate: [(component: { canLeave(): boolean }) => component.canLeave()], loadComponent: () => import('./note-editor/note-editor-page.component').then(m => m.NoteEditorPageComponent) },
    ],
  },
  { path: "**", redirectTo: "curriculum" },
];
