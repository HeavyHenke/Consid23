using System.Diagnostics;
using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class DennisModelTests
{
    [TestMethod]
    public void SolveGöteborgGivesGoodSolution()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solver = new HenrikSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.CalcSolution();

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);

        for (int i = 0; i < 1000; i++)
            score = scorer.CalculateScore(solution);

//        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");

        Assert.IsTrue(score.GameScore!.Total >= 56_826_754);
    }

    [TestMethod]
    public void SolveGöteborgGivesGoodSolutionNew()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solver = new HenrikSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.CalcSolution();

        var scorer1 = new Scoring(generalData, mapData);
        var score1= scorer1.CalculateScore(solution);
        
        
        
        var scorer = new DennisModel(generalData, mapData);
        var solutionLocations = scorer.ConvertFromSubmitSolution(solution);
        double totalScore = 0;

        for (int i = 0; i < 1000; i++)
            totalScore = scorer.CalculateScore(solutionLocations);

        Assert.AreEqual(score1.GameScore.Total, totalScore);
    }
}