import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter, RouteReuseStrategy } from "@angular/router";
import { ProgrammeRouteReuseStrategy } from './app/programme-route-reuse-strategy';
import { provideHttpClient } from "@angular/common/http";
import { AppComponent } from "./app/app.component";
import { routes } from "./app/app.routes";
import { serviceProviders } from "./app/service-providers";
bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes), { provide: RouteReuseStrategy, useClass: ProgrammeRouteReuseStrategy }, provideHttpClient(), ...serviceProviders],
}).catch(console.error);
