using LLama;
using LLama.Common;
using LLama.Sampling;

namespace ExpenseManager.App.Services;

/// <summary>Runs a small local LLM entirely on-device (via LLamaSharp/llama.cpp) to turn a
/// spending summary into plain-language insights. No network calls after the one-time model
/// download, no API costs, no data ever leaves the machine — a deliberate choice given this is
/// a one-time-purchase app with no subscription revenue to fund a hosted AI API per user.
///
/// Model: Gemma 2 2B Instruct, Q4_K_M GGUF (~1.7 GB) from bartowski's public mirror. Picked over
/// the newer Gemma 4 family because Gemma 2 has long-proven, stable support in llama.cpp/GGUF —
/// Gemma 4's GGUF support is still new and its stability with the llama.cpp version LLamaSharp
/// currently bundles hasn't been verified. Swapping to a different model later only means
/// changing ModelFileName/ModelDownloadUrl below.</summary>
public class AiInsightsService
{
    private const string ModelFileName = "gemma-2-2b-it-Q4_K_M.gguf";
    private const string ModelDownloadUrl =
        "https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf";

    private static readonly string ModelDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ExpenseManagerPro", "AiModel");

    private static string ModelPath => Path.Combine(ModelDirectory, ModelFileName);
    private static string PartialDownloadPath => ModelPath + ".download";

    public bool IsModelDownloaded => File.Exists(ModelPath);

    public async Task DownloadModelAsync(IProgress<double> progress, CancellationToken ct = default)
    {
        Directory.CreateDirectory(ModelDirectory);

        using var client = new HttpClient();
        client.Timeout = Timeout.InfiniteTimeSpan;

        try
        {
            using var response = await client.GetAsync(ModelDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var destination = File.Create(PartialDownloadPath))
            {
                // A 1 MB buffer with reporting throttled to every 0.5% — reporting on every chunk
                // of a small buffer means tens of thousands of progress events for a multi-GB
                // file, each one marshaled onto the UI thread as a property-change + re-render.
                // That floods the UI dispatcher queue badly enough to look like the whole app
                // hanging until the backlog drains, well after the download itself finished.
                var buffer = new byte[1024 * 1024];
                long readSoFar = 0;
                int read;
                var lastReportedPercent = -1.0;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), ct);
                    readSoFar += read;
                    if (totalBytes is > 0)
                    {
                        var percent = (double)readSoFar / totalBytes.Value;
                        if (percent - lastReportedPercent >= 0.005)
                        {
                            progress.Report(percent);
                            lastReportedPercent = percent;
                        }
                    }
                }
                progress.Report(1.0);
            }

            File.Move(PartialDownloadPath, ModelPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(PartialDownloadPath))
                File.Delete(PartialDownloadPath);
            throw;
        }
    }

    public void DeleteModel()
    {
        if (File.Exists(ModelPath))
            File.Delete(ModelPath);
    }

    /// <summary>Loads the model, runs one prompt, and unloads it again — a chat-style app would
    /// keep the model resident, but this is an occasional "generate insights" action, and
    /// releasing the ~2GB back after each run keeps the app's idle footprint small on the
    /// low-end machines this app's users are likely to run on.</summary>
    public async Task<string> GenerateInsightsAsync(string systemPrompt, string userPrompt, IProgress<string>? tokenProgress, CancellationToken ct = default)
    {
        if (!IsModelDownloaded)
            throw new InvalidOperationException("The AI model hasn't been downloaded yet.");

        var parameters = new ModelParams(ModelPath)
        {
            ContextSize = 4096,
            GpuLayerCount = 0 // CPU-only backend — guaranteed to work on any machine, no GPU driver dependency
        };

        using var model = await LLamaWeights.LoadFromFileAsync(parameters, ct);
        var executor = new StatelessExecutor(model, parameters)
        {
            ApplyTemplate = true,
            SystemMessage = systemPrompt
        };

        var inferenceParams = new InferenceParams
        {
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.6f },
            AntiPrompts = ["<end_of_turn>", "User:"],
            MaxTokens = 500
        };

        // Same UI-flooding risk as the download progress above, just smaller scale — reporting on
        // every single generated token can still be a few hundred UI-thread updates for a long
        // response. Throttled to ~10/sec, which still reads as smooth "typing" progress.
        var result = new System.Text.StringBuilder();
        var lastReport = System.Diagnostics.Stopwatch.StartNew();
        await foreach (var token in executor.InferAsync(userPrompt, inferenceParams, ct))
        {
            result.Append(token);
            if (lastReport.ElapsedMilliseconds >= 100)
            {
                tokenProgress?.Report(result.ToString());
                lastReport.Restart();
            }
        }
        tokenProgress?.Report(result.ToString());

        return result.ToString().Trim();
    }
}
