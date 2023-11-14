﻿using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisOptimizer2Gradient
{
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;

    private readonly double[] _salesVolume;
    
    public HenrikDennisOptimizer2Gradient(DennisModel model, ISolutionSubmitter submitter)
    {
        _model = model;
        _solutionSubmitter = submitter;
        _salesVolume = new double[model.Locations.Length];
    }

    public SubmitSolution OptimizeSolution(SubmitSolution submitSolution)
    {
        var sol = _model.ConvertFromSubmitSolution(submitSolution);
        _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));

        var currScore = _model.CalculateScore(sol, _salesVolume);
        while (true)
        {
            // IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> optimizations = new[] { (sol, currScore, "initial") };

            var strategies = new[]
            {
                TryPlusOneAndMinusThreeOnNeighbours,
                RemoveOneFromAll, 
                TryPlusOneAndMinusOneOnNeighbour, 
                TryPlusOneAndMinusTwoOnNeighbours,
                AddOneFromAll, 
            };

            (DennisModel.SolutionLocation[] sol, double score, string optimizationName) latestBest = default;
            var minScore = currScore;
            foreach (var s in strategies)
            {
                var best = s(sol, minScore).LastOrDefault();
                if (best != default)
                {
                    minScore = best.score;
                    latestBest = best;
                }
            }

            if (latestBest != default)
            {
                Console.WriteLine($"Optimized using {latestBest.optimizationName}, earned {latestBest.score - currScore}");
                currScore = latestBest.score;
                sol = latestBest.sol!;
                _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));
            }
            else
            {
                return _model.ConvertToSubmitSolution(sol);
            }
        }
    }
    
    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> RemoveOneFromAll(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        
        for (var i = 0; i < sol.Length; i++)
        {
            if (RemoveOne(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol, _salesVolume);

            if (postScore > minScore)
            {
                minScore = postScore;
                yield return ((DennisModel.SolutionLocation[])sol.Clone(), minScore, $"RemoveOne({i})");
            }
            AddOneAt(sol, i);
        }
    }
    
    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> AddOneFromAll(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();

        for (var i = 0; i < sol.Length; i++)
        {
            if (AddOneAt(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol, _salesVolume);

            if (postScore > minScore)
            {
                minScore = postScore;
                yield return ((DennisModel.SolutionLocation[])sol.Clone(), minScore, $"AddOne({i})");
            }
            RemoveOne(sol, i);
        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> TryPlusOneAndMinusOneOnNeighbour(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        
        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                var score = _model.CalculateScore(clone);
                if (score > minScore)
                {
                    var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                    yield return (bestClone, score, "TryPlusOneAndMinusOneOnNeighbour");
                    minScore = score;
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }
        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> TryPlusOneAndMinusTwoOnNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();

        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                for (int b = a; b < neighbours.Count; b++)
                {
                    var bIndex = neighbours[b].index;
                    if (RemoveOne(clone, bIndex) == false)
                        continue;

                    var score = _model.CalculateScore(clone);
                    if (score > minScore)
                    {
                        var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                        yield return (bestClone, score, "TryPlusOneAndMinusTwoOnNeighbours");
                        minScore = score;
                    }

                    AddOneAt(clone, bIndex); // Reset to old sate
                }

                AddOneAt(clone, aIndex); // Reset to old sate
            }

        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> TryPlusOneAndMinusThreeOnNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        
        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                for (int b = a; b < neighbours.Count; b++)
                {
                    var bIndex = neighbours[b].index;
                    if (RemoveOne(clone, bIndex) == false)
                        continue;

                    for (int c = b; c < neighbours.Count; c++)
                    {
                        var cIndex = neighbours[c].index;
                        if(RemoveOne(clone, cIndex) == false)
                            continue;
                        
                        var score = _model.CalculateScore(clone);
                        if (score > minScore)
                        {
                            var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                            yield return (bestClone, score, "TryPlusOneAndMinusThreeOnNeighbours");
                            minScore = score;
                        }

                        AddOneAt(clone, cIndex); // Reset to old sate
                    }

                    AddOneAt(clone, bIndex);    // Reset to old sate
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }
        }
    }

    
    private static bool RemoveOne(DennisModel.SolutionLocation[] sol, int index)
    {
        if (sol[index].Freestyle3100Count > 0)
        {
            sol[index].Freestyle3100Count--;
            return true;
        }

        if (sol[index].Freestyle9100Count > 0)
        {
            sol[index].Freestyle9100Count--;
            sol[index].Freestyle3100Count++;
            return true;
        }

        return false;
    }
    
    private static bool AddOneAt(DennisModel.SolutionLocation[] sol, int index)
    {
        var loc = sol[index];
        if (loc.Freestyle3100Count == 0)
        {
            sol[index].Freestyle3100Count = 1;
            return true;
        }
        if (loc.Freestyle9100Count < 5)
        {
            sol[index].Freestyle3100Count--;
            sol[index].Freestyle9100Count++;
            return true;
        }
        return false;
    }
    
}