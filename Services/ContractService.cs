using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Data;
using CRMFinaciertoBackend.Models;
using CRMFinaciertoBackend.DTOs.Contract;

namespace CRMFinaciertoBackend.Services
{
    public class ContractService : IContractService
    {
        public const string AreaBanca = "Banca";
        public const string AreaSeguros = "Seguros";

        private readonly ApplicationDbContext _context;

        public ContractService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ContractGridDto>> GetAllContractsAsync(
            int? areaId, int? clientId, int? userId, int? contractStatusId, string? search)
        {
            var query = _context.ProductContract.AsQueryable();

            if (areaId.HasValue)
                query = query.Where(c => c.FinanceProduct!.AreaId == areaId.Value);

            if (clientId.HasValue)
                query = query.Where(c => c.ClientId == clientId.Value);

            if (userId.HasValue)
                query = query.Where(c => c.UserId == userId.Value);

            if (contractStatusId.HasValue)
                query = query.Where(c => c.ContractStatusId == contractStatusId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c => c.ReferenceNumber.Contains(term));
            }

            return await query
                .OrderByDescending(c => c.ContractId)
                .Select(c => new ContractGridDto(
                    c.ContractId,
                    c.ReferenceNumber,
                    c.Client!.PhisicPerson != null
                        ? c.Client.PhisicPerson.Names + " " + c.Client.PhisicPerson.FatherLastName + " " + c.Client.PhisicPerson.MotherLastName
                        : (c.Client.MoralPerson != null ? c.Client.MoralPerson.SocialRazon : "Sin nombre"),
                    c.FinanceProduct!.ProductNamme,
                    c.FinanceProduct.Area!.AreaName,
                    c.ContractStatus!.ContractStatusName!,
                    c.DateOpeningIssue
                ))
                .ToListAsync();
        }

        public async Task<object?> GetContractDetailAsync(int id)
        {
            // Se prueba una proyeccion y luego la otra en vez de sondear con AnyAsync primero:
            // son 1-2 consultas en vez de las 2-4 que costaba comprobar-y-luego-leer.
            return await GetBankContractAsync(id)
                ?? (object?)await GetInsuranceContractAsync(id);
        }

