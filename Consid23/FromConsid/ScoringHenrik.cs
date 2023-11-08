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
            Locations = new(),
            GameScore = new()
        };
        Dictionary<string, StoreLocationScoring> locationListNoRefillStation = new();
        foreach (KeyValuePair<string, StoreLocation> kvp in _mapEntity.locations)
        {
            if (solution.Locations.TryGetValue(kvp.Key, out var loc))
            {
                var storeLocationScoring = new StoreLocationScoring()
                {
                    LocationName = kvp.Value.LocationName,
                    LocationType = kvp.Value.LocationType,
                    Latitude = kvp.Value.Latitude,
                    Longitude = kvp.Value.Longitude,
                    Footfall = kvp.Value.Footfall,
                    Freestyle3100Count = loc.Freestyle3100Count,
                    Freestyle9100Count = loc.Freestyle9100Count,

                    SalesVolume = kvp.Value.SalesVolume * _generalData.RefillSalesFactor,

                    SalesCapacity = loc.Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek +
                                    loc.Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek,

                    LeasingCost = loc.Freestyle3100Count * _generalData.Freestyle3100Data.LeasingCostPerWeek +
                                  loc.Freestyle9100Count * _generalData.Freestyle9100Data.LeasingCostPerWeek
                };
                scored.Locations[kvp.Key] = storeLocationScoring;

                if (storeLocationScoring.SalesCapacity > 0 == false)
                {
                    throw new Exception($"You are not allowed to submit locations with no refill stations. Remove or alter location : {kvp.Value.LocationName}");
                }
            }
            else
                locationListNoRefillStation[kvp.Key] = new()
                {
                    LocationName = kvp.Value.LocationName,
                    LocationType = kvp.Value.LocationType,
                    Latitude = kvp.Value.Latitude,
                    Longitude = kvp.Value.Longitude,
                    SalesVolume = kvp.Value.SalesVolume * _generalData.RefillSalesFactor,
                };
        }

        if (scored.Locations.Count == 0)
        {
            scored.GameScore.Total = int.MinValue;
            return scored;
        }
        scored.Locations = DistributeSales(scored.Locations, locationListNoRefillStation);

        foreach (KeyValuePair<string, StoreLocationScoring> kvp in scored.Locations)
        {
            kvp.Value.SalesVolume = Math.Round(kvp.Value.SalesVolume, 0);

            double sales = kvp.Value.SalesVolume;
            if (kvp.Value.SalesCapacity < kvp.Value.SalesVolume) { sales = kvp.Value.SalesCapacity; }

            kvp.Value.GramCo2Savings = sales * (_generalData.ClassicUnitData.Co2PerUnitInGrams - _generalData.RefillUnitData.Co2PerUnitInGrams);
            scored.GameScore.KgCo2Savings += kvp.Value.GramCo2Savings / 1000;
            if (kvp.Value.GramCo2Savings > 0)
            {
                kvp.Value.IsCo2Saving = true;
            }

            kvp.Value.Revenue = sales * _generalData.RefillUnitData.ProfitPerUnit;
            scored.TotalRevenue += kvp.Value.Revenue;

            kvp.Value.Earnings = kvp.Value.Revenue - kvp.Value.LeasingCost;
            if (kvp.Value.Earnings > 0)
            {
                kvp.Value.IsProfitable = true;
            }

            scored.TotalLeasingCost += kvp.Value.LeasingCost;

            scored.TotalFreestyle3100Count += kvp.Value.Freestyle3100Count;
            scored.TotalFreestyle9100Count += kvp.Value.Freestyle9100Count;

            scored.GameScore.TotalFootfall += kvp.Value.Footfall;
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

    private Dictionary<string, StoreLocationScoring> DistributeSales(Dictionary<string, StoreLocationScoring> with, Dictionary<string, StoreLocationScoring> without)
    {
        foreach (KeyValuePair<string, StoreLocationScoring> kvpWithout in without)
        {
            Dictionary<string, double> distributeSalesTo = new();

            foreach (var neighbour in _neighbours[kvpWithout.Key])
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
                                                 _generalData.RefillDistributionRate * kvpWithout.Value.SalesVolume;//locationSalesFrom;
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