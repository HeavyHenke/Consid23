﻿using System.Collections.Concurrent;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23;

public interface ISolutionSubmitter : IDisposable
{
    void AddSolutionToSubmit(SubmitSolution sol, double score);
}

public class SolutionSubmitter : ISolutionSubmitter
{
    private readonly Api _api;
    private readonly string _apiKey;
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;
    private readonly ConcurrentQueue<(string json, double score)> _submitQueue = new();
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
    
    public void AddSolutionToSubmit(SubmitSolution sol, double score)
    {
        _submitQueue.Enqueue((JsonConvert.SerializeObject(sol), score));
    }

    public void Dispose()
    {
        while (_submitQueue.IsEmpty == false)
            Thread.Sleep(10);
        _exit = true;
    }

    private void Worker()
    {
        while (true)
        {
            var submitList = new List<(string json, double score)>();
            while (_submitQueue.IsEmpty == false)
            {
                if (_submitQueue.TryDequeue(out var sol))
                    submitList.Add(sol);
            }

            if (submitList.Count > 0)
            {
                var bestScoreItem = submitList.MaxBy(s => s.score);
                try
                {
                    if (bestScoreItem.score > _maxSubmitted)
                    {
                        var serverScore = _api.Sumbit(_mapData.MapName, JsonConvert.DeserializeObject<SubmitSolution>(bestScoreItem.json)!, _apiKey);

                        _maxSubmitted = bestScoreItem.score;
                        var time = DateTime.Now.ToString("dd_hh_mm_ss");
                        var filename = $"{_mapData.MapName}_{bestScoreItem.score}_{time}.json";
                        File.WriteAllText(filename, bestScoreItem.json);

                        Console.WriteLine($"GameScore: {serverScore.GameScore!.Total} co2 {serverScore.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {serverScore.GameScore.Earnings} footfall {serverScore.GameScore.TotalFootfall}. Skipped {submitList.Count - 1} items int submit list.");

                        if (serverScore.GameScore.Total != bestScoreItem.score)
                        {
                            Console.WriteLine($"SCORE NOT EQUAL!! see file {filename}");
                        }
                        // Console.WriteLine($"ServerScore: {serverScore.GameScore!.Total} co2 {serverScore.GameScore.KgCo2Savings * _generalData.Co2PricePerKiloInSek} earnings {serverScore.GameScore.Earnings} footfall {serverScore.GameScore.TotalFootfall}");
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    if (bestScoreItem != default)
                        _submitQueue.Enqueue(bestScoreItem);
                }
            }

            if (_exit && _submitQueue.IsEmpty)
                return;
            
            Thread.Sleep(10_000);
        }
    }
}