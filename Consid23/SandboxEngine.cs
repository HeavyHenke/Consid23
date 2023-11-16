using System.Diagnostics;
using Considition2023_Cs;

namespace Consid23;

public class SandboxEngine
{
    public async Task Run(string apikey)
    {
        var mapName = MapNames.GSandbox;

        HttpClient client = new();
        Api api = new(client);
        MapData mapData = await api.GetMapDataAsync(mapName, apikey);
        GeneralData generalData = await api.GetGeneralDataAsync();

        object lck = new object();
        double best = -100000;
        SubmitSolution? bestSol = null;


        var sw = Stopwatch.StartNew();


        var sandboxClusterHotspotsToLocations = new SandboxClusterHotspotsToLocations(generalData);
        var clustered = sandboxClusterHotspotsToLocations.ClusterHotspots(mapData);
        ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, clustered);

        Parallel.For(1, 11, DoWorkInOneThread);

        sw.Stop();

        submitter.Dispose();
        Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

        var localScore = new ScoringHenrik(generalData, clustered).CalculateScore(bestSol);

        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");
 // var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
 // Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");

        return;


        void DoWorkInOneThread(int ix)
        {
            var localMapData = clustered.Clone();
            localMapData.RandomizeLocationOrder(ix);

            var lastSol = new HenrikSolver1(generalData, localMapData, submitter).CalcSolution();

            // sandboxClusterHotspotsToLocations.OptimizeByMovingALittle(lastSol, localMapData);

            var score = new Scoring(generalData, localMapData).CalculateScore(lastSol);
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
}