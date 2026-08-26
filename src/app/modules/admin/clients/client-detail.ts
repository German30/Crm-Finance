import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { CrmService } from '../../../core/services/crm.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { MoralClientDetail, PhisicClientDetail } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';
import { formatAgo, formatDate, initials } from '../../../shared/utils/format';

interface Fact {
  label: string;
  value: string;
  mono?: boolean;
  href?: string;
}

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './client-detail.html',
  styleUrl: './client-detail.css',
})
export class ClientDetail {
  private readonly crm = inject(CrmService);

  /** Enlazados desde la ruta /admin/clients/:type/:id. */
  readonly type = input<string>();
  readonly id = input<string>();

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly phisic = signal<PhisicClientDetail | null>(null);
  protected readonly moral = signal<MoralClientDetail | null>(null);

  protected readonly initials = initials;
  protected readonly fmtDate = formatDate;
  protected readonly fmtAgo = formatAgo;

  protected readonly isMoral = computed(() => this.type() === 'moral');

  protected readonly displayName = computed(() => {
    const m = this.moral();
    if (m) return m.socialRazon;
    const p = this.phisic();
    if (!p) return 'Expediente';
    return [p.names, p.fatherLastName, p.motherLastName].filter(Boolean).join(' ');
  });

  protected readonly found = computed(() => !!this.phisic() || !!this.moral());

  protected readonly headline = computed(() => {
    const m = this.moral();
    if (m) {
      return {
        fiscalId: m.fiscalId,
        registerDate: m.registerDate,
        assignedUserName: m.assignedUserName,
        email: m.email,
        phone: m.phone,
        addressFiscal: m.addressFiscal,
      };
    }
    const p = this.phisic();
    return p
      ? {
          fiscalId: p.fiscalId,
          registerDate: p.registerDate,
          assignedUserName: p.assignedUserName,
          email: p.email,
          phone: p.phone,
          addressFiscal: p.addressFiscal,
        }
      : null;
  });

  /** Los datos propios del tipo de persona, ya listos para pintar. */
  protected readonly identityFacts = computed<Fact[]>(() => {
    const m = this.moral();
    if (m) {
      return [
        { label: 'Razón social', value: m.socialRazon },
        { label: 'Nombre comercial', value: m.comercialName || '—' },
        { label: 'Constitución', value: formatDate(m.dateConstitucion), mono: true },
        { label: 'Actividad comercial', value: m.comercialActivity || '—' },
        { label: 'Representante legal', value: m.representativeLegalName },
        { label: 'Identificación del representante', value: m.representativeId || '—', mono: true },
      ];
    }
    const p = this.phisic();
    if (!p) return [];
    return [
      { label: 'Nombres', value: p.names },
      { label: 'Apellido paterno', value: p.fatherLastName },
      { label: 'Apellido materno', value: p.motherLastName },
      { label: 'Fecha de nacimiento', value: formatDate(p.birthDate), mono: true },
      { label: 'Género', value: p.genderName },
      { label: 'Estado civil', value: p.civilStateName },
    ];
  });

  protected readonly contactFacts = computed<Fact[]>(() => {
    const h = this.headline();
    if (!h) return [];
    return [
      { label: 'Correo', value: h.email || '—', href: h.email ? 'mailto:' + h.email : undefined },
      { label: 'Teléfono', value: h.phone || '—', mono: true, href: h.phone ? this.telHref(h.phone) : undefined },
      { label: 'Identificación fiscal', value: h.fiscalId || '—', mono: true },
      { label: 'Domicilio fiscal', value: h.addressFiscal || '—' },
      { label: 'Ejecutivo asignado', value: h.assignedUserName || 'Sin asignar' },
      { label: 'Alta en el sistema', value: formatDate(h.registerDate), mono: true },
    ];
  });

  constructor() {
    effect(() => {
      const raw = this.id();
      const kind = this.type();
      const clientId = Number(raw);

      if (!raw || !Number.isInteger(clientId) || clientId <= 0 || (kind !== 'moral' && kind !== 'phisic')) {
        this.errorText.set('La dirección del expediente no es válida.');
        this.loading.set(false);
        return;
      }

      this.loading.set(true);
      this.errorText.set('');
      this.phisic.set(null);
      this.moral.set(null);

      const request: Observable<MoralClientDetail | PhisicClientDetail> =
        kind === 'moral' ? this.crm.getMoralClient(clientId) : this.crm.getPhisicClient(clientId);

      request.subscribe({
        next: (data) => {
          if (kind === 'moral') this.moral.set(data as MoralClientDetail);
          else this.phisic.set(data as PhisicClientDetail);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          this.errorText.set(describeHttpError(err, 'No fue posible abrir el expediente.'));
          this.loading.set(false);
        },
      });
    });
  }

  /** tel: no admite espacios ni separadores. */
  protected telHref(phone: string): string {
    return 'tel:' + phone.replace(/[^+\d]/g, '');
  }
}