        public async Task<BankContractDetailDto?> GetBankContractAsync(int id)
        {
            return await _context.BankContract
                .Where(b => b.ContractId == id)
                .Select(b => new BankContractDetailDto(
                    b.ContractId,
                    b.ProductsContract!.ReferenceNumber,
                    b.ProductsContract.Client!.PhisicPerson != null
                        ? b.ProductsContract.Client.PhisicPerson.Names + " " + b.ProductsContract.Client.PhisicPerson.FatherLastName + " " + b.ProductsContract.Client.PhisicPerson.MotherLastName
                        : (b.ProductsContract.Client.MoralPerson != null ? b.ProductsContract.Client.MoralPerson.SocialRazon : "Sin nombre"),
                    b.ProductsContract.FinanceProduct!.ProductNamme,
                    b.ProductsContract.ContractStatus!.ContractStatusName!,
                    b.ProductsContract.DateOpeningIssue,
                    b.ProductsContract.DateEnd,
                    b.InterbankCode,
                    b.BalanceActual,
                    b.LoanAmountGranted,
                    b.AgreeInterestRate,
                    b.MonthlyCutoffDay
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<InsuranceContractDetailDto?> GetInsuranceContractAsync(int id)
        {
            return await _context.InsuranceContract
                .Where(i => i.ContractId == id)
                .Select(i => new InsuranceContractDetailDto(
                    i.ContractId,
                    i.ProductsContract!.ReferenceNumber,
                    i.ProductsContract.Client!.PhisicPerson != null
                        ? i.ProductsContract.Client.PhisicPerson.Names + " " + i.ProductsContract.Client.PhisicPerson.FatherLastName + " " + i.ProductsContract.Client.PhisicPerson.MotherLastName
                        : (i.ProductsContract.Client.MoralPerson != null ? i.ProductsContract.Client.MoralPerson.SocialRazon : "Sin nombre"),
                    i.ProductsContract.FinanceProduct!.ProductNamme,
                    i.ProductsContract.ContractStatus!.ContractStatusName!,
                    i.ProductsContract.DateOpeningIssue,
                    i.ProductsContract.DateEnd,
                    i.InsuranceSumeTotal,
                    i.TotalAnnualPremiu,
                    i.PayForm!.PayFormName!,
                    i.PorcentDeductible,
                    i.BeneficiaryName
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<BankContractDetailDto> CreateBankContractAsync(BankContractCrateDto dto)
        {
            await ValidateClientAsync(dto.ClientId);
            await ValidateUserAsync(dto.UserId);
            await ValidateContractStatusAsync(dto.ContractStatusId);
            await ValidateProductAreaAsync(dto.ProductId, AreaBanca);
            await ValidateReferenceIsFreeAsync(dto.ReferenceNumber, null);
            await ValidateInterbankCodeIsFreeAsync(dto.InterbankCode, null);
            ValidateDates(dto.DateOpeningIssue, dto.DateEnd);
            ValidateCutoffDay(dto.MonthlyCutoffDay);

            var contract = new ProductsContract
            {
                ClientId = dto.ClientId,
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                ReferenceNumber = dto.ReferenceNumber,
                DateOpeningIssue = dto.DateOpeningIssue,
                DateEnd = dto.DateEnd,
                ContractStatusId = dto.ContractStatusId,
                DateRegister = DateTime.UtcNow,
                BankContract = new BankContract
                {
                    InterbankCode = NormalizeInterbankCode(dto.InterbankCode),
                    BalanceActual = dto.BalanceActual,
                    LoanAmountGranted = dto.LoanAmountGranted,
                    AgreeInterestRate = dto.AgreedInterestRate,
                    MonthlyCutoffDay = dto.MonthlyCutoffDay
                }
            };

            _context.ProductContract.Add(contract);
            await _context.SaveChangesAsync();

            return (await GetBankContractAsync(contract.ContractId))!;
        }

        public async Task<InsuranceContractDetailDto> CreateInsuranceContractAsync(InsuranceContranctCreateDto dto)
        {
            await ValidateClientAsync(dto.ClientId);
            await ValidateUserAsync(dto.UserId);
            await ValidateContractStatusAsync(dto.ContractStatusId);
            await ValidateProductAreaAsync(dto.ProductId, AreaSeguros);
            await ValidateReferenceIsFreeAsync(dto.ReferenceNumber, null);
            await ValidatePayFormAsync(dto.PayFormId);
            ValidateDates(dto.DateOpeningIssue, dto.DateEnd);

            var contract = new ProductsContract
            {
                ClientId = dto.ClientId,
                ProductId = dto.ProductId,
                UserId = dto.UserId,
                ReferenceNumber = dto.ReferenceNumber,
                DateOpeningIssue = dto.DateOpeningIssue,
                DateEnd = dto.DateEnd,
                ContractStatusId = dto.ContractStatusId,
                DateRegister = DateTime.UtcNow,
                InsuranceContract = new InsuranceContract
                {
                    InsuranceSumeTotal = dto.InsuranceAmountGranted,
                    TotalAnnualPremiu = dto.TotalAnnualPremium,
                    PayFormId = dto.PayFormId,
                    PorcentDeductible = dto.PorcentDeductible,
                    BeneficiaryName = dto.BeneficiaryName
                }
            };

            _context.ProductContract.Add(contract);
            await _context.SaveChangesAsync();

            return (await GetInsuranceContractAsync(contract.ContractId))!;
        }

        public async Task<BankContractDetailDto?> UpdateBankContractAsync(int id, BankContractUpdateDto dto)
        {
            var bank = await _context.BankContract
                .Include(b => b.ProductsContract)
                .FirstOrDefaultAsync(b => b.ContractId == id);

            if (bank?.ProductsContract == null) return null;

            await ValidateUserAsync(dto.UserId);
            await ValidateContractStatusAsync(dto.ContractStatusId);
            await ValidateInterbankCodeIsFreeAsync(dto.InterbankCode, id);
            ValidateDates(dto.DateOpeningIssue, dto.DateEnd);
            ValidateCutoffDay(dto.MonthlyCutoffDay);

            bank.ProductsContract.UserId = dto.UserId;
            bank.ProductsContract.DateOpeningIssue = dto.DateOpeningIssue;
            bank.ProductsContract.DateEnd = dto.DateEnd;
            bank.ProductsContract.ContractStatusId = dto.ContractStatusId;

            bank.InterbankCode = NormalizeInterbankCode(dto.InterbankCode);
            bank.LoanAmountGranted = dto.LoanAmountGranted;
            bank.AgreeInterestRate = dto.AgreedInterestRate;
            bank.MonthlyCutoffDay = dto.MonthlyCutoffDay;

            await _context.SaveChangesAsync();
            return await GetBankContractAsync(id);
        }

        public async Task<InsuranceContractDetailDto?> UpdateInsuranceContractAsync(int id, InsuranceContractUpdateDto dto)
        {
            var insurance = await _context.InsuranceContract
                .Include(i => i.ProductsContract)
                .FirstOrDefaultAsync(i => i.ContractId == id);

            if (insurance?.ProductsContract == null) return null;

            await ValidateUserAsync(dto.UserId);
            await ValidateContractStatusAsync(dto.ContractStatusId);
            await ValidatePayFormAsync(dto.PayFormId);
            ValidateDates(dto.DateOpeningIssue, dto.DateEnd);

            insurance.ProductsContract.UserId = dto.UserId;
            insurance.ProductsContract.DateOpeningIssue = dto.DateOpeningIssue;
            insurance.ProductsContract.DateEnd = dto.DateEnd;
            insurance.ProductsContract.ContractStatusId = dto.ContractStatusId;

            insurance.InsuranceSumeTotal = dto.InsuranceSumeTotal;
            insurance.TotalAnnualPremiu = dto.TotalAnnualPremium;
            insurance.PayFormId = dto.PayFormId;
            insurance.PorcentDeductible = dto.PorcentDeductible;
            insurance.BeneficiaryName = dto.BeneficiaryName;

            await _context.SaveChangesAsync();
            return await GetInsuranceContractAsync(id);
        }

        public async Task<bool> ChangeContractStatusAsync(int id, int contractStatusId)
        {
            var contract = await _context.ProductContract.FindAsync(id);
            if (contract == null) return false;

            await ValidateContractStatusAsync(contractStatusId);

            contract.ContractStatusId = contractStatusId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteContractAsync(int id)
        {
            var contract = await _context.ProductContract.FindAsync(id);
            if (contract == null) return false;

            // bank_contract e insurance_contranct caen en cascada, pero sus movimientos
            // (transacciones y siniestros) son Restrict: hay que bloquear el borrado.
            if (await _context.BankTransaction.AnyAsync(t => t.ContractId == id))
                throw new InvalidOperationException("El contrato tiene transacciones registradas y no puede eliminarse.");

            if (await _context.InsuranceClaim.AnyAsync(c => c.ContractId == id))
                throw new InvalidOperationException("El contrato tiene siniestros registrados y no puede eliminarse.");

            _context.ProductContract.Remove(contract);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ValidateClientAsync(int clientId)
        {
            if (!await _context.Clients.AnyAsync(c => c.ClientId == clientId))
                throw new InvalidOperationException($"El cliente {clientId} no existe.");
        }

        private async Task ValidateUserAsync(int userId)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == userId))
                throw new InvalidOperationException($"El usuario {userId} no existe.");
        }

        private async Task ValidateContractStatusAsync(int contractStatusId)
        {
            if (!await _context.ContarctStatus.AnyAsync(s => s.ContractSatusId == contractStatusId))
                throw new InvalidOperationException($"El estado de contrato {contractStatusId} no existe.");
        }

        private async Task ValidatePayFormAsync(int payFormId)
        {
            if (!await _context.PayForm.AnyAsync(p => p.PayFormId == payFormId))
                throw new InvalidOperationException($"La forma de pago {payFormId} no existe.");
        }

        private async Task ValidateProductAreaAsync(int productId, string expectedAreaName)
        {
            var areaName = await _context.FinanceProducts
                .Where(p => p.ProductId == productId)
                .Select(p => p.Area!.AreaName)
                .FirstOrDefaultAsync();

            if (areaName == null)
                throw new InvalidOperationException($"El producto {productId} no existe.");

            if (areaName != expectedAreaName)
                throw new InvalidOperationException(
                    $"El producto {productId} pertenece al area {areaName} y no puede contratarse como producto de {expectedAreaName}.");
        }

        private async Task ValidateReferenceIsFreeAsync(string referenceNumber, int? excludeContractId)
        {
            var taken = await _context.ProductContract
                .AnyAsync(c => c.ReferenceNumber == referenceNumber
                            && (excludeContractId == null || c.ContractId != excludeContractId));

            if (taken)
                throw new InvalidOperationException($"El numero de referencia {referenceNumber} ya esta registrado.");
        }

        /// <summary>
        /// <c>bank_contract.interbank_code</c> tiene un indice UNIQUE y admite nulos.
        /// <para>
        /// Solo se comprueban los codigos indicados: los contratos SIN codigo no compiten entre
        /// si. MySQL trata cada NULL como distinto en un indice unico, asi que admite tantos
        /// como haga falta. (SQL Server los consideraba iguales y solo dejaba uno, por eso alli
        /// el indice tuvo que pasar a filtrado: <c>WHERE interbank_code IS NOT NULL</c>.)
        /// </para>
        /// </summary>
        private async Task ValidateInterbankCodeIsFreeAsync(string? interbankCode, int? excludeContractId)
        {
            var normalized = NormalizeInterbankCode(interbankCode);
            if (normalized == null) return;

            var taken = await _context.BankContract
                .AnyAsync(b => b.InterbankCode == normalized
                            && (excludeContractId == null || b.ContractId != excludeContractId));

            if (taken)
                throw new InvalidOperationException($"El codigo interbancario {normalized} ya esta registrado.");
        }

        private static string? NormalizeInterbankCode(string? interbankCode)
            => string.IsNullOrWhiteSpace(interbankCode) ? null : interbankCode.Trim();

        private static void ValidateDates(DateTime dateOpeningIssue, DateTime? dateEnd)
        {
            if (dateEnd.HasValue && dateEnd.Value < dateOpeningIssue)
                throw new InvalidOperationException("La fecha de fin no puede ser anterior a la fecha de apertura.");
        }

        private static void ValidateCutoffDay(int monthlyCutoffDay)
        {
            // Se limita a 28 para que el corte exista en todos los meses.
            if (monthlyCutoffDay < 1 || monthlyCutoffDay > 28)
                throw new InvalidOperationException("El dia de corte mensual debe estar entre 1 y 28.");
        }
    }
}
