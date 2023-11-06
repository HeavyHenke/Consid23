using Considition2023_Cs;

namespace Consid23;

public class ConsidDefaultSolver
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public ConsidDefaultSolver(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public SubmitSolution CalcSolution()
    {
        SubmitSolution solution = new() 
        {
            Locations = new()
        };
        foreach (KeyValuePair<string, StoreLocation> locationKeyPair in _mapData.locations)
        {
            StoreLocation location = locationKeyPair.Value;
            //string name = locationKeyPair.Key;
            var salesVolume = location.SalesVolume;
            if (salesVolume > 100)
            {
                solution.Locations[location.LocationName] = new PlacedLocations() 
                { 
                    Freestyle3100Count = 0, 
                    Freestyle9100Count = 1
                };
            }
        }

        return solution;
    }
}