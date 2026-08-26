using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.Operation;

namespace CRMFinaciertoBackend.Services
{
    public class OperationService : IOperationService
    {
        // bank_contract.balance_actual significa dos cosas distintas segun el producto:
        //
        //   - Cuenta de debito o de ahorro -> SALDO TOTAL (dinero del cliente).
        //   - Cuenta de credito            -> SALDO DEUDOR (lo que el cliente debe).
        //
        // Por eso el signo de un movimiento no depende solo del tipo de transaccion sino
        // tambien del producto contratado: un "Cobro de Comision" baja el saldo de una cuenta
        // de ahorro pero sube la deuda de una tarjeta. Ni transaction_type ni finance_products
        // guardan esa clasificacion, asi que se deduce del nombre (mismo criterio que ya se
        // usaba para el tipo de movimiento).
        //
        // Ojo: las dos tablas NO son inversas. "Interes Generado" suma en ambas: en la cuenta
        // de ahorro es interes ganado y en la de credito es interes cobrado, que engorda la deuda.

        /// <summary>Saldo total: entra dinero.</summary>
        private static readonly string[] DebitoSuman = { "deposito", "abono", "ingreso", "interes" };

        /// <summary>Saldo total: sale dinero.</summary>
        private static readonly string[] DebitoRestan = { "retiro", "cargo", "comision", "pago", "egreso", "disposicion" };

        /// <summary>Saldo deudor: la deuda crece.</summary>
        private static readonly string[] CreditoSuman = { "retiro", "cargo", "comision", "disposicion", "interes", "egreso" };

        /// <summary>Saldo deudor: la deuda se amortiza.</summary>
        private static readonly string[] CreditoRestan = { "pago", "abono", "deposito", "ingreso" };

        /// <summary>
        /// Palabras que marcan un producto de credito dentro del area de Banca. Cubren los que
        /// hay sembrados: Tarjeta de Credito, Credito Personal / de Nomina, Credito Hipotecario
        /// y Credito Automotriz. El resto (Cuenta de Ahorro, Cheques, Nomina, Pagare, Fondo de
        /// Inversion, Caja de Seguridad, Transferencias) se trata como saldo total.
        /// </summary>
        private static readonly string[] ProductosDeCredito = { "credito", "tarjeta" };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<OperationService> _logger;

        public OperationService(ApplicationDbContext context, ILogger<OperationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByContractAsync(int contractId)
        {
            return await _context.BankTransaction
                .Where(t => t.ContractId == contractId)
                .OrderByDescending(t => t.DateTransaction)
                .Select(t => new TransactionResponseDto(
                    t.TransactionId,
                    t.BankContract!.ProductsContract!.ReferenceNumber,
                    t.TransactionType!.TransactionTypeName!,
                    t.Amount,
                    t.DateTransaction,
                    t.Description
                ))
                .ToListAsync();
        }

        public async Task<TransactionResponseDto?> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.BankTransaction
                .Where(t => t.TransactionId == transactionId)
                .Select(t => new TransactionResponseDto(
                    t.TransactionId,
                    t.BankContract!.ProductsContract!.ReferenceNumber,
                    t.TransactionType!.TransactionTypeName!,
                    t.Amount,
                    t.DateTransaction,
                    t.Description
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<TransactionResponseDto> CreateTransactionAsync(TransactionCreateDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("El importe de la transaccion debe ser mayor que cero.");

            // Se trae el nombre del producto en la misma consulta que comprueba el contrato:
            // hace falta para saber si balance_actual es saldo total o saldo deudor.
            var productName = await _context.BankContract
                .Where(b => b.ContractId == dto.ContractId)
                .Select(b => b.ProductsContract!.FinanceProduct!.ProductNamme)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"El contrato bancario {dto.ContractId} no existe.");

            var transactionType = await _context.TransactionType
                .Where(t => t.TransactionTypeId == dto.TransactionTypeId)
                .Select(t => new { t.TransactionTypeId, t.TransactionTypeName })
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"El tipo de transaccion {dto.TransactionTypeId} no existe.");

            var esCredito = EsProductoDeCredito(productName);
            var signo = ResolveSigno(transactionType.TransactionTypeName, esCredito);

            if (signo == 0)
            {
                _logger.LogWarning(
                    "El tipo de transaccion {TransactionTypeId} ({Nombre}) no esta clasificado para un producto de tipo {Tipo}: el balance del contrato {ContractId} no se modifico.",
                    transactionType.TransactionTypeId, transactionType.TransactionTypeName,
                    esCredito ? "credito" : "debito", dto.ContractId);
            }

            var delta = signo * dto.Amount;
            var transactionId = 0;

            // La estrategia de reintento de EF exige que las transacciones explicitas se
            // ejecuten dentro de ella: si un intento falla, se reejecuta el bloque entero.
            // Por eso la entidad se construye AQUI DENTRO: tras un reintento la anterior
            // quedaria con su clave ya asignada y en un estado que no se puede reinsertar.
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var transaction = new BankTransaction
                {
                    ContractId = dto.ContractId,
                    TransactionTypeId = dto.TransactionTypeId,
                    Amount = dto.Amount,
                    DateTransaction = DateTime.UtcNow,
                    Description = dto.Description
                };

                _context.BankTransaction.Add(transaction);
                await _context.SaveChangesAsync();

                await ApplyBalanceDeltaAsync(dto.ContractId, delta);

                await dbTransaction.CommitAsync();
                transactionId = transaction.TransactionId;
            });

