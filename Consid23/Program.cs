using System.Diagnostics;
using Consid23;
using Considition2023_Cs;

const string apikey = "347f7d9f-c846-4bdf-a0be-d82da397dbe8";

// Console.WriteLine($"1: {MapNames.Stockholm}");
// Console.WriteLine($"2: {MapNames.Goteborg}");
// Console.WriteLine($"3: {MapNames.Malmo}");
// Console.WriteLine($"4: {MapNames.Uppsala}");
// Console.WriteLine($"5: {MapNames.Vasteras}");
// Console.WriteLine($"6: {MapNames.Orebro}");
// Console.WriteLine($"7: {MapNames.London}");
// Console.WriteLine($"8: {MapNames.Linkoping}");
// Console.WriteLine($"9: {MapNames.Berlin}");
//
// Console.Write("Select the map you wish to play: ");
// string option = Console.ReadLine();
//
// var mapName = option switch
// {
//     "1" => MapNames.Stockholm,
//     "2" => MapNames.Goteborg,
//     "3" => MapNames.Malmo,
//     "4" => MapNames.Uppsala,
//     "5" => MapNames.Vasteras,
//     "6" => MapNames.Orebro,
//     "7" => MapNames.London,
//     "8" => MapNames.Linkoping,
//     "9" => MapNames.Berlin,
//     _ => null
// };
//
// if (mapName is null)
// {
//     Console.WriteLine("Invalid map selected");
//     return;
// }

var mapName = MapNames.Uppsala;

HttpClient client = new();
Api api = new(client);
MapData mapData = await api.GetMapDataAsync(mapName, apikey);
GeneralData generalData = await api.GetGeneralDataAsync();
ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, mapData);

var sw = Stopwatch.StartNew();
object lck = new object();
double best = 0;
SubmitSolution? bestSol = null;

Parallel.For(1, 10, DoWorkInOneThread);

sw.Stop();

submitter.Dispose();
Console.WriteLine($"Done, it took {sw.Elapsed}, best found was {best}");

// var submittedScore = api.Sumbit(mapData.MapName, bestSol!, apikey);
// Console.WriteLine($"Score from server {submittedScore.GameScore.Total} {submittedScore.GameScore.TotalFootfall} {submittedScore.GameScore.KgCo2Savings} {submittedScore.GameScore.Earnings}");
// var localScore = new Scoring(generalData, mapData).CalculateScore(bestSol!);
// Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");


return;


void DoWorkInOneThread(int ix)
{
    var localMapData = mapData.Clone();
    localMapData.RandomizeLocationOrder(ix);
    //var model = new DennisModel(generalData, localMapData);
    
    //var startPoint1 = new HenrikDennisStaticInitialStateCreator(model, generalData).CreateInitialSolution();
    // var score1 = model.CalculateScore(model.ConvertFromSubmitSolution(startPoint1));
    
    //var lastSol = new HenrikDennisOptimizer2Gradient(model, submitter).OptimizeSolution(startPoint1);
    //var score2 = model.CalculateScore(model.ConvertFromSubmitSolution(lastSol));

    var solver = new HenrikSolver1(generalData, localMapData, submitter);
    var solution = solver.CalcSolution();
    var score = new ScoringHenrik(generalData, mapData).CalculateScore(solution);
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

// GameData score = new Scoring(generalData, mapData).CalculateScore(solution);
// Console.WriteLine($"GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");

// Console.WriteLine("Press S to submit");
//
// var inp = Console.ReadKey();
// if (inp.Key == ConsoleKey.S)
// {
//     GameData prodScore = await api.SumbitAsync(mapName, solution, apikey);
//     Console.WriteLine($"GameId: {prodScore.Id}");
//     Console.WriteLine($"Server score: {prodScore.GameScore.Total}");
//     Console.ReadLine();
// }
