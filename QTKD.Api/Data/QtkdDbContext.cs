using Microsoft.EntityFrameworkCore;
using QTKD.Api.Entities;

namespace QTKD.Api.Data;

public class QtkdDbContext : DbContext
{
    public QtkdDbContext(DbContextOptions<QtkdDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    protected override void OnModelCreating(ModelBuilder p_ModelBuilder)
    {
        base.OnModelCreating(p_ModelBuilder);

        // config table customer
        p_ModelBuilder.Entity<Customer>(entity => 
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CustomerCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.CustomerCode).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(50);
        });

        // config table contract & relationship 1-n with customer
        p_ModelBuilder.Entity<Contract>(entity => 
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ContractNo).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => c.ContractNo).IsUnique();
            entity.Property(c => c.ContractValue).HasPrecision(18, 2);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(50);

            entity.HasOne(c => c.Customer)
                .WithMany(cu => cu.Contracts)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // config table price_list
        p_ModelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
        });

        // config table price_item & relationship n-1 with price_list
        p_ModelBuilder.Entity<PriceItem>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.ServiceCode).IsRequired().HasMaxLength(50);
            entity.Property(pi => pi.ServiceName).IsRequired().HasMaxLength(200);
            entity.Property(pi => pi.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(pi => pi.PriceList)
                .WithMany(pl => pl.PriceItems)
                .HasForeignKey(pi => pi.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // seed sample data for customer
        p_ModelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = 1,
                CustomerCode = "cus-001",
                Name = "vct company",
                TaxCode = "0778042702",
                Address = "district 01, Ho Chi Minh city",
                ContactEmail = "contact@vct.vn",
                Status = "active"
            },
            new Customer
            {
                Id = 2,
                CustomerCode = "cus-002",
                Name = "logistics company",
                TaxCode = "0842702778",
                Address = "district 07, Ho Chi Minh city",
                ContactEmail = "contact@logistic.vn",
                Status = "active"
            }
        );
        
        // seed sample data for contract
        p_ModelBuilder.Entity<Contract>().HasData(
            new Contract
            {
                Id = 1,
                ContractNo = "CON-2026-018",
                CustomerId = 1,
                StartDate = new DateOnly(2026, 5, 1),
                EndDate = new DateOnly(2026, 12, 31),
                ContractValue = 150000000m,
                ServiceTerms = "dich vu boc xep va luu bai container tai cang abc",
                AttachmentUrl = null,
                Status = "draft"
            }
        );

        // seed sample data for price_list
        p_ModelBuilder.Entity<PriceList>().HasData(
            new PriceList
            {
                Id = 1,
                Name = "bang gia dich vu khai thac cang tieu chuan 2026",
                EffectiveDate = new DateOnly(2026, 1, 1),
                ExpiryDate = new DateOnly(2026, 12, 31),
                Status = "effective"
            }
        );

        // seed sample data for price_item
        p_ModelBuilder.Entity<PriceItem>().HasData(
            new PriceItem
            {
                Id = 1,
                PriceListId = 1,
                ServiceCode = "THC-20DC",
                ServiceName = "boc xep container 20 feet hang kho",
                UnitPrice = 1250000m,
                Unit = "container"
            },
            new PriceItem
            {
                Id = 2,
                PriceListId = 1,
                ServiceCode = "STORAGE-20DC",
                ServiceName = "luu kho bai container 20 feet",
                UnitPrice = 180000m,
                Unit = "day"
            }
        );
    }
}