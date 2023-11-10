using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisSolver1
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;

    public HenrikDennisSolver1(GeneralData generalData, MapData mapData, ISolutionSubmitter solutionSubmitter)
    {
        _mapData = mapData;
        _generalData = generalData;
        _model = new DennisModel(_generalData, _mapData);
        _solutionSubmitter = solutionSubmitter;
    }

    public SubmitSolution OptimizeSolution(SubmitSolution submitSolution)
    {
        var sol = _model.ConvertFromSubmitSolution(submitSolution);

        // Try -1 and +1 on all (one at a time)
        var currScore = _model.CalculateScore(sol);
        while (true)
        {
            var optimizations = new List<(DennisModel.SolutionLocation[] sol, double score)>(); 
            optimizations.Add(RemoveOneFromAll(sol));
            optimizations.Add(AddOneForAll(sol));

            var best = optimizations.MaxBy(o => o.score);
            if (best.score <= currScore)
                break;

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
            RemoveOne(sol, i);
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
            AddOneAt(sol, i);
            var postScore = _model.CalculateScore(sol);

            if (postScore - currScore < 0)
                RemoveOne(sol, i);
            else if (postScore > currScore)
                currScore = postScore;
        }
        
        return (sol, currScore);
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
}