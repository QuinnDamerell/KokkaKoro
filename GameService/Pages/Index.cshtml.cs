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
            response = TournamentMaster.Get().List(null, null);
            if (response.Data is ListTournamentResponse tResponse)
            {
                Tournaments = tResponse.Tournaments;
            }
            foreach(KokkaKoroTournament t in Tournaments)
            {
                if(MostRecentTour == null || t.CreatedAt > MostRecentTour.CreatedAt)
                {
                    MostRecentTour = t;
                }
            }
        }

        public List<KokkaKoroGame> Games = new List<KokkaKoroGame>();
        public List<KokkaKoroTournament> Tournaments = new List<KokkaKoroTournament>();
        public KokkaKoroTournament MostRecentTour = null;
    }
}
