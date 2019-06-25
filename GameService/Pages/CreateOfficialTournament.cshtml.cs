using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameService.ServiceCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameService.Pages
{
    public class CreateOfficialTournamentModel : PageModel
    {
        public async Task<IActionResult> OnGet()
        {
            Tournament = await TournamentMaster.Get().Create(true, 0, 0, "", "WebHost", null);
            if(Tournament != null)
            {
                return new RedirectResult($"/");
                //return new RedirectResult($"ViewTournament/?id={Tournament.GetId()}");
            }
            return null;
        }
        public ServiceTournament Tournament = null;
    }
}