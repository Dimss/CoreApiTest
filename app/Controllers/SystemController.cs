using app.Dal;
using app.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Collections.Generic;

namespace app.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {

      public SystemController(IOptions<ControllerSettings> settings)
      {

          Console.WriteLine(settings.Value.DbConfig.DbConnectionString);
          Console.WriteLine(settings.Value.DbConfig.DbName);

      }


        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            try
            {
              Dictionary<string, string> response = new Dictionary<string, string>();
                String hostName = Dns.GetHostName();
                response.Add("hostName",hostName);
                return Ok(response);

            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error - " + ex.Message);
            }
        }



    }
}
