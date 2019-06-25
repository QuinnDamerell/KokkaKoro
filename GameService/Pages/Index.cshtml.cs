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
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

            // Sort by the most recent first
            Tournaments.Sort((KokkaKoroTournament l, KokkaKoroTournament r) => { return l.CreatedAt > r.CreatedAt ? -1 : 1; });
            Games.Sort((KokkaKoroGame l, KokkaKoroGame r) => { return l.Created > r.Created ? -1 : 1; });

            // Get the most recent for the leader board.
            if(Tournaments.Count > 0)
            {
                MostRecentTour = Tournaments[0];
            }  
        }

        public List<KokkaKoroGame> Games = new List<KokkaKoroGame>();
        public List<KokkaKoroTournament> Tournaments = new List<KokkaKoroTournament>();
        public KokkaKoroTournament MostRecentTour = null;
    }
}
