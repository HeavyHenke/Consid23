using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23;

public class ConsoleOnlySubmitter : ISolutionSubmitter
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    private double _maxSubmitted = 0;
    
    public ConsoleOnlySubmitter(Api api, string apiKey, GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public void AddSolutionToSubmit(SubmitSolution sol, double score)
    {
        lock(this)
        if (score > _maxSubmitted)
        {
            //Console.WriteLine($"Not submitting (ConsoleOnlySubmitter) GameScore: {score.GameScore.Total} co2 {score.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}");

            _maxSubmitted = score;
            var time = DateTime.Now.ToString("dd_hh_mm_ss");
            var filename = $"{_mapData.MapName}_{score}_{time}.json";
            if(File.Exists(filename) == false)
                File.WriteAllText(filename, JsonConvert.SerializeObject(sol));
        }
    }

    public void Dispose()
    {
    }
}