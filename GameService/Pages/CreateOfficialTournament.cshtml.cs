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
        public IActionResult OnGet()
        {
            TournamentMaster.Get().Create(true, 0, 0, "", "WebHost", null);
            return new RedirectResult("/");
        }
    }
}