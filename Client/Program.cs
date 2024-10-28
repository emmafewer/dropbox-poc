using Grpc.Net.Client;
using Google.Protobuf;
using Grpc.Core;

namespace Client;
internal class Program
{
    const int ChunkSize = 1024 * 32;
    static async Task Main()
    {
        // ignore invalid certificate for local development
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        
        var channel = GrpcChannel.ForAddress("https://localhost:7282",
            new GrpcChannelOptions { HttpHandler = handler });

        var client = new Uploader.UploaderClient(channel);
        
        //Notification code isn't complete/working
        //await SubscribeToNotifications(client);

        //Create a directory to watch 
        var downloadsPath = Environment.GetEnvironmentVariable("PATH");
        var downloadIdPath = Path.Combine(downloadsPath!, "Client1");
        Directory.CreateDirectory(downloadIdPath);
        
        MonitorDirectory(downloadIdPath, client);
        
        Console.ReadKey();
    }

    private static void MonitorDirectory(string path, dynamic client)
    {
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = path;
        watcher.Created += (s, e) => OnFileCreatedAsync(e, client);
        watcher.Renamed += FileSystemWatcher_Renamed;
        watcher.Deleted += FileSystemWatcher_Deleted;
        watcher.EnableRaisingEvents = true;

    }
    
    private static async Task OnFileCreatedAsync(FileSystemEventArgs e, Uploader.UploaderClient client)
    {
        try
        {
            await FileSystemWatcher_Created(e, client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing file {e.Name}: {ex.Message}");
        }
    }
    
    private static async Task FileSystemWatcher_Created(FileSystemEventArgs e, Uploader.UploaderClient client)
    {
        var call = client.UploadFile();
        Console.WriteLine("Sending file metadata");
        
        // Sending metadata to the server
        await call.RequestStream.WriteAsync(new UploadFileRequest
        {
            Metadata = new FileMetadata
            {
                FileName = Path.GetFileName(e.Name) 
            }
        });

        // Prepare a buffer to read the file data in chunks
        var buffer = new byte[ChunkSize];
        await using var readStream = File.OpenRead(e.FullPath); 

        while (true)
        {
            // Read a chunk of data from the file into the buffer
            var count = await readStream.ReadAsync(buffer);
            Console.WriteLine($"Received {count} bytes");
            
            //exit the loop if no more data
            if (count == 0)
            {
                break;
            }

            Console.WriteLine("Sending file data chunk of length " + count);
            await call.RequestStream.WriteAsync(new UploadFileRequest
            {
                Data = ByteString.CopyFrom(buffer, 0, count)
            });
        }

        Console.WriteLine("Complete request");
        await call.RequestStream.CompleteAsync();

        while (await call.ResponseStream.MoveNext())
        {
            var response = call.ResponseStream.Current;
            Console.WriteLine($"Upload id: {response.Id}");
        }

        Console.WriteLine("Shutting down");
    }
    
    private static async Task SubscribeToNotifications(Uploader.UploaderClient client)
    {
        using var subscription = client.Subscribe(new SubscribeRequest { ClientId = "Client1" });
        while (await subscription.ResponseStream.MoveNext())
        {
            var notification = subscription.ResponseStream.Current;
            Console.WriteLine($"Received notification: {notification.Message}");
            // Trigger logic to pull the latest files from the server
            await PullLatestFilesFromServer(client);
        }
    }
    
    private static async Task PullLatestFilesFromServer(Uploader.UploaderClient client)
    {
        // Logic to retrieve the latest files from the server
        var call = client.GetLatestFiles(new GetLatestFilesRequest());
    
        while (await call.ResponseStream.MoveNext())
        {
            var fileResponse = call.ResponseStream.Current;
            var downloadsPath = Environment.GetEnvironmentVariable("PATH");
            string filePath = Path.Combine($"{downloadsPath}/Client2", fileResponse.FileName);
        
            // Download file to the mirrored directory
            await File.WriteAllBytesAsync(filePath, fileResponse.FileData.ToByteArray());
            Console.WriteLine($"Downloaded: {fileResponse.FileName}");
        }
    }

    private static void FileSystemWatcher_Renamed(object sender, FileSystemEventArgs e)

    {
        Console.WriteLine("File renamed: {0}", e.Name);
        byte[] bytes = File.ReadAllBytes(e.FullPath);
        Console.WriteLine("Bytes: {0}", bytes.Length);
        Console.WriteLine("Success");
    }

    private static void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)

    {
        Console.WriteLine("File deleted: {0}", e.Name);
    }
    
}





