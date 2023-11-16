using Considition2023_Cs;

namespace Consid23;

public class SandboxClusterHotspotsToLocations
{
    private readonly GeneralData _generalData;

    class Cluster
    {
        public readonly List<Hotspot> _hotspots;
        public double CenterLat;
        public double CenterLong;
        public int Size => _hotspots.Count;
        public string Name { get; }
        public double Importance { get; set; }

        public Cluster(Hotspot hotspot, string name)
        {
            _hotspots = new List<Hotspot> { hotspot };
            CenterLat = hotspot.Latitude;
            CenterLong = hotspot.Longitude;
            Name = name;
        }

        public bool TryAddToCluster(Hotspot h)
        {
            var latitude = (CenterLat * _hotspots.Count + h.Latitude) / (_hotspots.Count + 1);
            var longitude = (CenterLong * _hotspots.Count + h.Longitude) / (_hotspots.Count + 1);

            foreach (var existing in _hotspots.Append(h))
            {
                var dist = DistanceBetweenPoint(existing.Latitude, existing.Longitude, latitude, longitude);
                if (dist > existing.Spread)
                    return false;
            }

            _hotspots.Add(h);
            CenterLat = latitude;
            CenterLong = longitude;
            return true;
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

        var scoringLoc = clusters.Select((s, i) => new StoreLocationScoring
        {
            Longitude = s.CenterLong,
            Latitude = s.CenterLat,
            LocationName = s.Name
        }).ToDictionary(key => key.LocationName, val => val);

        new ScoringHenrik(_generalData, output).CalcualteFootfall(scoringLoc);
        foreach (var cluster in clusters)
        {
            cluster.Importance = scoringLoc[cluster.Name].Footfall;
        }
        clusters = clusters.OrderByDescending(c => c.Importance).ToList();
        
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
            var lat = clusters[i].CenterLat;
            var lon = clusters[i].CenterLong;

            if (lat > output.Border.LatitudeMax)
                lat = output.Border.LatitudeMax;
            if (lat < output.Border.LatitudeMin)
                lat = output.Border.LatitudeMin;
            if (lon > output.Border.LongitudeMax)
                lon = output.Border.LongitudeMax;
            if (lon < output.Border.LongitudeMin)
                lon = output.Border.LongitudeMin;
            
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

}