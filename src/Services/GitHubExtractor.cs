// Project Name: CopilotModeler
// File Name: GitHubExtractor.cs
// Author:  Kyle Crowder
// Github:  OldSkoolzRoolz
// Distributed under Open Source License
// Do not remove file headers




#region

using System.IO.Compression;
using System.Net;
using System.Text;

using Microsoft.Extensions.Logging;

using Octokit;

using Range = Octokit.Range;

#endregion



namespace CopilotModeler.Services;


/// <summary>
///     Provides functionality to interact with GitHub repositories, including searching for repositories,
///     retrieving file paths, fetching file content, and extracting default branch project details.
/// </summary>
public class GitHubExtractor
{

    private readonly GitHubClient _gitHubClient;
    private readonly ILogger<GitHubExtractor> _logger;






    /// <summary>
    ///     Initializes a new instance of the <see cref="GitHubExtractor" /> class.
    /// </summary>
    /// <param name="accessToken">
    ///     The personal access token used for authenticating with the GitHub API.
    /// </param>
    /// <param name="logger">
    ///     The logger instance used for logging operations within the <see cref="GitHubExtractor" />.
    /// </param>
    public GitHubExtractor(string? accessToken, ILogger<GitHubExtractor> logger)
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("AICodingHelper"))
        {
                    Credentials = new Credentials(accessToken)
        };
        _logger = logger;
    }






    /// <summary>
    ///     Retrieves the default branch project archive for a given GitHub repository and extracts contents into a temporary
    ///     folder
    ///     the path of the folder is returned.
    /// </summary>
    /// <param name="repo">The GitHub repository for which to retrieve the default branch project archive.</param>
    /// <returns>
    ///     A string representing the location of the project archive of the given repository.
    /// </returns>
    /// <exception cref="Octokit.ApiException">
    ///     Thrown when an error occurs while communicating with the GitHub API.
    /// </exception>
    /// <exception cref="System.Exception">
    ///     Thrown when an unexpected error occurs during the operation.
    /// </exception>
    public async Task<string> ExtractDefaultBranchProjectAsync(Repository repo)
    {
        if (repo == null) throw new ArgumentNullException(nameof(repo), "Repository cannot be null.");

        try
        {
            // 3. Extract to a temp folder
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _ = Directory.CreateDirectory(tempDir);

            using var zipStream = new MemoryStream(await GetRepositoryArchiveAsync(repo.Id));
            using var archive = new ZipArchive(zipStream);
            await archive.ExtractToDirectoryAsync(tempDir);

            return tempDir;
        }
        catch (ApiException apiEx)
        {
            _logger.LogError(apiEx, $"GitHub API error while retrieving project archive for repository: {repo.FullName}.");

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while retrieving project archive for repository: {repo.FullName}.");

            throw;
        }
    }






    /// <summary>
    ///     Gets C# file paths for a given repository.
    /// </summary>
    /// <param name="repositoryId">The GitHub repository ID.</param>
    /// <param name="owner">The repository owner's login.</param>
    /// <param name="repoName">The repository name.</param>
    /// <returns>A list of TreeItem objects representing C# files.</returns>
    public async Task<List<TreeItem>> GetCSharpRepositoryFilePaths(long repositoryId, string owner, string repoName)
    {
        _logger.LogInformation($"Getting C# file paths for {owner}/{repoName}...");
        var csharpFiles = new List<TreeItem>();

        try
        {
            var repo = await _gitHubClient.Repository.Get(owner, repoName);
            var defaultBranch = repo.DefaultBranch;

            var treeResponse = await _gitHubClient.Git.Tree.GetRecursive(repositoryId, defaultBranch);


            foreach (var treeItem in treeResponse.Tree)
                if (treeItem.Type == TreeType.Blob && treeItem.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    csharpFiles.Add(treeItem);
        }
        catch (RateLimitExceededException rle)
        {
            _logger.LogError("API rate limit exceeded.. Pausing until reset..");
            var reset = GetTimeDifference(rle.Reset.DateTime, DateTime.Now);

            _logger.LogInformation("Time of limit reset={0} seconds from now.", reset.TotalSeconds);
            await PrintRateLimitInfoAsync();
            await Task.Delay(reset.Milliseconds);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"Repository {owner}/{repoName} or its default branch not found. Skipping file path extraction.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting file paths for {owner}/{repoName}: {ex.Message}");
        }

        return csharpFiles;
    }






    /// <summary>
    ///     Retrieves the raw content of a file from a GitHub repository using its blob information.
    /// </summary>
    /// <param name="repos">
    ///     The <see cref="Repository" /> instance representing the GitHub repository containing the file.
    /// </param>
    /// <param name="gitBlob">
    ///     The <see cref="TreeItem" /> representing the file's blob information, including its SHA and path.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The result contains the file content as a
    ///     <see cref="string" />, or <c>null</c> if an error occurs during retrieval.
    /// </returns>
    /// <exception cref="RateLimitExceededException">
    ///     Thrown when the GitHub API rate limit is exceeded. The method will pause and retry the operation after the rate
    ///     limit resets.
    /// </exception>
    /// <remarks>
    ///     The method handles Base64-encoded content and decodes it to UTF-8 if necessary. It also logs errors and retries
    ///     operations when rate limits are exceeded.
    /// </remarks>
    public async Task<string?> GetFileContentAsync(Repository repos, TreeItem gitBlob)
    {
        try
        {
            var blob = await _gitHubClient.Git.Blob.Get(repos.Id, gitBlob.Sha);

            return blob.Encoding == EncodingType.Base64 ? Encoding.UTF8.GetString(Convert.FromBase64String(blob.Content)) : blob.Content;
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogError("API rate limit exceeded.. Pausing until reset..");
            var reset = GetTimeDifference(ex.Reset.DateTime, DateTime.Now);

            _logger.LogInformation("Time of limit reset={0} seconds from now.", reset.TotalSeconds);
            await Task.Delay(reset.Milliseconds);

            // Retry the operation after waiting for the rate limit reset
            return await GetFileContentAsync(repos, gitBlob);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error getting file content for {gitBlob.Path}: {e.Message}");

            return null;
        }
    }






    /// <summary>
    ///     Retrieves the ZIP archive (as a byte array) for the default branch of the given repository using Octokit.
    /// </summary>
    /// <param name="repositoryId">The GitHub repository ID.</param>
    /// <returns>Byte array containing the ZIP archive, or throws if retrieval fails.</returns>
    private async Task<byte[]> GetRepositoryArchiveAsync(long repositoryId)
    {
        try
        {
            // Octokit provides GetArchive method, but you must specify the ArchiveFormat and reference (branch/commit/tag)
            var repo = await _gitHubClient.Repository.Get(repositoryId);
            var defaultBranch = repo.DefaultBranch;

            // Download the archive as a byte array (zipball of the default branch)
            var archiveBytes = await _gitHubClient.Repository.Content.GetArchive(repositoryId, ArchiveFormat.Zipball, defaultBranch);

            return archiveBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to retrieve repository archive for repository ID: {repositoryId}.");

            throw;
        }
    }






    /// <summary>
    ///     Calculates the time difference between two specified <see cref="DateTime" /> values.
    ///     ---Formula = End - Start
    /// </summary>
    /// <param name="startTime">
    ///     The start time of the interval.
    /// </param>
    /// <param name="endTime">
    ///     The end time of the interval.
    /// </param>
    /// <returns>
    ///     A <see cref="TimeSpan" /> representing the difference between <paramref name="endTime" /> and
    ///     <paramref name="startTime" />.
    /// </returns>
    private TimeSpan GetTimeDifference(DateTime startTime, DateTime endTime)
    {
        var difference = endTime - startTime;

        return difference;
    }






    internal async Task PrintRateLimitInfoAsync()
    {
        var info = await _gitHubClient.RateLimit.GetRateLimits();
        Console.WriteLine($"Rate Limit Remaining: {info.Rate.Remaining}");
        Console.WriteLine($"Search Limit Reset in seconds:  {info.Rate.ResetAsUtcEpochSeconds}");
        Console.WriteLine($"Rate Limit Reset: {info.Rate.Reset}");
        Console.WriteLine("Search Limit {0}", info.Resources.Search.Limit);
        Console.WriteLine($"Search Limit Remaining: {info.Resources.Search.Remaining}");
        Console.WriteLine($"Search Limit Reset: {info.Resources.Search.Reset}");
        Console.WriteLine($"Search Limit Reset in seconds: {info.Resources.Search.ResetAsUtcEpochSeconds}");
    }






    /// <summary>
    ///     Searches for C# repositories on GitHub based on star count.
    /// </summary>
    /// <param name="minStars">Minimum stars for a repository to be included.</param>
    /// <param name="resultsPerPage">Number of results per page.</param>
    /// <param name="maxPages">Maximum number of pages to fetch.</param>
    /// <param name="searchTerm">Search term to filter repositories.</param>
    /// <returns>A list of Repository objects.</returns>
    public async Task<List<Repository>> SearchCsharpRepositoriesAsync(int minStars = 500, int resultsPerPage = 50, int maxPages = 2, string searchTerm = "language:C#")
    {
        _logger.LogInformation($"Searching for C# repositories with at least {minStars} stars...");
        var repositories = new List<Repository>();
        var request = new SearchRepositoriesRequest(searchTerm)
        {
                    Stars = Range.GreaterThan(minStars),
                    User = null,
                    Created = null,
                    Updated = DateRange.GreaterThan(DateTimeOffset.Now.AddYears(-1)), //Repo has been updated in the last year. filters outdated code and stagnant repos
                    License = null,
                    Archived = false,
                    Topic = null,
                    Topics = null,
                    CustomProperties = null,
                    SortField = RepoSearchSort.Stars,
                    In = null,
                    Forks = null,
                    Fork = null,
                    Size = null,
                    Language = Language.CSharp,
                    Order = SortDirection.Descending,
                    Page = 0,
                    PerPage = resultsPerPage
        };

        for (var page = 1; page <= maxPages; page++)
        {
            request.Page = page;

            try
            {
                var searchResult = await _gitHubClient.Search.SearchRepo(request);
                _logger.LogInformation($"Page {page}: Found {searchResult.Items.Count} repositories.");

                repositories.AddRange(searchResult.Items);

                if (searchResult.Items.Count < resultsPerPage)
                {
                    _logger.LogInformation("Reached end of results or max pages specified.");

                    break; // No more results
                }

                // Implement a delay to respect GitHub API rate limits
                await Task.Delay(TimeSpan.FromSeconds(5)); // Adjust based on rate limits
            }
            catch (RateLimitExceededException ex)
            {
                _logger.LogError("API rate limit exceeded.. Pausing until reset..");
                var reset = GetTimeDifference(ex.Reset.DateTime, DateTime.Now);

                _logger.LogInformation("Time of limit reset={0} seconds from now.", reset.TotalSeconds);
                await PrintRateLimitInfoAsync();
                await Task.Delay(reset.Milliseconds);
            }
            catch (ApiException ex)
            {
                _logger.LogError($"GitHub API Error: {ex.Message} (Status: {ex.StatusCode})");

                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred during repository search: {ex.Message}");

                break;
            }
        }

        return repositories;
    }

}