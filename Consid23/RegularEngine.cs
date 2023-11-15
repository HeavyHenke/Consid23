﻿using System.Diagnostics;
using Considition2023_Cs;

namespace Consid23;

public class RegularEngine
{
    public async Task Run(string apikey)
    {
        var mapName = MapNames.Goteborg;

        HttpClient client = new();
        Api api = new(client);
        MapData mapData = await api.GetMapDataAsync(mapName, apikey);
        GeneralData generalData = await api.GetGeneralDataAsync();

        object lck = new object();
        double best = -100000;
        SubmitSolution? bestSol = null;


        var sw = Stopwatch.StartNew();

        ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, mapData);

        Parallel.For(1, 11, DoWorkInOneThread);

        sw.Stop();

        submitter.Dispose();
        Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

        var localScore = new ScoringHenrik(generalData, mapData).CalculateScore(bestSol);

 // var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
 // Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");
        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");

        return;


// Max Goteborg: 6166,82
// Score local 6166,82 0,0324 4644,99 1049,594

        void DoWorkInOneThread(int ix)
        {
            var localMapData = mapData.Clone();
            localMapData.RandomizeLocationOrder(ix);
            var model = new DennisModel(generalData, localMapData);

            var startPoint1 = new HenrikDennisStaticInitialStateCreator(model, generalData).CreateInitialSolution();

            var solver = new HenrikDennisSolver1(generalData, localMapData, submitter);
            // var solver = new HenrikDennisOptimizer2Gradient(model, submitter);
            var solution = solver.OptimizeSolution(startPoint1);

            var score = new Scoring(generalData, localMapData).CalculateScore(solution);
            var score2 = score.GameScore!.Total;

            lock (lck)
            {
                if (score2 > best)
                {
                    best = score2;
                    bestSol = solution;
                }
            }

            Console.WriteLine($"Best score found: {score2}");
        }
    }

}