import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Icon } from '../../shared/ui/icon';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="nf">
      <div class="nf-inner">
        <p class="nf-code num">404</p>
        <h1>Esta ruta no existe en el panel</h1>
        <p class="nf-copy">
          El enlace pudo cambiar o el registro fue dado de baja. Vuelve al panorama
          para retomar desde la cartera.
        </p>
        <a class="btn btn-primary" routerLink="/admin/overview">
          Ir al panorama
          <app-icon name="arrowRight" [size]="15" />
        </a>
      </div>
    </div>
  `,
  styles: [`
    .nf {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      min-height: 100dvh;
      padding: 32px;
      background: var(--bg-base);
    }
    .nf-inner { max-width: 44ch; text-align: center; }
    .nf-code {
      font-size: 3.25rem;
      font-weight: 500;
      letter-spacing: -0.05em;
      color: var(--accent);
      line-height: 1;
    }
    h1 { margin-top: 14px; font-size: var(--fs-xl); }
    .nf-copy { margin: 12px 0 24px; color: var(--text-muted); line-height: 1.65; }
  `],
})
export class NotFound {}
