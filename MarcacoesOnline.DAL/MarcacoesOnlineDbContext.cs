using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.DAL
{
    public class MarcacoesOnlineDbContext : DbContext
    {
        public MarcacoesOnlineDbContext(DbContextOptions<MarcacoesOnlineDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PedidoMarcacao> PedidosMarcacao { get; set; }
        public DbSet<ActoClinico> ActosClinicos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Pedidos)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<PedidoMarcacao>()
                .HasMany(p => p.ActosClinicos)
                .WithOne(a => a.PedidoMarcacao)
                .HasForeignKey(a => a.PedidoMarcacaoId);
        }
    }

}
