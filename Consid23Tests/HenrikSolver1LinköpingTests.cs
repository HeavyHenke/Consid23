using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class HenrikSolver1LinköpingTests
{
    [TestMethod]
    public void SolveSmallWorldGivesGoodSolution()
    {
        var mapDataJson = File.ReadAllText("linkoping.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solver = new HenrikSolver1(generalData, mapData);
        var solution = solver.CalcSolution();

        var scorer = new Scoring();
        var score = scorer.CalculateScore(mapData.MapName, solution, mapData, generalData);
        
        Assert.IsTrue(score.GameScore.Total >= 340_565);
    }
}