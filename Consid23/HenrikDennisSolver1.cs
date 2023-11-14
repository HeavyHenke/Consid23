using Considition2023_Cs;

namespace Consid23;

public class HenrikDennisSolver1
{
    private readonly DennisModel _model;
    private readonly ISolutionSubmitter _solutionSubmitter;
    private readonly List<int[]> _neighbourhoodWithTwo;
    private readonly List<int[]> _neighbourhoodWithThree;
    private readonly List<int[]> _neighbourhoodWithFour;
    private readonly List<int[]> _neighbourhoodWithFive;
    private readonly List<int[]> _neighbourhoodWithSix;

    public HenrikDennisSolver1(DennisModel model, ISolutionSubmitter submitter)
    {
        _model = model;
        _solutionSubmitter = submitter;

        _neighbourhoodWithTwo = new();
        for (var index = 0; index < _model.Neighbours.Length; index++)
        {
            var n = _model.Neighbours[index];
            if (n.Count == 1 && n.All(q => q.index > index))
            {
                _neighbourhoodWithTwo.Add(new[] { index, n[0].index });
            }
        }

        _neighbourhoodWithThree = new();
        for (var index = 0; index < _model.Neighbours.Length; index++)
        {
            var n = _model.Neighbours[index];
            if (n.Count == 2 && n.All(q => q.index > index))
            {
                _neighbourhoodWithThree.Add(new[] { index, n[0].index, n[1].index });
            }
        }

        _neighbourhoodWithFour = new();
        for (var index = 0; index < _model.Neighbours.Length; index++)
        {
            var n = _model.Neighbours[index];
            if (n.Count == 3 && n.All(q => q.index > index))
            {
                _neighbourhoodWithFour.Add(new[] { index, n[0].index, n[1].index, n[2].index });
            }
        }

        _neighbourhoodWithFive = new();
        for (var index = 0; index < _model.Neighbours.Length; index++)
        {
            var n = _model.Neighbours[index];
            if (n.Count == 4 && n.All(q => q.index > index))
            {
                _neighbourhoodWithFive.Add(new[] { index, n[0].index, n[1].index, n[2].index, n[3].index });
            }
        }

        _neighbourhoodWithSix = new();
        for (var index = 0; index < _model.Neighbours.Length; index++)
        {
            var n = _model.Neighbours[index];
            if (n.Count == 5 && n.All(q => q.index > index))
            {
                _neighbourhoodWithSix.Add(new[] { index, n[0].index, n[1].index, n[2].index, n[3].index, n[4].index });
            }
        }
    }
    
    public HenrikDennisSolver1(GeneralData generalData, MapData mapData, ISolutionSubmitter solutionSubmitter)
        : this(new DennisModel(generalData, mapData), solutionSubmitter)
    {
    }

