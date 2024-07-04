using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using Web.Model;

namespace Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValueController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ValueController> _logger;

        public ValueController(IConfiguration configuration, ILogger<ValueController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Getting all log entries");

            string query = @"
                    SELECT ID, MachineName, Logged, Level, Message, Logger, Properties, Callsite, Exception
                    FROM dbo.NLog
                    ";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("NLog");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        using (SqlDataReader myReader = myCommand.ExecuteReader())
                        {
                            table.Load(myReader);
                        }
                    }
                }

                _logger.LogInformation("Successfully retrieved all log entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting all log entries");
                return StatusCode(500, "Internal server error");
            }

            // Преобразуем DataTable в List<LogEntry> для сериализации в JSON
            var logEntries = new List<LogEntry>();
            foreach (DataRow row in table.Rows)
            {
                logEntries.Add(new LogEntry
                {
                    Id = Convert.ToInt32(row["ID"]),
                    MachineName = row["MachineName"].ToString(),
                    Logged = Convert.ToDateTime(row["Logged"]),
                    Level = row["Level"].ToString(),
                    Message = row["Message"].ToString(),
                    Logger = row["Logger"].ToString(),
                    Properties = row["Properties"].ToString(),
                    Callsite = row["Callsite"].ToString(),
                    Exception = row["Exception"].ToString()
                });
            }

            return Ok(logEntries); // Используем Ok() для возврата данных в формате JSON
        }


        [HttpPost]
        public IActionResult Post([FromBody] LogEntry logEntry)
        {
            _logger.LogInformation("Adding new log entry");

            string query = @"
                           INSERT INTO dbo.NLog (MachineName, Logged, Level, Message, Logger, Properties, Callsite, Exception)
                           VALUES (@MachineName, @Logged, @Level, @Message, @Logger, @Properties, @Callsite, @Exception)
                            ";

            string sqlDataSource = _configuration.GetConnectionString("NLog");

            try
            {
                using (SqlConnection myCon = new SqlConnection(sqlDataSource))
                {
                    myCon.Open();
                    using (SqlCommand myCommand = new SqlCommand(query, myCon))
                    {
                        myCommand.Parameters.AddWithValue("@MachineName", logEntry.MachineName);
                        myCommand.Parameters.AddWithValue("@Logged", logEntry.Logged);
                        myCommand.Parameters.AddWithValue("@Level", logEntry.Level);
                        myCommand.Parameters.AddWithValue("@Message", logEntry.Message);
                        myCommand.Parameters.AddWithValue("@Logger", logEntry.Logger);
                        myCommand.Parameters.AddWithValue("@Properties", logEntry.Properties);
                        myCommand.Parameters.AddWithValue("@Callsite", logEntry.Callsite);
                        myCommand.Parameters.AddWithValue("@Exception", logEntry.Exception);

                        myCommand.ExecuteNonQuery();
                    }
                }

                _logger.LogInformation("Successfully added new log entry");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding new log entry");
                return StatusCode(500, "Internal server error");
            }

            return Ok("Added Successfully");
        }
    }
}
