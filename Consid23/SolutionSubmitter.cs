using System.Collections.Concurrent;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23;

public interface ISolutionSubmitter
{
    void AddSolutionToSubmit(SubmitSolution sol);
}

public class SolutionSubmitter : ISolutionSubmitter
{
    private readonly Api _api;
    private readonly string _apiKey;
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;
    private readonly ConcurrentQueue<string> _submitQueue = new();
    private bool _exit;

    public SolutionSubmitter(Api api, string apiKey, GeneralData generalData, MapData mapData)
    {
        _api = api;
        _apiKey = apiKey;
        _generalData = generalData;
        _mapData = mapData;
        new Thread(Worker).Start();
    }
    
    public void AddSolutionToSubmit(SubmitSolution sol)
    {
        _submitQueue.Enqueue(JsonConvert.SerializeObject(sol));
    }

    public void Dispose()
    {
        while (_submitQueue.IsEmpty == false)
            Thread.Sleep(10);
        _exit = true;
    }

    private void Worker()
    {
        var scoring = new Scoring(_generalData, _mapData);
        while (true)
        {
            var submitList = new List<SubmitSolution>();
            while (_submitQueue.IsEmpty == false)
            {
                if (_submitQueue.TryDequeue(out var sol))
                    submitList.Add(JsonConvert.DeserializeObject<SubmitSolution>(sol)!);
            }

            if (submitList.Count > 0)
            {
                var bestScoreItem = submitList.OrderByDescending(s => scoring.CalculateScore(s).GameScore!.Total).First();
                var score = _api.Sumbit(_mapData.MapName, bestScoreItem, _apiKey);
                Console.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}. Skipped {submitList.Count - 1} items int submit list.");
            }

            if (_exit)
                return;
            
            Thread.Sleep(2000);
        }
    }
}