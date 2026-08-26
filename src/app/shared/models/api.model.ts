/**
 * Contratos de la API .NET (CRMFinacieroBackend).
 *
 * Los nombres siguen EXACTAMENTE lo que serializa el backend, incluidas sus
 * erratas (`clinetId`, `prdoductName`, `succesProbability`). Corregirlas aquí
 * rompería el mapeo en silencio; se corrigen en el backend o no se corrigen.
 */

/* --------------------------------------------------------------- sesión -- */

export interface LoginRequest {
  Email: string;
  Password: string;
}

export interface LoginResponse {
  token: string;
  expirationInMinutes: number;
}

/** GET /api/Auth/me */
export interface CurrentUser {
  userId: number;
  name: string;
  email: string;
  roleName: string;
  areaName: string;
}

export interface ChangePasswordRequest {
  CurrentPassword: string;
  NewPassword: string;
}

/* -------------------------------------------------------------- usuarios -- */

export interface UserResponse {
  userId: number;
  name: string;
  email: string;
  roleId: number;
  roleName: string;
  areaName: string;
  statusName: string;
  creationDate: string;
}

export interface CreateUserRequest {
  RoleId: number;
  Name: string;
  Email: string;
  Password: string;
}

export interface UpdateUserRequest {
  Name: string;
  Email: string;
  StatusId: number;
  RoleId: number;
}

export interface ResetPasswordRequest {
  NewPassword: string;
}

export interface Role {
  roleId: number;
  areaId: number;
  roleName: string;
  category: string;
  description: string | null;
}

export interface Area {
  areaId: number;
  areaName: string;
  description: string | null;
}

export interface UserStatus {
  statusId: number;
  statusName: string;
}

/* ------------------------------------------------------------- catálogos -- */

export interface CatalogItem {
  id: number;
  name: string;
}

/* -------------------------------------------------------------- clientes -- */

export interface ClientGrid {
  /** El backend serializa `clinetId` (errata en ClientGridDto). */
  clinetId: number;
  typePersonName: string;
  fiscalId: string | null;
  clientName: string;
  email: string | null;
  phone: string | null;
  assignedUserName: string | null;
}

export interface PhisicClientDetail {
  clientId: number;
  fiscalId: string | null;
  email: string | null;
  phone: string | null;
  addressFiscal: string | null;
  registerDate: string;
  assignedUserName: string | null;
  names: string;
  fatherLastName: string;
  motherLastName: string;
  birthDate: string;
  genderName: string;
  civilStateName: string;
}

export interface MoralClientDetail {
  clientId: number;
  fiscalId: string | null;
  email: string | null;
  phone: string | null;
  addressFiscal: string | null;
  registerDate: string;
  assignedUserName: string | null;
  socialRazon: string;
  comercialName: string | null;
  dateConstitucion: string;
  comercialActivity: string | null;
  representativeLegalName: string;
  representativeId: string | null;
}

/* ------------------------------------------------------------- productos -- */

export interface FinanceProduct {
  productId: number;
  areaName: string;
  productName: string;
  description: string | null;
  tasaInteresOPrimaBase: number;
  statusName: string;
}

/* ------------------------------------------------------------- contratos -- */

export interface ContractGrid {
  contractId: number;
  referenceNumber: string;
  clientName: string;
  productName: string;
  areaName: string;
  contractStatusName: string;
  dateOpeningIssue: string;
}

export interface BankContractDetail {
  contractId: number;
  referenceNumber: string;
  clientName: string;
  productName: string;
  contractStatusName: string;
  dateOpeningIssue: string;
  dateEnd: string | null;
  interbankCode: string | null;
  balanceActual: number;
  loanAmountGranted: number;
  agreedInterestRate: number;
  monthlyCotoffDay: number;
}

export interface InsuranceContractDetail {
  contractId: number;
  referenceNumber: string;
  clientName: string;
  productName: string;
  contractStatusName: string;
  dateOpeningIssue: string;
  dateEnd: string | null;
  insuranceSumeTotal: number;
  totalAnnualPremium: number;
  payFromName: string;
  porcentDeductible: number;
  beneficiaryName: string | null;
}

/* ----------------------------------------------------------- operaciones -- */

export interface TransactionResponse {
  transactionId: number;
  contractId: number;
  transactionTypeName: string;
  amount: number;
  dateTransaction: string;
  description?: string | null;
  referenceNumber?: string | null;
}

export interface InsuranceClaimResponse {
  insuranceId: number;
  contractId: number;
  disasterStateName: string;
  dateClaim: string;
  description?: string | null;
  amountClaimed?: number | null;
  amountApproved?: number | null;
}

/* ---------------------------------------------------------- oportunidades -- */

export interface OportunityResponse {
  oportunityId: number;
  clientId: number;
  clientName: string;
  /** El backend serializa `prdoductName` (errata en OportunityResponseDto). */
  prdoductName: string;
  areaName: string;
  assignedUserName: string;
  estimatedMont: number | null;
  stageName: string;
  /** El backend serializa `succesProbability` (errata en el DTO). */
  succesProbability: number;
  dateEstimatedClose: string | null;
  dateRegister: string;
}

export interface OportunityCreateRequest {
  ClientId: number;
  ProductId: number;
  UserId: number;
  EstimatedMont: number | null;
  StageId: number;
  DateEstimatedClose: string | null;
  SuccessProbability: number;
}

export interface OportunityStageUpdateRequest {
  StageId: number;
}
