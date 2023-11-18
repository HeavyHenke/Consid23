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

    [TestMethod]
    public void LocationConfigurationTest()
    {
        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var locationConfiguration = new LocationConfiguration(generalData);
        Assert.AreEqual(0,locationConfiguration.IndexFromDouble(-100));
        Assert.AreEqual(0,locationConfiguration.IndexFromDouble(0));
        Assert.AreEqual(1,locationConfiguration.IndexFromDouble(0.2));
        Assert.AreEqual(8,locationConfiguration.IndexFromDouble(.99));
        Assert.AreEqual(8,locationConfiguration.IndexFromDouble(100));
        
        
    }

    [TestMethod]
    public void Transform()
    {
        double r = 6371e3;
        var latitudeMax=57.7335032;
        var latitudeMin=57.6346191;
        var longitudeMax = 12.0524978;
        var longitudeMin = 11.8488913;
        var distPolar = Scoring.DistanceBetweenPoint(latitudeMin, longitudeMin, latitudeMax, longitudeMax);
        var x1 = r * Math.Sin((latitudeMax) * 2 * Math.PI / 360) * Math.Cos((longitudeMax) * 2 * Math.PI / 360);
        var y1 = r * Math.Sin((latitudeMax) * 2 * Math.PI / 360) * Math.Sin((longitudeMax) * 2 * Math.PI / 360);
        var z1 = r * Math.Cos((latitudeMax) * 2 * Math.PI / 360);
        var x2 = r * Math.Sin((latitudeMin) * 2 * Math.PI / 360) * Math.Cos((longitudeMin) * 2 * Math.PI / 360);
        var y2 = r * Math.Sin((latitudeMin) * 2 * Math.PI / 360) * Math.Sin((longitudeMin) * 2 * Math.PI / 360);
        var z2 = r * Math.Cos((latitudeMin) * 2 * Math.PI / 360);
        var distCart = Math.Sqrt((x2-x1) * (x2-x1) + (y2-y1) * (y2-y1) + (z2-z1) * (z2-z1));
        var diff = distPolar - distCart;
    }
    
    [TestMethod]
    public void SolveGöteborgSandbox()
    {
        var mapDataJson = File.ReadAllText("g-sandbox.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solutionJson=File.ReadAllText("g-sandbox_3348,35_17_06_30_54.json");
        var solution = JsonConvert.DeserializeObject<SubmitSolution>(solutionJson);

        var scorer1 = new Scoring(generalData, mapData);
        var score1= scorer1.CalculateScore(solution);
        
        
        
        var scorer = new DennisModel(generalData, mapData);
        var solutionLocations = scorer.InitiateSandboxLocations(solution);
        double totalScore = 0;

//        for (int i = 0; i < 1000; i++)
            totalScore = scorer.CalculateScore(solutionLocations,null,score1);

        Assert.AreEqual(score1.GameScore.Total, totalScore);
    }

}