using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("CorsPolicy")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository repo;

        public UserController(IUserRepository repo)
        {
            this.repo = repo;
        }

        //POST: localhost:7229/VAT
        [HttpPost("Save")]
        public async Task<ActionResult<OpResponse<SaveNumber>>> SaveNumber([FromBody] SaveNumber number)
        {
            try
            {
                var result = await repo.SaveNumber(number);

                if (!result.isSuccess)
                {
                    throw new Exception(result.Message);
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return StatusCode(500, e.Message);
            }
        }
    }

    //all needed for singular api call 
    public class SaveNumber
    {
        [Required]
        public int Number { get; set; }
    }

    public class BaseRepositoryFunctions
    {
        public DbResponse ErrorResponse() => new DbResponse { IsSuccess = false, IsError = true, Message = "An error occured trying to connect to the database." };
        public DbResponse NoResponse() => new DbResponse { IsSuccess = false, IsError = false, Message = "Did not get a response from the database (no expected value was returned)." };
        public DbResponse SuccessResponse() => new DbResponse { IsSuccess = true, IsError = false, Message = "Response from the database is successful." };
    }

    public class DbResponse
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsError { get; set; }
    }

    public class OpResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
        public bool isSuccess { get; set; }
        public bool isError { get; set; }
    }

    public interface IUserRepository
    {
        Task<OpResponse<SaveNumber>> SaveNumber(SaveNumber number);
    }

    internal class UserRepository : BaseRepositoryFunctions, IUserRepository
    {
        #region Initialisation
        private readonly IConfiguration config;

        public UserRepository(IConfiguration config)
        {
            this.config = config;
        }
        #endregion

        #region Save number
        public async Task<OpResponse<SaveNumber>> SaveNumber(SaveNumber number)
        {
            var opresponse = new OpResponse<SaveNumber>
            {
                Data = number,
            };

            var sql = "[dbo].[SaveNumber]";
            var param = new SaveNumber
            {
                Number = number.Number
            };

            using (var conn = new SqlConnection(config.GetConnectionString("PhishingDb")))
            {
                var result = await conn.QueryAsync<OpResponse<SaveNumber>>(sql, param, commandType: System.Data.CommandType.StoredProcedure);

                if (result.Count() == 0)
                {
                    opresponse.isSuccess = true;
                    opresponse.Message = "Succesfully completed operation";
                    opresponse.Data = null;
                    return opresponse;
                }
                else
                {
                    opresponse.isSuccess = false;
                    opresponse.Message = "Did not save user number";
                    opresponse.Data = null;
                    return opresponse;
                }
            }
        }
        #endregion
    }
}
