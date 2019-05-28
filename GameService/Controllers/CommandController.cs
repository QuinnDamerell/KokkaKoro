using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameService.ServiceCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceProtocol;

namespace GameService
{
    [Route("api/v1/[controller]")]
    public class CommandController : Controller
    {
        public static string CookieUserId = "userId";
        public static string GetUserId(HttpRequest req, HttpResponse response)
        {
            // Get the user's id
            string userId = null;
            if (req.Cookies.ContainsKey(CookieUserId))
            {
                userId = req.Cookies["userId"];
            }
            else
            {
                userId = Guid.NewGuid().ToString();
            }

            // Always set the user id.
            response.Cookies.Append("userId", userId, new CookieOptions { Expires = DateTime.Now.AddDays(365), IsEssential = true});
            return userId;
        }

        [HttpGet]
        public IActionResult Get(string command)
        {
            return Handle(command);
        }

        [HttpPost]
        public object Post()
        {
            Stream req = Request.Body;
            string command = new StreamReader(req).ReadToEnd();
            return Handle(command);
        }

        private IActionResult Handle(string command)
        {
            string userId = GetUserId(Request, Response);

            // Handle the response
            KokkaKoroResponse<object> result = GameMaster.Get().HandleCommand(userId, command);
            if (String.IsNullOrWhiteSpace(result.Error))
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
