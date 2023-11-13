using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisStaticInitialStateCreator
{
    private readonly DennisModel _model;
    private readonly GeneralData _generalData;

    public HenrikDennisStaticInitialStateCreator(DennisModel model, GeneralData generalData)
    {
        _generalData = generalData;
        _model = model;
    }

    public SubmitSolution CreateInitialSolution()
    {
        var sol = _model.CreateSolutionLocations();
        
        for (int i = 0; i < sol.Length; i++)
        {
            double sales = _generalData.Freestyle3100Data.RefillCapacityPerWeek;
            sol[i].Freestyle3100Count++;
            while (sales < _model.Locations[i].ScaledSalesVolume)
            {
                sol[i].Freestyle9100Count++;
                sales += _generalData.Freestyle9100Data.RefillCapacityPerWeek;
            }

            if (sales - _generalData.Freestyle3100Data.RefillCapacityPerWeek > _model.Locations[i].ScaledSalesVolume)
            {
                sol[i].Freestyle3100Count = 0;
            }
        }

        return _model.ConvertToSubmitSolution(sol);
    }
    
    public SubmitSolution CreateInitialSolution2()
    {
        var sol = _model.CreateSolutionLocations();
        var maxSales = new double[sol.Length];
        var salesVolume = new double[sol.Length];

        for (int i = 0; i < sol.Length; i++)
        {
            sol[i].Freestyle3100Count = 5;
            _model.CalculateScore(sol, salesVolume);
            maxSales[i] = salesVolume[i];
            sol[i].Freestyle3100Count = 0;
        }
        
        for (int i = 0; i < sol.Length; i++)
        {
            double sales = _generalData.Freestyle3100Data.RefillCapacityPerWeek;
            sol[i].Freestyle3100Count++;
            while (sales < maxSales[i])
            {
                sol[i].Freestyle9100Count++;
                sales += _generalData.Freestyle9100Data.RefillCapacityPerWeek;
            }

            if (sales - _generalData.Freestyle3100Data.RefillCapacityPerWeek > maxSales[i])
            {
                sol[i].Freestyle3100Count = 0;
            }
        }

        return _model.ConvertToSubmitSolution(sol);
    }
}