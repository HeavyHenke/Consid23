using System.Diagnostics;
using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class DennisSolver1Tests
{
    [TestMethod]
    public void SolveGöteborgGivesGoodSolution()
    {
//        var mapDataJson = File.ReadAllText("linkoping.cached.json");
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;
        
        var scorer = new DennisModel(generalData, mapData);
        var scorer1 = new Scoring(generalData, mapData);
        var solver = new DennisSolver1(scorer, new DummySubmitter(),generalData);
        for (int i = 0; i < 5; i++)
        {
            var solutionLocations = solver.OptimizeSolution(1012);
            var totalScore = scorer.CalculateScore(solutionLocations);

            var score1 = scorer1.CalculateScore(scorer.ConvertToSubmitSolution(solutionLocations));
            Trace.WriteLine($"GameScore: {score1.GameScore!.Total} co2 {score1.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score1.GameScore.Earnings} footfall {score1.GameScore.TotalFootfall}");
            Assert.AreEqual(score1.GameScore.Total, totalScore);
        }

    }

}