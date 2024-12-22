using BCrypt.Net;
using LLamaWebAPI.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LLamaWebAPI.Data
{
    public class AuthRepository : IAuthRepository
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger _logger;

        public AuthRepository(ApplicationDbContext dbContext, ILoggerFactory loggerFactory)
        {
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger("Security");
        }

        //USERS
        public async Task <User?> GetUserById(int id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _dbContext.Users.Where(u => u.Username.ToUpper() == username.ToUpper()).FirstOrDefaultAsync(); 
        }

        public async Task<User> CreateOrUpdateUser(User user)
        {
            _logger.LogInformation("Try to create or update user with id {id}", user.Id);

            using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var existingUser = await GetUserById(user.Id);

                if (existingUser != null)
                {
                    existingUser.Username = user.Username;
                    existingUser.Email = user.Email;
                    existingUser.PasswordHash = user.PasswordHash;
                }
                else
                {
                    await _dbContext.Users.AddAsync(user);
                }
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (existingUser != null)
                {
                    _logger.LogInformation("User with id {id} is updatetd.", existingUser.Id);
                    return existingUser;
                }
                else
                {
                    _logger.LogInformation("New User with id {id} is created.", user.Id);
                    return user;
                }
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rollback: new or updated user with id {id} was not persisted. Exception: {ex}.", user.Id, ex);
                throw;
            }
        }

        //TOKENS
        public async Task<Token?> GetTokenByUserId(int userId)
        {
            return await _dbContext.Tokens.FirstOrDefaultAsync(t => t.UserId == userId);
        }

        public async Task<Token> CreateOrUpdateToken(Token refreshToken)
        {
            _logger.LogInformation("Try to create or update Token for user with id {id}.", refreshToken.UserId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var existingToken = await GetTokenByUserId(refreshToken.UserId);

                if (existingToken != null)
                {
                    existingToken.HashedRefreshToken = refreshToken.HashedRefreshToken;
                    existingToken.TokenExpiration = refreshToken.TokenExpiration;
                    existingToken.IsRevoked = refreshToken.IsRevoked;
                }
                else
                {
                    await _dbContext.Tokens.AddAsync(refreshToken);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (existingToken != null)
                {
                    _logger.LogInformation("Token for user with id {id} is updatetd.", refreshToken.UserId);
                    return existingToken;
                }
                else
                {
                    _logger.LogInformation("New Token for user with id {id} is created.", refreshToken.UserId);
                    return refreshToken;
                }
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Transaction rollback: new or updated toke for user with id {id} was not persisted. Exception: {ex}.", refreshToken.UserId, ex);
                throw;
            }
            
        }
    }
}
