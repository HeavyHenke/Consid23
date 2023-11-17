using Considition2023_Cs;

namespace Consid23;


public class SandboxClusterHotspotsToLocations2(GeneralData generalData)
{
    private readonly GeneralData _generalData = generalData;

    public MapData ClusterHotspots(MapData input)
    {
        var output = input.Clone();
        RemoveHotspotsThatAreTooFarOutsideOfBorder(output);

        var clusters = new List<Cluster> { new Cluster(input.Hotspots[0]) };

        for (int i = 1; i < input.Hotspots.Count; i++)
        {
            bool foundCluster = false;
            foreach (var c in clusters)
            {
                if (c.TryAddToCluster(input.Hotspots[i]))
                {
                    foundCluster = true;
                    break;
                }
            }

            if (!foundCluster)
                clusters.Add(new Cluster(input.Hotspots[i]));
        }

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

        var locations = new ClusterHotSpotConstructor(clusters).GetOptimalLocations(toPlace);
        
        output.locations.Clear();
        foreach (var loc in locations)
            output.locations.Add(loc.LocationName, loc);

        output.LocationTypeCount.Add("groceryStoreLarge", maxGroceryStoreLarge);
        output.LocationTypeCount.Add("groceryStore", maxGroceryStore);
        output.LocationTypeCount.Add("convenience", maxConvenience);
        output.LocationTypeCount.Add("maxGasStation", maxGasStation);
        output.LocationTypeCount.Add("kiosk", maxKiosk);
        
        
        return output;
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
            
            if(changed && DistanceBetweenPoint(lat, lon, hp.Latitude, hp.Longitude) > hp.Spread)
                md.Hotspots.RemoveAt(i);
        }
    }
    
    public static int DistanceBetweenPoint(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double r = 6371e3;
        double latRadian1 = latitude1 * Math.PI / 180;
        double latRadian2 = latitude2 * Math.PI / 180;

        double latDelta = (latitude2 - latitude1) * Math.PI / 180;
        double longDelta = (longitude2 - longitude1) * Math.PI / 180;

        double a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                   Math.Cos(latRadian1) * Math.Cos(latRadian2) *
                   Math.Sin(longDelta / 2) * Math.Sin(longDelta / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        int distance = (int)(r * c + .5);
        return distance;
    }

    public static double DistanceBetweenPointDouble(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double r = 6371e3;
        double latRadian1 = latitude1 * Math.PI / 180;
        double latRadian2 = latitude2 * Math.PI / 180;

        double latDelta = (latitude2 - latitude1) * Math.PI / 180;
        double longDelta = (longitude2 - longitude1) * Math.PI / 180;

        double a = Math.Sin(latDelta / 2) * Math.Sin(latDelta / 2) +
                   Math.Cos(latRadian1) * Math.Cos(latRadian2) *
                   Math.Sin(longDelta / 2) * Math.Sin(longDelta / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c;
    }
    
}

file class Cluster(Hotspot hotspot)
{
    public List<Hotspot> Hotspots { get; } = new() { hotspot };

    public bool TryAddToCluster(Hotspot h)
    {
        bool foundConnection = (from existing in Hotspots
                let dist = SandboxClusterHotspotsToLocations2.DistanceBetweenPoint(existing.Latitude, existing.Longitude, h.Latitude, h.Longitude)
                where dist < existing.Spread
                select existing)
            .Any();

        if (!foundConnection)
            return false;

        Hotspots.Add(h);
        return true;
    }
}

file class ClusterToLocationStrategy
{
    private readonly Cluster _cluster;

    public ClusterToLocationStrategy(Cluster cluster)
    {
        _cluster = cluster;
    }

    public IEnumerable<(double lat, double lon, double points)> GetLocations()
    {
        const int size = 100;

        var hotSpots = _cluster.Hotspots.ToList();
        while (hotSpots.Any())
        {
            if (hotSpots.Count == 1)
            {
                yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, GetFootFall(hotSpots[0], 0));
                yield break;
            }

            var minLong = hotSpots.Select(h => h.Longitude).Min();
            var maxLong = hotSpots.Select(h => h.Longitude).Max();
            var minLat = hotSpots.Select(h => h.Latitude).Min();
            var maxLat = hotSpots.Select(h => h.Latitude).Max();

            if (minLong == maxLong && minLat == maxLat)
            {
                yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, GetFootFall(hotSpots[0], 0));
                yield break;
            }

            var points = FillMatrix(hotSpots, size, minLat, maxLat, minLong, maxLong);
            var (maxPointLat, maxPointLong, maxVal) = GetMaxValue(points, size);

            if (maxPointLat >= 0)
            {
                var lat = minLat + maxPointLat * (maxLat - minLat) / size;
                var lon = minLong + maxPointLong * (maxLong - minLong) / size;
                var usedHotSpots = new List<Hotspot>();

                for (int ix = hotSpots.Count - 1; ix >= 0; ix--)
                {
                    var dist = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble(lat, lon, hotSpots[ix].Latitude, hotSpots[ix].Longitude);
                    if (dist < hotSpots[ix].Spread)
                    {
                        usedHotSpots.Add(hotSpots[ix]);
                        hotSpots.RemoveAt(ix);
                    }
                }

                yield return OptimizePoint(lat, lon, maxVal, usedHotSpots, (maxLat - minLat) / size, (maxLong - minLong) / size);
                //yield return (lat, lon, maxVal);
            }
            else
            {
                Console.WriteLine("Error finding max value");
                yield break;
            }
        }
    }

    private static (double lat, double lon, double points) OptimizePoint(double lat, double lon, double points, List<Hotspot> hotspots, double latSizePerPixel, double longSizePerPixel)
    {
        const int size = 100;
        var minLat = lat - 2 * latSizePerPixel;
        var maxLat = lat + 2 * latSizePerPixel;
        var minLong = lon - 2 * longSizePerPixel;
        var maxLong = lon + 2 * longSizePerPixel;
        var matrix = FillMatrix(hotspots, size, minLat, maxLat, minLong, maxLong);
        var optimizedLocation = GetMaxValue(matrix, size);
        var optimalLat = minLat + optimizedLocation.latIx * (maxLat - minLat) / size;
        var optimalLong = minLong + optimizedLocation.longIx * (maxLong - minLong) / size;
        return (optimalLat, optimalLong, optimizedLocation.points);
    }

    private static double[,] FillMatrix(List<Hotspot> hotSpots, int size, double minLat, double maxLat, double minLong, double maxLong)
    {
        var latPerIx = (maxLat - minLat) / size;
        var longPerIx = (maxLong - minLong) / size;
        var meterPerLatIx = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble(minLat, (minLong + maxLong) / 2, maxLat, (minLong + maxLong) / 2) / size;
        var meterPerLongIx = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble((minLat + maxLat) / 2, minLong, (minLat + maxLat) / 2, maxLong) / size;
        var points = new double[size, size];

        foreach (var l in hotSpots)
        {
            var hotSpotLatIx = (l.Latitude - minLat) / latPerIx;

            for (int latIx = 0; latIx < size; latIx++)
            {
                var latDistMeters = Math.Abs(latIx - hotSpotLatIx) * meterPerLatIx;

                for (int longIx = 0; longIx < size; longIx++)
                {
                    var hotSpotLongIx = (l.Longitude - minLong) / longPerIx;
                    var longDistMeters = Math.Abs(longIx - hotSpotLongIx) * meterPerLongIx;

                    var dist = (int)(Math.Sqrt(latDistMeters * latDistMeters + longDistMeters * longDistMeters) + .5);
                    if (dist > l.Spread)
                        continue;

                    points[latIx, longIx] += GetFootFall(l, dist);
                }
            }
        }

        return points;
    }

    private static (int latIx, int longIx, double points) GetMaxValue(double[,] points, int size)
    {
        double maxVal = -1;
        int maxPointLat = -1;
        int maxPointLong = -1;
        for (int latIx = 0; latIx < size; latIx++)
        {
            for (int longIx = 0; longIx < size; longIx++)
            {
                if (points[latIx, longIx] > maxVal)
                {
                    maxVal = points[latIx, longIx];
                    maxPointLat = latIx;
                    maxPointLong = longIx;
                }
            }
        }

        return (maxPointLat, maxPointLong, maxVal);
    }

    private static double GetFootFall(Hotspot hs, int distanceInMeters)
    {
        double val = hs.Footfall * (1 - (distanceInMeters / hs.Spread));
        return val / 10;
    }
}


