using System;
using System.Text;
using System.Globalization;
using System.IO;


public class LexicalAnalyzer
{
    private readonly char[] delimiters = { ';', ',' };
    private readonly char[] opersigns = { '+', '-', '*', '=', '|', '!' };
    private readonly string[] keywords = {"abs", "absoluteand", "arctan", "array", "as", "asm", "begin", "boolean", "break",
        "case", "char", "class", "const", "constructor", "continue", "cos", "destructor", "dispose", "div", "do", "downto",
        "else", "end", "eof", "eoln", "except", "exp", "exports", "false", "file", "finalization","finally", "for", "function",
        "goto", "if", "implementation", "in", "inherited", "initialization", "inline", "input", "integer", "interface", "is",
        "label", "library", "ln", "maxint", "mod", "new", "nil", "not", "object", "odd", "of", "on", "operator", "or", "ord",
        "output", "pack", "packed", "page", "pred", "procedure", "program", "property", "raise", "read", "readln", "real",
        "record", "reintroduce", "repeat", "reset", "rewrite", "round", "self", "set", "shl", "shr", "sin", "sqr", "sqrt",
        "string", "succ", "text", "then", "threadvar", "to", "true", "trunc", "try", "type", "unit", "until", "uses", "var",
        "while", "with", "write", "writelnxor"};

    private enum States : int
    {
        error = -1,
        eof = 0,            // конец файла
        delimit = 1,        // разделители
        opersign = 2,       // знаки операций
        keyword = 3,        // ключевые слова
        identifier = 4,     // идентификаторы
        liter = 5,          // символьные литералы
        strliter = 6,       // строковые литералы
        integer = 7,        // целые числа
        real = 8,           // вещественные числа
        shortcomment = 9,   // однострочные комментарии
        longcomment = 10,   // многострочные комментарии
    };

    public LexicalAnalyzer(string path)
	{
        input = File.OpenText(path);
	}

    private StreamReader input;
    private int CharInd = 0;
    private int LineInd = 1;
    private string Str = "";
    private string StrVal = "";
    private int IntVal = -1;
    private double DoubleVal = -1.0;
    private char LastChar = ' ';

    public string GetLexemType(int st)
    {
        return st switch
        {
            0 => "EOF",
            1 => "Delimiter",
            2 => "Operation sign",
            3 => "Keyword",
            4 => "Identifier",
            5 => "Char",
            6 => "String",
            7 => "Integer",
            8 => "Real",
            9 => "Single line comment",
            10 => "Multiple lines comment",
            _ => "Error",
        };
    }

    public string GetLexemValue()
    {
        if (StrVal.Length > 0) return StrVal;
        else if (IntVal != -1) return IntVal.ToString();
        else if (DoubleVal != -1.0) return DoubleVal.ToString();
        else if (input.EndOfStream) return "EOF";
        else return LastChar.ToString();
    }

    public int GetLexemState()
    {
        Str = "";
        StrVal = "";
        IntVal = -1;
        DoubleVal = -1.0;

        do
        {
            if (LastChar.Equals('\n'))
            {
                LineInd++;
                CharInd = 0;
            }
            LastChar = (char)input.Read();
            CharInd++;
        }
        while (char.IsWhiteSpace(LastChar) && !input.EndOfStream);


        if (input.EndOfStream) return (int)States.eof;

        Str = string.Concat(Str, LastChar.ToString());

        foreach (char ch in delimiters)
        {
            if (LastChar.Equals(ch)) return (int)States.delimit;
        }

        foreach (char ch in opersigns)
        {
            if (LastChar.Equals(ch)) return (int)States.opersign;
        }

        if (char.IsLetter(LastChar) || LastChar == '_')
        {
            while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('_'))
            {
                LastChar = (char)input.Read();
                Str = string.Concat(Str, LastChar.ToString());
            }
            StrVal = Str;
            CharInd += Str.Length - 1;
            string strlow = Str.ToLower();
            foreach (string s in keywords)
            {
                if (strlow.Equals(s)) return (int)States.keyword;
            }
            return (int)States.identifier;
        }

        if (LastChar.Equals('\''))
        {
            while (!(LastChar = (char)input.Read()).Equals('\''))
            {
                Str = string.Concat(Str, LastChar.ToString());
            }
            Str = string.Concat(Str, LastChar.ToString());
            StrVal = Str[1..^1];
            CharInd += Str.Length - 1;
            return (int)States.strliter;
        }

