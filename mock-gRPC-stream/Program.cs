using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using IVSoftware.Portable;

Console.Title = "Mock gRPC stream";
var mockRequestStream = new MockRequestStream(new List<RequestProtoDTO>
        {
            new RequestProtoDTO { Data = "First Request" },
            new RequestProtoDTO { Data = "Second Request" },
            new RequestProtoDTO { Data = "Third Request" }
        });

var mockResponseStream = new MockResponseStream();

var server = new StreamingService();
var serverCallContext = new MockServerCallContext();

await server.StreamingMethodName(mockRequestStream, mockResponseStream, serverCallContext);

Console.WriteLine("Streaming Completed!");
Console.ReadKey();



// Mock Request Stream (Simulates Client Streaming)
public class MockRequestStream : IAsyncStreamReader<RequestProtoDTO>
{
    private readonly Queue<RequestProtoDTO> _requests;
    private RequestProtoDTO? _current;

    public MockRequestStream(IEnumerable<RequestProtoDTO> requests)
    {
        _requests = new Queue<RequestProtoDTO>(requests);
    }

    public RequestProtoDTO Current => _current ?? throw new InvalidOperationException();

    public async Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        await Task.Delay(500); // Simulate network latency
        if (_requests.Count > 0)
        {
            _current = _requests.Dequeue();
            return true;
        }
        return false;
    }
}

// Mock Response Stream (Simulates Server Streaming)
public class MockResponseStream : IServerStreamWriter<ResponseProtoDTO>
{
    public Task WriteAsync(ResponseProtoDTO message)
    {
        Console.WriteLine($"Server Response: {message.Data}");
        return Task.CompletedTask;
    }

    public WriteOptions WriteOptions { get; set; } = new WriteOptions();
}

// Mock Server Call Context (Simulates gRPC Call Context)
public class MockServerCallContext : ServerCallContext
{
    protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(5);
    protected override Metadata RequestHeadersCore => new Metadata();
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override AuthContext AuthContextCore => null!;
    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options) => null!;
    protected override string MethodCore => "MockMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "MockPeer";
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override Metadata ResponseTrailersCore => throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}

// Simulated gRPC Request/Response DTOs
public class RequestProtoDTO
{
    public string Data { get; set; } = string.Empty;
}

public class ResponseProtoDTO
{
    public string Data { get; set; } = string.Empty;
}

public class StreamingService
{
    // <PackageReference Include = "IVSoftware.Portable.WatchdogTimer" Version="1.2.1" />
    // Singleton instance
    
    public WatchdogTimer WDT
    {
        get
        {
            if (_wdt is null)
            {
                _wdt = new WatchdogTimer { Interval = TimeSpan.FromSeconds(5) };
                _wdt.RanToCompletion += (sender, e) =>
                {
                    Console.WriteLine($@"{DateTime.Now:hh\:mm\:ss\:fff}  Time out after {WDT.Interval} of inactivity.");
                };
            }
            return _wdt;
        }
    }
    WatchdogTimer? _wdt = default;


    public async Task StreamingMethodName(
        IAsyncStreamReader<RequestProtoDTO> requestStream,
        IServerStreamWriter<ResponseProtoDTO> responseStream,
        ServerCallContext context)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

        while (await requestStream.MoveNext(cts.Token))
        {
            // THIS IS A MOCK!! FOR TESTING ONLY USE THIS DELAY
            await Task.Delay(1000, context.CancellationToken); 


            var request = requestStream.Current;
            Console.WriteLine($"Processing: {request.Data}");

            var response = new ResponseProtoDTO { Data = $"Processed: {request.Data}" };
            await responseStream.WriteAsync(response);

            Console.WriteLine($@"{DateTime.Now:hh\:mm\:ss\:fff} WDT Running={WDT.Running}");
            WDT.StartOrRestart();
        }
    }
}
