﻿using System.Collections.Concurrent;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23;

public interface ISolutionSubmitter : IDisposable
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
    private double _maxSubmitted = 0;

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
                try
                {
                    var score = scoring.CalculateScore(bestScoreItem);
                    if (score.GameScore!.Total > _maxSubmitted)
                    {
                        // var serverScore = _api.Sumbit(_mapData.MapName, bestScoreItem, _apiKey);

                        _maxSubmitted = score.GameScore.Total;
                        var time = DateTime.Now.ToString("dd_hh_mm_ss");
                        var filename = $"{_mapData.MapName}_{score.GameScore.Total}_{time}.json";
                        File.WriteAllText(filename, JsonConvert.SerializeObject(bestScoreItem));

                        Console.WriteLine($"GameScore: {score.GameScore!.Total} co2 {score.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {score.GameScore.Earnings} footfall {score.GameScore.TotalFootfall}. Skipped {submitList.Count - 1} items int submit list.");
                        // Console.WriteLine($"ServerScore: {serverScore.GameScore!.Total} co2 {serverScore.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {serverScore.GameScore.Earnings} footfall {serverScore.GameScore.TotalFootfall}");
                    }
                    
                    bestScoreItem = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if(bestScoreItem != null)
                        _submitQueue.Enqueue(JsonConvert.SerializeObject(bestScoreItem));
                }
            }

            if (_exit && _submitQueue.IsEmpty)
                return;
            
            Thread.Sleep(2000);
        }
    }
}