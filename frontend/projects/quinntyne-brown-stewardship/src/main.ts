import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { provideHttpClient } from "@angular/common/http";
import { AppComponent } from "./app/app.component";
import { routes } from "./app/app.routes";
import { serviceProviders } from "./app/service-providers";
bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes), provideHttpClient(), ...serviceProviders],
}).catch(console.error);
