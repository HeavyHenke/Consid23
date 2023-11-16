using Considition2023_Cs;

namespace Consid23;

public class SandboxClusterHotspotsToLocations
{
    private readonly GeneralData _generalData;

    class Cluster
    {
        private readonly List<Hotspot> _hotspots;
        public int Size => _hotspots.Count;
        public string Name { get; }
        public double Importance { get; set; }

        public Cluster(Hotspot hotspot, string name)
        {
            _hotspots = new List<Hotspot> { hotspot };
            Name = name;
        }

        public bool TryAddToCluster(Hotspot h)
        {
            bool foundConnection = (from existing in _hotspots
                    let dist = DistanceBetweenPoint(existing.Latitude, existing.Longitude, h.Latitude, h.Longitude)
                    where dist < existing.Spread
                    select existing)
                .Any();

            if (!foundConnection)
                return false;

            _hotspots.Add(h);
            return true;
        }

        public IEnumerable<(double lat, double lon, double points)> GetLocations()
        {
            const int size = 100;

            var hotSpots = _hotspots.ToList();
            while (hotSpots.Any())
            {
                if (hotSpots.Count == 1 || (hotSpots.Count == 2 && hotSpots.Select(h => (h.Latitude, h.Longitude)).Distinct().Count() == 1))
                {
                    yield return (hotSpots[0].Latitude, hotSpots[0].Longitude, GetFootFall(hotSpots[0], 0));
                    yield break;
                }
                
                var minLat = hotSpots.Select(h => h.Latitude).Min();
                var maxLat = hotSpots.Select(h => h.Latitude).Max();
                var minLong = hotSpots.Select(h => h.Longitude).Min();
                var maxLong = hotSpots.Select(h => h.Longitude).Max();
                var latPerIx = (maxLat - minLat) / size;
                var longPerIx = (maxLong - minLong) / size;
                var meterPerLatIx = DistanceBetweenPointDouble(minLat, (minLong + maxLong) / 2, maxLat, (minLong + maxLong) / 2) / size;
                var meterPerLongIx = DistanceBetweenPointDouble((minLat + maxLat) / 2, minLong, (minLat + maxLat) / 2, maxLong) / size;
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

                            var dist = Math.Sqrt(latDistMeters * latDistMeters + longDistMeters * longDistMeters);
                            if (dist > l.Spread)
                                continue;

                            points[latIx, longIx] += GetFootFall(l, dist);
                        }
                    }
                }

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

                if (maxPointLat >= 0)
                {
                    var lat = minLat + maxPointLat * (maxLat - minLat) / size;
                    var lon = minLong + maxPointLong * (maxLong - minLong) / size;

                    for (int ix = hotSpots.Count - 1; ix >= 0; ix--)
                    {
                        var dist = DistanceBetweenPointDouble(lat, lon, hotSpots[ix].Latitude, hotSpots[ix].Longitude);
                        if (dist < hotSpots[ix].Spread)
                            hotSpots.RemoveAt(ix);
                    }
                    
                    yield return (lat, lon, maxVal);
                }
            }
        }

        private static double GetFootFall(Hotspot hs, double distanceInMeters)
        {
            double val = hs.Footfall * (1 - (distanceInMeters / hs.Spread));
            return val / 10;
        }
    }

    public SandboxClusterHotspotsToLocations(GeneralData generalData)
    {
        _generalData = generalData;
    }
    
    public MapData ClusterHotspots(MapData input)
    {
        var output = input.Clone();
        RemoveHotspotsThatAreTooFarOutsideOfBorder(output);

        var clusters = new List<Cluster> { new Cluster(input.Hotspots[0], "location1") };

        int locationNum = 2;
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
                clusters.Add(new Cluster(input.Hotspots[i], "location" + locationNum++));
        }

        var locations = clusters.SelectMany(c => c.GetLocations())
            .OrderByDescending(c => c.points)
            .ToList();
        
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

        locationNum = 1;
        for (int i = 0; i < toPlace.Count; i++)
        {
            string locationName = "location" + locationNum++;
            var lat = locations[i].lat;
            var lon = locations[i].lon;

            output.locations.Add(locationName,
                new StoreLocation
                {
                    LocationName = locationName,
                    LocationType = toPlace[i].Type,
                    Latitude = lat,
                    Longitude = lon,
                    SalesVolume = toPlace[i].SalesVolume
                });
        }
        
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
    
    public void OptimizeByMovingALittle(SubmitSolution sol, MapData mapData)
    {
        var scoring = new ScoringHenrik(_generalData, mapData);
        foreach (var (name, loc) in sol.Locations)
        {
            // Flytta rakt
            MoveLocation(scoring, mapData.Border, sol, loc, name, 0.001, 0);
            MoveLocation(scoring, mapData.Border, sol, loc, name, -0.001, 0);
            MoveLocation(scoring, mapData.Border, sol, loc, name, 0, 0.001);
            MoveLocation(scoring, mapData.Border, sol, loc, name, 0, -0.001);
            
            // Flytta diagonalt
            MoveLocation(scoring, mapData.Border, sol, loc, name, 0.0001, 0.0001);
            MoveLocation(scoring, mapData.Border, sol, loc, name, -0.0001, -0.0001);
            MoveLocation(scoring, mapData.Border, sol, loc, name, -0.0001, 0.0001);
            MoveLocation(scoring, mapData.Border, sol, loc, name, 0.0001, -0.0001);
        }
    }

    private static void MoveLocation(IScoring scoring, Border border, SubmitSolution sol, PlacedLocations placed, string name, double dlong, double dlat)
    {
        var startScore = scoring.CalculateScore(sol).GameScore.Total;
        // var startScore = Math.Abs(2320.5 - scoring.CalculateScore(sol).GameScore.Total);
        while (true)
        {
            placed.Longitude += dlong;
            placed.Latitude += dlat;
            scoring.UpdateLocationPos(name);

            var score = scoring.CalculateScore(sol).GameScore.Total;
            // var score = Math.Abs(2320.5 - scoring.CalculateScore(sol).GameScore.Total);
            if (score > startScore && 
                placed.Latitude > border.LatitudeMin && placed.Latitude < border.LatitudeMax && 
                placed.Longitude > border.LongitudeMin && placed.Longitude < border.LongitudeMax)
            {
                startScore = score;
                continue;
            }

            placed.Longitude -= dlong;
            placed.Latitude -= dlat;
            scoring.UpdateLocationPos(name);
            return;
        }
    }
    
    private static int DistanceBetweenPoint(double latitude1, double longitude1, double latitude2, double longitude2)
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

    private static double DistanceBetweenPointDouble(double latitude1, double longitude1, double latitude2, double longitude2)
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