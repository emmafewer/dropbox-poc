using Google.Protobuf;
using Grpc.Core;
using Npgsql;
using Server;

namespace Server.Services;

public class UploaderService : Uploader.UploaderBase
{
    private readonly NotificationService _notificationService;

    public UploaderService(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<UploadFileResponse> UploadFile(IAsyncStreamReader<UploadFileRequest> requestStream, ServerCallContext context)
    {
        string connectionString = ConfigurationService.GetConnectionString();
        // Connect to the PostgreSQL server
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // SQL query for inserting filemetadata
        const string insertMetadataQuery = @"
        INSERT INTO filemetadata (id, created_at, name) 
        VALUES (@id, @createdAt, @name)";

        // SQL query for inserting file data
        const string insertFileDataQuery = @"
        INSERT INTO file (id, created_at, file_data, filemetadata_id) 
        VALUES (@id, @createdAt, @fileData, @fileMetadataId)";

        var fileId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var metadataInserted = false;
        Guid metadataId = Guid.NewGuid(); 

        using var memoryStream = new MemoryStream();

        await foreach (var message in requestStream.ReadAllAsync())
        {
            if (message.Metadata != null && !metadataInserted)
            {
                // Insert metadata 
                await using var metadataCmd = new NpgsqlCommand(insertMetadataQuery, conn);
                metadataCmd.Parameters.AddWithValue("@id", metadataId);
                metadataCmd.Parameters.AddWithValue("@createdAt", createdAt);
                metadataCmd.Parameters.AddWithValue("@name", message.Metadata.FileName);

                await metadataCmd.ExecuteNonQueryAsync();
                metadataInserted = true;
            }

            if (message.Data.Length > 0)
            {
                // Accumulate data chunks in memoryStream
                await memoryStream.WriteAsync(message.Data.ToByteArray());
            }
        }

        // Insert the file data after the stream ends
        if (memoryStream.Length > 0)
        {
            await using var fileDataCmd = new NpgsqlCommand(insertFileDataQuery, conn);
            fileDataCmd.Parameters.AddWithValue("@id", fileId);
            fileDataCmd.Parameters.AddWithValue("@createdAt", createdAt);
            fileDataCmd.Parameters.AddWithValue("@fileData", memoryStream.ToArray());
            fileDataCmd.Parameters.AddWithValue("@filemetadataId", metadataId); 

            await fileDataCmd.ExecuteNonQueryAsync();
        }
        
        await _notificationService.NotifyAllAsync("A new file has been uploaded.");

        return new UploadFileResponse()
        {
            Id = fileId.ToString(),
        };
    }
}