    public SubmitSolution OptimizeSolution(SubmitSolution submitSolution)
    {
        var sol = _model.ConvertFromSubmitSolution(submitSolution);
        _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));
        
        // Do cheap iterations first
        var currScore = _model.CalculateScore(sol);
        while (true)
        {
            var optimizations = new List<(DennisModel.SolutionLocation[] sol, double score)>
            {
                RemoveOneFromAll(sol),
                AddOneForAll(sol),
                TryPlusOneAndMinusOneOnNeighbour(sol),
            };

            var best = optimizations.MaxBy(o => o.score);
            if (best.score <= currScore)
                break;

            var ix = optimizations.IndexOf(best);
            Console.WriteLine($"Best ix: {ix} with added score {best.score - currScore} total score {best.score}");
            
            sol = best.sol;
            currScore = best.score;
            _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));
        }
        
        // More expensive loop
        Console.WriteLine("Starting expensive loop");
        while (true)
        {
            var optimizations = new List<(DennisModel.SolutionLocation[] sol, double score)>
            {
                RemoveOneFromAll(sol),
                AddOneForAll(sol),
                TryPlusOneAndMinusOneOnNeighbour(sol),
                TryPlusOneAndMinusTwoOnNeighbours(sol),
                TryPlusOneAndMinusThreeOnNeighbours(sol),
                // TryOptimizeNeighbourhoods(sol, 5),
            };

            var best = optimizations.MaxBy(o => o.score);
            if (best.score <= currScore) 
                break;

            var ix = optimizations.IndexOf(best);
            Console.WriteLine($"Best ix (expensive loop): {ix} with added score {best.score - currScore} total score {best.score}");

            
            sol = best.sol;
            currScore = best.score;
            _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(sol));
        }
        
        
        return _model.ConvertToSubmitSolution(sol);
    }

    private (DennisModel.SolutionLocation[] sol, double score) TryOptimizeNeighbourhoods(DennisModel.SolutionLocation[] original, int maxNeighborhoodSize)
    {
        if (maxNeighborhoodSize < 2)
            return (original, -1);

        var bestSolution = (DennisModel.SolutionLocation[])original.Clone();
        var bestScore = _model.CalculateScore(bestSolution);
        var workingCopy = (DennisModel.SolutionLocation[])bestSolution.Clone();

        foreach(var neighbourhood in _neighbourhoodWithTwo)
        {
            foreach (var a in SolutionLocationsInOrder)
            foreach (var b in SolutionLocationsInOrder)
            {
                Array.Copy(bestSolution, workingCopy, bestSolution.Length);
                workingCopy[neighbourhood[0]] = a;
                workingCopy[neighbourhood[1]] = b;
                var score = _model.CalculateScore(workingCopy);
                if (score > bestScore)
                {
                    bestSolution = (DennisModel.SolutionLocation[])workingCopy.Clone();
                    bestScore = score;
                    _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(bestSolution));
                }
            }
        }

        if (maxNeighborhoodSize == 2)
            return (bestSolution, bestScore);

        foreach (var neighbourhood in _neighbourhoodWithThree)
        {
            foreach (var a in SolutionLocationsInOrder)
            foreach (var b in SolutionLocationsInOrder)
            foreach (var c in SolutionLocationsInOrder)
            {
                Array.Copy(bestSolution, workingCopy, bestSolution.Length);
                workingCopy[neighbourhood[0]] = a;
                workingCopy[neighbourhood[1]] = b;
                workingCopy[neighbourhood[2]] = c;
                var score = _model.CalculateScore(workingCopy);
                if (score > bestScore)
                {
                    bestSolution = (DennisModel.SolutionLocation[])workingCopy.Clone();
                    bestScore = score;
                    _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(bestSolution));
                }
            }
        }
        
        if (maxNeighborhoodSize == 3)
            return (bestSolution, bestScore);

        
        foreach (var neighbourhood in _neighbourhoodWithFour)
        {
            var numSmallGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
            var numBigGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);

            foreach (var a in SolutionLocationsInOrder)
            foreach (var b in SolutionLocationsInOrder)
            foreach (var c in SolutionLocationsInOrder)
            foreach (var d in SolutionLocationsInOrder)
            {
                Array.Copy(bestSolution, workingCopy, bestSolution.Length);
                workingCopy[neighbourhood[0]] = a;
                workingCopy[neighbourhood[1]] = b;
                workingCopy[neighbourhood[2]] = c;
                workingCopy[neighbourhood[3]] = d;
                
                var numSmall = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
                if (numSmall != numSmallGoal)
                    continue;
                var numBig = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);
                if (numBig != numBigGoal)
                    continue;
                
                var score = _model.CalculateScore(workingCopy);
                if (score > bestScore)
                {
                    bestSolution = (DennisModel.SolutionLocation[])workingCopy.Clone();
                    bestScore = score;
                    _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(bestSolution));
                }
            }
        }

        if (maxNeighborhoodSize == 4)
            return (bestSolution, bestScore);

        
        foreach (var neighbourhood in _neighbourhoodWithFive)
        {
            var numSmallGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
            var numBigGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);
            
            foreach (var a in SolutionLocationsInOrder)
            foreach (var b in SolutionLocationsInOrder)
            foreach (var c in SolutionLocationsInOrder)
            foreach (var d in SolutionLocationsInOrder)
            foreach (var e in SolutionLocationsInOrder)
            {
                Array.Copy(bestSolution, workingCopy, bestSolution.Length);
                workingCopy[neighbourhood[0]] = a;
                workingCopy[neighbourhood[1]] = b;
                workingCopy[neighbourhood[2]] = c;
                workingCopy[neighbourhood[3]] = d;
                workingCopy[neighbourhood[4]] = e;

                var numSmall = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
                if (numSmall != numSmallGoal)
                    continue;
                var numBig = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);
                if (numBig != numBigGoal)
                    continue;
                
                var score = _model.CalculateScore(workingCopy);
                if (score > bestScore)
                {
                    bestSolution = (DennisModel.SolutionLocation[])workingCopy.Clone();
                    bestScore = score;
                    _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(bestSolution));
                }
            }
        }
        
        if (maxNeighborhoodSize == 5)
            return (bestSolution, bestScore);


        foreach (var neighbourhood in _neighbourhoodWithSix)
        {
            var numSmallGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
            var numBigGoal = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);
            
            foreach (var a in SolutionLocationsInOrder)
            foreach (var b in SolutionLocationsInOrder)
            foreach (var c in SolutionLocationsInOrder)
            foreach (var d in SolutionLocationsInOrder)
            foreach (var e in SolutionLocationsInOrder)
            foreach (var f in SolutionLocationsInOrder)
            {
                Array.Copy(bestSolution, workingCopy, bestSolution.Length);
                workingCopy[neighbourhood[0]] = a;
                workingCopy[neighbourhood[1]] = b;
                workingCopy[neighbourhood[2]] = c;
                workingCopy[neighbourhood[3]] = d;
                workingCopy[neighbourhood[4]] = e;
                workingCopy[neighbourhood[5]] = f;

                var numSmall = neighbourhood.Sum(n => bestSolution[n].Freestyle3100Count);
                if (numSmall != numSmallGoal)
                    continue;
                var numBig = neighbourhood.Sum(n => bestSolution[n].Freestyle9100Count);
                if (numBig != numBigGoal)
                    continue;
                
                var score = _model.CalculateScore(workingCopy);
                if (score > bestScore)
                {
                    bestSolution = (DennisModel.SolutionLocation[])workingCopy.Clone();
                    bestScore = score;
                    _solutionSubmitter.AddSolutionToSubmit(_model.ConvertToSubmitSolution(bestSolution));
                }
            }
        }

        return (bestSolution, bestScore);
    }

    private static readonly DennisModel.SolutionLocation[] SolutionLocationsInOrder = {
        new()
        {
            Freestyle9100Count = 0,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 0,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 1,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 1,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 2,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 2,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 3,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 3,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 4,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 4,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 0
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 1
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 2
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 3
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 4
        },
        new()
        {
            Freestyle9100Count = 5,
            Freestyle3100Count = 5
        },
    };

    private (DennisModel.SolutionLocation[] sol, double score) RemoveOneFromAll(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        for (var i = 0; i < sol.Length; i++)
        {
            if (RemoveOne(sol, i) == false)
                continue;
            var postScore = _model.CalculateScore(sol);

            if (postScore - currScore < 0)
                AddOneAt(sol, i);
            else if (postScore > currScore)
                currScore = postScore;
        }
        
        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) AddOneForAll(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        for (var i = 0; i < sol.Length; i++)
        {
            if(AddOneAt(sol, i) == false)
                continue;
            
            var postScore = _model.CalculateScore(sol);

            if (postScore - currScore < 0)
                RemoveOne(sol, i);
            else if (postScore > currScore)
                currScore = postScore;
        }
        
        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusOneOnNeighbour(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            var bestClone = sol;
            var bestScore = currScore;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                var score = _model.CalculateScore(clone);
                if (score > bestScore)
                {
                    bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                    bestScore = score;
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
   
        }
        
        return (sol, currScore);
    }
    
    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusTwoOnNeighbours(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            var bestClone = sol;
            var bestScore = currScore;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                for (int b = a; b < neighbours.Count; b++)
                {
                    var bIndex = neighbours[b].index;
                    if (RemoveOne(clone, bIndex) == false)
                        continue;

                    var score = _model.CalculateScore(clone);
                    if (score > bestScore)
                    {
                        bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                        bestScore = score;
                    }

                    AddOneAt(clone, bIndex);    // Reset to old sate
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
        }

        return (sol, currScore);
    }

    private (DennisModel.SolutionLocation[] sol, double score) TryPlusOneAndMinusThreeOnNeighbours(DennisModel.SolutionLocation[] original)
    {
        var sol = (DennisModel.SolutionLocation[])original.Clone();
        var currScore = _model.CalculateScore(sol);
        
        for (int i = 0; i < _model.Locations.Length; i++)
        {
            if (sol[i].Freestyle9100Count == 5 && sol[i].Freestyle3100Count == 5)
                continue;

            var neighbours = _model.Neighbours[i];
            if (neighbours.Count == 0)
                continue;

            var clone = (DennisModel.SolutionLocation[])sol.Clone();
            if (AddOneAt(clone, i) == false)
                continue;

            var bestClone = sol;
            var bestScore = currScore;

            for (int a = 0; a < neighbours.Count; a++)
            {
                var aIndex = neighbours[a].index;
                if (RemoveOne(clone, aIndex) == false)
                    continue;

                for (int b = a; b < neighbours.Count; b++)
                {
                    var bIndex = neighbours[b].index;
                    if (RemoveOne(clone, bIndex) == false)
                        continue;

                    for (int c = b; c < neighbours.Count; c++)
                    {
                        var cIndex = neighbours[c].index;
                        if(RemoveOne(clone, cIndex) == false)
                            continue;
                        
                        var score = _model.CalculateScore(clone);
                        if (score > bestScore)
                        {
                            bestClone = (DennisModel.SolutionLocation[])clone.Clone();
                            bestScore = score;
                        }

                        AddOneAt(clone, cIndex); // Reset to old sate
                    }
                    

                    AddOneAt(clone, bIndex);    // Reset to old sate
                }

                AddOneAt(clone, aIndex);    // Reset to old sate
            }

            sol = bestClone;
            currScore = bestScore;
        }

        return (sol, currScore);
    }

    private static bool AddOneAt(DennisModel.SolutionLocation[] sol, int index)
    {
        var loc = sol[index];
        if (loc.Freestyle3100Count < 2)
        {
            sol[index].Freestyle3100Count ++;
            return true;
        }
        if (loc.Freestyle9100Count < 2)
        {
            sol[index].Freestyle3100Count-=2;
            sol[index].Freestyle9100Count++;
            return true;
        }
        return false;
    }

    private static bool RemoveOne(DennisModel.SolutionLocation[] sol, int index)
    {
        if (sol[index].Freestyle3100Count > 0)
        {
            sol[index].Freestyle3100Count--;
            return true;
        }

        if (sol[index].Freestyle9100Count > 0)
        {
            sol[index].Freestyle9100Count--;
            sol[index].Freestyle3100Count=2;
            return true;
        }

        return false;
    }
}