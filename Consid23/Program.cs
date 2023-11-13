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

var mapName = MapNames.Goteborg;

HttpClient client = new();
Api api = new(client);
MapData mapData = await api.GetMapDataAsync(mapName, apikey);
GeneralData generalData = await api.GetGeneralDataAsync();
ISolutionSubmitter submitter = new ConsoleOnlySubmitter(api, apikey, generalData, mapData);

var sw = Stopwatch.StartNew();

Parallel.For(1, 2, DoWorkInOneThread);

sw.Stop();

submitter.Dispose();
Console.WriteLine($"Done, it took {sw.Elapsed}");
return;


void DoWorkInOneThread(int ix)
{
    var localMapData = mapData.Clone();
    localMapData.RandomizeLocationOrder(ix);
    var model = new DennisModel(generalData, localMapData);
    
    var startPoint1 = new HenrikDennisStaticInitialStateCreator(model, generalData).CreateInitialSolution();
    // var score1 = model.CalculateScore(model.ConvertFromSubmitSolution(startPoint1));
    
    var lastSol = new HenrikDennisOptimizer2Gradient(model, submitter).OptimizeSolution(startPoint1);
    var score2 = model.CalculateScore(model.ConvertFromSubmitSolution(lastSol));
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
