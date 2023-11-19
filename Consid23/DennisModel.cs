using System.Diagnostics;

namespace Considition2023_Cs
{

    public class LocationConfiguration
    {
        [DebuggerDisplay("{Capacity}: {LeasingCost}   {StaticCo2}")]
        class Configuration
        {
            public double Capacity;
            public double LeasingCost;
            public double StaticCo2;
            public byte Freestyle3100Count;
            public byte Freestyle9100Count;
        }
        private readonly int _numLocations;

        public byte[] Freestyle3100Count;
        public byte[] Freestyle9100Count;
        public double[] Capacity;
        public double[] LeasingCost;
        public double[] StaticCo2;
        
        public LocationConfiguration(GeneralData generalData)
        {
            List<Configuration> configurations = new List<Configuration>();
            for (int i = 0; i <= 2; i++)
                for (int j = 0; j <= 2; j++)
                {
                    var configuration = new Configuration();
                    configuration.Freestyle3100Count = (byte)i;
                    configuration.Freestyle9100Count = (byte)j;
                    configuration.Capacity = i * generalData.Freestyle3100Data.RefillCapacityPerWeek + j * generalData.Freestyle9100Data.RefillCapacityPerWeek;
                    configuration.StaticCo2 = i * generalData.Freestyle3100Data.StaticCo2 / 1000 + j * generalData.Freestyle9100Data.StaticCo2 / 1000;
                    configuration.LeasingCost = i * generalData.Freestyle3100Data.LeasingCostPerWeek + j * generalData.Freestyle9100Data.LeasingCostPerWeek;
                    configurations.Add(configuration);
                }

//            configurations=configurations.OrderBy(p =>p.LeasingCost).ThenBy((p=>p.Capacity)).ToList();
            configurations = configurations.OrderBy(p => p.StaticCo2).ThenBy((p => p.Capacity)).ToList();
            Freestyle3100Count = configurations.Select(c => c.Freestyle3100Count).ToArray();
            Freestyle9100Count = configurations.Select(c => c.Freestyle9100Count).ToArray();
            Capacity = configurations.Select(c => c.Capacity).ToArray();
            LeasingCost = configurations.Select(c => c.LeasingCost).ToArray();
            StaticCo2 = configurations.Select(c => c.StaticCo2).ToArray();
            _numLocations = configurations.Capacity;
        }

        public byte IndexFromDouble(double d)
        {
//            int index = (int)(d - .2) * _numLocations;
            int index = (int)(d * _numLocations);
            if (index < 0)
                index = 0;
            if (index >= _numLocations)
                index = _numLocations - 1;
            return (byte)index;
        }
    }
    public class DennisModel
    {
        private readonly GeneralData _generalData;
        private readonly MapData _mapEntity;
        private int _numLocations;

        public struct Location
        {
            public required string LocationType { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Footfall { get; set; }
            public int footfallScale { get; set; }
            public double ScaledSalesVolume { get; set; }
        }

        public Location[] Locations;

        public List<(int index, double distanceScaleFactor)>[] Neighbours;

        [DebuggerDisplay("{Freestyle9100Count} + {Freestyle3100Count}")]
        public struct SolutionLocation
        {
            public byte Freestyle9100Count;
            public byte Freestyle3100Count;
        }

        public SolutionLocation[] CreateSolutionLocations()
        {
            return new SolutionLocation[Locations.Length];
        }

        public readonly Dictionary<string, int> LocationNameToIndex = new Dictionary<string, int>();
        public string[] IndexToLocationName;
        

        public SolutionLocation[] ConvertFromSubmitSolution(SubmitSolution submitSolution)
        {
            var solutionLocations = CreateSolutionLocations();
            foreach (var location in submitSolution.Locations)
            {
                var i = LocationNameToIndex[location.Key];
                solutionLocations[i].Freestyle3100Count = (byte)location.Value.Freestyle3100Count;
                solutionLocations[i].Freestyle9100Count = (byte)location.Value.Freestyle9100Count;
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
                    sol.Locations.Add(IndexToLocationName[i], new PlacedLocations { Freestyle3100Count = solutionLocations[i].Freestyle3100Count, Freestyle9100Count = solutionLocations[i].Freestyle9100Count, LocationType = Locations[i].LocationType, Latitude = Locations[i].Latitude, Longitude = Locations[i].Longitude});
            }

            return sol;
        }