file class HotspotWithUses : Hotspot
{
    private int _uses;
    private readonly double _originalFootFall;

    public int Uses
    {
        get => _uses;
        set
        {
            _uses = value;
            Footfall = _originalFootFall / (_uses + 1);
        }
    }

    public HotspotWithUses(Hotspot org)
    {
        Longitude = org.Longitude;
        Latitude = org.Latitude;
        _originalFootFall = Footfall = org.Footfall;
        Spread = org.Spread;
        Name = org.Name;
        _uses = 0;
    }
    
}

file class ClusterToLocationStrategyReuseHotspotsFromBest
{
    private readonly Cluster _cluster;

    public ClusterToLocationStrategyReuseHotspotsFromBest(Cluster cluster)
    {
        _cluster = cluster;
    }

    public IEnumerable<(double lat, double lon, double points)> GetLocations(double minPoints)
    {
        const int size = 100;

        var hotSpots = _cluster.Hotspots.Select(h => new HotspotWithUses(h)).ToList();
        (double lat, double lon, double pointsWhenAlone, ICollection<HotspotWithUses> hotspots) first;


        if (hotSpots.Count == 1)
        {
            var footFall = GetFootFall(hotSpots[0], 0);
            if(footFall >= minPoints)
                yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, footFall);
            yield break;
        }

        var minLong = hotSpots.Select(h => h.Longitude).Min();
        var maxLong = hotSpots.Select(h => h.Longitude).Max();
        var minLat = hotSpots.Select(h => h.Latitude).Min();
        var maxLat = hotSpots.Select(h => h.Latitude).Max();

        if (minLong == maxLong && minLat == maxLat)
        {
            var footFall = GetFootFall(hotSpots[0], 0);
            if (footFall >= minPoints)
                yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, footFall);
            yield break;
        }

        var points = FillMatrix(hotSpots, size, minLat, maxLat, minLong, maxLong);
        var (maxPointLat, maxPointLong, maxVal) = GetMaxValue(points, size);

        if (maxPointLat >= 0)
        {
            var lat = minLat + maxPointLat * (maxLat - minLat) / size;
            var lon = minLong + maxPointLong * (maxLong - minLong) / size;
            var usedHotSpots = new HashSet<HotspotWithUses>();

            for (int ix = hotSpots.Count - 1; ix >= 0; ix--)
            {
                var dist = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble(lat, lon, hotSpots[ix].Latitude, hotSpots[ix].Longitude);
                if (dist < hotSpots[ix].Spread)
                {
                    usedHotSpots.Add(hotSpots[ix]);
                    hotSpots[ix].Uses++;
                }
            }

            var optimized = OptimizePoint(lat, lon, usedHotSpots, (maxLat - minLat) / size, (maxLong - minLong) / size);
            first = (optimized.lat, optimized.lon, optimized.points, usedHotSpots);
        }
        else
        {
            Console.WriteLine("Error finding max value");
            yield break;
        }

        if(first == default)
            yield break;
        if(first.pointsWhenAlone <= minPoints)
            yield break;
        
        while (hotSpots.Count != 0)
        {
            if (hotSpots.Count == 1)
            {
                if(hotSpots[0].Uses > 1)
                    break;
                var footFall = GetFootFall(hotSpots[0], 0);
                if(footFall >= minPoints)
                    yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, footFall);
                yield return (first.lat, first.lon, FirstFootFallUseOnlyOnce());
                yield break;
            }

            minLong = hotSpots.Select(h => h.Longitude).Min();
            maxLong = hotSpots.Select(h => h.Longitude).Max();
            minLat = hotSpots.Select(h => h.Latitude).Min();
            maxLat = hotSpots.Select(h => h.Latitude).Max();

            if (minLong == maxLong && minLat == maxLat)
            {
                if(hotSpots[0].Uses > 1)
                    break;
                var footFall = GetFootFall(hotSpots[0], 0);
                if(footFall >= minPoints)
                    yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, footFall);
                yield return (first.lat, first.lon, FirstFootFallUseOnlyOnce());
                yield break;
            }

            points = FillMatrix(hotSpots, size, minLat, maxLat, minLong, maxLong);
            (maxPointLat, maxPointLong, maxVal) = GetMaxValue(points, size);

            if (maxPointLat >= 0)
            {
                if (maxVal < minPoints)
                    break;

                var lat = minLat + maxPointLat * (maxLat - minLat) / size;
                var lon = minLong + maxPointLong * (maxLong - minLong) / size;
                var usedHotSpots = new HashSet<HotspotWithUses>();

                for (int ix = hotSpots.Count - 1; ix >= 0; ix--)
                {
                    var dist = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble(lat, lon, hotSpots[ix].Latitude, hotSpots[ix].Longitude);
                    if (dist < hotSpots[ix].Spread)
                    {
                        usedHotSpots.Add(hotSpots[ix]);
                        hotSpots.RemoveAt(ix);
                    }
                }

                // var optimized =  OptimizePoint(lat, lon, usedHotSpots, (maxLat - minLat) / size, (maxLong - minLong) / size);
                yield return (lat, lon, maxVal);
            }
            else
            {
                Console.WriteLine("Error finding max value");
                break;
            }
        }
        
        yield return (first.lat, first.lon, FirstFootFallUseOnlyOnce());
        yield break;


        double FirstFootFallUseOnlyOnce()
        {
            foreach (var hs in first.hotspots)
                hs.Uses--;
            return first.hotspots.Sum(h => GetFootFall(h, SandboxClusterHotspotsToLocations2.DistanceBetweenPoint(first.lat, first.lon, h.Latitude, h.Longitude)));
        }
    }

    private static (double lat, double lon, double points) OptimizePoint(double lat, double lon, ICollection<HotspotWithUses> hotspots, double latSizePerPixel, double longSizePerPixel)
    {
        const int size = 100;
        var minLat = lat - 2 * latSizePerPixel;
        var maxLat = lat + 2 * latSizePerPixel;
        var minLong = lon - 2 * longSizePerPixel;
        var maxLong = lon + 2 * longSizePerPixel;
        var matrix = FillMatrix(hotspots, size, minLat, maxLat, minLong, maxLong);
        var optimizedLocation = GetMaxValue(matrix, size);
        var optimalLat = minLat + optimizedLocation.latIx * (maxLat - minLat) / size;
        var optimalLong = minLong + optimizedLocation.longIx * (maxLong - minLong) / size;
        return (optimalLat, optimalLong, optimizedLocation.points);
    }

    private static double[,] FillMatrix(IEnumerable<HotspotWithUses> hotSpots, int size, double minLat, double maxLat, double minLong, double maxLong)
    {
        var latPerIx = (maxLat - minLat) / size;
        var longPerIx = (maxLong - minLong) / size;
        var meterPerLatIx = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble(minLat, (minLong + maxLong) / 2, maxLat, (minLong + maxLong) / 2) / size;
        var meterPerLongIx = SandboxClusterHotspotsToLocations2.DistanceBetweenPointDouble((minLat + maxLat) / 2, minLong, (minLat + maxLat) / 2, maxLong) / size;
        var points = new double[size, size];

        foreach (var l in hotSpots)
        {
            var hotSpotLatIx = (l.Latitude - minLat) / latPerIx;

            for (int latIx = 0; latIx < size; latIx++)
            {
                var latDistMeters = Math.Abs(latIx - hotSpotLatIx) * meterPerLatIx;

                for (int longIx = 0; longIx < size; longIx++)
                {
                    var hotSpotLongIx = (l.Longitude - minLong) / longPerIx;
                    var longDistMeters = Math.Abs(longIx - hotSpotLongIx) * meterPerLongIx;

                    var dist = (int)(Math.Sqrt(latDistMeters * latDistMeters + longDistMeters * longDistMeters) + .5);
                    if (dist > l.Spread)
                        continue;

                    points[latIx, longIx] += GetFootFall(l, dist);
                }
            }
        }

        return points;
    }

    private static (int latIx, int longIx, double points) GetMaxValue(double[,] points, int size)
    {
        double maxVal = -1;
        int maxPointLat = -1;
        int maxPointLong = -1;
        for (int latIx = 0; latIx < size; latIx++)
        {
            for (int longIx = 0; longIx < size; longIx++)
            {
                if (points[latIx, longIx] > maxVal)
                {
                    maxVal = points[latIx, longIx];
                    maxPointLat = latIx;
                    maxPointLong = longIx;
                }
            }
        }

        return (maxPointLat, maxPointLong, maxVal);
    }

    private static double GetFootFall(Hotspot hs, int distanceInMeters)
    {
        var val = hs.Footfall * (1 - (distanceInMeters / hs.Spread));
        return val / 10;
    }
}


file class ClusterHotSpotConstructor(ICollection<Cluster> clusters)
{
    private readonly ICollection<Cluster> _clusters = clusters;

    public IEnumerable<StoreLocation> GetOptimalLocations(IReadOnlyList<LocationType> typesToPlace)
    {
        var first = _clusters
            .SelectMany(c => new ClusterToLocationStrategy(c).GetLocations())
            .OrderByDescending(c => c.points)
            .Take(typesToPlace.Count)
            .ToList();

        var firstPoints = first.Sum(f => f.points);
        var minPoints = first.Last().points;

        var second = _clusters
            .SelectMany(c => new ClusterToLocationStrategyReuseHotspotsFromBest(c).GetLocations(minPoints))
            .OrderByDescending(c => c.points)
            .Take(typesToPlace.Count)
            .ToList();

        var secondPoints = second.Sum(s => s.points);

        return second
            .Select((l, i) => new StoreLocation
            {
                Footfall = l.points,
                footfallScale = 1,
                Latitude = l.lat,
                Longitude = l.lon,
                LocationName = "location" + (i + 1),
                SalesVolume = typesToPlace[i].SalesVolume,
                LocationType = typesToPlace[i].Type
            });
    }
}
