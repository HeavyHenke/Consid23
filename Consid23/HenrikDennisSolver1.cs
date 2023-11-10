using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisSolver1
{
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;

    public HenrikDennisSolver1(GeneralData generalData, MapData mapData, ISolutionSubmitter solutionSubmitter)
    {
        _model = new DennisModel(generalData, mapData);
        _solutionSubmitter = solutionSubmitter;
    }

    public SubmitSolution OptimizeSolution(SubmitSolution submitSolution)
    {
        var sol = _model.ConvertFromSubmitSolution(submitSolution);

        
        var currScore = _model.CalculateScore(sol);
        while (true)
        {
            var optimizations = new List<(DennisModel.SolutionLocation[] sol, double score)>
            {
                RemoveOneFromAll(sol),
                AddOneForAll(sol),
                TryPlusOneAndMinusOneOnNeighbour(sol),
                TryPlusOneAndMinusTwoOnNeighbours(sol),
                TryPlusOneAndMinusThreeOnNeighbours(sol)
            };

            var best = optimizations.MaxBy(o => o.score);
            if (best.score <= currScore)
                break;

            var ix = optimizations.IndexOf(best);
            Console.WriteLine($"Best ix: {ix} with added score {best.score - currScore} total score {best.score}");

            
            sol = best.sol;
            currScore = best.score;
            _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));
        }

        return _model.ConvertToSubmitSolution(sol);
    }

    private (DennisModel.SolutionLocation[] sol, double score) RemoveOneFromAll(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        for (var i = 0; i < sol.Length; i++)
        {
            if (RemoveOne(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol);

            if (postScore - currScore < 0)
                AddOneAt(sol, i);
            else if (postScore > currScore)
                currScore = postScore;
        }
        
        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) AddOneForAll(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        for (var i = 0; i < sol.Length; i++)
        {
            if(AddOneAt(sol, i) == false)
                continue;
            
            var postScore = _model.CalculateScore(sol);

            if (postScore - currScore < 0)
                RemoveOne(sol, i);
            else if (postScore > currScore)
                currScore = postScore;
        }
        
        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusOneOnNeighbour(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
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

            var bestClone = sol;
            var bestScore = currScore;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                var score = _model.CalculateScore(clone);
                if (score > bestScore)
                {
                    bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                    bestScore = score;
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
   
        }
        
        return (sol, currScore);
    }
    
    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusTwoOnNeighbours(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
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

            var bestClone = sol;
            var bestScore = currScore;

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
                    if (score > bestScore)
                    {
                        bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                        bestScore = score;
                    }

                    AddOneAt(clone, bIndex);    // Reset to old sate
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
        }

        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusThreeOnNeighbours(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
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

            var bestClone = sol;
            var bestScore = currScore;

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
                        if (score > bestScore)
                        {
                            bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                            bestScore = score;
                        }

                        AddOneAt(clone, cIndex); // Reset to old sate
                    }
                    

                    AddOneAt(clone, bIndex);    // Reset to old sate
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
        }

        return (sol, currScore);
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
}