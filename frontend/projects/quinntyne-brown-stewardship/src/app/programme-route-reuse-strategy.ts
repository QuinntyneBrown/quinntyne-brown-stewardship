import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from '@angular/router';

@Injectable()
export class ProgrammeRouteReuseStrategy extends BaseRouteReuseStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, current: ActivatedRouteSnapshot): boolean {
    // Keep the shared shell; keep a screen that asks to be kept while its identifier is unchanged, so a child route
    // such as a preview leaves every unsaved value in place; a changed document or filter gets a fresh screen.
    if (future.routeConfig !== current.routeConfig) return false;
    const config = future.routeConfig;
    // A route with no component of its own, such as the authoring branch, costs nothing to keep and lets its children be compared one by one.
    if (config?.path === '' || (!config?.component && !config?.loadComponent)) return true;
    return future.data['reuse'] === true && future.params['id'] === current.params['id'];
  }
}
