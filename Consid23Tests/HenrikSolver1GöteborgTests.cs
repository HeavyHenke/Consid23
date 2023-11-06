using System.Diagnostics;
using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class HenrikSolver1GöteborgTests
{
    [TestMethod]
    public void SolveGöteborgGivesGoodSolution()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solver = new HenrikSolver1(generalData, mapData);
        var solution = solver.CalcSolution();

        var scorer = new Scoring();
        var score = scorer.CalculateScore(mapData.MapName, solution, mapData, generalData);
        
        Debug.WriteLine($"GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        
        Assert.IsTrue(score.GameScore!.Total >= 56_826_754);
    }
}