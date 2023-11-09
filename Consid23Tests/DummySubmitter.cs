using Consid23;
using Considition2023_Cs;
using Newtonsoft.Json;

namespace Consid23Tests;

public class DummySubmitter : ISolutionSubmitter
{
    public void AddSolutionToSubmit(SubmitSolution sol)
    {
        JsonConvert.SerializeObject(sol);   // To measure the cost
    }

    public void Dispose()
    {
    }
}