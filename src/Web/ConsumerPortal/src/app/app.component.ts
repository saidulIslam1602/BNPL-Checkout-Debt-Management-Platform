import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter, map } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatSnackBarModule,
    MatProgressBarModule,
    MatDividerModule
  ],
  template: `
    <div class="app-container">
      <!-- Progress bar for loading states -->
      <mat-progress-bar 
        *ngIf="isLoading" 
        mode="indeterminate" 
        class="loading-bar">
      </mat-progress-bar>

      <!-- Main header -->
      <mat-toolbar class="app-toolbar" color="primary">
        <div class="toolbar-content">
          <div class="toolbar-left">
            <img 
              src="assets/images/yourcompany-logo.svg" 
              alt="YourCompany" 
              class="logo"
              (click)="navigateHome()">
            <span class="app-title">BNPL Norge</span>
          </div>

          <div class="toolbar-center">
            <div class="market-indicator">
              <img src="assets/images/norway-flag.svg" alt="Norge" class="flag-icon">
              <span class="market-text">Trygg norsk BNPL</span>
            </div>
          </div>

          <div class="toolbar-right">
            <button mat-icon-button [matMenuTriggerFor]="helpMenu" matTooltip="Hjelp">
              <mat-icon>help_outline</mat-icon>
            </button>
            
            <button mat-icon-button [matMenuTriggerFor]="languageMenu" matTooltip="Språk">
              <mat-icon>language</mat-icon>
            </button>
            
            <button mat-button routerLink="/my-payments" class="my-payments-btn">
              <mat-icon>account_balance_wallet</mat-icon>
              Mine betalinger
            </button>
          </div>
        </div>
      </mat-toolbar>

      <!-- Help Menu -->
      <mat-menu #helpMenu="matMenu">
        <button mat-menu-item (click)="openSupport()">
          <mat-icon>support_agent</mat-icon>
          <span>Kundeservice</span>
        </button>
        <button mat-menu-item routerLink="/faq">
          <mat-icon>quiz</mat-icon>
          <span>Ofte stilte spørsmål</span>
        </button>
        <button mat-menu-item (click)="openChat()">
          <mat-icon>chat</mat-icon>
          <span>Live chat</span>
        </button>
        <mat-divider></mat-divider>
        <button mat-menu-item routerLink="/terms">
          <mat-icon>description</mat-icon>
          <span>Vilkår og betingelser</span>
        </button>
        <button mat-menu-item routerLink="/privacy">
          <mat-icon>privacy_tip</mat-icon>
          <span>Personvern</span>
        </button>
      </mat-menu>

      <!-- Language Menu -->
      <mat-menu #languageMenu="matMenu">
        <button mat-menu-item (click)="setLanguage('no')" [class.active]="currentLanguage === 'no'">
          <img src="assets/images/norway-flag.svg" alt="Norsk" class="flag-icon-small">
          <span>Norsk</span>
          <mat-icon *ngIf="currentLanguage === 'no'">check</mat-icon>
        </button>
        <button mat-menu-item (click)="setLanguage('en')" [class.active]="currentLanguage === 'en'">
          <img src="assets/images/uk-flag.svg" alt="English" class="flag-icon-small">
          <span>English</span>
          <mat-icon *ngIf="currentLanguage === 'en'">check</mat-icon>
        </button>
      </mat-menu>

      <!-- Main content area -->
      <main class="main-content">
        <router-outlet></router-outlet>
      </main>

      <!-- Footer -->
      <footer class="app-footer" *ngIf="showFooter">
        <div class="footer-content">
          <div class="footer-section">
            <h4>YourCompany BNPL</h4>
            <p>Trygg og enkel "kjøp nå, betal senere" løsning for norske forbrukere.</p>
            <div class="social-links">
              <a href="#" aria-label="Facebook">
                <mat-icon>facebook</mat-icon>
              </a>
              <a href="#" aria-label="Twitter">
                <mat-icon>twitter</mat-icon>
              </a>
              <a href="#" aria-label="LinkedIn">
                <mat-icon>linkedin</mat-icon>
              </a>
            </div>
          </div>

          <div class="footer-section">
            <h4>Tjenester</h4>
            <ul>
              <li><a routerLink="/how-it-works">Slik fungerer det</a></li>
              <li><a routerLink="/merchants">Våre partnere</a></li>
              <li><a routerLink="/mobile-app">Mobilapp</a></li>
              <li><a routerLink="/calculator">Betalingskalkulator</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Support</h4>
            <ul>
              <li><a routerLink="/faq">Ofte stilte spørsmål</a></li>
              <li><a routerLink="/contact">Kontakt oss</a></li>
              <li><a routerLink="/complaints">Klager</a></li>
              <li><a href="tel:+4721000000">+47 21 00 00 00</a></li>
            </ul>
          </div>

          <div class="footer-section">
            <h4>Juridisk</h4>
            <ul>
              <li><a routerLink="/terms">Vilkår og betingelser</a></li>
              <li><a routerLink="/privacy">Personvernerklæring</a></li>
              <li><a routerLink="/cookies">Informasjonskapsler</a></li>
              <li><a routerLink="/gdpr">GDPR</a></li>
            </ul>
          </div>
        </div>

        <div class="footer-bottom">
          <div class="footer-bottom-content">
            <div class="regulatory-info">
              <p>YourCompany Norge AS - Org.nr: 123 456 789</p>
              <p>Regulert av Finanstilsynet | Medlem av Finansklagenemnda</p>
            </div>
            <div class="certifications">
              <div class="cert-badge">
                <mat-icon>security</mat-icon>
                <span>SSL Sikret</span>
              </div>
              <div class="cert-badge">
                <mat-icon>verified_user</mat-icon>
                <span>GDPR Kompatibel</span>
              </div>
              <div class="cert-badge norway-accent">
                <img src="assets/images/norway-flag.svg" alt="Norge" class="flag-icon-small">
                <span>Norsk Regulert</span>
              </div>
            </div>
          </div>
          <div class="copyright">
            <p>&copy; {{ currentYear }} YourCompany Norge AS. Alle rettigheter reservert.</p>
          </div>
        </div>
      </footer>

      <!-- PWA Install Prompt -->
      <div class="pwa-install-prompt" *ngIf="showInstallPrompt">
        <div class="prompt-content">
          <mat-icon class="prompt-icon">get_app</mat-icon>
          <div class="prompt-text">
            <div class="prompt-title">Installer YourCompany BNPL</div>
            <div class="prompt-description">Få rask tilgang til dine betalinger</div>
          </div>
          <div class="prompt-actions">
            <button mat-button (click)="dismissInstallPrompt()">Ikke nå</button>
            <button mat-raised-button color="primary" (click)="installPWA()">Installer</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  private router = inject(Router);
  private swUpdate = inject(SwUpdate, { optional: true });

  title = 'YourCompany Consumer Portal';
  currentYear = new Date().getFullYear();
  currentLanguage = 'no';
  isLoading = false;
  showFooter = true;
  showInstallPrompt = false;
  
  private deferredPrompt: any;

  ngOnInit(): void {
    this.setupRouterEvents();
    this.setupPWAPrompt();
    this.setupServiceWorkerUpdates();
  }

  private setupRouterEvents(): void {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(event => event as NavigationEnd)
    ).subscribe((event) => {
      // Hide footer on checkout pages
      this.showFooter = !event.url.includes('/checkout');
      
      // Scroll to top on route change
      window.scrollTo(0, 0);
    });
  }

  private setupPWAPrompt(): void {
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      this.deferredPrompt = e;
      this.showInstallPrompt = true;
    });

    window.addEventListener('appinstalled', () => {
      this.showInstallPrompt = false;
      this.deferredPrompt = null;
    });
  }

  private setupServiceWorkerUpdates(): void {
    if (this.swUpdate?.isEnabled) {
      this.swUpdate.versionUpdates.pipe(
        filter((evt): evt is VersionReadyEvent => evt.type === 'VERSION_READY')
      ).subscribe(() => {
        if (confirm('Ny versjon tilgjengelig. Last inn på nytt?')) {
          window.location.reload();
        }
      });
    }
  }

  navigateHome(): void {
    this.router.navigate(['/']);
  }

  setLanguage(language: string): void {
    this.currentLanguage = language;
    // Implement language switching logic
    localStorage.setItem('preferred-language', language);
  }

  openSupport(): void {
    window.open('https://support.yourcompany.no', '_blank');
  }

  openChat(): void {
    // Implement chat widget opening
    console.log('Opening chat widget...');
  }

  installPWA(): void {
    if (this.deferredPrompt) {
      this.deferredPrompt.prompt();
      this.deferredPrompt.userChoice.then((choiceResult: any) => {
        if (choiceResult.outcome === 'accepted') {
          console.log('User accepted the PWA install prompt');
        }
        this.deferredPrompt = null;
        this.showInstallPrompt = false;
      });
    }
  }

  dismissInstallPrompt(): void {
    this.showInstallPrompt = false;
    // Remember user dismissed the prompt
    localStorage.setItem('pwa-prompt-dismissed', 'true');
  }
}