using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

[TestClass]
public class SandboxModeTests
{
    [TestMethod]
    public void Test_SandboxPaintToLocations2_with_g_sandbox()
    {
        var generalData = JsonConvert.DeserializeObject<GeneralData>(File.ReadAllText("Cached_general.json"))!;
        var mapData = JsonConvert.DeserializeObject<MapData>(File.ReadAllText("g-sandbox.cached.json"))!;
        
        var sandboxEngine = new SandboxPaintToLocations2(generalData);
        var clustered = sandboxEngine.ClusterHotspots(mapData);

         Assert.AreEqual(56, clustered.locations.Count);
    }
    
    [TestMethod]
    public void Test_SandboxPaintToLocations2_with_g_sandbox_scaled_to_london()
    {
        var generalData = JsonConvert.DeserializeObject<GeneralData>(File.ReadAllText("Cached_general.json"))!;
        var mapData = JsonConvert.DeserializeObject<MapData>(File.ReadAllText("g-sandbox.cached.json"))!;
        
        mapData.Border.LatitudeMax += 2.5 * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
        mapData.Border.LongitudeMax += 2.5 * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);

        var rnd = new Random(1337);
        var göteborgSize = mapData.locations.Count;
        for (int i = 0; i < 14 * göteborgSize; i++)
        {
            var longitud = mapData.Border.LongitudeMin + rnd.NextDouble() * (mapData.Border.LongitudeMax - mapData.Border.LongitudeMin);
            var latitude = mapData.Border.LatitudeMin + rnd.NextDouble() * (mapData.Border.LatitudeMax - mapData.Border.LatitudeMin);
            var hotspot = new Hotspot
            {
                Name = "radom_" + i,
                Footfall = 990 * rnd.NextDouble(), // * 2.2234818602607578,
                Spread = 250 * rnd.NextDouble() + 50,
                Longitude = longitud,
                Latitude = latitude
            };

            mapData.Hotspots.Add(hotspot);
        }
        
        var sandboxEngine = new SandboxPaintToLocations2(generalData);
        var clustered = sandboxEngine.ClusterHotspots(mapData);

        Assert.AreEqual(56, clustered.locations.Count);


        var dennisModel = new DennisModel(generalData, clustered);
        var initial = new HenrikDennisStaticInitialStateCreator(dennisModel, generalData).CreateInitialSolution();
        dennisModel.InitiateSandboxLocations(initial);
        
        var lastSol = new HenrikDennisOptimizer2Gradient(dennisModel, new DummySubmitter()) .OptimizeSolution(initial);

        var localScore = new ScoringHenrik(generalData, clustered).CalculateScore(lastSol);
        
        Console.WriteLine($"Score local {localScore.GameScore.Total} {localScore.GameScore.TotalFootfall} {localScore.GameScore.KgCo2Savings} {localScore.GameScore.Earnings}");

    }
}