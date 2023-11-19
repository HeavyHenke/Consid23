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


        var sandboxClusterHotspotsToLocations = new SandboxClusterHotspotsToLocations(_generalData);
        var clustered = sandboxClusterHotspotsToLocations.ClusterHotspots(mapData);
        ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, _generalData, clustered);

        Parallel.For(1, 2, DoWorkInOneThread);

        sw.Stop();

        submitter.Dispose();
        Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

        var localScore = new Scoring(_generalData, clustered).CalculateScore(bestSol);

        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");
 // var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
 // Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");

        return;


        void DoWorkInOneThread(int ix)
        {
            var localMapData = clustered.Clone();
            localMapData.RandomizeLocationOrder(ix);

            //var lastSol = new HenrikSolver1(_generalData, localMapData, submitter).CalcSolution();

            var initial = new HenrikDennisStaticInitialStateCreator(null, null).CreateInitialSolution();
            var lastSol = new HenrikDennisOptimizer2Gradient(null, null).OptimizeSolution(initial);
            EmptyAndMoveKiosks(lastSol);
            
            var validation = Scoring.SandboxValidation(mapName, lastSol, localMapData);
            if (validation != null)
            {
                Console.WriteLine("Error: " + validation);
            }
            
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

    private void EmptyAndMoveKiosks(SubmitSolution lastSol)
    {
        var locationTypeToKey = _generalData.LocationTypes.ToDictionary(key => key.Value.Type, val => val.Key);
            
        var locationsBySalesOverhead = lastSol.Locations
            .Where(l => l.Value.LocationType == "Kiosk")
            .OrderByDescending(l => _generalData.LocationTypes[locationTypeToKey[l.Value.LocationType]].SalesVolume - l.Value.Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek - l.Value.Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek)
            .ToList();
        var fillLoc = 0;
            
        foreach (var k in lastSol.Locations.Where(l => l.Value.LocationType == "Kiosk"))
        {
            k.Value.Freestyle3100Count = 0;
            k.Value.Freestyle9100Count = 0;

            k.Value.Latitude = locationsBySalesOverhead[fillLoc].Value.Latitude;
            k.Value.Longitude = locationsBySalesOverhead[fillLoc].Value.Longitude;
            fillLoc++;
        }
    }
}