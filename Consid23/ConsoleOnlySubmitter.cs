using Considition2023_Cs;

namespace Consid23;

public class ConsoleOnlySubmitter : ISolutionSubmitter
{
    private readonly Scoring _scorer;
    private readonly GeneralData _generalData;

    public ConsoleOnlySubmitter(Api api, string apiKey, GeneralData generalData, MapData mapData)
    {
        _generalData = generalData;
        _scorer = new Scoring(generalData, mapData);
    }
    
    public void AddSolutionToSubmit(SubmitSolution sol)
    {
        var score = _scorer.CalculateScore(sol);
        Console.WriteLine($"Not submitting (ConsoleOnlySubmitter) GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");
    }

    public void Dispose()
    {
    }
}