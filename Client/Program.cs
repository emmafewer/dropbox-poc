using System.Threading.Tasks;
using Grpc.Net.Client;
using Client;

// ignore invalid certificate for local development
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = 
    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

// The port number must match the port of the gRPC server.
var channel = GrpcChannel.ForAddress("https://localhost:7282",
    new GrpcChannelOptions { HttpHandler = handler });
var client = new Greeter.GreeterClient(channel);
var reply = await client.SayHelloAsync(
    new HelloRequest { Name = "GreeterClient" });
Console.WriteLine("Greeting: " + reply.Message);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();