$args = @{
    Uri = "http://localhost:5000/api/heartbeat"
    Method = "Get"
    Headers = @{ "DiagnosticsAPIKey"="Secret" }
}

Invoke-RestMethod @args -UseBasicParsing

