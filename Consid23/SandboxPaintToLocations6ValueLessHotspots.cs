using System.Collections.Concurrent;
using System.Drawing;
using Considition2023_Cs;

namespace Consid23;

public class SandboxPaintToLocations6ValueLessHotspots
{
    private readonly GeneralData _generalData;
    private const int HeatMapSize = 2048;
    private const int OptimizationSize = 100;

    public SandboxPaintToLocations6ValueLessHotspots(GeneralData generalData)
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
        PlaceLocations(output, toPlace);

        return output;
    }

    private void PlaceLocations(MapData mapData, List<LocationType> locationTypes)
    {
        var storeLocations = new List<StoreLocation>(locationTypes.Count);
        
        var minLong = mapData.Hotspots.Select(h => h.Longitude).Min();
        var maxLong = mapData.Hotspots.Select(h => h.Longitude).Max();
        var minLat = mapData.Hotspots.Select(h => h.Latitude).Min();
        var maxLat = mapData.Hotspots.Select(h => h.Latitude).Max();

        var heatMap = new HeatMapWithNumHotspots(HeatMapSize, minLong, maxLong, minLat, maxLat);
        var hotSpotsLeft = mapData.Hotspots.ToList();
        foreach(var hs in hotSpotsLeft)
            heatMap.AddHotspot(hs);

        heatMap.SaveAsBitmap(@"c:\temp\2 delete\newImage.bmp");
        
        int locationIx = 0;
        
        while (storeLocations.Count < locationTypes.Count)
        {
            var maxPos = heatMap.GetMaxPos(locationTypes.Count - storeLocations.Count);

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

            // var optimized = OptimizePoint(maxPos.lat, maxPos.lon, usedHotspots, heatMap.LatPerPixel, heatMap.LongPerPixel);
            var optimized = maxPos;
            foreach(var hs in usedHotspots.ToArray())
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

            heatMap.RemoveDist(optimized.lat, optimized.lon, _generalData.WillingnessToTravelInMeters*1.2);
            heatMap.SaveAsBitmap($@"c:\temp\2 delete\newImage_{locationName}.bmp");
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

internal class HeatMapWithNumHotspots
{
    private readonly int _size;
    private readonly double _latPerIx;
    private readonly double _longPerIx;
    private readonly double _meterPerLatIx;
    private readonly double _meterPerLongIx;
    private readonly double _minLong;
    private readonly double _maxLong;
    private readonly double _minLat;
    private readonly double _maxLat;
    private readonly double[,] _map;
    private readonly int[,] _numHotspots;
    private readonly double _latToIndexFactor;
    private readonly double _longToIndexFactor;

    public double LatPerPixel => _latPerIx;
    public double LongPerPixel => _longPerIx;

    
    public HeatMapWithNumHotspots(int size, double minLong, double maxLong, double minLat, double maxLat)
    {
        _maxLat = maxLat;
        _minLat = minLat;
        _maxLong = maxLong;
        _minLong = minLong;
        _size = size;
        _map = new double[size, size];
        _numHotspots = new int[size, size];
        
        _latPerIx = (maxLat - minLat) / _size;
        _longPerIx = (maxLong - minLong) / _size;
        _meterPerLatIx = Helper.DistanceBetweenPointDouble(minLat, (minLong + maxLong) / 2, maxLat, (minLong + maxLong) / 2) / _size;
        _meterPerLongIx = Helper.DistanceBetweenPointDouble((minLat + maxLat) / 2, minLong, (minLat + maxLat) / 2, maxLong) / _size;

        _latToIndexFactor = _size / (_maxLat - _minLat);
        _longToIndexFactor = _size / (_maxLong - _minLong);
    }

    public HeatMapWithNumHotspots Clone()
    {
        var clone = new HeatMapWithNumHotspots(_size, _minLong, _maxLong, _minLat, _maxLat);
        Array.Copy(_map, 0, clone._map, 0, _size * _size);
        Array.Copy(_numHotspots, 0, clone._numHotspots, 0, _size);
        return clone;
    }
    
    public void AddHotspot(Hotspot h)
    {
        AddHotspotWithFactor(h, false);
    }

    public void RemoveHotspot(Hotspot h)
    {
        AddHotspotWithFactor(h, true);
    }
    
    public void RemoveHotspotWithNeg(Hotspot h, double placedStoreLat, double placedStoreLong)
    {
        return;
        AddHotspotWithFactor(h, true);
        
        // Add negative scores (and positive close to the hotspot)
        var centerLatIx = (int)((h.Latitude - _minLat) * _latToIndexFactor);
        var centerLongIx = (int)((h.Longitude - _minLong) * _longToIndexFactor);
        var latSize = (int)(h.Spread / _meterPerLatIx);
        var longSize = (int)(h.Spread / _meterPerLongIx);
        
        var startX = Math.Max(0, centerLatIx - latSize);
        var startY = Math.Max(0, centerLongIx - longSize);
        var stopX = Math.Min(_size - 1, centerLatIx + latSize);
        var stopY = Math.Min(_size - 1, centerLongIx + longSize);
        
        var locDist = Helper.DistanceBetweenPoint(placedStoreLat, placedStoreLong, h.Latitude, h.Longitude);
        var distanceLost = GetFootFall(h, locDist) / 2;
                
        for (int x = startX; x < stopX; x++)
        {
            var latDist = (x - centerLatIx) * _meterPerLatIx;
            var latDistSquare = latDist * latDist;
            for (int y = startY; y < stopY; y++)
            {
                var longDist = (y - centerLongIx) * _meterPerLongIx;
                var dist = (int)(Math.Sqrt(latDistSquare + longDist * longDist));
                var footFallGain = GetFootFall(h, dist) / 2;
                if (dist > 200)
                    continue;
        
                _map[x, y] += footFallGain - distanceLost;
            }
        }
    }

    public void RemoveDist(double placedStoreLat, double placedStoreLong, double spread)
    {
        // Add negative scores (and positive close to the hotspot)
        var centerLatIx = (int)((placedStoreLat - _minLat) * _latToIndexFactor);
        var centerLongIx = (int)((placedStoreLong - _minLong) * _longToIndexFactor);
        var latSize = (int)(spread / _meterPerLatIx);
        var longSize = (int)(spread / _meterPerLongIx);
        
        var startX = Math.Max(0, centerLatIx - latSize);
        var startY = Math.Max(0, centerLongIx - longSize);
        var stopX = Math.Min(_size - 1, centerLatIx + latSize);
        var stopY = Math.Min(_size - 1, centerLongIx + longSize);
        
        for (int x = startX; x < stopX; x++)
        {
            var latDist = (x - centerLatIx) * _meterPerLatIx;
            var latDistSquare = latDist * latDist;
            for (int y = startY; y < stopY; y++)
            {
                var longDist = (y - centerLongIx) * _meterPerLongIx;
                var dist = (int)(Math.Sqrt(latDistSquare + longDist * longDist));
                if (dist > spread)
                    continue;
        
                _map[x, y] = -10;
            }
        }
        
    }
    
    private void AddHotspotWithFactor(Hotspot h, bool neg)
    {
        var centerLatIx = (int)((h.Latitude - _minLat) * _latToIndexFactor + .5);
        var centerLongIx = (int)((h.Longitude - _minLong) * _longToIndexFactor + .5);
        var latSize = (int)(h.Spread / _meterPerLatIx);
        var longSize = (int)(h.Spread / _meterPerLongIx);

        var startX = Math.Max(0, centerLatIx - latSize - 1);
        var startY = Math.Max(0, centerLongIx - longSize - 1);
        var stopX = Math.Min(_size - 1, centerLatIx + latSize + 1);
        var stopY = Math.Min(_size - 1, centerLongIx + longSize + 1);

        for (int x = startX; x <= stopX; x++)
        {
            var latDist = (x - centerLatIx) * _meterPerLatIx;
            var latDistSquare = latDist * latDist;
            for (int y = startY; y <= stopY; y++)
            {
                var longDist = (y - centerLongIx) * _meterPerLongIx;
                var dist = (int)(Math.Sqrt(latDistSquare + longDist * longDist) + .5);
                var footFall = GetFootFall(h, dist);
                if (footFall <= 0)
                    continue;

                if (!neg)
                {
                    _map[x, y] += footFall;
                    _numHotspots[x, y]++;
                }
                else
                {
                    _map[x, y] -= footFall;
                    _numHotspots[x, y]--;
                }
            }
        }
    }

    public (double lat, double lon, double points) GetMaxPosNonParallel()
    {
        int maxLatIx = -1;
        int maxLongIx = -1;
        double maxPoints = double.MinValue;
        
        for (int x = 0; x < _size; x++)
        for (int y = 0; y < _size; y++)
        {
            var points = _map[x, y];
            if (points > maxPoints)
            {
                maxPoints = points;
                maxLatIx = x;
                maxLongIx = y;
            }
        }

        return (_minLat + maxLatIx * _latPerIx, _minLong + maxLongIx * _longPerIx, maxPoints);
    }
    
    public (double lat, double lon, double points) GetMaxPos(int numLeft)
    {
        ConcurrentQueue<(int lat, int lon, double points, int numPoints)> maxPerRowQueue = new();
        double threshold = 0.1;
        
        Parallel.For(0, _size,
            x =>
            {
                int maxLatIx = -1;
                int maxLongIx = -1;
                double maxPoints = double.MinValue;
                int maxNumHotSpots = -1;
                for (int y = 0; y < _size; y++)
                {
                    var points = _map[x, y];
                    if (points > maxPoints)
                    {
                        maxPoints = points;
                        maxLatIx = x;
                        maxLongIx = y;
                        maxNumHotSpots = _numHotspots[x, y];
                    }
                }

                maxPerRowQueue.Enqueue((maxLatIx, maxLongIx, maxPoints, maxNumHotSpots));
            }
        );

        var result = maxPerRowQueue.ToArray();
        var maxPoints = result.Select(r => r.points).Max();
        (int lat, int lon, double points, int numPoints) best;
        // if(numLeft > 30)
        //     best = result.Where(r => r.points >= (maxPoints - threshold)).MinBy(n => n.numPoints);
        // else
             best = result.MaxBy(n => n.points);

        // var topList = result.Where(r => r.points >= (maxPoints - threshold)).ToArray();
        // var ix = new Random(numLeft).Next(0, topList.Length);
        // var best = topList[ix];
        return (_minLat + best.lat * _latPerIx, _minLong + best.lon * _longPerIx, maxPoints);

        // int maxLatIx = -1;
        // int maxLongIx = -1;
        // double maxPoints = double.MinValue;
        // foreach (var row in maxPerRowQueue.ToArray())
        // {
        //     if (row.points > maxPoints)
        //     {
        //         maxPoints = row.points;
        //         maxLatIx = row.lat;
        //         maxLongIx = row.lon;
        //     }
        // }

        // return (_minLat + maxLatIx * _latPerIx, _minLong + maxLongIx * _longPerIx, maxPoints);
    }
    
    private static double GetFootFall(Hotspot hs, int distanceInMeters)
    {
        double val = hs.Footfall * (1 - (distanceInMeters / hs.Spread));
        return val / 10;
    }
    
    public void SaveAsBitmap(string fileName)
    {
        return;
        var bitmap = new Bitmap(_size, _size);
        double min = 10000;
        double max = -1;
        for (int x = 0; x < _size; x++)
        for (int y = 0; y < _size; y++)
        {
            min = Math.Min(min, _map[y, x]);
            max = Math.Max(max, _map[y, x]);
        }

        for (int x = 0; x < _size; x++)
        for (int y = 0; y < _size; y++)
        {
            if (Math.Abs(_map[y, x]) < 0.0000001)
            {
                bitmap.SetPixel(x, _size - y - 1, Color.FromArgb(255, 0, 0));
            }
            else if(_map[y,x] > 0)
            {
                var val = _map[y, x] / max;
                int rgb = (int)(val * 255);
                bitmap.SetPixel(x, _size - y - 1, Color.FromArgb(rgb, rgb, rgb));
            }
            else if(min < -0.001)
            {
                var val = 1-(_map[y, x] - min) / (-min);
                int rgb = (int)(val * 255);
                bitmap.SetPixel(x, _size - y - 1, Color.FromArgb(0, rgb, 0));
            }
        }

        bitmap.Save(fileName);
    }
}
