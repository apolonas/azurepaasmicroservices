using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace OnlineShop.Api.OrderApi.Controllers
{
    [Produces("application/json")]
    [Route("api/Start")]
    public class StartController : Controller
    {
        private readonly AppSettings _appSettings;

        public StartController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        // GET: api/Start
        [HttpGet]
        public IActionResult Get()
        {
            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = StatusCodes.Status200OK,
                Content = "<html><body><h2>Order Processing Service Running...</h2></body></html>"
            };
        }
    }
}