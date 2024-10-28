using Grpc.Core;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Server;

namespace Server.Services;
public class NotificationService : Uploader.UploaderBase
{
    private readonly ConcurrentDictionary<string, IServerStreamWriter<NotificationResponse>> _subscribers = new();

    public async Task Subscribe(SubscribeRequest request, IServerStreamWriter<NotificationResponse> responseStream, ServerCallContext context)
    {
        // Add client to the subscribers list
        _subscribers[request.ClientId] = responseStream;

        try
        {
            // Keep the stream open for the client as long as they are connected
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000); 
            }
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
        {
            Console.WriteLine($"Client {request.ClientId} disconnected.");
        }
        finally
        {
            _subscribers.TryRemove(request.ClientId, out _);
        }
    }

    // Method to send a notification to all connected clients
    public async Task NotifyAllAsync(string message)
    {
        var notification = new NotificationResponse { Message = message };

        foreach (var subscriber in _subscribers)
        {
            try
            {
                // Send the notification message to each client
                await subscriber.Value.WriteAsync(notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying client {subscriber.Key}: {ex.Message}");
                _subscribers.TryRemove(subscriber.Key, out _); 
            }
        }
    }
}