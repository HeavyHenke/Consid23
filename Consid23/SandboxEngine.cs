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


        var sw = Stopwatch.StartNew();


        // var sandboxClusterHotspotsToLocations = new SandboxClusterHotspotsToLocations(_generalData);
        var sandboxClusterHotspotsToLocations = new SandboxPaintToLocations2(_generalData);
        var clustered = sandboxClusterHotspotsToLocations.ClusterHotspots(mapData);
        ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, _generalData, clustered);

        Parallel.For(1, 2, DoWorkInOneThread);

        sw.Stop();

        submitter.Dispose();
        Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

        
        foreach (var loc in clustered.locations)
        {
            clustered.locations[loc.Key].Latitude = loc.Value.Latitude;
            clustered.locations[loc.Key].Longitude = loc.Value.Longitude;
        }
        var localScore = new Scoring(_generalData, clustered).CalculateScore(bestSol);

        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings} {localScore.Locations.Sum(l => l.Value.SalesVolume)}");

        var dm = new DennisModel(_generalData, mapData);
        dm.InitiateSandboxLocations(bestSol);
        Console.WriteLine($"DennisModel: {dm.CalculateScore(dm.ConvertFromSubmitSolution(bestSol))}");
        
 // var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
 // Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");

        return;


        void DoWorkInOneThread(int ix)
        {
            var localMapData = clustered.Clone();
            localMapData.RandomizeLocationOrder(ix);

            var model = new DennisModel(_generalData, localMapData);
            var initial = new HenrikDennisStaticInitialStateCreator(model, _generalData).CreateInitialSolution();
            model.InitiateSandboxLocations(initial);
            var lastSol = new HenrikDennisSolver1(model, submitter) .OptimizeSolution(initial);

            EmptyAndMoveKiosks(lastSol, localMapData.Border);
            foreach (var loc in lastSol.Locations)
            {
                localMapData.locations[loc.Key].Latitude = loc.Value.Latitude;
                localMapData.locations[loc.Key].Longitude = loc.Value.Longitude;
            }

            // lastSol = new HenrikSolver1(_generalData, localMapData, submitter).CalcSolution(lastSol);

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