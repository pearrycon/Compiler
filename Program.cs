using System;
using System.Text;
using System.Globalization;
using System.IO;


namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                switch (args[1])
                {
                    case "-la":
                        LexicalAnalyzer la = new LexicalAnalyzer(args[0]);
                        Console.WriteLine(la.GetAllLexems());
                        break;
                    default:
                        Console.WriteLine("The program is not designed to work with this key.");
                        break;
                }
            }
            else if (args.Length == 1 && args[0] == "-test")
            {
                for (int i = 0; i < 44; i++)
                {
                    string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\tests\";
                    string output = File.ReadAllText(string.Format(projectDirectory + @"\output\{0}.txt", i));
                    LexicalAnalyzer la = new LexicalAnalyzer(string.Format(projectDirectory + @"input\{0}.txt", i));
                    string ans = la.GetAllLexems();
                    if (output.Equals(ans)) Console.WriteLine(string.Format("Test {0} is good", i));
                    else Console.WriteLine(string.Format("Test {0} is bad", i));
                }
            }
            else Console.WriteLine("Incorrect number of arguments entered.");
        }
    }
}
