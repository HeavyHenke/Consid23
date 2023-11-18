using System.Diagnostics;
using Considition2023_Cs;

namespace Consid23;

public class DennisSolver1
{
    private readonly GeneralData _generalData;
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;

    public DennisSolver1(DennisModel model, ISolutionSubmitter submitter, GeneralData generalData)
    {
        _model = model;
        _solutionSubmitter = submitter;
        _generalData = generalData;
    }

    public DennisModel.SolutionLocation[] OptimizeSolution(int seed = 1337)
    {
        var w = .8;
        var rp = 1.2;
        var rg = 1.2;
            
        var numParticles = 10;
        var locationConfiguration = new LocationConfiguration(_generalData);
        var rnd = new Random();
        var solutionLocations = _model.CreateSolutionLocations();

        for (var x = 0; x < 1; x++)
        {
            var particle = new double[numParticles][];
            var velocity = new double[numParticles][];
            var localBest = new double[numParticles][];
            var globalBest = new double[solutionLocations.Length];
            var maxLocal = new double[numParticles];
            var maxGlobal = Double.MinValue;
            var iteration = 0;
            for (int p = 0; p < numParticles; p++)
            {
                particle[p] = new double[solutionLocations.Length];
                velocity[p] = new double[solutionLocations.Length];
                for (int i = 0; i < solutionLocations.Length; i++)
                {
                    particle[p][i] = rnd.NextDouble();
                    velocity[p][i] = (rnd.NextDouble() - .5)*.2;                
                }
                
                localBest[p]=new double[solutionLocations.Length];
                particle[p].CopyTo(localBest[p],0);
                maxLocal[p] = CalcScore(particle[p]);
                if (maxLocal[p] > maxGlobal)
                {
                    maxGlobal = maxLocal[p];
                    localBest[p].CopyTo(globalBest,0);
                }
            }

            for (var unchangedCount = 0; unchangedCount < 10000; unchangedCount++, iteration++)
            {
                for (int p = 0; p < numParticles; p++)
                {
//                    if(p==0)
//                        PrintVelocity(particle[p]);
                    for (int i = 0; i < solutionLocations.Length; i++)
                    {
                        velocity[p][i] = w * velocity[p][i] + rp * rnd.NextDouble() * (localBest[p][i] - particle[p][i]) + rg * rnd.NextDouble() * (globalBest[i] - particle[p][i]);
                        particle[p][i] += velocity[p][i];
                    }

                    var score = CalcScore(particle[p]);
                    if(p==0)
                        Trace.WriteLine($"{score}");
                    if (score > maxLocal[p])
                    {
                        maxLocal[p] = score;
                        particle[p].CopyTo(localBest[p], 0);
                        if (score > maxGlobal)
                        {
                            maxGlobal = score;
                            particle[p].CopyTo(globalBest, 0);
                            //                        Trace.WriteLine($"{iteration}: Particle {p} MaxGlobal {maxGlobal}");
                            unchangedCount = 0;
                        }
                    }
                }
            }
            Trace.WriteLine($"Iterations: {iteration} maxGlobal {maxGlobal}");
        }

//        foreach(var solutionLocation in solutionLocations )
//            Trace.WriteLine($"[{solutionLocation.Freestyle3100Count}, {solutionLocation.Freestyle9100Count}]");
        return solutionLocations;

        double CalcScore(double[] doubles)
        {
            for (int i = 0; i < doubles.Length; i++)
            {
                var index = locationConfiguration.IndexFromDouble(doubles[i]);
                solutionLocations[i].Freestyle3100Count = locationConfiguration.Freestyle3100Count[index];
                solutionLocations[i].Freestyle9100Count = locationConfiguration.Freestyle9100Count[index];
            }

            var score = _model.CalculateScore(solutionLocations);
            return score;
        }
    }

    private void PrintVelocity(double[] doubles)
    {
        var sum = 0.0;
        foreach (var d in doubles)
        {
            sum += d * d;
        }
        Trace.WriteLine($"{Math.Sqrt(sum)}");
    }
}
