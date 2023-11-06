﻿using Considition2023_Cs;

namespace Consid23;

public class HenrikSolver1
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public HenrikSolver1(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public SubmitSolution CalcSolution()
    {
        var scorer = new Scoring();
        
        SubmitSolution sol = new SubmitSolution
        {
            Locations = new()
        };

        foreach (var loc in _mapData.locations)
        {
            while (true)
            {
                AddOneAt(sol, loc.Key);

                var score = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

                if (score.Locations[loc.Key].SalesCapacity > score.Locations[loc.Key].SalesVolume)
                    break;
            }
        }

        // Does not help :(
        TryPlusOneAndMinusThreeOnNeighbours(scorer, ref sol);
        
        // Try -1 and +1 on all (one at a time)
        while (true)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            RemoveOneFromAll(scorer, sol);
            AddOneForAll(scorer, sol);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (ScoreDiff(postScore, preScore) <= 0)
                break;
        }

        // Does not help :(
        while (true)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            TryPlusOneAndMinusTwoOnNeighbours(scorer, ref sol);
            TryMinusOneAndPlusTwoOnNeighbours(scorer, sol);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            if (ScoreDiff(postScore, preScore) <= 0)
                break;
        }
        
        return sol;
    }

    private void TryMinusOneAndPlusTwoOnNeighbours(Scoring scorer, SubmitSolution sol)
    {
        var bestSore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
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

            var max = solutions.MaxBy(s => s.scoreDiff);
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
        var bestSore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
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

            var max = solutions.MaxBy(s => s.scoreDiff);
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
        var preScore = scorer.CalculateScore(_mapData.MapName, startSol, _mapData, _generalData);
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
        
        var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
        var scoreDiff = ScoreDiff(postScore, preScore);
        return (sol, postScore, scoreDiff);
    }

    private void RemoveOneFromAll(Scoring scorer, SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            RemoveOne(sol, loc);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (ScoreDiff(postScore, preScore) < 0)
                AddOneAt(sol, loc);
        }
    }

    private void AddOneForAll(Scoring scorer, SubmitSolution sol)
    {
        foreach (var loc in _mapData.locations.Keys)
        {
            var preScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            AddOneAt(sol, loc);
            var postScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

            if (ScoreDiff(postScore, preScore) < 0)
                RemoveOne(sol, loc);
        }
    }


    private static void AddOneAt(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            sol.Locations.Add(location, new PlacedLocations
            {
                Freestyle3100Count = 1,
                Freestyle9100Count = 0
            });
        }
        else if (loc.Freestyle3100Count == 0)
        {
            loc.Freestyle3100Count++;
        }
        else if(loc.Freestyle9100Count < 5)
        {
            loc.Freestyle3100Count--;
            loc.Freestyle9100Count++;
        }
        else if (loc.Freestyle9100Count < 5)
        {
            loc.Freestyle3100Count++;
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
            loc.Freestyle3100Count++;
        }

        if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
            sol.Locations.Remove(location);
    }

    private static double ScoreDiff(GameData score1, GameData score2)
    {
        return score1.GameScore!.Total - score2.GameScore!.Total;
    }
}