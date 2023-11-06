using Considition2023_Cs;

namespace Consid23;

public class HenrikSolver1
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public HenrikSolver1(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public SubmitSolution CalcSolution()
    {
        var scorer = new Scoring();
        
        SubmitSolution sol = new SubmitSolution
        {
            Locations = new()
        };

        foreach (var loc in _mapData.locations)
        {
            while (true)
            {
                AddOneAt(sol, loc.Key);

                var score = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

                if (score.Locations[loc.Key].SalesCapacity > score.Locations[loc.Key].SalesVolume)
                    break;
            }
        }
        
        // Try -1 and +1 on all (one at a time)
        while (true)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            RemoveOneFromAll(scorer, sol);
            AddOneForAll(scorer, sol);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (postScore.GameScore.Total <= preScore.GameScore.Total)
                break;
        }
        
        return sol;
    }

    private void RemoveOneFromAll(Scoring scorer, SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            RemoveOne(sol, loc);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (postScore.GameScore.Total < preScore.GameScore.Total)
                AddOneAt(sol, loc);
        }
    }

    private void AddOneForAll(Scoring scorer, SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            AddOneAt(sol, loc);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (postScore.GameScore.Total < preScore.GameScore.Total)
                RemoveOne(sol, loc);
        }
    }


    private static void AddOneAt(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            sol.Locations.Add(location, new PlacedLocations
            {
                Freestyle3100Count = 1,
                Freestyle9100Count = 0
            });
        }
        else if (loc.Freestyle3100Count < 1)
        {
            loc.Freestyle3100Count++;
        }
        else
        {
            loc.Freestyle3100Count--;
            loc.Freestyle9100Count++;
        }
    }

    private static void RemoveOne(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            return;
        }
        
        if (loc.Freestyle3100Count > 0)
        {
            loc.Freestyle3100Count--;
        }
        else if(loc.Freestyle9100Count > 0)
        {
            loc.Freestyle9100Count--;
            loc.Freestyle3100Count++;
        }

        if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
            sol.Locations.Remove(location);
    }
}