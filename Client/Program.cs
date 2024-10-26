using System.Threading.Tasks;
using Grpc.Net.Client;
using Client;
using Grpc.Core;

// ignore invalid certificate for local development
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = 
    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

// The port number must match the port of the gRPC server.
var channel = GrpcChannel.ForAddress("https://localhost:7282",
    new GrpcChannelOptions { HttpHandler = handler });

var client = new Downloader.DownloaderClient(channel);

//var downloadsPath = Path.Combine(Environment.CurrentDirectory, "downloads");
var downloadsPath = "/Users/emmafewer/Desktop";
var downloadId = Path.GetRandomFileName();
var downloadIdPath = Path.Combine(downloadsPath, downloadId);
Directory.CreateDirectory(downloadIdPath);

Console.WriteLine("Starting call");

using var call = client.DownloadFile(new DownloadFileRequest
{
    Id = downloadId
});

await using var writeStream = File.Create(Path.Combine(downloadIdPath, "data.bin"));

await foreach (var message in call.ResponseStream.ReadAllAsync())
{
    if (message.Metadata != null)
    {
        Console.WriteLine("Saving metadata to file");
        var metadata = message.Metadata.ToString();
        await File.WriteAllTextAsync(Path.Combine(downloadIdPath, "metadata.json"), metadata);
    }
    if (message.Data != null)
    {
        var bytes = message.Data.Memory;
        Console.WriteLine($"Saving {bytes.Length} bytes to file");
        await writeStream.WriteAsync(bytes);
    }
}

Console.WriteLine();
Console.WriteLine("Files were saved in: " + downloadIdPath);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();



/*
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(
    new HelloRequest { Name = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
*/