            return (await GetTransactionByIdAsync(transactionId))!;
        }

        public async Task<bool> DeleteTransactionAsync(int transactionId)
        {
            var transaction = await _context.BankTransaction
                .Include(t => t.TransactionType)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transaction == null) return false;

            var contractId = transaction.ContractId;

            var productName = await _context.BankContract
                .Where(b => b.ContractId == contractId)
                .Select(b => b.ProductsContract!.FinanceProduct!.ProductNamme)
                .FirstOrDefaultAsync();

            // Se revierte el efecto sobre el balance con el mismo criterio que al crearla:
            // mismo tipo de movimiento y mismo tipo de producto.
            var signo = ResolveSigno(transaction.TransactionType?.TransactionTypeName, EsProductoDeCredito(productName));
            var delta = -signo * transaction.Amount;

            // Igual que en el alta: dentro de la estrategia de reintento. La fila se vuelve a
            // buscar aqui dentro porque tras un reintento la instancia anterior ya estaria
            // desasociada del contexto y no se podria borrar de nuevo.
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var fila = await _context.BankTransaction
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (fila != null)
                {
                    _context.BankTransaction.Remove(fila);
                    await _context.SaveChangesAsync();

                    await ApplyBalanceDeltaAsync(contractId, delta);
                }

                await dbTransaction.CommitAsync();
            });

            return true;
        }

        /// <summary>
        /// Aplica el movimiento al balance con un UPDATE aritmetico en el servidor
        /// (<c>balance_actual = balance_actual + @delta</c>).
        /// <para>
        /// Antes se leia la entidad, se sumaba en memoria y se guardaba: dos transacciones
        /// concurrentes sobre el mismo contrato leian el mismo balance de partida y la segunda
        /// pisaba a la primera (lost update). Dejando la suma en SQL, el bloqueo de fila del
        /// UPDATE serializa los movimientos y ninguno se pierde.
        /// </para>
        /// </summary>
        private async Task ApplyBalanceDeltaAsync(int contractId, decimal delta)
        {
            if (delta == 0) return;

            await _context.BankContract
                .Where(b => b.ContractId == contractId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(b => b.BalanceActual, b => b.BalanceActual + delta));
        }

        public async Task<IEnumerable<InsuranceClaiResponseDto>> GetClaimsByContractAsync(int contractId)
        {
            return await _context.InsuranceClaim
                .Where(c => c.ContractId == contractId)
                .OrderByDescending(c => c.DateOcurrence)
                .Select(c => new InsuranceClaiResponseDto(
                    c.InsuranceId,
                    c.InsuranceContract!.ProductsContract!.ReferenceNumber,
                    c.ReportNumber,
                    c.DateOcurrence,
                    c.AmountClaimed,
                    c.AmountPaid,
                    c.DisasterState!.DisasterStateName!,
                    c.ReportDetails,
                    c.DateRegister
                ))
                .ToListAsync();
        }

        public async Task<InsuranceClaiResponseDto?> GetClaimByIdAsync(int insuranceId)
        {
            return await _context.InsuranceClaim
                .Where(c => c.InsuranceId == insuranceId)
                .Select(c => new InsuranceClaiResponseDto(
                    c.InsuranceId,
                    c.InsuranceContract!.ProductsContract!.ReferenceNumber,
                    c.ReportNumber,
                    c.DateOcurrence,
                    c.AmountClaimed,
                    c.AmountPaid,
                    c.DisasterState!.DisasterStateName!,
                    c.ReportDetails,
                    c.DateRegister
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<InsuranceClaiResponseDto> CreateClaimAsync(InsuranceClaimCreateDto dto)
        {
            var insuranceContract = await _context.InsuranceContract
                .FirstOrDefaultAsync(i => i.ContractId == dto.ContractId)
                ?? throw new InvalidOperationException($"El contrato de seguro {dto.ContractId} no existe.");

            await ValidateDisasterStateAsync(dto.DisasterStateId);
            await ValidateReportNumberIsFreeAsync(dto.ReportNumber, null);

            if (dto.AmountClaimed <= 0)
                throw new InvalidOperationException("El monto reclamado debe ser mayor que cero.");

            if (dto.AmountClaimed > insuranceContract.InsuranceSumeTotal)
                throw new InvalidOperationException("El monto reclamado no puede superar la suma asegurada del contrato.");

            if (dto.AmountPaid < 0 || dto.AmountPaid > dto.AmountClaimed)
                throw new InvalidOperationException("El monto pagado debe estar entre cero y el monto reclamado.");

            if (dto.DateOccurrence > DateTime.UtcNow)
                throw new InvalidOperationException("La fecha del siniestro no puede ser futura.");

            var claim = new InsuranceClaim
            {
                ContractId = dto.ContractId,
                ReportNumber = dto.ReportNumber,
                DateOcurrence = dto.DateOccurrence,
                AmountClaimed = dto.AmountClaimed,
                AmountPaid = dto.AmountPaid,
                DisasterStateId = dto.DisasterStateId,
                ReportDetails = dto.ReportDetails,
                DateRegister = DateTime.UtcNow
            };

            _context.InsuranceClaim.Add(claim);
            await _context.SaveChangesAsync();

            return (await GetClaimByIdAsync(claim.InsuranceId))!;
        }

        public async Task<InsuranceClaiResponseDto?> UpdateClaimAsync(int insuranceId, InsuranceClaimUpdateDto dto)
        {
            var claim = await _context.InsuranceClaim.FindAsync(insuranceId);
            if (claim == null) return null;

            await ValidateDisasterStateAsync(dto.DisasterStateId);

            if (dto.AmountPaid < 0 || dto.AmountPaid > claim.AmountClaimed)
                throw new InvalidOperationException("El monto pagado debe estar entre cero y el monto reclamado.");

            claim.DisasterStateId = dto.DisasterStateId;
            claim.AmountPaid = dto.AmountPaid;
            claim.ReportDetails = dto.ReportDetails;

            await _context.SaveChangesAsync();
            return await GetClaimByIdAsync(insuranceId);
        }

        public async Task<bool> DeleteClaimAsync(int insuranceId)
        {
            var claim = await _context.InsuranceClaim.FindAsync(insuranceId);
            if (claim == null) return false;

            _context.InsuranceClaim.Remove(claim);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateDisasterStateAsync(int disasterStateId)
        {
            if (!await _context.DisaterState.AnyAsync(d => d.DisasterStateId == disasterStateId))
                throw new InvalidOperationException($"El estado de siniestro {disasterStateId} no existe.");
        }

        private async Task ValidateReportNumberIsFreeAsync(string reportNumber, int? excludeInsuranceId)
        {
            var taken = await _context.InsuranceClaim
                .AnyAsync(c => c.ReportNumber == reportNumber
                            && (excludeInsuranceId == null || c.InsuranceId != excludeInsuranceId));

            if (taken)
                throw new InvalidOperationException($"El numero de reporte {reportNumber} ya esta registrado.");
        }

        /// <summary>
        /// Signo con el que un movimiento afecta a <c>balance_actual</c>. Devuelve 0 cuando el
        /// tipo no encaja en ninguna lista: es preferible dejar el balance intacto y avisar a
        /// mover dinero con una suposicion.
        /// </summary>
        private static int ResolveSigno(string? transactionTypeName, bool esProductoDeCredito)
        {
            if (string.IsNullOrWhiteSpace(transactionTypeName)) return 0;

            var nombre = Normalize(transactionTypeName);

            var suman = esProductoDeCredito ? CreditoSuman : DebitoSuman;
            var restan = esProductoDeCredito ? CreditoRestan : DebitoRestan;

            if (suman.Any(t => nombre.Contains(t))) return 1;
            if (restan.Any(t => nombre.Contains(t))) return -1;

            return 0;
        }

        private static bool EsProductoDeCredito(string? productName)
        {
            if (string.IsNullOrWhiteSpace(productName)) return false;

            var nombre = Normalize(productName);
            return ProductosDeCredito.Any(p => nombre.Contains(p));
        }

        private static string Normalize(string value) => RemoveDiacritics(value).ToLowerInvariant();

        private static string RemoveDiacritics(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
