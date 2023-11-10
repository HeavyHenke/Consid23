using System.Diagnostics;
using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisSolver1
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;
    private readonly IScoring _scorer;
    private readonly DennisModel _model;

    public HenrikDennisSolver1(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
        _scorer = new ScoringHenrik(_generalData, _mapData);
        _model = new DennisModel(_generalData, _mapData);
    }

    public SubmitSolution CalcSolution()
    {
        var sol = CreateStartPointByAddOneAt();

        // Does not help :(
        //TryPlusOneAndMinusThreeOnNeighbours(scorer, ref sol);

        // Try -1 and +1 on all (one at a time)
        while (true)
        {
            var preScore = _model.CalculateScore(sol);
            RemoveOneFromAll(sol);
            AddOneForAll(sol);
            var postScore = _model.CalculateScore(sol);

            if (postScore - preScore <= 0)
                break;
        }

        // Does not help :(
        // while (true)
        // {
        //     var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
        //     TryPlusOneAndMinusTwoOnNeighbours(scorer, ref sol);
        //     TryMinusOneAndPlusTwoOnNeighbours(scorer, sol);
        //     var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
        //     if (ScoreDiff(postScore, preScore) <= 0)
        //         break;
        // }

        return _model.ConvertToSubmitSolution(sol);
    }

/*    private SubmitSolution CreateStartPointBySalesVolume()
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
    }*/

    private DennisModel.SolutionLocation[] CreateStartPointByAddOneAt()
    {
        var initialSolutionLocations = _model.CreateSolutionLocations();

        for (var i = 0; i < initialSolutionLocations.Length; i++)
        {
            var solutionLocations = _model.CreateSolutionLocations();
            solutionLocations[i].Freestyle9100Count = 1;
            _model.CalculateScore(solutionLocations);
            // Trasig implementation...
            initialSolutionLocations[i].Freestyle9100Count = (int)(solutionLocations[i].MaxSalesVolume / _generalData.Freestyle3100Data.RefillCapacityPerWeek + .5);
            if (initialSolutionLocations[i].Freestyle9100Count == 0)
                initialSolutionLocations[i].Freestyle3100Count = 1;
        }

        var sol = _model.ConvertToSubmitSolution(initialSolutionLocations);
        return initialSolutionLocations;
    }

/*    private void TryMinusOneAndPlusTwoOnNeighbours(Scoring scorer, SubmitSolution sol)
    {
        var bestSore = scorer.CalculateScore(sol);
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
                    var scoreWhenChanged = ScoreWhenChanged(scorer, sol, (loc, false), (neighbours[i].LocationName, true), (neighbours[j].LocationName, true));
                    if(scoreWhenChanged != default)
                        solutions.Add(scoreWhenChanged);
                }
            }

            var max = solutions.MaxBy(q => q.scoreDiff);
            if (max.scoreDiff > 0)
            {
                Console.WriteLine("Successful move with diff2: " + max.scoreDiff);
                sol = max.sol;
                bestSore = max.score;
            }
        }
    }

    private void TryPlusOneAndMinusThreeOnNeighbours(Scoring scorer, ref SubmitSolution sol)
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
                        var scoreWhenChanged = ScoreWhenChanged(scorer, sol, (loc, true), (neighbours[i].LocationName, false), (neighbours[j].LocationName, false), (neighbours[k].LocationName, false));
                        if(scoreWhenChanged != default)
                            solutions.Add(scoreWhenChanged);
                    }
                }
            }

            var max = solutions.Where(q => q.scoreDiff != 0).OrderByDescending(q => q.scoreDiff).FirstOrDefault();
            if (max != default && max.scoreDiff > 0)
            {
                Console.WriteLine("Successful move with diff3: " + max.scoreDiff);
                sol = max.sol;
            }
        }
    }
    private void TryPlusOneAndMinusTwoOnNeighbours(Scoring scorer, ref SubmitSolution sol)
    {
        var bestSore = scorer.CalculateScore(sol);
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
                    var scoreWhenChanged = ScoreWhenChanged(scorer, sol, (loc, true), (neighbours[i].LocationName, false), (neighbours[j].LocationName, false));
                    if(scoreWhenChanged != default)
                        solutions.Add(scoreWhenChanged);
                }
            }

            var max = solutions.MaxBy(q => q.scoreDiff);
            if (max.scoreDiff > 0)
            {
                Console.WriteLine("Successful move with diff: " + max.scoreDiff);
                sol = max.sol;
                bestSore = max.score;
            }
        }
    }

    private (SubmitSolution sol, GameData score, double scoreDiff) ScoreWhenChanged(Scoring scorer, SubmitSolution startSol, params (string loc, bool add)[] changes)
    {
        var preScore = scorer.CalculateScore(startSol);
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

        var postScore = scorer.CalculateScore(sol);
        var scoreDiff = ScoreDiff(postScore, preScore);
        return (sol, postScore, scoreDiff);
    }*/

    private void RemoveOneFromAll(DennisModel.SolutionLocation[] sol)
    {
        for (var i = 0; i < sol.Length; i++)
        {
            var preScore = _model.CalculateScore(sol);
            RemoveOne(sol, i);
            var postScore = _model.CalculateScore(sol);

            if (postScore - preScore < 0)
                AddOneAt(sol, i);
        }
    }

    private void AddOneForAll(DennisModel.SolutionLocation[] sol)
    {
        for (var i = 0; i < sol.Length; i++)
        {
            var preScore = _model.CalculateScore(sol);
            AddOneAt(sol, i);
            var postScore = _model.CalculateScore(sol);

            if (postScore - preScore < 0)
                RemoveOne(sol, i);
        }
    }


    private static void AddOneAt(DennisModel.SolutionLocation[] sol, int index)
    {
        var loc = sol[index];
        if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
        {
            sol[index].Freestyle3100Count = 1;
        }
        else if (loc.Freestyle3100Count == 0)
        {
            sol[index].Freestyle3100Count++;
        }
        else if (loc.Freestyle9100Count < 5)
        {
            sol[index].Freestyle3100Count--;
            sol[index].Freestyle9100Count++;
        }
        else if (loc.Freestyle9100Count < 5)
        {
            sol[index].Freestyle3100Count++;
        }
    }

    private static void RemoveOne(DennisModel.SolutionLocation[] sol, int index)
    {
        var loc = sol[index];
        if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
        {
            return;
        }

        if (loc.Freestyle3100Count > 0)
        {
            sol[index].Freestyle3100Count--;
        }
        else if (loc.Freestyle9100Count > 0)
        {
            sol[index].Freestyle9100Count--;
            sol[index].Freestyle3100Count++;
        }
    }

    private static double ScoreDiff(GameData score1, GameData score2)
    {
        return score1.GameScore!.Total - score2.GameScore!.Total;
    }
}