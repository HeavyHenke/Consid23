using System.Diagnostics;
using Consid23;
using Considition2023_Cs;

const string apikey = "347f7d9f-c846-4bdf-a0be-d82da397dbe8";

var mapName = MapNames.Goteborg;

HttpClient client = new();
Api api = new(client);
MapData mapData = await api.GetMapDataAsync(mapName, apikey);
GeneralData generalData = await api.GetGeneralDataAsync();

object lck = new object();
double best = -100000;
SubmitSolution? bestSol = null;


var sw = Stopwatch.StartNew();

/*
var clustered = new SandboxClusterHotspotsToLocations(generalData).ClusterHotspots(mapData);
ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, clustered);
*/

var clustered = mapData;
ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, clustered);

Parallel.For(1, 11, DoWorkInOneThread);

sw.Stop();

submitter.Dispose();
Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

var localScore = new Scoring(generalData, clustered).CalculateScore(bestSol);

// var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
// Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");
Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");
// api.Sumbit(mapName, bestSol, apikey);

return;


// Max Goteborg: 6166,82
// Score local 6166,82 0,0324 4644,99 1049,594

void DoWorkInOneThread(int ix)
{
    var localMapData = clustered.Clone();
    localMapData.RandomizeLocationOrder(ix);
    var model = new DennisModel(generalData, localMapData);

    // var startPoint1 = new SubmitSolution()
    // {
    //     Locations = new()
    // };
    var startPoint1 = new HenrikDennisStaticInitialStateCreator(model, generalData).CreateInitialSolution();
    // var score1 = model.CalculateScore(model.ConvertFromSubmitSolution(startPoint1));
    
    var lastSol = new HenrikDennisSolver1(model, submitter).OptimizeSolution(startPoint1);
    var score2 = model.CalculateScore(model.ConvertFromSubmitSolution(lastSol));

    // var solver = new HenrikSolver1(generalData, localMapData, submitter);
    // var solution = solver.CalcSolution();
    // var score = new Scoring(generalData, localMapData).CalculateScore(lastSol);
    // score2 = score.GameScore!.Total;

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
