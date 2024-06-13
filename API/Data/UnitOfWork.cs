using System.Threading.Tasks;
using API.Interfaces;
using AutoMapper;
using EntityFrameworkCore.UnitOfWork.Interfaces;


namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        public UnitOfWork(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public IUserRepository UserRepository => new UserRepository(_context, _mapper);
    }
}