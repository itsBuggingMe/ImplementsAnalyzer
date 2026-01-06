using System.Runtime.CompilerServices;

namespace ImplementsAttributeExample;

internal class Program : IDisposable, IA
{
    static void Main()
    {
        Console.WriteLine("Compiled!");
    }

    [Impl<IDisposable>]
    public void Dispose()
    {

    }

    [Impl<IA>]
    public int X { get; set; }
}

interface IA
{
    public int X { get; set; }
}