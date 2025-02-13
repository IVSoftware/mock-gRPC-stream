From the comments:

> @Jeroen van Langen: I'm not aware of anything existing

Here is an existing IP [WatchDog Timer@Nuget.org](https://www.nuget.org/packages/IVSoftware.Portable.WatchdogTimer/1.3.0-prerelease) you could try. It [does not rely on cancelling tasks](https://github.com/IVSoftware/IVSoftware.Portable.WatchdogTimer.git) and therefore can be less messy.

___
_DISCLOSURE: Yes, I am the author._
___

I did a console mock that shows a typical use.

```
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
```

[![console][1]][1]


  [1]: https://i.sstatic.net/0bdceD9C.png