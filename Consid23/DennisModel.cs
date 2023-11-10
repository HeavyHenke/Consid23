using System.Diagnostics;

namespace Considition2023_Cs
{
    public class DennisModel
    {
        private readonly GeneralData _generalData;
        private readonly int _numLocations;

        public struct Location
        {
            public required string LocationType { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Footfall { get; set; }
            public int footfallScale { get; set; }
            public double SalesVolume { get; set; }
        }

        public readonly Location[] Locations;

        public readonly List<(int index, double distanceScaleFactor)>[] Neighbours;

        public struct SolutionLocation
        {
            public int Freestyle9100Count;
            public int Freestyle3100Count;
        }

        public SolutionLocation[] CreateSolutionLocations()
        {
            return new SolutionLocation[Locations.Length];
        }

        public readonly Dictionary<string, int> LocationNameToIndex = new Dictionary<string, int>();
        public readonly string[] IndexToLocationName;

        public SolutionLocation[] ConvertFromSubmitSolution(SubmitSolution submitSolution)
        {
            var solutionLocations = CreateSolutionLocations();
            foreach (var location in submitSolution.Locations)
            {
                var i = LocationNameToIndex[location.Key];
                solutionLocations[i].Freestyle3100Count = location.Value.Freestyle3100Count;
                solutionLocations[i].Freestyle9100Count = location.Value.Freestyle9100Count;
            }

            return solutionLocations;
        }

        public SubmitSolution ConvertToSubmitSolution(SolutionLocation[] solutionLocations)
        {
            var sol = new SubmitSolution
            {
                Locations = new()
            };
            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count != 0 || solutionLocations[i].Freestyle9100Count != 0)
                    sol.Locations.Add(IndexToLocationName[i], new PlacedLocations { Freestyle3100Count = solutionLocations[i].Freestyle3100Count, Freestyle9100Count = solutionLocations[i].Freestyle9100Count });
            }

            return sol;
        }

        public DennisModel(GeneralData generalData, MapData mapEntity)
        {
            _generalData = generalData;
            _numLocations = mapEntity.locations.Count;
            Locations = new Location[_numLocations];
            Neighbours = new List<(int index, double distanceScaleFactor)>[_numLocations];
            for (int i = 0; i < Neighbours.Length; i++)
                Neighbours[i] = new List<(int index, double distanceScaleFactor)>();

            IndexToLocationName = new string[_numLocations];

            var index = 0;
            foreach (var location in mapEntity.locations)
            {
                LocationNameToIndex.Add(location.Value.LocationName, index);
                IndexToLocationName[index] = location.Value.LocationName;
                Locations[index].LocationType = location.Value.LocationType;
                Locations[index].Latitude = location.Value.Latitude;
                Locations[index].Longitude = location.Value.Longitude;
                Locations[index].Footfall = location.Value.Footfall;
                Locations[index].footfallScale = location.Value.footfallScale;
                Locations[index].SalesVolume = location.Value.SalesVolume;
                index++;
            }

            CalculateNeighbours();
        }

        private void CalculateNeighbours()
        {
            for (var i = 0; i < _numLocations - 1; i++)
            {
                for (var j = i + 1; j < _numLocations; j++)
                {
                    var distance = Scoring.DistanceBetweenPoint(Locations[i].Latitude, Locations[i].Longitude, Locations[j].Latitude, Locations[j].Longitude);
                    if (distance < _generalData.WillingnessToTravelInMeters)
                    {
                        var distanceScaleFactor = Math.Pow(_generalData.ConstantExpDistributionFunction, _generalData.WillingnessToTravelInMeters - distance) - 1;
                        Neighbours[i].Add(new(j, distanceScaleFactor));
                        Neighbours[j].Add(new(i, distanceScaleFactor));
                    }
                }
            }
        }

        public double CalculateScore(SolutionLocation[] solutionLocations, double[] salesVolume=null)
        {
            if (salesVolume == null)
                salesVolume = new double[_numLocations];
            else
                Array.Clear(salesVolume);
            
            for (var i = 0; i < _numLocations; i++)
                salesVolume[i] = 0;

            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count != 0 || solutionLocations[i].Freestyle9100Count != 0)
                {
                    salesVolume[i] += Locations[i].SalesVolume * _generalData.RefillSalesFactor;
                }
                else
                {
                    if (Neighbours[i].Count == 0)
                        continue;

                    double total = 0;
                    foreach (var neighbour in Neighbours[i])
                    {
                        total += neighbour.distanceScaleFactor;
                    }

                    var currentSalesVolume = Locations[i].SalesVolume * _generalData.RefillSalesFactor;

                    //Add boosted sales to original sales volume
                    foreach (var neighbour in Neighbours[i])
                    {
                        salesVolume[neighbour.index] += neighbour.distanceScaleFactor / total * _generalData.RefillDistributionRate * currentSalesVolume;
                    }
                }
            }

            double kgCo2Savings = 0;
            double totalRevenue = 0;
            double totalLeasingCost = 0;
            int totalFreestyle3100Count = 0;
            int totalFreestyle9100Count = 0;
            double totalFootfall = 0;

            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count == 0 && solutionLocations[i].Freestyle9100Count == 0)
                    continue;

                var salesCapacity = solutionLocations[i].Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek + solutionLocations[i].Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek;
                var sales = Math.Min(Round(salesVolume[i]), salesCapacity);

                kgCo2Savings += sales * (_generalData.ClassicUnitData.Co2PerUnitInGrams - _generalData.RefillUnitData.Co2PerUnitInGrams) / 1000;

                totalRevenue += sales * _generalData.RefillUnitData.ProfitPerUnit;
                totalLeasingCost += solutionLocations[i].Freestyle3100Count * _generalData.Freestyle3100Data.LeasingCostPerWeek + solutionLocations[i].Freestyle9100Count * _generalData.Freestyle9100Data.LeasingCostPerWeek;

                totalFreestyle3100Count += solutionLocations[i].Freestyle3100Count;
                totalFreestyle9100Count += solutionLocations[i].Freestyle9100Count;

                totalFootfall += Locations[i].Footfall;
            }

            //Just some rounding for nice whole numbers
            totalRevenue = Round(totalRevenue);
            kgCo2Savings = Round(kgCo2Savings - totalFreestyle3100Count * _generalData.Freestyle3100Data.StaticCo2 / 1000 - totalFreestyle9100Count * _generalData.Freestyle9100Data.StaticCo2 / 1000);

            //Calculate Earnings
            var earnings = totalRevenue - totalLeasingCost;

            //Calculate total score
            return Round((kgCo2Savings * _generalData.Co2PricePerKiloInSek + earnings) * (1 + totalFootfall));
        }

        long Round(double d)
        {
            return (long)(d+.5);
        }
    }
}