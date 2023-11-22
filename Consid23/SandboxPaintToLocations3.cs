using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Considition2023_Cs;

namespace Consid23;

public class SandboxPaintToLocations3
{
    private readonly GeneralData _generalData;
    private const int HeatMapSize = 2048;
    private const int OptimizationSize = 100;

    public SandboxPaintToLocations3(GeneralData generalData)
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
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStoreLarge"], maxGroceryStoreLarge));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStore"], maxGroceryStore));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["gasStation"], maxGasStation));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["convenience"], maxConvenience));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["kiosk"], maxKiosk));

        output.LocationTypeCount.Add("groceryStoreLarge", maxGroceryStoreLarge);
        output.LocationTypeCount.Add("groceryStore", maxGroceryStore);
        output.LocationTypeCount.Add("convenience", maxConvenience);
        output.LocationTypeCount.Add("maxGasStation", maxGasStation);
        output.LocationTypeCount.Add("kiosk", maxKiosk);
        
        RemoveHotspotsThatAreTooFarOutsideOfBorder(output);
        PlaceLocations(output, toPlace);

        return output;
    }

    private static void PlaceLocations(MapData mapData, List<LocationType> locationTypes)
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

        var usedHotSpotsPerLocation = new List<List<Hotspot>>(locationTypes.Count);
        
        // heatMap.SaveAsBitmap(@"c:\temp\newImage.bmp");
        
        int locationIx = 0;
        
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

            var optimized = OptimizePoint(maxPos.lat, maxPos.lon, maxPos.points, usedHotspots, heatMap.LatPerPixel, heatMap.LongPerPixel);
            foreach(var hs in usedHotspots)
            {
                var dist = Helper.DistanceBetweenPoint(optimized.lat, optimized.lon, hs.Latitude, hs.Longitude);
                if (dist > hs.Spread)
                {
                    usedHotspots.Remove(hs);
                    hotSpotsLeft.Add(hs);
                }
            }
            
            usedHotSpotsPerLocation.Add(usedHotspots);
            
            
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

        OptimizeManyPoints(hotSpotsLeft, usedHotSpotsPerLocation, storeLocations);

        // Remap types
        locationIx = 0;
        foreach (var loc in storeLocations.OrderByDescending(s => s.Footfall))
        {
            loc.LocationType = locationTypes[locationIx].Type;
            loc.SalesVolume = locationTypes[locationIx].SalesVolume;
            locationIx++;
        }
        
        mapData.locations = storeLocations.ToDictionary(key => key.LocationName);
    }

    private static void OptimizeManyPoints(List<Hotspot> hotSpotsLeft, List<List<Hotspot>> usedHotSpotsPerLocation, List<StoreLocation> storeLocations)
    {
        var maxSpread = hotSpotsLeft.Concat(usedHotSpotsPerLocation.SelectMany(q => q)).Select(h => h.Spread).Max();

        for (int i = 0; i < storeLocations.Count; i++)
        for (int j = 0; j < storeLocations.Count; j++)
        {
            if(i == j)
                continue;
            
            var distance = Helper.DistanceBetweenPointDouble(storeLocations[i].Latitude, storeLocations[i].Longitude, storeLocations[j].Latitude, storeLocations[j].Longitude);
            if (distance > 2 * maxSpread)
                continue;

            OptimizePoints( storeLocations[i], storeLocations[j], usedHotSpotsPerLocation[i], usedHotSpotsPerLocation[j], hotSpotsLeft);
        }
    }

    private static void OptimizePoints(StoreLocation loc1, StoreLocation loc2, List<Hotspot> hotspots1, List<Hotspot> hotspots2, List<Hotspot> unused)
    {
        var bestTotal = loc1.Footfall + loc2.Footfall;

        var allHotspotsToOptimizeFor = hotspots1.Concat(hotspots2).Concat(unused).ToList();
        var minLat = allHotspotsToOptimizeFor.Select(h => h.Latitude).Min();
        var maxLat = allHotspotsToOptimizeFor.Select(h => h.Latitude).Max();
        var minLon = allHotspotsToOptimizeFor.Select(h => h.Longitude).Min();
        var maxLon = allHotspotsToOptimizeFor.Select(h => h.Longitude).Max();

        var heatMapWithUsedHotspots = new HeatMap(HeatMapSize, minLon, maxLon, minLat, maxLat);
        foreach (var hs in hotspots1.Concat(hotspots2).Concat(unused))
            heatMapWithUsedHotspots.AddHotspot(hs);
        
        for (int i = 0; i < hotspots1.Count; i++)
        {
            var heatMap = heatMapWithUsedHotspots.Clone();
            heatMap.RemoveHotspot(hotspots1[i]);

            var best1 = heatMap.GetMaxPos();
            var used1 = GetUsedHotspots(allHotspotsToOptimizeFor.Except(new[] { hotspots1[i] }), best1.lat, best1.lon).ToList();
            var opt1 = OptimizePoint(best1.lat, best1.lon, best1.points, used1, heatMap.LatPerPixel, heatMap.LongPerPixel);
            //var opt1 = best1;
            foreach(var hs in used1)
                heatMap.RemoveHotspotWithNeg(hs, opt1.lat, opt1.lon);

            heatMap.AddHotspot(hotspots1[i]);
            var best2 = heatMap.GetMaxPos();
            var used2 = GetUsedHotspots(allHotspotsToOptimizeFor.Except(used1), best2.lat, best2.lon).ToList();
            var opt2 = OptimizePoint(best2.lat, best2.lon, best2.points, allHotspotsToOptimizeFor.Except(used1).ToList(), heatMap.LatPerPixel, heatMap.LongPerPixel);
            //var opt2 = best2;
            
            if (opt1.points + opt2.points > bestTotal)
            {
                Console.WriteLine($"Found better, diff: {opt1.points + opt2.points - bestTotal} at {i} for {loc1.LocationName}({loc1.LocationType}) and {loc2.LocationName}({loc2.LocationType})");
                loc1.Latitude = opt1.lat;
                loc1.Longitude = opt1.lon;
                loc1.Footfall = opt1.points;
                loc2.Latitude = opt2.lat;
                loc2.Longitude = opt2.lon;
                loc2.Footfall = opt2.points;
                bestTotal = opt1.points + opt2.points;
                hotspots1.Clear();
                hotspots1.AddRange(used1);
                hotspots2.Clear();
                hotspots2.AddRange(used2);
                unused.Clear();
                unused.AddRange(allHotspotsToOptimizeFor.Except(used1).Except(used2));
            }
        }
    }

    private static IEnumerable<Hotspot> GetUsedHotspots(IEnumerable<Hotspot> hotspots, double lat, double lon)
    {
        foreach (var hs in hotspots)
        {
            var dist = Helper.DistanceBetweenPoint(lat, lon, hs.Latitude, hs.Longitude);
            if (dist < hs.Spread)
                yield return hs;
        }
    }
    
    private static (double lat, double lon, double points) OptimizePoint(double lat, double lon, double orgPoints, List<Hotspot> hotspots, double latSizePerPixel, double longSizePerPixel)
    {
        var minLat = lat - 2 * latSizePerPixel;
        var maxLat = lat + 2 * latSizePerPixel;
        var minLong = lon - 2 * longSizePerPixel;
        var maxLong = lon + 2 * longSizePerPixel;

        var heatMap = new HeatMap(OptimizationSize, minLong, maxLong, minLat, maxLat);
        foreach(var hs in hotspots)
            heatMap.AddHotspot(hs);

        var optimizedLocation = heatMap.GetMaxPos();
        //if(optimizedLocation.points > orgPoints)
            return optimizedLocation;
        return (lat, lon, orgPoints);
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
