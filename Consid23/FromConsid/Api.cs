﻿using Newtonsoft.Json;

namespace Considition2023_Cs;

public class Api
{
    private readonly HttpClient _httpClient;

    private bool _useCache = true;
    
    public Api(HttpClient httpClient)
    {
        _httpClient = httpClient;
        httpClient.BaseAddress = new Uri("https://api.considition.com/");
    }
    
    public async Task<MapData> GetMapDataAsync(string mapName, string apiKey)
    {
        if (_useCache && File.Exists(mapName+".cached.json"))
        {
            var json = await File.ReadAllTextAsync(mapName+".cached.json");
            return JsonConvert.DeserializeObject<MapData>(json)!;
        }
        
        using HttpRequestMessage request = new();
        request.Method = HttpMethod.Get;
        request.RequestUri = new Uri($"/api/game/getmapdata?mapName={Uri.EscapeDataString(mapName)}", UriKind.Relative);
        request.Headers.Add("x-api-key", apiKey);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string responseText = await response.Content.ReadAsStringAsync();

        if (_useCache) 
            await File.WriteAllTextAsync(mapName + ".cached.json", responseText);

        return JsonConvert.DeserializeObject<MapData>(responseText)!;
    }

    public async Task<GeneralData> GetGeneralDataAsync()
    {
        if (_useCache && File.Exists("Cached_general.json"))
        {
            var json = await File.ReadAllTextAsync("Cached_general.json");
            return JsonConvert.DeserializeObject<GeneralData>(json)!;
        }
        
        using var response = await _httpClient.GetAsync("/api/game/getgeneralgamedata");
        response.EnsureSuccessStatusCode();
        string responseText = await response.Content.ReadAsStringAsync();
        
        if(_useCache)
            await File.WriteAllTextAsync("Cached_general.json", responseText);
        
        return JsonConvert.DeserializeObject<GeneralData>(responseText)!;
    }

    public async Task<GameData> GetGameAsync(Guid id)
    {
        using var response = await _httpClient.GetAsync($"/api/game/getgamedata{id}");
        response.EnsureSuccessStatusCode();
        string responseText = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameData>(responseText)!;
    }

    public async Task<GameData> SumbitAsync(string mapName, SubmitSolution solution, string apiKey)
    {
        using HttpRequestMessage request = new();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"/api/Game/submitSolution?mapName={Uri.EscapeDataString(mapName)}", UriKind.Relative);
        request.Headers.Add("x-api-key", apiKey);
        request.Content = new StringContent(JsonConvert.SerializeObject(solution), System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = _httpClient.Send(request);
        string responseText = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameData>(responseText)!;
    }
    
    public GameData Sumbit(string mapName, SubmitSolution solution, string apiKey)
    {
        using HttpRequestMessage request = new();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"/api/Game/submitSolution?mapName={Uri.EscapeDataString(mapName)}", UriKind.Relative);
        request.Headers.Add("x-api-key", apiKey);
        request.Content = new StringContent(JsonConvert.SerializeObject(solution), System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = _httpClient.Send(request);
        string responseText = new StreamReader(response.Content.ReadAsStream()).ReadToEnd();
        return JsonConvert.DeserializeObject<GameData>(responseText)!;
    }
}   
