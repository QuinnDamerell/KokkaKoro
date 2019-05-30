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
        public static string CookieUserId = "UserName";
        public static string GetUserName(HttpRequest req, HttpResponse response)
        {
            // Get the user's id
            string userId = null;
            if (req.Cookies.ContainsKey(CookieUserId))
            {
                userId = req.Cookies[CookieUserId];
            }
            else
            {
                // Check if it was passed in the get
                if (req.Query.ContainsKey(CookieUserId))
                {
                    userId = req.Query[CookieUserId];
                }
                else
                {
                    userId = Guid.NewGuid().ToString();
                }
            }

            // Always set the user id.
            response.Cookies.Append(CookieUserId, userId, new CookieOptions { Expires = DateTime.Now.AddDays(365), IsEssential = true});
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string command)
        {
            return await Handle(command);
        }

        [HttpPost]
        public async Task<object> Post()
        {
            Stream req = Request.Body;
            string command = new StreamReader(req).ReadToEnd();
            return await Handle(command);
        }

        private async Task<IActionResult> Handle(string command)
        {
            string userName = GetUserName(Request, Response);

            // Handle the response
            KokkaKoroResponse<object> result = await GameMaster.Get().HandleCommand(userName, command);
            if (String.IsNullOrWhiteSpace(result.Error))
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