        if (LastChar.Equals('/'))
        {
            if (input.Peek().Equals('/')) {
                do
                {
                    LastChar = (char)input.Read();
                    Str = string.Concat(Str, LastChar.ToString());
                } while (!input.Peek().Equals('\n'));
                CharInd += Str.Length - 1;
                StrVal = Str[2..^0];
                return (int)States.shortcomment;
            }
            else return (int)States.opersign;
        }

        if (LastChar.Equals('{'))
        {
            while (!(LastChar = (char)input.Read()).Equals('}'))
            {
                CharInd++;
                Str = string.Concat(Str, LastChar.ToString());
                if (LastChar.Equals('\n'))
                {
                    LineInd++;
                    CharInd = 0;
                }
            }
            Str = string.Concat(Str, LastChar.ToString());
            StrVal = Str[1..^1];
            return (int)States.longcomment;
        }

        if (LastChar.Equals('$'))
        {
            while (char.IsLetterOrDigit((char)input.Peek()))
            {
                LastChar = (char)input.Read();
                Str = string.Concat(Str, LastChar.ToString());
            }
            try
            {
                IntVal = Convert.ToInt32(Str.Remove(0, 1), 16);
            }
            catch (FormatException)
            {
                Console.WriteLine(string.Format("({0}, {1}): The number does not match the Hexadecimal format.", LineInd, CharInd));
                return -1;
            }
            catch (OverflowException)
            {
                Console.WriteLine(string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd));
                return -1;
            }
            CharInd += Str.Length - 1;
            return (int)States.integer;
        }

        if (LastChar.Equals('&'))
        {
            while (char.IsLetterOrDigit((char)input.Peek()))
            {
                LastChar = (char)input.Read();
                Str = string.Concat(Str, LastChar.ToString());
            }
            try
            {
                IntVal = Convert.ToInt32(Str.Remove(0, 1), 8);
            }
            catch (FormatException)
            {
                Console.WriteLine(string.Format("({0}, {1}): The number does not match the octal notation.", LineInd, CharInd));
                return -1;
            }
            catch (OverflowException)
            {
                Console.WriteLine(string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd));
                return -1;
            }
            CharInd += Str.Length - 1;
            return (int)States.integer;
        }

        if (LastChar.Equals('%'))
        {
            while (char.IsLetterOrDigit((char)input.Peek()))
            {
                LastChar = (char)input.Read();
                Str = string.Concat(Str, LastChar.ToString());
            }
            try
            {
                IntVal = Convert.ToInt32(Str.Remove(0, 1), 2);
            }
            catch (FormatException)
            {
                Console.WriteLine(string.Format("({0}, {1}): The number does not match the binary notation.", LineInd, CharInd));
                return -1;
            }
            catch (OverflowException)
            {
                Console.WriteLine(string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd));
                return -1;
            }
            CharInd += Str.Length - 1;
            return (int)States.integer;
        }

        if (char.IsDigit(LastChar))
        {
            while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
            {
                LastChar = (char)input.Read();
                Str = string.Concat(Str, LastChar.ToString());
            }
            if (Str.Contains('.'))
            {
                try
                {
                    DoubleVal = Convert.ToDouble(Str);
                }
                catch (FormatException)
                {
                    Console.WriteLine(string.Format("({0}, {1}): The number does not match the real notation.", LineInd, CharInd));
                    return -1;
                }
                catch (OverflowException)
                {
                    Console.WriteLine(string.Format("({0}, {1}): Overflow in string to double conversion.", LineInd, CharInd));
                    return -1;
                }
                CharInd += Str.Length - 1;
                return (int)States.real;
            }
            else
            {
                try
                {
                    IntVal = Convert.ToInt32(Str);
                }
                catch (FormatException)
                {
                    Console.WriteLine(string.Format("({0}, {1}): The number does not match the normal decimal format.", LineInd, CharInd));
                    return -1;
                }
                catch (OverflowException)
                {
                    Console.WriteLine(string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd));
                    return -1;
                }
                CharInd += Str.Length - 1;
                return (int)States.integer;
            }            
        }

        return (int)States.error;
    }

    public string GetLexem()
    {
        int st = GetLexemState();
        if (st == -1) return null;
        string ans = LineInd.ToString() + " " + (CharInd - Str.Length + 1).ToString() + " " + GetLexemType(st) + " " + GetLexemValue();
        return string.Concat(ans, " ", Str);
    }

    public string GetAllLexems()
    {

        string ans = "";
        string lex = "";
        while (!input.EndOfStream)
        {
            lex = GetLexem();
            if (lex != null) ans = string.Concat(ans, lex, "\n");
            else break;
        }
        return ans;
    }
}