        public DennisModel(GeneralData generalData, MapData mapEntity)
        {
            _generalData = generalData;
            _mapEntity = mapEntity;
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
                Locations[index].ScaledSalesVolume = location.Value.SalesVolume * _generalData.RefillSalesFactor;
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
                        Neighbours[i].Add((j, distanceScaleFactor));
                        Neighbours[j].Add((i, distanceScaleFactor));
                    }
                }
            }
        }

        public SolutionLocation[] InitiateSandboxLocations(SubmitSolution submitSolution)
        {
            _numLocations = submitSolution.Locations.Count;
            Locations = new Location[_numLocations];
            Neighbours = new List<(int index, double distanceScaleFactor)>[_numLocations];
            for (int i = 0; i < Neighbours.Length; i++)
                Neighbours[i] = new List<(int index, double distanceScaleFactor)>();

            IndexToLocationName = new string[_numLocations];
            LocationNameToIndex.Clear();
            var solutionLocations = new SolutionLocation[submitSolution.Locations.Count];

            var index = 0;
            foreach (var location in submitSolution.Locations)
            {
                LocationNameToIndex.Add(location.Key, index);
                IndexToLocationName[index] = location.Key;
                Locations[index].LocationType = location.Value.LocationType;
                Locations[index].Latitude = location.Value.Latitude;
                Locations[index].Longitude = location.Value.Longitude;
                
                foreach (Hotspot hotspot in _mapEntity.Hotspots)
                {
                    double distanceInMeters = Scoring.DistanceBetweenPoint(hotspot.Latitude, hotspot.Longitude, Locations[index].Latitude, Locations[index].Longitude);
                    double maxSpread = hotspot.Spread;
                    if (distanceInMeters <= maxSpread)
                    {
                        double val = hotspot.Footfall * (1 - (distanceInMeters / maxSpread));
                        Locations[index].Footfall += val / 10;
                    }
                }
                
                Locations[index].footfallScale = 1;// location.Value.footfallScale;
                Locations[index].ScaledSalesVolume = Scoring.GetSalesVolume(location.Value.LocationType, _generalData);
                solutionLocations[index].Freestyle3100Count = (byte)location.Value.Freestyle3100Count;
                solutionLocations[index].Freestyle9100Count = (byte)location.Value.Freestyle9100Count;
                index++;
            }

            CalculateNeighbours();
            return solutionLocations;
        }
        public double CalculateScore(SolutionLocation[] solutionLocations, double[]? salesVolume = null,GameData gameData=null)
        {
            if (salesVolume == null)
                salesVolume = new double[_numLocations];
            else
                Array.Clear(salesVolume);
            
            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count != 0 || solutionLocations[i].Freestyle9100Count != 0)
                {
                    salesVolume[i] += Locations[i].ScaledSalesVolume;
                }
                else
                {
                    if (Neighbours[i].Count == 0)
                        continue;

                    double total = 0;
                    foreach (var neighbour in Neighbours[i])
                    {
                        if(solutionLocations[neighbour.index].Freestyle3100Count != 0 || solutionLocations[neighbour.index].Freestyle9100Count != 0)
                            total += neighbour.distanceScaleFactor;
                    }

                    var currentScaledSalesVolume = Locations[i].ScaledSalesVolume;

                    //Add boosted sales to original sales volume
                    foreach (var neighbour in Neighbours[i])
                    {
                        if(solutionLocations[neighbour.index].Freestyle3100Count != 0 || solutionLocations[neighbour.index].Freestyle9100Count != 0)
                            salesVolume[neighbour.index] += neighbour.distanceScaleFactor / total * _generalData.RefillDistributionRate * currentScaledSalesVolume;
                    }
                }
            }

            int totalFreestyle3100Count = 0;
            int totalFreestyle9100Count = 0;
            double totalFootfall = 0;
            double totalSales = 0;
            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count == 0 && solutionLocations[i].Freestyle9100Count == 0)
                    continue;

                var salesCapacity = solutionLocations[i].Freestyle3100Count * _generalData.Freestyle3100Data.RefillCapacityPerWeek + solutionLocations[i].Freestyle9100Count * _generalData.Freestyle9100Data.RefillCapacityPerWeek;
                var sales = Math.Min(Round(salesVolume[i]), salesCapacity);
//                Trace.WriteLine($"Location: {IndexToLocationName[i]} sales {salesVolume[i]}");
                totalSales += sales;

                totalFreestyle3100Count += solutionLocations[i].Freestyle3100Count;
                totalFreestyle9100Count += solutionLocations[i].Freestyle9100Count;
            }

            var totalLeasingCost = totalFreestyle3100Count * _generalData.Freestyle3100Data.LeasingCostPerWeek + totalFreestyle9100Count * _generalData.Freestyle9100Data.LeasingCostPerWeek;
            var kgCo2Savings = totalSales * (_generalData.ClassicUnitData.Co2PerUnitInGrams - _generalData.RefillUnitData.Co2PerUnitInGrams) / 1000;
            var totalRevenue = Math.Round(totalSales * _generalData.RefillUnitData.ProfitPerUnit,2);
            totalFootfall = Math.Round(CalculateFootfall(solutionLocations) / 1000,4);
            
            //Just some rounding for nice whole numbers
            kgCo2Savings = Math.Round(kgCo2Savings - totalFreestyle3100Count * _generalData.Freestyle3100Data.StaticCo2 / 1000 - totalFreestyle9100Count * _generalData.Freestyle9100Data.StaticCo2 / 1000,2);

            //Calculate Earnings
            var earnings = (totalRevenue - totalLeasingCost)/1000;

            //Calculate total score
            var totalScore=Math.Round((kgCo2Savings * _generalData.Co2PricePerKiloInSek + earnings) * (1 + totalFootfall),2);
            return totalScore;
        }

        private double CalculateFootfall(SolutionLocation[] solutionLocations)
        {
            var totalFootfall = 0.0;
            for (var i = 0; i < _numLocations; i++)
            {
                if (solutionLocations[i].Freestyle3100Count == 0 && solutionLocations[i].Freestyle9100Count == 0)
                    continue;

                var count = 1;
                for (int j = 0; j < Neighbours[i].Count; j++)
                {
                    if (solutionLocations[Neighbours[i][j].index].Freestyle3100Count == 0 && solutionLocations[Neighbours[i][j].index].Freestyle9100Count == 0)
                        continue;
                    count++;
                }

                totalFootfall += Locations[i].Footfall / count;
            }

            return totalFootfall;
        }

        private static long Round(double d)
        {
            return (long)(d + .5);
        }
    }
}