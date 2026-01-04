using System.Runtime.CompilerServices;

namespace ImplementsAttributeExample;

internal class Program : IDisposable
{
    static void Main()
    {
        Console.WriteLine("Compiled!");
    }

    [Impl<IDisposable>]
    public void Dispose()
    {

    }
}