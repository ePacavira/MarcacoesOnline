using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcacoesOnline.Model
{
    public enum Perfil
    {
        Anonimo,
        Registado,
        Administrativo,
        Administrador
    }

    public class User
    {
        public int Id { get; set; }
        public string NumeroUtente { get; set; } // Opcional para alguns perfis
        public string NomeCompleto { get; set; }
        public DateTime DataNascimento { get; set; }
        public string Genero { get; set; }
        public string Telemovel { get; set; }
        public string Email { get; set; }
        public string Morada { get; set; }
        public string PasswordHash { get; set; }
        public string? FotoPath { get; set; }
        public Perfil Perfil { get; set; }
        public ICollection<PedidoMarcacao> Pedidos { get; set; }
    }

}
