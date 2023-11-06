using Considition2023_Cs;

namespace Consid23;

public class HenrikSolverOnePoint
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public HenrikSolverOnePoint(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public SubmitSolution CalcSolution()
    {
        var scorer = new Scoring();

        SubmitSolution? bestSol = null;
        GameData bestScore = new GameData
        {
            GameScore = new Score
            {
                Total = int.MinValue
            }
        };

        
        for (int i = 0; i < _mapData.locations.Count - 1; i++)
        {
            SubmitSolution sol = new SubmitSolution
            {
                Locations = new()
            };
            
            foreach (var loc in _mapData.locations.Skip(i))
            {
                while (true)
                {
                    var score = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
                    AddOneAt(sol, loc.Key);
                    var score2 = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);

                    if (IsBetterScore(score2, score))
                    {
                        RemoveOne(sol, loc.Key);
                        break;
                    }
                }
            }

            // Close, move around a bit
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var source in sol.Locations.Keys.ToList())
                {
                    var score = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
                    RemoveOne(sol, source);
                    bool movedLast = false;
                    foreach (var dest in _mapData.locations.Keys.Where(k => k != source))
                    {
                        AddOneAt(sol, dest);
                        var score2 = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
                        if (IsBetterScore(score, score2))
                        {
                            changed = true;
                            if (sol.Locations.ContainsKey(source) == false)
                            {
                                movedLast = true;
                                break;
                            }

                            RemoveOne(sol, source);
                            score = score2;
                        }
                        else
                        {
                            RemoveOne(sol, dest);
                        }
                    }

                    if (!movedLast)
                        AddOneAt(sol, source);
                }
            }
            
            var finalScore = scorer.CalculateScore(_mapData.MapName, sol, _mapData, _generalData);
            if (IsBetterScore(bestScore, finalScore))
            {
                bestScore = finalScore;
                bestSol = sol;
            }
        }

        return bestSol;
    }

    private static bool IsBetterScore(GameData score2, GameData score)
    {
        return Math.Abs(1337 - score2.GameScore.Total) > Math.Abs(1337 - score.GameScore.Total);
    }

    private static void AddOneAt(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            sol.Locations.Add(location, new PlacedLocations
            {
                Freestyle3100Count = 1,
                Freestyle9100Count = 0
            });
        }
        else if (loc.Freestyle3100Count < 1)
        {
            loc.Freestyle3100Count++;
        }
        else
        {
            loc.Freestyle3100Count--;
            loc.Freestyle9100Count++;
        }
    }

    private static void RemoveOne(SubmitSolution sol, string location)
    {
        if (sol.Locations.TryGetValue(location, out var loc) == false)
        {
            return;
        }
        
        if (loc.Freestyle3100Count > 0)
        {
            loc.Freestyle3100Count--;
        }
        else if(loc.Freestyle9100Count > 0)
        {
            loc.Freestyle9100Count--;
            loc.Freestyle3100Count++;
        }

        if (loc is { Freestyle9100Count: 0, Freestyle3100Count: 0 })
            sol.Locations.Remove(location);
    }
}