using System.Diagnostics;
using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class HenrikDennisSolver1GöteborgTests
{
    [TestMethod]
    public void SolveGöteborgGivesGoodSolution()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var startPoint = new HenrikSolver1(generalData, mapData, new DummySubmitter()).CreateStartPointByAddOneAt();
        
        var solver = new HenrikDennisSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);

        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");

        Assert.AreEqual(56_826_754, score.GameScore!.Total);
    }

    [TestMethod]
    public void SolveGöteborgPlus300()
    {
        var mapData = CreateBigGöteborg();

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var dennisModel = new DennisModel(generalData, mapData);
        var startPoint = new HenrikDennisStaticInitialStateCreator(dennisModel, generalData).CreateInitialSolution();

        
        // 518429541440

        
        // double bestScore = 0;
        // for (int i = 0; i < 200; i++)
        // {
        //     mapData.RandomizeLocationOrder(i+17777);
        //     dennisModel = new DennisModel(generalData, mapData);
        //     
        //     var solver = new HenrikDennisSolver1(dennisModel, new DummySubmitter());
        //     var solution = solver.OptimizeSolution(startPoint);
        //
        //     var scorer = new Scoring(generalData, mapData);
        //     var score = scorer.CalculateScore(solution);
        //
        //     // Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        //
        //     if (score.GameScore.Total > bestScore)
        //         bestScore = score.GameScore.Total;
        // } 
        //
        // Console.WriteLine("Best score: " + bestScore);

        // Best max found:
        // 518437265412

        
        // Now:
        // 518437265412
        // Utan super-expensive loop:
        // 518429541440
        
        var solver = new HenrikDennisSolver1(dennisModel, new DummySubmitter());
        var sol = solver.OptimizeSolution(startPoint);
        
        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(sol);
        
        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
    }
    
    [TestMethod]
    public void SolveGöteborgTransformedToLondon()
    {
        var mapData = CreateLondonFromGöteborg();

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var startPoint = new HenrikSolver1(generalData, mapData, new DummySubmitter()).CreateStartPointByAddOneAt();
        
        var solver = new HenrikDennisSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);

        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
    }
    
    [TestMethod]
    public void TestHenrikDennisOptimizer2Gradient_göteborg()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var dennisModel = new DennisModel(generalData, mapData);

        var startPoint = new HenrikDennisStaticInitialStateCreator(dennisModel, generalData).CreateInitialSolution();
        
        var solver = new HenrikDennisOptimizer2Gradient(dennisModel, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);

        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");

        Assert.AreEqual(56_826_754, score.GameScore!.Total);
    }
    
    [TestMethod]
    public void TestHenrikDennisOptimizer2Gradient_CreateBigGöteborg()
    {
        var mapData = CreateBigGöteborg();

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var dennisModel = new DennisModel(generalData, mapData);

        var startPoint = new HenrikDennisStaticInitialStateCreator(dennisModel, generalData).CreateInitialSolution2();
        
        var solver = new HenrikDennisOptimizer2Gradient(dennisModel, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);

        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        // 518437265412
    }
    
    [TestMethod]
    public void TestHenrikDennisOptimizer2Gradient_GöteborgTransformedToLondon()
    {
        double best = 0;
        for (int i = 0; i < 2; i++)
        {
            var mapData = CreateLondonFromGöteborg();
            mapData.RandomizeLocationOrder(i);

            var generalDataJson = File.ReadAllText("Cached_general.json");
            var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

            var dennisModel = new DennisModel(generalData, mapData);

            var startPoint = new HenrikDennisStaticInitialStateCreator(dennisModel, generalData).CreateInitialSolution();

            // var fastSolver = new HenrikDennisSolver1(dennisModel, new DummySubmitter());
            // var fastSolved = fastSolver.OptimizeSolution(startPoint);
            
            var solver = new HenrikDennisOptimizer2Gradient(dennisModel, new DummySubmitter());
            var solution = solver.OptimizeSolution(startPoint);

            var scorer = new Scoring(generalData, mapData);
            var score = scorer.CalculateScore(solution);

            Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
            best = Math.Max(score.GameScore.Total, best);
        }
        Console.WriteLine($"Best solution {best}");

        // Best of 10 with HenrikDennisSolver1
        // 55928621922993
    }
    
    private static MapData CreateBigGöteborg()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        // Make it bigger
        mapData.Border.LatitudeMax += (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
        mapData.Border.LongitudeMax += (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);

        var rnd = new Random(1337);
        for (int i = 0; i < 300; i++)
        {
            var longitud = mapData.Border.LongitudeMin + rnd.NextDouble() * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);
            var latitude = mapData.Border.LatitudeMin + rnd.NextDouble() * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
            var loc = new StoreLocation
            {
                LocationName = "radom_" + i,
                LocationType = "Random",
                SalesVolume = rnd.NextDouble() * 373,
                footfallScale = rnd.Next(0, 10),
                Footfall = 990 * rnd.NextDouble(),
                Longitude = longitud,
                Latitude = latitude
            };

            mapData.locations.Add(loc.LocationName, loc);
        }

        return mapData;
    }
        
    private static MapData CreateLondonFromGöteborg()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        // Make it bigger to mimic London
        mapData.Border.LatitudeMax += 2.5 * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
        mapData.Border.LongitudeMax += 2.5 * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);

        var rnd = new Random(1337);
        var göteborgSize = mapData.locations.Count;
        for (int i = 0; i < 14 * göteborgSize; i++)
        {
            var longitud = mapData.Border.LongitudeMin + rnd.NextDouble() * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);
            var latitude = mapData.Border.LatitudeMin + rnd.NextDouble() * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
            var loc = new StoreLocation
            {
                LocationName = "radom_" + i,
                LocationType = "Random",
                SalesVolume = rnd.NextDouble() * 373,
                footfallScale = rnd.Next(0, 10),
                Footfall = 990 * rnd.NextDouble(), // * 2.2234818602607578,
                Longitude = longitud,
                Latitude = latitude
            };

            mapData.locations.Add(loc.LocationName, loc);
        }

        return mapData;
    }

}