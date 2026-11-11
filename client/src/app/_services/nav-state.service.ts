import { Injectable, signal } from '@angular/core';

/** Shares the mobile navbar state between the nav and the pages that need to open it. */
@Injectable({
  providedIn: 'root'
})
export class NavStateService {
  readonly menuOpen = signal(false);

  openMenu(): void {
    this.menuOpen.set(true);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  toggleMenu(): void {
    this.menuOpen.update(open => !open);
  }
}
