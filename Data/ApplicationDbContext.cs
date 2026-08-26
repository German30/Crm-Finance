using Microsoft.EntityFrameworkCore;
using CRMFinaciertoBackend.Models;

namespace CRMFinaciertoBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Area> Areas { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserStatus> UserStatuses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<TypePerson> TypePerson { get; set; }
        public DbSet<Gender> Gender { get; set; }
        public DbSet<CivilState> CivilState { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<PhisicPersonClient> PhisicPersonClient { get; set; }
        public DbSet<MoralPersonClient> MoralPersonClient { get; set; }
        public DbSet<FinanceStatusProduct> FinanceStatusProducts { get; set; }
        public DbSet<FinanceProduct> FinanceProducts { get; set; }
        public DbSet<ContractStatus> ContarctStatus { get; set; }
        public DbSet<ProductsContract> ProductContract { get; set; }
        public DbSet<BankContract> BankContract { get; set; }
        public DbSet<PayForm> PayForm { get; set; }
        public DbSet<InsuranceContract> InsuranceContract { get; set; }
        public DbSet<Stage> Stage { get; set; }
        public DbSet<ComercialOportunity> ComercialOportunity { get; set; }
        public DbSet<TransactionType> TransactionType { get; set; }
        public DbSet<BankTransaction> BankTransaction { get; set; }
        public DbSet<DisasterState> DisaterState { get; set; }
        public DbSet<InsuranceClaim> InsuranceClaim { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PhisicPersonClient>()
                .HasKey(p => p.ClientId);

            modelBuilder.Entity<PhisicPersonClient>()
                .HasOne(p => p.Client)
                .WithOne(c => c.PhisicPerson)
                .HasForeignKey<PhisicPersonClient>(p => p.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MoralPersonClient>()
                .HasKey(b => b.ClientId);

            modelBuilder.Entity<MoralPersonClient>()
                .HasOne(m => m.Client)
                .WithOne(c => c.MoralPerson)
                .HasForeignKey<MoralPersonClient>(m => m.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BankContract>()
                .HasKey(b => b.ContractId);

            modelBuilder.Entity<BankContract>()
                .HasOne(b => b.ProductsContract)
                .WithOne(c => c.BankContract)
                .HasForeignKey<BankContract>(b => b.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InsuranceContract>()
                .HasKey(i => i.ContractId);

            modelBuilder.Entity<InsuranceContract>()
                .HasOne(i => i.ProductsContract)
                .WithOne(p => p.InsuranceContract)
                .HasForeignKey<InsuranceContract>(i => i.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FinanceProduct>()
                .Property(p => p.TasaInteresOPrimaBase)
                .HasPrecision(5, 2);

            modelBuilder.Entity<BankContract>()
                .Property(b => b.BalanceActual)
                .HasPrecision(15, 2);

            modelBuilder.Entity<BankContract>()
                .Property(b => b.LoanAmountGranted)
                .HasPrecision(15, 2);

            // La columna real es decimal(5,2). Estaba declarada aqui como (15,2), lo que
            // contradecia al [Column(TypeName = "decimal(5,2)")] de la entidad: el fluent API
            // gana, asi que EF aceptaba tasas de mas de 999.99 y el INSERT reventaba.
            modelBuilder.Entity<BankContract>()
                .Property(b => b.AgreeInterestRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<InsuranceContract>()
                .Property(i => i.InsuranceSumeTotal)
                .HasPrecision(15, 2);

            modelBuilder.Entity<InsuranceContract>()
                .Property(i => i.TotalAnnualPremiu)
                .HasPrecision(15, 2);

            modelBuilder.Entity<InsuranceContract>()
                .Property(i => i.PorcentDeductible)
                .HasPrecision(4, 2);

            modelBuilder.Entity<ComercialOportunity>()
                .Property(c => c.EstimatedMont)
                .HasPrecision(15, 2);

            modelBuilder.Entity<BankTransaction>()
                .Property(t => t.Amount)
                .HasPrecision(15, 2);

            modelBuilder.Entity<InsuranceClaim>()
                .Property(c => c.AmountClaimed)
                .HasPrecision(15, 2);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.Area)
                .WithMany(a => a.Role)
                .HasForeignKey(r => r.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.TypePerson)
                .WithMany(t => t.Clients)
                .HasForeignKey(c => c.TypePersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductsContract>()
                .HasOne(p => p.Client)
                .WithMany(c => c.ProductsContracts)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductsContract>()
                .HasOne(p => p.FinanceProduct)
                .WithMany(f => f.ProductsContracts)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductsContract>()
                .HasOne(p => p.User)
                .WithMany(u => u.ProductsContracts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BankTransaction>()
                .HasOne(t => t.BankContract)
                .WithMany(b => b.BankTransactions)
                .HasForeignKey(t => t.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InsuranceClaim>()
                .HasOne(c => c.InsuranceContract)
                .WithMany(i => i.InsuranceClaims)
                .HasForeignKey(t => t.ContractId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
