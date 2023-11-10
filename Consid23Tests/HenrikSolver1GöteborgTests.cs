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

        var solver = new HenrikSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.CalcSolution();

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);
        
        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        
        Assert.IsTrue(score.GameScore!.Total >= 56_826_754);
    }
    
    [TestMethod]
    public void SolveGöteborgPlus300()
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
                LocationName = "radom_"+i,
                LocationType = "Random",
                SalesVolume = rnd.NextDouble() * 373,
                footfallScale = rnd.Next(0, 10),
                Footfall = 990 * rnd.NextDouble(),
                Longitude = longitud,
                Latitude = latitude
            };
            
            mapData.locations.Add(loc.LocationName, loc);
        }

        mapData.RandomizeLocationOrder(1335);
        
        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var startPoint = new HenrikSolver1(generalData, mapData, new DummySubmitter()).CreateStartPointByAddOneAt();
        
        var solver = new HenrikDennisSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);
        
        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        
        // 518411259617
        
        // 518363357243
    }
    
    [TestMethod]
    public void SolveGöteborgPlus5100()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        // Make it bigger
        mapData.Border.LatitudeMax += 2*(mapData.Border.LatitudeMax - mapData.Border.LatitudeMin); 
        mapData.Border.LongitudeMax += 2*(mapData.Border.LongitudeMax - mapData.Border.LongitudeMin); 
        
        var rnd = new Random(1337);
        for (int i = 0; i < 5100; i++)
        {
            var longitud = mapData.Border.LongitudeMin + rnd.NextDouble() * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);
            var latitude = mapData.Border.LatitudeMin + rnd.NextDouble() * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
            var loc = new StoreLocation
            {
                LocationName = "radom_"+i,
                LocationType = "Random",
                SalesVolume = rnd.NextDouble() * 373,
                footfallScale = rnd.Next(0, 10),
                Footfall = rnd.NextDouble() * 2.2234818602607578,
                Longitude = longitud,
                Latitude = latitude
            };
            
            mapData.locations.Add(loc.LocationName, loc);
        }

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var startPoint = new HenrikSolver1(generalData, mapData, new DummySubmitter()).CreateStartPointByAddOneAt();
        
        var solver = new HenrikDennisSolver1(generalData, mapData, new DummySubmitter());
        var solution = solver.OptimizeSolution(startPoint);

        var scorer = new Scoring(generalData, mapData);
        var score = scorer.CalculateScore(solution);
        
        Trace.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        // Gives score: 74739633967
    }
}