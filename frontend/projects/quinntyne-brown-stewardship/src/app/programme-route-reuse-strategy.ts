import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from '@angular/router';

@Injectable()
export class ProgrammeRouteReuseStrategy extends BaseRouteReuseStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, current: ActivatedRouteSnapshot): boolean {
    // Keep the shared shell; a changed document or filter gets a fresh screen.
    return future.routeConfig === current.routeConfig && future.routeConfig?.path === '';
  }
}
