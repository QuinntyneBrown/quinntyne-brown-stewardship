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
  ...["curriculum", "modules/:id", "sessions", "notes"].map((path) => ({
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
