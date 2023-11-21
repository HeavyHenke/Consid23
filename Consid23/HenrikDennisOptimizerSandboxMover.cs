using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisOptimizerSandboxMover
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public HenrikDennisOptimizerSandboxMover(GeneralData generalData, MapData mapData)
    {
        _generalData = generalData;
        _mapData = mapData;
    }

    public SubmitSolution OptimizeByMoving(SubmitSolution sol, ISolutionSubmitter submitter)
    {
        // Determine order to remove
        var localMap = _mapData.Clone();
        var scoring = new Scoring(_generalData, localMap);
        var score = scoring.CalculateScore(sol);
        
        var removeOrder = score
            .Locations!.Values
            .OrderBy(l => (l.GramCo2Savings * +l.Earnings) * (1 + l.Footfall))
            .Select(l => l.LocationName)
            .ToList();
        
        var locationTypeToKey = _generalData.LocationTypes.ToDictionary(key => key.Value.Type, val => val.Key);
        var locationsBySalesOverhead = sol.Locations
            .Where(l => l.Value.LocationType == "Kiosk")
            .OrderByDescending(l => _generalData.LocationTypes[locationTypeToKey[l.Value.LocationType]].SalesVolume - l.Value.Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek - l.Value.Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek)
            .ToList();

        var bestModel = sol.Clone();
        var bestScore = score.GameScore.Total; 
        
        for (int i = 0; i < locationsBySalesOverhead.Count; i++)
        {
            var workingCopy = bestModel.Clone();

            var toRemove = removeOrder[0];
            removeOrder.RemoveAt(0);

            var dest = locationsBySalesOverhead[i];

            workingCopy.Locations[toRemove].Longitude = dest.Value.Longitude;
            workingCopy.Locations[toRemove].Latitude = dest.Value.Latitude;
            workingCopy.Locations[toRemove].Freestyle3100Count = 0;
            workingCopy.Locations[toRemove].Freestyle9100Count = 0;

            localMap.locations[toRemove].Longitude = dest.Value.Longitude;
            localMap.locations[toRemove].Latitude = dest.Value.Latitude;

            // var model = new DennisModel(_generalData, localMap);
            // workingCopy = new HenrikDennisSolver1(model, submitter).OptimizeSolution(workingCopy);

            var score2 = scoring.CalculateScore(workingCopy);
            if (score2.GameScore.Total < bestScore)
                break;
            bestScore = score2.GameScore.Total;
            bestModel = workingCopy;
        }

        return bestModel;
    }
    
    private static bool AddOneAt(PlacedLocations loc)
    {
        if (loc.Freestyle3100Count < 2)
        {
            loc.Freestyle3100Count++;
            return true;
        }
        if (loc.Freestyle9100Count < 2)
        {
            loc.Freestyle3100Count = 0;
            loc.Freestyle9100Count++;
            return true;
        }
        return false;
    }
    
    private static bool RemoveOne(PlacedLocations loc)
    {
        if (loc.Freestyle3100Count > 0)
        {
            loc.Freestyle3100Count--;
            return true;
        }

        if (loc.Freestyle9100Count > 0)
        {
            loc.Freestyle9100Count--;
            loc.Freestyle3100Count += 2;
            return true;
        }

        return false;
    }


}