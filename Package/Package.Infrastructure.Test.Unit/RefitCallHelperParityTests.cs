using Package.Infrastructure.Utility.UI;
using Refit;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class RefitCallHelperParityTests
{
    [TestMethod]
    public async Task Timeout_classification_parity_between_full_and_slim()
    {
        static async Task<string> Slow(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return "ok";
        }

        var full = await RefitCallHelperFull.TryApiCallAsync(
            Slow,
            options: new RefitCallHelperFull.CallOptions(
                Timeout: TimeSpan.FromMilliseconds(50),
                FailOnTimeout: false,
                OperationName: "TimeoutParity"));

        var slim = await RefitCallHelperSlim.TryApiCallAsync(
            Slow,
            failOnTimeout: false,
            timeout: TimeSpan.FromMilliseconds(50),
            operationName: "TimeoutParity");

        Assert.IsFalse(full.IsSuccess);
        Assert.IsFalse(slim.IsSuccess);
        Assert.AreEqual(504, full.Problem?.Status);
        Assert.AreEqual(504, slim.Problem?.Status);
    }

    [TestMethod]
    public async Task Network_error_classification_parity_between_full_and_slim()
    {
        Task<string> Fail() => Task.FromException<string>(new HttpRequestException("network down"));

        var full = await RefitCallHelperFull.TryApiCallAsync(Fail,
            options: new RefitCallHelperFull.CallOptions(OperationName: "NetworkParity"));

        var slim = await RefitCallHelperSlim.TryApiCallAsync(Fail,
            operationName: "NetworkParity");

        Assert.IsFalse(full.IsSuccess);
        Assert.IsFalse(slim.IsSuccess);
        Assert.AreEqual(503, full.Problem?.Status);
        Assert.AreEqual(503, slim.Problem?.Status);
    }

    [TestMethod]
    public async Task Wasm_streaming_error_classification_parity_between_full_and_slim()
    {
        Task<string> Fail() => Task.FromException<string>(new InvalidOperationException("Synchronous reads are not supported by BrowserHttpReadStream."));

        var full = await RefitCallHelperFull.TryApiCallAsync(Fail,
            options: new RefitCallHelperFull.CallOptions(OperationName: "WasmParity"));

        var slim = await RefitCallHelperSlim.TryApiCallAsync(Fail,
            operationName: "WasmParity");

        Assert.IsFalse(full.IsSuccess);
        Assert.IsFalse(slim.IsSuccess);
        Assert.AreEqual(500, full.Problem?.Status);
        Assert.AreEqual(500, slim.Problem?.Status);
        Assert.AreEqual(".NET 10 WASM Configuration Required", full.Problem?.Title);
        Assert.AreEqual(".NET 10 WASM Configuration Required", slim.Problem?.Title);
        Assert.AreEqual("WasmParity", full.Problem?.Extensions?["operation"] as string);
        Assert.AreEqual("WasmParity", slim.Problem?.Extensions?["operation"] as string);
    }

    [TestMethod]
    public async Task Noop_path_returns_expected_problem_for_full_and_slim()
    {
        ProblemDetails? callbackProblem = null;

        var full = await RefitCallHelperFull.TryApiCallIfAsync(
            condition: false,
            apiCall: () => Task.FromResult("ignored"),
            noOpReason: "skipped",
            options: new RefitCallHelperFull.CallOptions(OperationName: "NoOpParity"),
            onFailure: pd => callbackProblem = pd);

        var slim = await RefitCallHelperSlim.TryApiCallIfAsync(
            condition: false,
            apiCall: () => Task.FromResult("ignored"),
            noOpReason: "skipped",
            operationName: "NoOpParity");

        Assert.IsFalse(full.IsSuccess);
        Assert.IsFalse(slim.IsSuccess);
        Assert.AreEqual(460, full.Problem?.Status);
        Assert.AreEqual(460, slim.Problem?.Status);
        Assert.AreEqual("No Operation", full.Problem?.Title);
        Assert.AreEqual("No Operation", slim.Problem?.Title);
        Assert.IsNotNull(callbackProblem);
        Assert.AreEqual(460, callbackProblem.Status);
    }
}
