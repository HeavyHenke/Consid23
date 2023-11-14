using Considition2023_Cs;

namespace Consid23;

public class SandboxClusterHotspotsToLocations
{
    private readonly GeneralData _generalData;

    class Cluster
    {
        private readonly List<Hotspot> _hotspots;
        public double CenterLat;
        public double CenterLong;
        public int Size => _hotspots.Count;

        public Cluster(Hotspot hotspot)
        {
            _hotspots = new List<Hotspot> { hotspot };
            CenterLat = hotspot.Latitude;
            CenterLong = hotspot.Longitude;
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
            
            if(!foundCluster)
                clusters.Add(new Cluster(input.Hotspots[i]));
        }

        clusters = clusters.OrderByDescending(c => c.Size).ToList();
        
        const int maxGroceryStoreLarge = 5;
        const int maxGroceryStore = 20;
        const int maxConvenience = 20;
        const int maxGasStation = 8;
        const int maxKiosk = 3;

        var toPlace = new List<LocationType>();
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStoreLarge"], maxGroceryStoreLarge));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["groceryStore"], maxGroceryStore));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["convenience"], maxConvenience));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["gasStation"], maxGasStation));
        toPlace.AddRange(Enumerable.Repeat(_generalData.LocationTypes["kiosk"], maxKiosk));

        int locationNum = 1;
        for (int i = 0; i < toPlace.Count; i++)
        {
            string locationName = "location" + locationNum++;
            output.locations.Add(locationName,
                new StoreLocation
                {
                    LocationName = locationName,
                    LocationType = toPlace[i].Type,
                    Latitude = clusters[i].CenterLat,
                    Longitude = clusters[i].CenterLong,
                    SalesVolume = toPlace[i].SalesVolume
                });
        }
        
        
        return output;
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