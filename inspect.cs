using System;
using System.Linq;
using Nethereum.ABI.Model;

class Program
{
    static void Main()
    {
        var type = typeof(Parameter);
        foreach (var ctor in type.GetConstructors())
        {
            Console.WriteLine(ctor);
        }
        Console.WriteLine("Properties:");
        foreach (var prop in type.GetProperties())
        {
            Console.WriteLine(prop.Name + ": " + prop.PropertyType);
        }
    }
}
