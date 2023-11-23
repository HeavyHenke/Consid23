using System.Diagnostics;
using Considition2023_Cs;

namespace Consid23;

public class SandboxEngine
{
    private GeneralData _generalData;

    public async Task Run(string apikey)
    {
        var mapName = MapNames.GSandbox;

        HttpClient client = new();
        Api api = new(client);
        MapData mapData = await api.GetMapDataAsync(mapName, apikey);
        _generalData = await api.GetGeneralDataAsync();

        object lck = new object();
        double best = -100000;
        SubmitSolution? bestSol = null;
        MapData? bestMap = null;


        var sw = Stopwatch.StartNew();


        // var sandboxClusterHotspotsToLocations = new SandboxClusterHotspotsToLocations(_generalData);
        var sandboxClusterHotspotsToLocations = new SandboxPaintToLocations6ValueLessHotspots(_generalData);
        ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, _generalData, mapData);

        Parallel.For(1, 2, DoWorkInOneThread);

        sw.Stop();

        submitter.Dispose();
        Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

        var localScore = new Scoring(_generalData, bestMap).CalculateScore(bestSol);

        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings} {localScore.Locations.Sum(l => l.Value.SalesVolume)}");

        var dm = new DennisModel(_generalData, bestMap);
        dm.InitiateSandboxLocations(bestSol);
        Console.WriteLine($"DennisModel: {dm.CalculateScore(dm.ConvertFromSubmitSolution(bestSol))}");
        
  // var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
  // Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings} gameid: {submittedScore.Id}");

        return;


        void DoWorkInOneThread(int ix)
        {
            var localMapData = mapData.Clone();
            localMapData.RandomizeLocationOrder(ix);

            localMapData = sandboxClusterHotspotsToLocations.ClusterHotspots(localMapData);
            
            var typeToSmall = new Dictionary<string, int>
            {
                { "Convenience", 1 },
                { "Gas-station", 1 },
                { "Grocery-store", 2 },
                { "Kiosk", 0 },
                { "Grocery-store-large", 0 }
            };
            
            Console.WriteLine("Starting solver!");
            var lastSol = new SubmitSolution
            {
                Locations = new()
            };
            foreach (var loc in localMapData.locations)
            {
                lastSol.Locations.Add(loc.Key, new PlacedLocations
                {
                    Latitude = loc.Value.Latitude,
                    Longitude = loc.Value.Longitude,
                    LocationType = loc.Value.LocationType,
                    Freestyle3100Count = typeToSmall[loc.Value.LocationType],
                    Freestyle9100Count = loc.Value.LocationType == "Grocery-store-large" ? 1 : 0
                });
            }
            
            var numWithNeighbours = new DennisModel(_generalData, localMapData).Neighbours.Count(n => n.Count != 0);
            Console.WriteLine($"Num negihbours; {numWithNeighbours}");
            

            var validation = Scoring.SandboxValidation(mapName, lastSol, localMapData);
            if (validation != null) Console.WriteLine("Error: " + validation);

            var score = new Scoring(_generalData, localMapData).CalculateScore(lastSol);
            var score2 = score.GameScore!.Total;

            lock (lck)
            {
                if (score2 > best)
                {
                    best = score2;
                    bestSol = lastSol;
                    bestMap = localMapData;
                }
            }

            Console.WriteLine($"Best score found: {score2}");
        }
    }

    private void EmptyAndMoveKiosks(SubmitSolution lastSol, Border border)
    {
        var locationTypeToKey = _generalData.LocationTypes.ToDictionary(key => key.Value.Type, val => val.Key);
            
        var locationsBySalesOverhead = lastSol.Locations
            //.Where(l => l.Value.LocationType == "Kiosk" )
            .Where(l => l.Value.LocationType == "Convenience")
            .OrderByDescending(l => _generalData.LocationTypes[locationTypeToKey[l.Value.LocationType]].SalesVolume - l.Value.Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek - l.Value.Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek)
            .ToList();
        var fillLoc = 0;
// No move:
// Score local 3393,38 0,0359 2547,76 575,156 4786
// Move all to one Convenience
// Score local 3381,09 0,0347 2541,49 573,726 4769

        
        foreach (var k in lastSol.Locations.Where(l => l.Value.LocationType == "Kiosk").Take(3))
        {
            k.Value.Freestyle3100Count = 0;
            k.Value.Freestyle9100Count = 0;

            // TODO: Dennis, här kan vi flytta kiosker vilket ger olika sales!
            // k.Value.Latitude = border.LatitudeMax;
            // k.Value.Longitude = border.LongitudeMax;
            // fillLoc++;
        }

        // foreach (var k in lastSol.Locations.Where(l => l.Value.Freestyle3100Count == 0 && l.Value.Freestyle9100Count == 0))
        // {
        //     k.Value.Latitude = border.LatitudeMax;
        //     k.Value.Longitude = border.LongitudeMax;
        // }
    }
}