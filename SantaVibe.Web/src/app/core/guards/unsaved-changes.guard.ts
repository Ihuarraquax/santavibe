import { CanDeactivateFn } from '@angular/router';

/**
 * Interface for components that can be deactivated with unsaved changes confirmation.
 */
export interface CanComponentDeactivate {
  canDeactivate: () => boolean;
}

/**
 * Guard that prevents navigation away from a component with unsaved changes
 * without user confirmation. Components must implement CanComponentDeactivate interface.
 */
export const unsavedChangesGuard: CanDeactivateFn<CanComponentDeactivate> = (component) => {
  return component.canDeactivate ? component.canDeactivate() : true;
};
