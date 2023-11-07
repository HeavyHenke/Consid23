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
        
        Trace.WriteLine($"GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
        
        Assert.IsTrue(score.GameScore!.Total >= 56_826_754);
    }
    
    [TestMethod]
    public void SolveGöteborgPlus300GivesGoodSolution()
    {
        var mapDataJson = File.ReadAllText("goteborg.cached.json");
        var mapData = JsonConvert.DeserializeObject<MapData>(mapDataJson)!;

        // Make it bigger
        mapData.Border.LatitudeMax += (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin); 
        mapData.Border.LongitudeMax += (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin); 
        
        var rnd = new Random();
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
                Footfall = rnd.NextDouble() * 2.2234818602607578,
                Longitude = longitud,
                Latitude = latitude
            };
            
            mapData.locations.Add(loc.LocationName, loc);
        }
        
        

        var generalDataJson = File.ReadAllText("Cached_general.json");
        var generalData = JsonConvert.DeserializeObject<GeneralData>(generalDataJson)!;

        var solver = new HenrikSolver1(generalData, mapData);
        var solution = solver.CalcSolution();

        var scorer = new Scoring();
        var score = scorer.CalculateScore(mapData.MapName, solution, mapData, generalData);
        
        Trace.WriteLine($"GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
    }
}