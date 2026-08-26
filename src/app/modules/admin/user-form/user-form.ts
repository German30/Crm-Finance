import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { ToastService } from '../../../core/services/toast.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { Role, UserStatus } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';

/**
 * One shape for the form model, in one casing. The API DTOs are built at submit
 * time — the template never binds to a field the component does not declare.
 */
interface UserFormModel {
  name: string;
  email: string;
  password: string;
  roleId: number | null;
  statusId: number | null;
}

const MIN_PASSWORD = 8;

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [FormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-form.html',
  styleUrl: './user-form.css',
})
export class UserForm {
  private readonly crm = inject(CrmService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly isEditMode = signal(false);
  protected readonly userId = signal<number | null>(null);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly loadError = signal('');
  protected readonly submitError = signal('');
  protected readonly showPassword = signal(false);

  protected readonly roles = signal<Role[]>([]);
  protected readonly statuses = signal<UserStatus[]>([]);

  protected readonly model: UserFormModel = {
    name: '',
    email: '',
    password: '',
    roleId: null,
    statusId: null,
  };

  protected readonly title = computed(() =>
    this.isEditMode() ? 'Editar usuario' : 'Dar de alta un usuario',
  );

  protected readonly lede = computed(() =>
    this.isEditMode()
      ? 'Los cambios de rol se aplican la próxima vez que el usuario entre al panel.'
      : 'La contraseña es temporal: el usuario podrá cambiarla desde sus ajustes.',
  );

  protected readonly minPassword = MIN_PASSWORD;

  constructor() {
    const idParam = this.route.snapshot.paramMap.get('id');
    const parsed = idParam === null ? NaN : Number(idParam);

    if (idParam !== null && Number.isInteger(parsed) && parsed > 0) {
      this.isEditMode.set(true);
      this.userId.set(parsed);
    } else if (idParam !== null) {
      // A malformed :id must not silently create a user instead of editing one.
      this.loadError.set('El identificador de usuario en la dirección no es válido.');
      this.loading.set(false);
      return;
    }

    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.loadError.set('');

    const id = this.userId();

    forkJoin({
      // A missing catalogue endpoint degrades to an empty select, not a dead page.
      roles: this.crm.getRoles().pipe(catchError(() => of([] as Role[]))),
      statuses: this.crm.getUserStatuses().pipe(catchError(() => of([] as UserStatus[]))),
      user: id ? this.crm.getUser(id) : of(null),
    }).subscribe({
      next: ({ roles, statuses, user }) => {
        this.roles.set(roles ?? []);
        this.statuses.set(statuses ?? []);

        if (user) {
          this.model.name = user.name ?? '';
          this.model.email = user.email ?? '';
          this.model.roleId = this.coerceId(user.roleId, roles, 'roleId');
          this.model.statusId = this.matchStatus(user.statusName, statuses);
        } else {
          // Sensible defaults so a new user is one keystroke from valid.
          this.model.roleId = roles?.length ? Number(roles[0].roleId) : null;
          this.model.statusId = this.matchStatus('Activo', statuses);
        }

        this.loading.set(false);
      },
      error: (err) => {
        this.loadError.set(describeHttpError(err, 'No fue posible cargar el usuario.'));
        this.loading.set(false);
      },
    });
  }

  /** The API has returned roleId as both a number and a numeric string. */
  private coerceId(raw: unknown, list: Role[], key: 'roleId'): number | null {
    const asNumber = Number(raw);
    if (Number.isFinite(asNumber) && asNumber > 0) return asNumber;
    const match = list.find((item) => String(item[key]) === String(raw));
    return match ? Number(match[key]) : null;
  }

  private matchStatus(statusName: string | null | undefined, list: UserStatus[]): number | null {
    if (!list.length) return null;
    const target = (statusName ?? '').trim().toLowerCase();
    const match = list.find((s) => (s.statusName ?? '').trim().toLowerCase() === target);
    return match ? Number(match.statusId) : Number(list[0].statusId);
  }

  protected onSubmit(form: NgForm): void {
    if (this.saving()) return;

    this.submitError.set('');

    if (form.invalid) {
      form.control.markAllAsTouched();
      this.submitError.set('Revisa los campos marcados antes de guardar.');
      return;
    }

    const id = this.userId();
    this.saving.set(true);

    const done = (message: string) => {
      this.saving.set(false);
      this.toast.success(message);
      this.router.navigate(['/admin/users']);
    };

    const fail = (fallback: string) => (err: unknown) => {
      this.saving.set(false);
      this.submitError.set(describeHttpError(err, fallback));
    };

    if (this.isEditMode() && id) {
      this.crm
        .updateUser(id, {
          Name: this.model.name.trim(),
          Email: this.model.email.trim(),
          RoleId: Number(this.model.roleId),
          StatusId: Number(this.model.statusId),
        })
        .subscribe({
          next: () => done(`Se guardaron los cambios de ${this.model.name.trim()}.`),
          error: fail('No fue posible guardar los cambios.'),
        });
    } else {
      this.crm
        .createUser({
          Name: this.model.name.trim(),
          Email: this.model.email.trim(),
          Password: this.model.password,
          RoleId: Number(this.model.roleId),
        })
        .subscribe({
          next: () => done(`${this.model.name.trim()} quedó dado de alta.`),
          error: fail('No fue posible crear el usuario.'),
        });
    }
  }
}
