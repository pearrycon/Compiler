using System;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[1] == "-la")
            {
                LexicalAnalyzer la = new LexicalAnalyzer(args[0]);
                Console.WriteLine(la.GetAllLexems());
            }
        }
    }
}
