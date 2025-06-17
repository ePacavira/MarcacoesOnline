using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarcacoesOnline.Model;

namespace MarcacoesOnline.Interfaces
{
    public interface IActoClinicoRepository
    {
        Task<ActoClinico?> GetByIdAsync(int id);
        Task<IEnumerable<ActoClinico>> GetAllAsync();
        Task AddAsync(ActoClinico acto);
        void Delete(ActoClinico acto);
        Task SaveChangesAsync();
    }

}
