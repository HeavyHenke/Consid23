namespace Considition2023_Cs;

public class ScoringHenrik : IScoring
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapEntity;
    private readonly Dictionary<string, List<(string neighbour, int distance)>> _neighbours = new();

    public ScoringHenrik(GeneralData generalData, MapData mapEntity)
    {
        _generalData = generalData;
        _mapEntity = mapEntity;

        // Calculate all neighbours
        foreach (var loc1 in mapEntity.locations.Values)
        {
            var list = new List<(string neighbour, int distance)>();
            _neighbours.Add(loc1.LocationName, list);
            foreach (var loc2 in mapEntity.locations.Values)
            {
                if (loc2.LocationName == loc1.LocationName)
                    continue;

                var distance = DistanceBetweenPoint(loc1.Latitude, loc1.Longitude, loc2.Latitude, loc2.Longitude);
                if (distance < generalData.WillingnessToTravelInMeters)
                {
                    list.Add((loc2.LocationName, distance));
                }
            }
        }
    }
        
    public GameData CalculateScore(SubmitSolution solution)
    {
        GameData scored = new() 
        {
            MapName = _mapEntity.MapName,
            TeamId = Guid.Empty,
            TeamName = string.Empty,
            Locations = new(_mapEntity.locations.Count),
            GameScore = new()
        };
        
        List<(string, StoreLocationScoring)> locationListNoRefillStation = new();
        foreach (var (key, value) in _mapEntity.locations)
        {
            if (solution.Locations.TryGetValue(key, out var loc))
            {
                var storeLocationScoring = new StoreLocationScoring
                {
                    LocationName = value.LocationName,
                    LocationType = value.LocationType,
                    Latitude = value.Latitude,
                    Longitude = value.Longitude,
                    Footfall = value.Footfall,
                    Freestyle3100Count = loc.Freestyle3100Count,
                    Freestyle9100Count = loc.Freestyle9100Count,

                    SalesVolume = value.SalesVolume * _generalData.RefillSalesFactor,

                    SalesCapacity = loc.Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek +
                                    loc.Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek,

                    LeasingCost = loc.Freestyle3100Count * _generalData.Freestyle3100Data.LeasingCostPerWeek +
                                  loc.Freestyle9100Count * _generalData.Freestyle9100Data.LeasingCostPerWeek
                };
                scored.Locations[key] = storeLocationScoring;

                if (storeLocationScoring.SalesCapacity > 0 == false)
                {
                    throw new Exception($"You are not allowed to submit locations with no refill stations. Remove or alter location : {value.LocationName}");
                }
            }
            else
                locationListNoRefillStation.Add((key, new()
                {
                    LocationName = value.LocationName,
                    LocationType = value.LocationType,
                    Latitude = value.Latitude,
                    Longitude = value.Longitude,
                    SalesVolume = value.SalesVolume * _generalData.RefillSalesFactor,
                }));
        }

        if (scored.Locations.Count == 0)
        {
            scored.GameScore.Total = int.MinValue;
            return scored;
        }
        scored.Locations = DistributeSales(scored.Locations, locationListNoRefillStation);

        foreach (var location in scored.Locations.Values)
        {
            location.SalesVolume = Math.Round(location.SalesVolume, 0);

            double sales = location.SalesVolume;
            if (location.SalesCapacity < location.SalesVolume) { sales = location.SalesCapacity; }

            location.GramCo2Savings = sales * (_generalData.ClassicUnitData.Co2PerUnitInGrams - _generalData.RefillUnitData.Co2PerUnitInGrams);
            scored.GameScore.KgCo2Savings += location.GramCo2Savings / 1000;
            if (location.GramCo2Savings > 0)
            {
                location.IsCo2Saving = true;
            }

            location.Revenue = sales * _generalData.RefillUnitData.ProfitPerUnit;
            scored.TotalRevenue += location.Revenue;

            location.Earnings = location.Revenue - location.LeasingCost;
            if (location.Earnings > 0)
            {
                location.IsProfitable = true;
            }

            scored.TotalLeasingCost += location.LeasingCost;

            scored.TotalFreestyle3100Count += location.Freestyle3100Count;
            scored.TotalFreestyle9100Count += location.Freestyle9100Count;

            scored.GameScore.TotalFootfall += location.Footfall;
        }

        //Just some rounding for nice whole numbers
        scored.TotalRevenue = Math.Round(scored.TotalRevenue, 0);
        scored.GameScore.KgCo2Savings = Math.Round(
            scored.GameScore.KgCo2Savings
            - scored.TotalFreestyle3100Count * _generalData.Freestyle3100Data.StaticCo2 / 1000
            - scored.TotalFreestyle9100Count * _generalData.Freestyle9100Data.StaticCo2 / 1000
            , 0);

        //Calculate Earnings
        scored.GameScore.Earnings = scored.TotalRevenue - scored.TotalLeasingCost;

        //Calculate total score
        scored.GameScore.Total = Math.Round(
            (scored.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek + scored.GameScore.Earnings) *
            (1 + scored.GameScore.TotalFootfall),
            0
        );

        return scored;
    }

    private Dictionary<string, StoreLocationScoring> DistributeSales(Dictionary<string, StoreLocationScoring> with, IEnumerable<(string location, StoreLocationScoring value)> without)
    {
        foreach (var (key, value) in without)
        {
            Dictionary<string, double> distributeSalesTo = new();

            foreach (var neighbour in _neighbours[key])
            {
                if(with.ContainsKey(neighbour.neighbour))
                    distributeSalesTo[neighbour.neighbour] = neighbour.distance;
            }

            double total = 0;
            if (distributeSalesTo.Count > 0)
            {
                foreach (KeyValuePair<string, double> kvp in distributeSalesTo)
                {
                    distributeSalesTo[kvp.Key] = Math.Pow(_generalData.ConstantExpDistributionFunction, _generalData.WillingnessToTravelInMeters - kvp.Value) - 1;
                    total += distributeSalesTo[kvp.Key];
                }

                //Add boosted sales to original sales volume
                foreach (KeyValuePair<string, double> kvp in distributeSalesTo)
                {
                    with[kvp.Key].SalesVolume += distributeSalesTo[kvp.Key] / total *
                                                 _generalData.RefillDistributionRate * value.SalesVolume;//locationSalesFrom;
                }
            }
        }

        return with;
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

        int distance = (int) (r * c + .5);
        return distance;
    }
}