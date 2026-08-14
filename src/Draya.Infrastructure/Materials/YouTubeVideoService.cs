using Draya.Application.Materials;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Draya.Infrastructure.Materials;

public class YouTubeVideoService : IVideoProviderService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeVideoService> _logger;

    public YouTubeVideoService(IConfiguration configuration, ILogger<YouTubeVideoService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> UploadVideoAsync(string filePath, string title, string description, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting YouTube upload for file: {FilePath}", filePath);

        var youtubeSettings = _configuration.GetSection("YouTubeSettings");
        var clientId = youtubeSettings["ClientId"];
        var clientSecret = youtubeSettings["ClientSecret"];
        var refreshToken = youtubeSettings["RefreshToken"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogError("YouTube settings are missing.");
            throw new InvalidOperationException("YouTube configuration is missing.");
        }


        var credentialWithClient = new UserCredential(
            new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { YouTubeService.Scope.YoutubeUpload }
                }),
            "user",
            new Google.Apis.Auth.OAuth2.Responses.TokenResponse { RefreshToken = refreshToken });

        using var youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credentialWithClient,
            ApplicationName = "Draya-API"
        });

        var video = new Google.Apis.YouTube.v3.Data.Video
        {
            Snippet = new Google.Apis.YouTube.v3.Data.VideoSnippet
            {
                Title = title,
                Description = description,
                CategoryId = "27" // Education
            },
            Status = new Google.Apis.YouTube.v3.Data.VideoStatus
            {
                PrivacyStatus = "unlisted"
            }
        };

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
        videosInsertRequest.ProgressChanged += (progress) =>
        {
            switch (progress.Status)
            {
                case Google.Apis.Upload.UploadStatus.Uploading:
                    _logger.LogInformation("YouTube Upload: {BytesSent} bytes sent.", progress.BytesSent);
                    break;
                case Google.Apis.Upload.UploadStatus.Failed:
                    _logger.LogError(progress.Exception, "YouTube Upload Failed");
                    break;
            }
        };

        var response = await videosInsertRequest.UploadAsync(cancellationToken);

        if (response.Status == Google.Apis.Upload.UploadStatus.Failed)
        {
            throw new Exception("YouTube upload failed.", response.Exception);
        }

        _logger.LogInformation("YouTube upload completed. Video ID: {VideoId}", videosInsertRequest.ResponseBody.Id);
        
        return videosInsertRequest.ResponseBody.Id;
    }
}
