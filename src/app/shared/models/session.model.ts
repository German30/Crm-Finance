/** Identidad que el frontend lee del JWT emitido por AuthService.cs. */
export interface SessionUser {
  name: string;
  email: string;
  /** ClaimTypes.Role — uno de los 19 roles del catálogo. */
  role: string;
  /** Claim "Area" — General, Seguros o Banca. Decide el alcance de negocio. */
  area: string;
  userId: number | null;
  expiresAt: number | null;
}
