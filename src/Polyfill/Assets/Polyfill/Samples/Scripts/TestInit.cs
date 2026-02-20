using System;

namespace xpTURN.Polyfill.Samples;

public class Data
{
    public string Id { get; init; }
    public int Value { get; init; }

    public Data()
    {
        Id = "a";
        Value = 1;
    }

    public void Setup()
    {
        // CS8852: Init-only property 'Data.Value' can only be set in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
        // Id = "a";
        // Value = 1;
    }

    static public Data d = new Data { Id = "a", Value = 1 };
}
