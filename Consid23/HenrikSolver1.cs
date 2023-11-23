using Considition2023_Cs;

namespace Consid23;

public class HenrikSolver1
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;
    private readonly IScoring _scorer;
    private readonly ISolutionSubmitter _solutionSubmitter;

    public HenrikSolver1(GeneralData generalData, MapData mapData, ISolutionSubmitter solutionSubmitter)
    {
        _mapData = mapData;
        _generalData = generalData;
        _scorer = new ScoringHenrik(_generalData, _mapData);
        _solutionSubmitter = solutionSubmitter;
    }

    public SubmitSolution CalcSolution()
    {
        var sol = CreateStartPointByAddOneAt();
        //_solutionSubmitter.AddSolutionToSubmit(CreateStartPointBySalesVolume());
        _solutionSubmitter.AddSolutionToSubmit(sol, _scorer.CalculateScore(sol).GameScore.Total);
        return CalcSolution(sol);
    }
    
    public SubmitSolution CalcSolution(SubmitSolution sol)
    {
        // Does not help :(
        //TryPlusOneAndMinusThreeOnNeighbours(scorer, ref sol);
        
        // Try -1 and +1 on all (one at a time)
        while (true)
        {
            var preScore = _scorer.CalculateScore(sol);
            RemoveOneFromAll(sol);
            _solutionSubmitter.AddSolutionToSubmit(sol, _scorer.CalculateScore(sol).GameScore.Total);
            AddOneForAll(sol);
            _solutionSubmitter.AddSolutionToSubmit(sol, _scorer.CalculateScore(sol).GameScore.Total);
            var postScore = _scorer.CalculateScore(sol);

            if (ScoreDiff(postScore, preScore) <= 0)
                break;
        }

        // Does not help :(
        while (true)
        {
            var preScore = _scorer.CalculateScore(sol);
            TryPlusOneAndMinusTwoOnNeighbours(ref sol);
            _solutionSubmitter.AddSolutionToSubmit(sol, _scorer.CalculateScore(sol).GameScore.Total);
            TryMinusOneAndPlusTwoOnNeighbours(ref sol);
            _solutionSubmitter.AddSolutionToSubmit(sol, _scorer.CalculateScore(sol).GameScore.Total);
            var postScore = _scorer.CalculateScore(sol);
            if (ScoreDiff(postScore, preScore) <= 0)
                break;
        }
        
        return sol;
    }

    private SubmitSolution CreateStartPointBySalesVolume()
    {
        SubmitSolution sol = new SubmitSolution
        {
            Locations = new()
        };
        
        int smallOnesPerBig = (int)(_generalData.Freestyle9100Data.RefillCapacityPerWeek / _generalData.Freestyle3100Data.RefillCapacityPerWeek); 
        foreach (var loc in _mapData.locations)
        {
            var bigOnes = (int) (loc.Value.SalesVolume / _generalData.Freestyle9100Data.RefillCapacityPerWeek);
            var smallOnes = (int)((loc.Value.SalesVolume - bigOnes * _generalData.Freestyle9100Data.RefillCapacityPerWeek) / _generalData.Freestyle3100Data.RefillCapacityPerWeek);

            while (smallOnes > 2)
            {
                smallOnes -= smallOnesPerBig;
                bigOnes++;
            }
            
            bigOnes = Math.Max(0, bigOnes);
            smallOnes = Math.Max(0, smallOnes);
            if (smallOnes > 0 || bigOnes > 0)
            {
                sol.Locations.Add(loc.Key, new PlacedLocations { Freestyle3100Count = smallOnes, Freestyle9100Count = bigOnes });
            }
        }

        return sol;
    }

    public SubmitSolution CreateStartPointByAddOneAt()
    {
        var sol = new SubmitSolution
        {
            Locations = new()
        };

        foreach (var loc in _mapData.locations)
        {
            while (true)
            {
                AddOneAt(sol, loc.Key);

                var score = _scorer.CalculateScore(sol);

                if (score.Locations[loc.Key].SalesCapacity > score.Locations[loc.Key].SalesVolume)
                    break;
            }
        }

        return sol;
    }
    
    private void TryMinusOneAndPlusTwoOnNeighbours(ref SubmitSolution sol)
    {
        var bestSore = _scorer.CalculateScore(sol);
        foreach (var (loc, locData) in _mapData.locations)
        {
            var s = sol;
            var neighbours = _mapData.locations.Values
                .Where(l => Scoring.DistanceBetweenPoint(locData.Latitude, locData.Longitude, l.Latitude, l.Longitude) <= _generalData.WillingnessToTravelInMeters)
                .Where(l => s.Locations.ContainsKey(l.LocationName))
                .ToList();

            var solutions = new List<(SubmitSolution sol, GameData score, double scoreDiff)>();
            solutions.Add((sol, bestSore, 0));

            for (int i = 0; i < neighbours.Count; i++)
            {
                for (int j = i; j < neighbours.Count; j++)
                {
                    var scoreWhenChanged = ScoreWhenChanged(sol, (loc, false), (neighbours[i].LocationName, true), (neighbours[j].LocationName, true));
                    if(scoreWhenChanged != default)
                        solutions.Add(scoreWhenChanged);
                }
            }

            var max = solutions.MaxBy(q => q.scoreDiff);
            if (max.scoreDiff > 0)
            {
                // Console.WriteLine("Successful move with diff2: " + max.scoreDiff);
                sol = max.sol;
                bestSore = max.score;
            }
        }
    }

    private void TryPlusOneAndMinusThreeOnNeighbours(ref SubmitSolution sol)
    {
        foreach (var (loc, locData) in _mapData.locations)
        {
            var s = sol;
            var neighbours = _mapData.locations.Values
                .Where(l => Scoring.DistanceBetweenPoint(locData.Latitude, locData.Longitude, l.Latitude, l.Longitude) <= _generalData.WillingnessToTravelInMeters)
                .Where(l => s.Locations.ContainsKey(l.LocationName))
                .ToList();

            var solutions = new List<(SubmitSolution sol, GameData score, double scoreDiff)>();

            for (int i = 0; i < neighbours.Count; i++)
            {
                for (int j = i; j < neighbours.Count; j++)
                {
                    for (int k = j; k < neighbours.Count; k++)
                    {
                        var scoreWhenChanged = ScoreWhenChanged(sol, (loc, true), (neighbours[i].LocationName, false), (neighbours[j].LocationName, false), (neighbours[k].LocationName, false));
                        if(scoreWhenChanged != default)
                            solutions.Add(scoreWhenChanged);
                    }
                }
            }

            var max = solutions.Where(q => q.scoreDiff != 0).OrderByDescending(q => q.scoreDiff).FirstOrDefault();
            if (max != default && max.scoreDiff > 0)
            {
                // Console.WriteLine("Successful move with diff3: " + max.scoreDiff);
                sol = max.sol;
            }
        }
    }
    private void TryPlusOneAndMinusTwoOnNeighbours(ref SubmitSolution sol)
    {
        var bestSore = _scorer.CalculateScore(sol);
        foreach (var (loc, locData) in _mapData.locations)
        {
            var s = sol;
            var neighbours = _mapData.locations.Values
                .Where(l => Scoring.DistanceBetweenPoint(locData.Latitude, locData.Longitude, l.Latitude, l.Longitude) <= _generalData.WillingnessToTravelInMeters)
                .Where(l => s.Locations.ContainsKey(l.LocationName))
                .ToList();

            var solutions = new List<(SubmitSolution sol, GameData score, double scoreDiff)>();
            solutions.Add((sol, bestSore, 0));

            for (int i = 0; i < neighbours.Count; i++)
            {
                for (int j = i; j < neighbours.Count; j++)
                {
                    var scoreWhenChanged = ScoreWhenChanged(sol, (loc, true), (neighbours[i].LocationName, false), (neighbours[j].LocationName, false));
                    if(scoreWhenChanged != default)
                        solutions.Add(scoreWhenChanged);
                }
            }

            var max = solutions.MaxBy(q => q.scoreDiff);
            if (max.scoreDiff > 0)
            {
                // Console.WriteLine("Successful move with diff: " + max.scoreDiff);
                sol = max.sol;
                bestSore = max.score;
            }
        }
    }
    
    private (SubmitSolution sol, GameData score, double scoreDiff) ScoreWhenChanged(SubmitSolution startSol, params (string loc, bool add)[] changes)
    {
        var preScore = _scorer.CalculateScore(startSol);
        var sol = startSol.Clone();
        foreach (var (loc, add) in changes)
        {
            if (add)
            {
                AddOneAt(sol, loc);
            }
            else if (sol.Locations.ContainsKey(loc))
            {
                RemoveOne(sol, loc);
            }
            else
            {
                return default;
            }
        }
        
        var postScore = _scorer.CalculateScore(sol);
        var scoreDiff = ScoreDiff(postScore, preScore);
        return (sol, postScore, scoreDiff);
    }

    private void RemoveOneFromAll(SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = _scorer.CalculateScore(sol);
            RemoveOne(sol, loc);
            var postScore = _scorer.CalculateScore(sol);

            if (ScoreDiff(postScore, preScore) < 0)
                AddOneAt(sol, loc);
        }
    }

    private void AddOneForAll(SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = _scorer.CalculateScore(sol);
            AddOneAt(sol, loc);
            var postScore = _scorer.CalculateScore(sol);

            if (ScoreDiff(postScore, preScore) < 0)
                RemoveOne(sol, loc);
        }
    }


    private void AddOneAt(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            sol.Locations.Add(location, new PlacedLocations
            {
                Freestyle3100Count = 1,
                Freestyle9100Count = 0,
                Longitude = _mapData.locations[location].Longitude,
                Latitude = _mapData.locations[location].Latitude,
                LocationType = _mapData.locations[location].LocationType
            });
        }
        else if (loc.Freestyle3100Count < 2)
        {
            loc.Freestyle3100Count++;
        }
        else if(loc.Freestyle9100Count < 2)
        {
            loc.Freestyle3100Count -= 2;
            loc.Freestyle9100Count++;
        }
    }

    private static void RemoveOne(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            return;
        }
        
        if (loc.Freestyle3100Count > 0)
        {
            loc.Freestyle3100Count--;
        }
        else if(loc.Freestyle9100Count > 0)
        {
            loc.Freestyle9100Count--;
            loc.Freestyle3100Count += 2;
        }

        // if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
        //     sol.Locations.Remove(location);
    }

    private static double ScoreDiff(GameData score1, GameData score2)
    {
        return score1.GameScore!.Total - score2.GameScore!.Total;
    }
}