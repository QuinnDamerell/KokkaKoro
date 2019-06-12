using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameService.ServiceCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Responses;

namespace GameService.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            KokkaKoroResponse<object> response = GameMaster.Get().ListGames();
            if (response.Data is ListGamesResponse listResponse)
            {
                Games = listResponse.Games;
            }
        }

        public List<KokkaKoroGame> Games = new List<KokkaKoroGame>();
    }
}
