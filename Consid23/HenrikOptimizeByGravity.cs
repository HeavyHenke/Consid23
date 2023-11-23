using Considition2023_Cs;

namespace Consid23;



public class HenrikOptimizeByGravity
{
    private readonly GeneralData _generalData;
    private readonly MapData _mapData;

    public HenrikOptimizeByGravity(GeneralData generalData, MapData mapData)
    {
        _mapData = mapData;
        _generalData = generalData;
    }

    public SubmitSolution Optimize(SubmitSolution sol)
    {
        var scorer = new ScoringNoRoundNoNeighbours(_generalData, _mapData);

        var gravityDist = _mapData.Hotspots.Select(h => h.Spread).Max() * 2;
        
        var best = sol;
        var bestScore = scorer.CalculateScore(best);
        

        var locationList = best
            .Locations
            .Select(kvp => (kvp.Key, kvp.Value.Latitude, kvp.Value.Longitude))
            .ToArray();

        while (true)
        {
            var work = best.Clone();

            for (int a = 0; a < locationList.Length - 1; a++)
            for (int b = a + 1; b < locationList.Length; b++)
            {
                var loca = locationList[a];
                var locb = locationList[b];
                var dist = Helper.DistanceBetweenPointDouble(loca.Latitude, loca.Longitude, locb.Latitude, locb.Longitude);
                if (dist > gravityDist)
                    continue;

                var gravity = bestScore.Locations[loca.Key].Footfall * bestScore.Locations[locb.Key].Footfall / (dist * dist * dist);
                gravity = gravity * 2000;

                var v = Math.Atan2(locationList[a].Latitude - locationList[b].Latitude, locationList[a].Longitude - locationList[b].Longitude);

                locationList[a].Latitude += gravity * Math.Sin(v);
                locationList[a].Longitude += gravity * Math.Cos(v);
                locationList[b].Latitude -= gravity * Math.Sin(v);
                locationList[b].Longitude -= gravity * Math.Cos(v);
            }
            
            // Score it
            foreach (var loc in locationList)
            {
                work.Locations[loc.Key].Latitude = loc.Latitude;
                work.Locations[loc.Key].Longitude = loc.Longitude;
            }
            
            // Local optimization
            var scoreBeforeOpt = scorer.CalculateScore(work);
            var optimized = OptimizeByMovingALittle(work);
            var scoreAfterOpt = scorer.CalculateScore(optimized);

            //var score = scorer.CalculateScore(work);
            if (scoreAfterOpt.GameScore.Total > bestScore.GameScore.Total)
            {
                best = work;
                bestScore = scoreAfterOpt;
            }
            else
            {
                break;
            }
        }

        return best;
    }

    private SubmitSolution OptimizeByMovingALittle(SubmitSolution best)
    {
        var work = best.Clone();
        var deltas = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1) };
        var scoring = new ScoringNoRoundNoNeighbours(_generalData, _mapData);
        var bestScore = scoring.CalculateScore(best).GameScore!.Total;

        foreach (var loc in work.Locations.Values)
        {
            while (true)
            {
                var bestMove = deltas
                    //.AsParallel()
                    .Select(d => MoveAndScore(work, d, 0.00001, loc, scoring))
                    .MaxBy(m => m.score);
                if (bestMove.score > bestScore)
                {
                    bestMove.doIt();
                    bestScore = bestMove.score;
                }
                else
                {
                    break;
                }
            }
        }

        return work;
    }

    private (double score, Action doIt) MoveAndScore(SubmitSolution sol, (int dx, int dy) delta, double factor, PlacedLocations loc, IScoring scoring)
    {
        loc.Latitude += delta.dx * factor;
        loc.Longitude += delta.dy * factor;

        var score = scoring.CalculateScore(sol).GameScore.Total;

        loc.Latitude -= delta.dx * factor;
        loc.Longitude -= delta.dy * factor;

        return (score, () =>
        {
            loc.Latitude += delta.dx * factor;
            loc.Longitude += delta.dy * factor;
        });
    }
}