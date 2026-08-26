import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Area,
  BankContractDetail,
  CatalogItem,
  ClientGrid,
  ContractGrid,
  CreateUserRequest,
  CurrentUser,
  FinanceProduct,
  InsuranceClaimResponse,
  InsuranceContractDetail,
  MoralClientDetail,
  OportunityCreateRequest,
  OportunityResponse,
  OportunityStageUpdateRequest,
  PhisicClientDetail,
  ResetPasswordRequest,
  Role,
  TransactionResponse,
  UpdateUserRequest,
  UserResponse,
  UserStatus,
} from '../../shared/models/api.model';

/** Descarta los parámetros vacíos para no mandar `?search=` al backend. */
function toParams(source: Record<string, string | number | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(source)) {
    if (value === null || value === undefined || value === '') continue;
    params = params.set(key, String(value));
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class CrmService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  /* ------------------------------------------------------------ sesión -- */

  /** La identidad autoritativa: rol y área tal como el backend los ve. */
  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.api}/Auth/me`);
  }

  /* ----------------------------------------------------------- usuarios -- */

  getUsers(): Observable<UserResponse[]> {
    return this.http.get<UserResponse[]>(`${this.api}/User`);
  }

  getUser(id: number): Observable<UserResponse> {
    return this.http.get<UserResponse>(`${this.api}/User/${id}`);
  }

  createUser(body: CreateUserRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.api}/User`, body);
  }

  updateUser(id: number, body: UpdateUserRequest): Observable<UserResponse> {
    return this.http.put<UserResponse>(`${this.api}/User/${id}`, body);
  }

  toggleUserStatus(id: number): Observable<unknown> {
    return this.http.patch(`${this.api}/User/${id}/toggle-status`, {});
  }

  resetUserPassword(id: number, body: ResetPasswordRequest): Observable<unknown> {
    return this.http.patch(`${this.api}/User/${id}/reset-password`, body);
  }

  /* ---------------------------------------------------- catálogos base -- */
  // Cualquier autenticado puede leerlos, y no cambian dentro de una sesión:
  // se comparten con shareReplay para no repetir la llamada en cada pantalla.

  private rolesCache?: Observable<Role[]>;
  getRoles(): Observable<Role[]> {
    this.rolesCache ??= this.http
      .get<Role[]>(`${this.api}/User/roles`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.rolesCache;
  }

  private areasCache?: Observable<Area[]>;
  getAreas(): Observable<Area[]> {
    this.areasCache ??= this.http
      .get<Area[]>(`${this.api}/User/areas`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.areasCache;
  }

  private statusCache?: Observable<UserStatus[]>;
  getUserStatuses(): Observable<UserStatus[]> {
    this.statusCache ??= this.http
      .get<UserStatus[]>(`${this.api}/User/status`)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    return this.statusCache;
  }

  private readonly catalogCache = new Map<string, Observable<CatalogItem[]>>();
  private catalog(name: string): Observable<CatalogItem[]> {
    if (!this.catalogCache.has(name)) {
      this.catalogCache.set(
        name,
        this.http
          .get<CatalogItem[]>(`${this.api}/Catalog/${name}`)
          .pipe(shareReplay({ bufferSize: 1, refCount: false })),
      );
    }
    return this.catalogCache.get(name)!;
  }

  getTypePersons(): Observable<CatalogItem[]> { return this.catalog('type-persons'); }
  getGenders(): Observable<CatalogItem[]> { return this.catalog('genders'); }
  getCivilStates(): Observable<CatalogItem[]> { return this.catalog('civil-states'); }
  getProductStatuses(): Observable<CatalogItem[]> { return this.catalog('product-status'); }
  getContractStatuses(): Observable<CatalogItem[]> { return this.catalog('contract-status'); }
  getPayForms(): Observable<CatalogItem[]> { return this.catalog('pay-forms'); }
  getStages(): Observable<CatalogItem[]> { return this.catalog('stages'); }
  getTransactionTypes(): Observable<CatalogItem[]> { return this.catalog('transaction-types'); }
  getDisasterStates(): Observable<CatalogItem[]> { return this.catalog('disaster-states'); }

  /* ----------------------------------------------------------- clientes -- */

  getClients(filter: {
    search?: string;
    typePersonId?: number | null;
    assignedUserId?: number | null;
  } = {}): Observable<ClientGrid[]> {
    return this.http.get<ClientGrid[]>(`${this.api}/Client`, { params: toParams(filter) });
  }

  getPhisicClient(id: number): Observable<PhisicClientDetail> {
    return this.http.get<PhisicClientDetail>(`${this.api}/Client/phisic/${id}`);
  }

  getMoralClient(id: number): Observable<MoralClientDetail> {
    return this.http.get<MoralClientDetail>(`${this.api}/Client/moral/${id}`);
  }

  deleteClient(id: number): Observable<unknown> {
    return this.http.delete(`${this.api}/Client/${id}`);
  }

  /* ---------------------------------------------------------- productos -- */

  getProducts(filter: { areaId?: number | null; statusId?: number | null } = {}): Observable<FinanceProduct[]> {
    return this.http.get<FinanceProduct[]>(`${this.api}/Product`, { params: toParams(filter) });
  }

  deleteProduct(id: number): Observable<unknown> {
    return this.http.delete(`${this.api}/Product/${id}`);
  }

  /* ---------------------------------------------------------- contratos -- */

  getContracts(filter: {
    areaId?: number | null;
    clientId?: number | null;
    userId?: number | null;
    contractStatusId?: number | null;
    search?: string;
  } = {}): Observable<ContractGrid[]> {
    return this.http.get<ContractGrid[]>(`${this.api}/Contract`, { params: toParams(filter) });
  }

  /** Requiere BancaOAdministrador. */
  getBankContract(id: number): Observable<BankContractDetail> {
    return this.http.get<BankContractDetail>(`${this.api}/Contract/bank/${id}`);
  }

  /** Requiere SegurosOAdministrador. */
  getInsuranceContract(id: number): Observable<InsuranceContractDetail> {
    return this.http.get<InsuranceContractDetail>(`${this.api}/Contract/insurance/${id}`);
  }

  changeContractStatus(id: number, contractStatusId: number): Observable<unknown> {
    return this.http.patch(`${this.api}/Contract/${id}/status`, { ContractStatusId: contractStatusId });
  }

  /* -------------------------------------------------------- operaciones -- */

  /** Requiere BancaOAdministrador. */
  getTransactions(contractId: number): Observable<TransactionResponse[]> {
    return this.http.get<TransactionResponse[]>(`${this.api}/Operation/transactions/contract/${contractId}`);
  }

  /** Requiere SegurosOAdministrador. */
  getClaims(contractId: number): Observable<InsuranceClaimResponse[]> {
    return this.http.get<InsuranceClaimResponse[]>(`${this.api}/Operation/claims/contract/${contractId}`);
  }

  /* ------------------------------------------------------ oportunidades -- */

  getOportunities(filter: {
    clientId?: number | null;
    userId?: number | null;
    stageId?: number | null;
    areaId?: number | null;
  } = {}): Observable<OportunityResponse[]> {
    return this.http.get<OportunityResponse[]>(`${this.api}/Oportunity`, { params: toParams(filter) });
  }

  createOportunity(body: OportunityCreateRequest): Observable<OportunityResponse> {
    return this.http.post<OportunityResponse>(`${this.api}/Oportunity`, body);
  }

  changeOportunityStage(id: number, body: OportunityStageUpdateRequest): Observable<OportunityResponse> {
    return this.http.patch<OportunityResponse>(`${this.api}/Oportunity/${id}/stage`, body);
  }

  deleteOportunity(id: number): Observable<unknown> {
    return this.http.delete(`${this.api}/Oportunity/${id}`);
  }
}
