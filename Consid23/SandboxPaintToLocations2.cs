using System.Drawing;
using Considition2023_Cs;

namespace Consid23;

public class SandboxPaintToLocations2
{
    private readonly GeneralData _generalData;
    private const int HeatMapSize = 2048;
    private const int OptimizationSize = 100;

    public SandboxPaintToLocations2(GeneralData generalData)
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
                if (dist < hs.Spread)
                {
                    hotSpotsLeft.RemoveAt(i);
                    heatMap.RemoveHotspot(hs);
                    usedHotspots.Add(hs);
                }
            }

            var optimized = OptimizePoint(maxPos.lat, maxPos.lon, usedHotspots, heatMap.LatPerPixel, heatMap.LongPerPixel);
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

internal static class Helper
{
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


file class HeatMap
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

    public double LatPerPixel => _latPerIx;
    public double LongPerPixel => _longPerIx;
    
    public HeatMap(int size, double minLong, double maxLong, double minLat, double maxLat)
    {
        _maxLat = maxLat;
        _minLat = minLat;
        _maxLong = maxLong;
        _minLong = minLong;
        _size = size;
        _map = new double[size, size];
        
        _latPerIx = (maxLat - minLat) / _size;
        _longPerIx = (maxLong - minLong) / _size;
        _meterPerLatIx = Helper.DistanceBetweenPointDouble(minLat, (minLong + maxLong) / 2, maxLat, (minLong + maxLong) / 2) / _size;
        _meterPerLongIx = Helper.DistanceBetweenPointDouble((minLat + maxLat) / 2, minLong, (minLat + maxLat) / 2, maxLong) / _size;
    }

    public void AddHotspot(Hotspot h)
    {
        AddHotspotWithFactor(h, false);
    }

    public void RemoveHotspot(Hotspot h)
    {
        AddHotspotWithFactor(h, true);
    }
    
    private void AddHotspotWithFactor(Hotspot h, bool neg)
    {
        var centerLatIx = (int)((h.Latitude - _minLat) / (_maxLat - _minLat) * _size);
        var centerLongIx = (int)((h.Longitude - _minLong) / (_maxLong - _minLong) * _size);
        var latSize = (int)(h.Spread / _meterPerLatIx);
        var longSize = (int)(h.Spread / _meterPerLongIx);

        var startX = Math.Max(0, centerLatIx - latSize);
        var startY = Math.Max(0, centerLongIx - longSize);
        var stopX = Math.Min(_size - 1, centerLatIx + latSize);
        var stopY = Math.Min(_size - 1, centerLongIx + longSize);
        
        for (int y = startY; y < stopY; y++)
        for (int x = startX; x < stopX; x++)
        {
            var latDist = (x - centerLatIx) * _meterPerLatIx;
            var longDist = (y - centerLongIx) * _meterPerLongIx;
            var dist = (int)(Math.Sqrt(latDist * latDist + longDist * longDist));
            var footFall = GetFootFall(h, dist);
            if (footFall < 0)
                continue;
            
            if(!neg)
                _map[x, y] += footFall;
            else
                _map[x, y] -= footFall;
        }
    }

    public (double lat, double lon, double points) GetMaxPos()
    {
        int maxLatIx = -1;
        int maxLongIx = -1;
        double maxPoints = double.MinValue;
        
        for (int y = 0; y < _size; y++)
        for (int x = 0; x < _size; x++)
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
    
    private static double GetFootFall(Hotspot hs, int distanceInMeters)
    {
        double val = hs.Footfall * (1 - (distanceInMeters / hs.Spread));
        return val / 10;
    }
    
    public void SaveAsBitmap(string fileName)
    {
        var bitmap = new Bitmap(_size, _size);
        double min = 10000;
        double max = -1;
        for (int y = 0; y < _size; y++)
        for (int x = 0; x < _size; x++)
        {
            min = Math.Min(min, _map[y, x]);
            max = Math.Max(max, _map[y, x]);
        }

        for (int y = 0; y < _size; y++)
        for (int x = 0; x < _size; x++)
        {
            if (_map[y, x] > 0)
            {
                var val = (_map[y, x] - min) / (max - min);
                int rgb = (int)(val * 255);
                bitmap.SetPixel(x, _size - y - 1, Color.FromArgb(rgb, rgb, rgb));
            }
            else
            {
                bitmap.SetPixel(x, _size - y - 1, Color.FromArgb(255, 0, 0));
            }
        }

        bitmap.Save(fileName);
    }
}
