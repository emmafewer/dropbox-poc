using Google.Protobuf;
using Grpc.Core;
using Server;

namespace Server.Services;

public class DownloaderService : Downloader.DownloaderBase
{
    private readonly ILogger<DownloaderService> _logger;
    private const int ChunkSize = 1024 * 32;

    public DownloaderService(ILogger<DownloaderService> logger)
    {
        _logger = logger;
    }

    public override async Task DownloadFile(DownloadFileRequest request, IServerStreamWriter<DownloadFileResponse> responseStream, ServerCallContext context)
    {
        var requestParam = request.Id; 
        var filename = requestParam switch
        {
            "4" => "pancakes4.png",
            _ => "pancakes.jpg",
        };

        await responseStream.WriteAsync(new DownloadFileResponse
        {
            Metadata = new FileMetadata { FileName = filename }
        });

        var buffer = new byte[ChunkSize];
        await using var fileStream = File.OpenRead(filename);

        while (true)
        {
            var numBytesRead = await fileStream.ReadAsync(buffer);
            if (numBytesRead == 0)
            {
                break;
            }

            _logger.LogInformation("Sending data chunk of {numBytesRead} bytes", numBytesRead);
            await responseStream.WriteAsync(new DownloadFileResponse
            {
                Data = UnsafeByteOperations.UnsafeWrap(buffer.AsMemory(0, numBytesRead))
            }) ;
        }
    }
}