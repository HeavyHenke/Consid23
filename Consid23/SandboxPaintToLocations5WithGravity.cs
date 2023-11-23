using System.Collections.Concurrent;
using System.Drawing;
using Considition2023_Cs;

namespace Consid23;

public class SandboxPaintToLocations5WithGravity
{
    private readonly GeneralData _generalData;
    private const int HeatMapSize = 2048;
    private const int OptimizationSize = 100;

    public SandboxPaintToLocations5WithGravity(GeneralData generalData)
    {
        _generalData = generalData;
    }

    public MapData ClusterHotspots(MapData input)
    {
        var output = input.Clone();
        output.locations.Clear();

        const int maxGroceryStoreLarge = 5;
        const int maxGroceryStore = 20;
        const int maxConvenience = 20;
        const int maxGasStation = 8;
        const int maxKiosk = 3;
        
        var toPlace = new List<LocationType>();
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["gasStation"], maxGasStation));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["convenience"], maxConvenience));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStore"], maxGroceryStore));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStoreLarge"], maxGroceryStoreLarge));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["kiosk"], maxKiosk));

        output.LocationTypeCount.Add("groceryStoreLarge", maxGroceryStoreLarge);
        output.LocationTypeCount.Add("groceryStore", maxGroceryStore);
        output.LocationTypeCount.Add("convenience", maxConvenience);
        output.LocationTypeCount.Add("maxGasStation", maxGasStation);
        output.LocationTypeCount.Add("kiosk", maxKiosk);

        RemoveHotspotsThatAreTooFarOutsideOfBorder(output);

        double bestScore = 0;
        MapData bestMap = null;
        
        var finishedPositions = new List<(double latitude, double longitude, double footfall)>();
        for (int i = 0; i < toPlace.Count - 1; i++)
        {
            output.locations.Clear();
            PlaceLocations(output, toPlace, finishedPositions);

            var gravityOptimizer = new HenrikOptimizeByGravity(_generalData, output);
            var sol = gravityOptimizer.Optimize(CreateSolution(output));
            var score = new Scoring(_generalData, output).CalculateScore(sol);
            if (score.GameScore.Total > bestScore)
            {
                bestScore = score.GameScore.Total;
                bestMap = output.Clone();
                Console.WriteLine($"Best score update: {bestScore}");

                var toAdd = sol.Locations.Skip(finishedPositions.Count).First();
                finishedPositions.Add((toAdd.Value.Latitude, toAdd.Value.Longitude, output.locations[toAdd.Key].Footfall));
            }
            else
            {
                var toAdd = output.locations.Skip(finishedPositions.Count).First();
                finishedPositions.Add((toAdd.Value.Latitude, toAdd.Value.Longitude, toAdd.Value.Footfall));
            }
        }

        return bestMap;
    }

    private static SubmitSolution CreateSolution(MapData mapData)
    {
        // [0]: {(Convenience, 1, 0)}
// [1]: {(Gas-station, 1, 0)}
// [2]: {(Grocery-store, 2, 0)}
// [3]: {(Kiosk, 0, 0)}
// [4]: {(Grocery-store-large, 0, 1)}
        var typeToSmall = new Dictionary<string, int>
        {
            { "Convenience", 1 },
            { "Gas-station", 1 },
            { "Grocery-store", 2 },
            { "Kiosk", 0 },
            { "Grocery-store-large", 0 }
        };

        var ret = new SubmitSolution
        {
            Locations = new()
        };
        foreach(var loc in mapData.locations)
        {
            ret.Locations.Add(loc.Key, new PlacedLocations
            {
                Longitude = loc.Value.Longitude,
                Latitude = loc.Value.Latitude,
                LocationType = loc.Value.LocationType,
                Freestyle3100Count = typeToSmall[loc.Value.LocationType],
                Freestyle9100Count = loc.Value.LocationType == "Grocery-store" ? 1 : 0
            });
        }

        return ret;
    }

    private static void PlaceLocations(MapData mapData, List<LocationType> locationTypes, List<(double latitude, double longitude, double footfall)> beginWithThesePositions)
    {
        var storeLocations = new List<StoreLocation>(locationTypes.Count);
        
        var minLong = mapData.Hotspots.Select(h => h.Longitude).Min();
        var maxLong = mapData.Hotspots.Select(h => h.Longitude).Max();
        var minLat = mapData.Hotspots.Select(h => h.Latitude).Min();
        var maxLat = mapData.Hotspots.Select(h => h.Latitude).Max();

        var heatMap = new HeatMap(HeatMapSize, minLong, maxLong, minLat, maxLat);
        var hotSpotsLeft = mapData.Hotspots.ToList();
        foreach(var hs in hotSpotsLeft)
            heatMap.AddHotspot(hs);

        // heatMap.SaveAsBitmap(@"c:\temp\newImage.bmp");
        
        int locationIx = 0;

        foreach (var s in beginWithThesePositions)
        {
            string locationName = "location" + (locationIx + 1);

            storeLocations.Add(new StoreLocation
            {
                LocationName = locationName,
                LocationType = locationTypes[locationIx].Type,
                Latitude = s.latitude,
                Longitude = s.longitude,
                SalesVolume = locationTypes[locationIx].SalesVolume,
                Footfall = s.footfall
            });
            locationIx++;
        }
        
        while (storeLocations.Count < locationTypes.Count)
        {
            var maxPos = heatMap.GetMaxPos();

            var usedHotspots = new List<Hotspot>();
            for (var i = hotSpotsLeft.Count - 1; i >= 0; i--)
            {
                var hs = hotSpotsLeft[i];
                var dist = Helper.DistanceBetweenPoint(maxPos.lat, maxPos.lon, hs.Latitude, hs.Longitude);
                if (dist <= hs.Spread)
                {
                    hotSpotsLeft.RemoveAt(i);
                    usedHotspots.Add(hs);
                }
            }

            var optimized = OptimizePoint(maxPos.lat, maxPos.lon, usedHotspots, heatMap.LatPerPixel, heatMap.LongPerPixel);
            foreach(var hs in usedHotspots)
            {
                var dist = Helper.DistanceBetweenPoint(optimized.lat, optimized.lon, hs.Latitude, hs.Longitude);
                if (dist > hs.Spread)
                {
                    usedHotspots.Remove(hs);
                    hotSpotsLeft.Add(hs);
                }
            }
            
            
            string locationName = "location" + (locationIx + 1);

            storeLocations.Add(new StoreLocation
            {
                LocationName = locationName,
                LocationType = locationTypes[locationIx].Type,
                Latitude = optimized.lat,
                Longitude = optimized.lon,
                SalesVolume = locationTypes[locationIx].SalesVolume,
                Footfall = optimized.points
            });
            locationIx++;

            foreach (var hs in usedHotspots)
                heatMap.RemoveHotspotWithNeg(hs, optimized.lat, optimized.lon);

            // heatMap.SaveAsBitmap($@"c:\temp\newImage_{locationName}.bmp");
        }

        mapData.locations = storeLocations.ToDictionary(key => key.LocationName);
    }
    
    private static (double lat, double lon, double points) OptimizePoint(double lat, double lon, List<Hotspot> hotspots, double latSizePerPixel, double longSizePerPixel)
    {
        var minLat = lat - 2 * latSizePerPixel;
        var maxLat = lat + 2 * latSizePerPixel;
        var minLong = lon - 2 * longSizePerPixel;
        var maxLong = lon + 2 * longSizePerPixel;

        var heatMap = new HeatMap(OptimizationSize, minLong, maxLong, minLat, maxLat);
        foreach(var hs in hotspots)
            heatMap.AddHotspot(hs);

        var optimizedLocation = heatMap.GetMaxPos();
        return optimizedLocation;
    }

    private static void RemoveHotspotsThatAreTooFarOutsideOfBorder(MapData md)
    {
        for (var i = md.Hotspots.Count - 1; i >= 0; i--)
        {
            var hp = md.Hotspots[i];
            double lat = hp.Latitude;
            double lon = hp.Longitude;
            bool changed = false;

            if (hp.Longitude < md.Border.LongitudeMin)
            {
                lon = md.Border.LongitudeMin;
                changed = true;
            }

            if (hp.Longitude > md.Border.LongitudeMax)
            {
                lon = md.Border.LongitudeMax;
                changed = true;
            }

            if (hp.Latitude < md.Border.LatitudeMin)
            {
                lat = md.Border.LatitudeMin;
                changed = true;
            }

            if (hp.Latitude > md.Border.LatitudeMax)
            {
                lat = md.Border.LatitudeMax;
                changed = true;
            }
            
            if(changed && Helper.DistanceBetweenPoint(lat, lon, hp.Latitude, hp.Longitude) > hp.Spread)
                md.Hotspots.RemoveAt(i);
        }
    }
}
