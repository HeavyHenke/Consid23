namespace Considition2023_Cs
{
    public interface IScoring
    {
        GameData CalculateScore(SubmitSolution solution);
    }

    public class CompareScoring : IScoring
    {
        private readonly IScoring _scoring1; 
        private readonly IScoring _scoring2; 
        public CompareScoring(GeneralData generalData, MapData mapEntity)
        {
            _scoring1 = new Scoring(generalData, mapEntity);
            _scoring2 = new ScoringHenrik(generalData, mapEntity);
        }

        public GameData CalculateScore(SubmitSolution solution)
        {
            var score1 = _scoring1.CalculateScore(solution);
            var score2 = _scoring2.CalculateScore(solution);
            if (score1.GameScore.Total != score2.GameScore.Total)
                throw new Exception("Olika poäng!");
            return score1;
        }
    }

    public class Scoring : IScoring
    {
        private readonly GeneralData _generalData;
        private readonly MapData _mapEntity;

        public Scoring(GeneralData generalData, MapData mapEntity)
        {
            _generalData = generalData;
            _mapEntity = mapEntity;
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
                if (solution.Locations.ContainsKey(kvp.Key) == true)
                {
                    scored.Locations[kvp.Key] = new()
                    {
                        LocationName = kvp.Value.LocationName,
                        LocationType = kvp.Value.LocationType,
                        Latitude = kvp.Value.Latitude,
                        Longitude = kvp.Value.Longitude,
                        Footfall = kvp.Value.Footfall,
                        Freestyle3100Count = solution.Locations[kvp.Key].Freestyle3100Count,
                        Freestyle9100Count = solution.Locations[kvp.Key].Freestyle9100Count,

                        SalesVolume = kvp.Value.SalesVolume * _generalData.RefillSalesFactor,
                        // await GetSalesVolume(kvp.Value.LocationType) ??
                        //     throw new Exception(string.Format("Location: {0}, have an invalid location type: {1}", kvp.Key, kvp.Value.LocationType)),

                        SalesCapacity = solution.Locations[kvp.Key].Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek +
                            solution.Locations[kvp.Key].Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek,

                        LeasingCost = solution.Locations[kvp.Key].Freestyle3100Count * _generalData.Freestyle3100Data.LeasingCostPerWeek +
                            solution.Locations[kvp.Key].Freestyle9100Count * _generalData.Freestyle9100Data.LeasingCostPerWeek
                    };

                    if (scored.Locations[kvp.Key].SalesCapacity > 0 == false)
                    {
                        throw new Exception(string.Format("You are not allowed to submit locations with no refill stations. Remove or alter location : {0}", kvp.Value.LocationName));
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
                        //await GetSalesVolume(kvp.Value.LocationType) ?? throw new Exception(string.Format("Location: {0}, have an invalid location type: {1}", kvp.Key, kvp.Value.LocationType)),
                    };
            }

            if (scored.Locations.Count == 0)
            {
                scored.GameScore.Total = int.MinValue;
                return scored;
                // throw new Exception(string.Format("No valid locations with refill stations were placed for map: {0}", mapName));
            }
            scored.Locations = DistributeSales(scored.Locations, locationListNoRefillStation, _generalData);

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

        private static Dictionary<string, StoreLocationScoring> DistributeSales(Dictionary<string, StoreLocationScoring> with, Dictionary<string, StoreLocationScoring> without, GeneralData generalData)
        {
            foreach (KeyValuePair<string, StoreLocationScoring> kvpWithout in without)
            {
                Dictionary<string, double> distributeSalesTo = new();
                //double locationSalesFrom = await GetSalesVolume(kvpWithout.Value.LocationType) ?? throw new Exception(string.Format("Location: {0}, have an invalid location type: {1}", kvpWithout.Key, kvpWithout.Value.LocationType));

                foreach (KeyValuePair<string, StoreLocationScoring> kvpWith in with)
                {
                    int distance = DistanceBetweenPoint(
                        kvpWithout.Value.Latitude, kvpWithout.Value.Longitude, kvpWith.Value.Latitude, kvpWith.Value.Longitude
                    );
                    if (distance < generalData.WillingnessToTravelInMeters)
                    {
                        distributeSalesTo[kvpWith.Value.LocationName] = distance;
                    }
                }

                double total = 0;
                if (distributeSalesTo.Count > 0)
                {
                    foreach (KeyValuePair<string, double> kvp in distributeSalesTo)
                    {
                        distributeSalesTo[kvp.Key] = Math.Pow(generalData.ConstantExpDistributionFunction, generalData.WillingnessToTravelInMeters - kvp.Value) - 1;
                        total += distributeSalesTo[kvp.Key];
                    }

                    //Add boosted sales to original sales volume
                    foreach (KeyValuePair<string, double> kvp in distributeSalesTo)
                    {
                        with[kvp.Key].SalesVolume += distributeSalesTo[kvp.Key] / total *
                        generalData.RefillDistributionRate * kvpWithout.Value.SalesVolume;//locationSalesFrom;
                    }
                }
            }

            return with;
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

            int distance = (int) (r * c + .5);
            return distance;
        }
    }
}
