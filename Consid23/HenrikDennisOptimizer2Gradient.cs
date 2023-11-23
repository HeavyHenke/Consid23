using System.Runtime.CompilerServices;
using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisOptimizer2Gradient
{
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;

    public HenrikDennisOptimizer2Gradient(DennisModel model, ISolutionSubmitter submitter)
    {
        _model = model;
        _solutionSubmitter = submitter;
    }

    public SubmitSolution OptimizeSolution(SubmitSolution submitSolution)
    {
        var sol = _model.ConvertFromSubmitSolution(submitSolution);
        _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));

        var currScore = _model.CalculateScore(sol);
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
                TryPlusOneAndMinusFourOnNeighbours,
                ExchangeNeighbours,
                RotateThreeNeighbours
            };

            var solForParallelLoop = sol;
            var minScoreForLoop = currScore;
            var bestPerStrategy = new (DennisModel.SolutionLocation[] sol, double score, string optimizationName)[strategies.Length];
            Parallel.For(0, strategies.Length, i => { bestPerStrategy[i] = strategies[i](solForParallelLoop, minScoreForLoop).LastOrDefault(); });
            var latestBest = bestPerStrategy.OrderByDescending(s => s.score).FirstOrDefault();

            // (DennisModel.SolutionLocation[] sol, double score, string optimizationName) latestBest = default;
            // var minScore = currScore;
            // foreach (var s in strategies)
            // {
            //     var best = s(sol, minScore).LastOrDefault();
            //     if (best != default)
            //     {
            //         minScore = best.score;
            //         latestBest = best;
            //     }
            // }

            if (latestBest != default)
            {
                // if(latestBest.optimizationName.Contains("RotateThreeNeighbours"))
                //     Console.WriteLine($"Optimized using {latestBest.optimizationName}, earned {latestBest.score - currScore}");
                currScore = latestBest.score;
                sol = latestBest.sol;
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
        var salesVolume = new double[_model.Locations.Length];

        for (var i = 0; i < sol.Length; i++)
        {
            if (RemoveOne(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol, salesVolume);

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
        var salesVolume = new double[_model.Locations.Length];

        for (var i = 0; i < sol.Length; i++)
        {
            if (AddOneAt(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol, salesVolume);

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
        var salesVolume = new double[_model.Locations.Length];

        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (ShouldWeTryAdding(sol[i]) == false)
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

                var score = _model.CalculateScore(clone, salesVolume);
                if (score > minScore)
                {
                    var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                    yield return (bestClone, score, "TryPlusOneAndMinusOneOnNeighbour");
                    minScore = score;
                }

                AddOneAt(clone, aIndex); // Reset to old sate
            }
        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> TryPlusOneAndMinusTwoOnNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var salesVolume = new double[_model.Locations.Length];

        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (ShouldWeTryAdding(sol[i]) == false)
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

                    var score = _model.CalculateScore(clone, salesVolume);
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
        var salesVolume = new double[_model.Locations.Length];

        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (ShouldWeTryAdding(sol[i]) == false)
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
                        if (RemoveOne(clone, cIndex) == false)
                            continue;

                        var score = _model.CalculateScore(clone, salesVolume);
                        if (score > minScore)
                        {
                            var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                            yield return (bestClone, score, "TryPlusOneAndMinusThreeOnNeighbours");
                            minScore = score;
                        }

                        AddOneAt(clone, cIndex); // Reset to old sate
                    }

                    AddOneAt(clone, bIndex); // Reset to old sate
                }

                AddOneAt(clone, aIndex); // Reset to old sate
            }
        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> TryPlusOneAndMinusFourOnNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var salesVolume = new double[_model.Locations.Length];

        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (ShouldWeTryAdding(sol[i]) == false)
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
                        if (RemoveOne(clone, cIndex) == false)
                            continue;

                        for (int d = c; d < neighbours.Count; d++)
                        {
                            var dIndex = neighbours[d].index;
                            if (RemoveOne(clone, dIndex))
                                continue;

                            var score = _model.CalculateScore(clone, salesVolume);
                            if (score > minScore)
                            {
                                var bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                                yield return (bestClone, score, "TryPlusOneAndMinusFourOnNeighbours");
                                minScore = score;
                            }

                            AddOneAt(clone, dIndex);
                        }

                        AddOneAt(clone, cIndex); // Reset to old sate
                    }

                    AddOneAt(clone, bIndex); // Reset to old sate
                }

                AddOneAt(clone, aIndex); // Reset to old sate
            }
        }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> ExchangeNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var salesVolume = new double[_model.Locations.Length];

        for (var i = 0; i < sol.Length - 1; i++)
            foreach (var n in _model.Neighbours[i])
            {
                var sola = sol[i];
                sol[i] = sol[n.index];
                sol[n.index] = sola;

                var postScore = _model.CalculateScore(sol, salesVolume);

                if (postScore > minScore)
                {
                    minScore = postScore;
                    yield return ((DennisModel.SolutionLocation[])sol.Clone(), minScore, $"ExchangeNeighbours({i},{n.index})");
                }

                sola = sol[i];
                sol[i] = sol[n.index];
                sol[n.index] = sola;
            }
    }

    private IEnumerable<(DennisModel.SolutionLocation[] sol, double score, string optimizationName)> RotateThreeNeighbours(DennisModel.SolutionLocation[] original, double minScore)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var salesVolume = new double[_model.Locations.Length];

        for (var i = 0; i < sol.Length - 1; i++)
        {
            var modelNeighbours = _model.Neighbours[i];
            if(modelNeighbours.Count < 2)
                continue;

            for (int j = 0; j < modelNeighbours.Count - 1; j++)
            for (int k = j + 1; k < modelNeighbours.Count; k++)
            {
                var a = sol[i];
                var b = sol[modelNeighbours[j].index];
                var c = sol[modelNeighbours[k].index];

                sol[i] = c;
                sol[modelNeighbours[j].index] = a;
                sol[modelNeighbours[k].index] = b;

                var postScore = _model.CalculateScore(sol, salesVolume);

                if (postScore > minScore)
                {
                    minScore = postScore;
                    yield return ((DennisModel.SolutionLocation[])sol.Clone(), minScore, $"RotateThreeNeighbours({i})");
                }

                sol[i] = a;
                sol[modelNeighbours[j].index] = b;
                sol[modelNeighbours[k].index] = c;
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
            sol[index].Freestyle3100Count += 2;
            return true;
        }

        return false;
    }

    private static bool AddOneAt(DennisModel.SolutionLocation[] sol, int index)
    {
        if (ShouldWeTryAdding(sol[index]) == false)
            return false;

        if (sol[index].Freestyle3100Count < 2)
        {
            sol[index].Freestyle3100Count++;
            return true;
        }

        if (sol[index].Freestyle9100Count < 2)
        {
            sol[index].Freestyle3100Count = 0;
            sol[index].Freestyle9100Count++;
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldWeTryAdding(DennisModel.SolutionLocation sol)
    {
        return sol.Freestyle9100Count < 2 || sol.Freestyle3100Count < 2;
    }
}