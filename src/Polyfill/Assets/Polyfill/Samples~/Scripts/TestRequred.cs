using System;
using System.Diagnostics.CodeAnalysis;

namespace xpTURN.Polyfill.Samples;

public class Config
{
    public required string Name { get; set; }
    public required int Port { get; init; }

    [SetsRequiredMembers]
    public Config()
    {
        Name = "Jone";
        Port = 1000;
    }
}
