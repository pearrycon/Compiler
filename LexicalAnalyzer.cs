using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace CS_Compiler_For_FreePascal
{
    public class LexicalAnalyzer
    {
        private readonly char[] delimiters = { ':', ';', ',', '(', ')', '[', ']' };
        private readonly char[] opersigns = { '+', '-', '*', '=', '@', '^', '>', '<', '.'};
        private readonly string[] keywords = {"abs", "absolute", "and", "arctan", "array", "as", "asm", "begin", "boolean", "break",
        "case", "char", "class", "const", "constructor", "continue", "cos", "destructor", "dispose", "div", "do", "downto",
        "else", "end", "eof", "eoln", "except", "exp", "exports", "false", "file", "finalization","finally", "for", "function",
        "goto", "if", "implementation", "in", "inherited", "initialization", "inline", "input", "integer", "interface", "is",
        "label", "library", "ln", "maxint", "mod", "new", "nil", "not", "object", "odd", "of", "on", "operator", "or", "ord",
        "output", "pack", "packed", "page", "pred", "procedure", "program", "property", "raise", "read", "readln", "real",
        "record", "reintroduce", "repeat", "reset", "rewrite", "round", "self", "set", "shl", "shr", "sin", "sqr", "sqrt",
        "string", "succ", "text", "then", "threadvar", "to", "true", "trunc", "try", "type", "unit", "until", "uses", "var",
        "while", "with", "write", "writelnxor", "xor"};

        private readonly char[] braces = { '(', ')' };
        private readonly char[] simpleOperSigns = { '+', '-', '*', '/' };
        public LexicalAnalyzer(string path)
        {
            input = File.OpenText(path);
        }

        public StreamReader input;
        public int CharInd = 0;
        public int LineInd = 1;
        private string Str = "";
        private string StrVal = "";
        private string ErrMsg = "";
        private int IntVal = -1;
        private double DoubleVal = -1.0;
        private int LastChar = ' ';

        private void ClearData()
        {
            Str = "";
            StrVal = "";
            ErrMsg = "";
            IntVal = -1;
            DoubleVal = -1.0;
        }

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
                _ => "Error",
            };
        }

        public string GetLexemValue()
        {
            if (StrVal.Length > 0) return StrVal;
            else if (IntVal != -1) return IntVal.ToString();
            else if (DoubleVal != -1.0) return DoubleVal.ToString().Replace(',', '.');
            else if (LastChar == -1) return "EOF";
            else return ((char)LastChar).ToString();
        }

        public int GetLexemState()
        {
            ClearData();

            if (LastChar == -1) return (int)States.eof;
            do
            {
                if (LastChar.Equals('\n'))
                {
                    LineInd++;
                    CharInd = 0;
                }
                LastChar = input.Read();
                CharInd++;
            }
            while (LastChar != -1 && char.IsWhiteSpace((char)LastChar));
            if (LastChar == -1) return (int)States.eof;

            Str += (char)LastChar;

            foreach (char ch in delimiters)
            {
                if (LastChar.Equals(ch)) return (int)States.delimit;
            }

            foreach (char ch in opersigns)
            {
                if (LastChar.Equals(ch)) return (int)States.opersign;
            }

            if (char.IsLetter((char)LastChar) || LastChar == '_')
            {
                while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('_'))
                {
                    LastChar = input.Read();
                    Str += (char)LastChar;
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
                while (!(LastChar = input.Read()).Equals('\''))
                {
                    if (LastChar == -1)
                    {
                        ErrMsg = string.Format("({0}, {1}): String error: missing closing quote.", LineInd, CharInd);
                        return (int)States.error;
                    }
                    Str += (char)LastChar;
                }
                Str += (char)LastChar;
                StrVal = Str.Length >= 3 ? Str[1..^1] : "";
                CharInd += Str.Length - 1;
                return (int)States.strliter;
            }

            if (LastChar.Equals('/'))
            {
                if (input.Peek().Equals('/'))
                {
                    do
                    {
                        LastChar = input.Read();
                        CharInd++;
                    } while (!input.Peek().Equals('\n') && input.Peek() != -1);
                    return GetLexemState();
                }
                return (int)States.opersign;
            }

            if (LastChar.Equals('{'))
            {
                while (!(LastChar = input.Read()).Equals('}'))
                {
                    CharInd++;
                    if (LastChar == -1) return (int)States.eof;
                    if (LastChar.Equals('\n'))
                    {
                        LineInd++;
                        CharInd = 0;
                    }
                }
                CharInd++;
                return GetLexemState();
            }

            if (LastChar.Equals('$'))
            {
                while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
                {
                    LastChar = input.Read();
                    Str += (char)LastChar;
                }
                try
                {
                    IntVal = Convert.ToInt32(Str.Remove(0, 1), 16);
                }
                catch (FormatException)
                {
                    ErrMsg = string.Format("({0}, {1}): The number does not match the Hexadecimal format.", LineInd, CharInd);
                    return (int)States.error;
                }
                catch (OverflowException)
                {
                    ErrMsg = string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd);
                    return (int)States.error;
                }
                CharInd += Str.Length - 1;
                return (int)States.integer;
            }

            if (LastChar.Equals('&'))
            {
                while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
                {
                    LastChar = input.Read();
                    Str += (char)LastChar;
                }
                try
                {
                    IntVal = Convert.ToInt32(Str.Remove(0, 1), 8);
                }
                catch (FormatException)
                {
                    ErrMsg = string.Format("({0}, {1}): The number does not match the octal notation.", LineInd, CharInd);
                    return (int)States.error;
                }
                catch (OverflowException)
                {
                    ErrMsg = string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd);
                    return (int)States.error;
                }
                CharInd += Str.Length - 1;
                return (int)States.integer;
            }

            if (LastChar.Equals('%'))
            {
                while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
                {
                    LastChar = input.Read();
                    Str += (char)LastChar;
                }
                try
                {
                    IntVal = Convert.ToInt32(Str.Remove(0, 1), 2);
                }
                catch (FormatException)
                {
                    ErrMsg = string.Format("({0}, {1}): The number does not match the binary notation.", LineInd, CharInd);
                    return (int)States.error;
                }
                catch (OverflowException)
                {
                    ErrMsg = string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd);
                    return (int)States.error;
                }
                CharInd += Str.Length - 1;
                return (int)States.integer;
            }

            if (char.IsDigit((char)LastChar))
            {
                while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
                {
                    LastChar = input.Read();
                    Str += (char)LastChar;
                    if (LastChar.Equals('.')) break;
                }
                if (LastChar.Equals('.'))
                {
                    while (char.IsLetterOrDigit((char)input.Peek()) || input.Peek().Equals('.'))
                    {
                        LastChar = input.Read();
                        Str += (char)LastChar;
                        if (LastChar.Equals('e') || LastChar.Equals('E'))
                        {
                            LastChar = input.Read();
                            Str += (char)LastChar;
                        }
                    }
                    try
                    {
                        DoubleVal = Convert.ToDouble(Str, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        ErrMsg = string.Format("({0}, {1}): The number does not match the real notation.", LineInd, CharInd);
                        return (int)States.error;
                    }
                    catch (OverflowException)
                    {
                        ErrMsg = string.Format("({0}, {1}): Overflow in string to double conversion.", LineInd, CharInd);
                        return (int)States.error;
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
                        ErrMsg = string.Format("({0}, {1}): The number does not match the normal decimal format.", LineInd, CharInd);
                        return (int)States.error;
                    }
                    catch (OverflowException)
                    {
                        ErrMsg = string.Format("({0}, {1}): Overflow in string to int conversion.", LineInd, CharInd);
                        return (int)States.error;
                    }
                    CharInd += Str.Length - 1;
                    return (int)States.integer;
                }
            }
            ErrMsg = string.Format("({0}, {1}): Unexpected lexem.", LineInd, CharInd);
            return (int)States.error;
        }

        public string[] GetLexem()
        {
            int st = GetLexemState();
            if (st == (int)States.error) return null;
            int dif = Str.Length > 0 ? Str.Length - 1 : 0;
            string[] ans = { LineInd.ToString(), (CharInd - dif).ToString(), GetLexemType(st), GetLexemValue(), "" };
            ans[4] = st == (int)States.eof ? "EOF" : Str;
            return ans;
        }

        public string GetAllLexems()
        {
            string ans = "";
            string[] lex = { };
            while (LastChar != -1)
            {
                lex = GetLexem();
                if (lex != null) ans += lex[0] + ' ' + lex[1] + ' ' + lex[2] + ' ' + lex[3] + ' ' + lex[4] + '\n';
                else break;
            }
            return lex == null ? ErrMsg : ans.Trim();
        }

        public string GetSimpleExpression(List<string> expr, int depth)
        {
            string ans = "";
            for (int i = 0; i < depth; i++) ans += "\t";
            if (expr.Count == 1)
            {
                return ans + expr[0];
            }
            int br = 0;
            int maxPriority = int.MaxValue, ind = 0;
            Dictionary<string, int> priority = new Dictionary<string, int>()
        {
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 }
        };
            List<string> leftExpr = new List<string>(), rightExpr = new List<string>();
            for (int i = 0; i < expr.Count; i++)
            {
                if (expr[i] == "(") br++;
                else if (expr[i] == ")") br--;
                else if (string.Concat(simpleOperSigns).Contains(expr[i]))
                {
                    if (priority[expr[i]] + 2 * br <= maxPriority)
                    {
                        maxPriority = priority[expr[i]] + 2 * br;
                        ind = i;
                    }
                }
            }
            if (ind == 0) return ans + expr[expr.Count / 2];
            int extraBr = (maxPriority - 1) / 2;
            leftExpr.AddRange(expr.GetRange(extraBr, ind - extraBr));
            rightExpr.AddRange(expr.GetRange(ind + 1, expr.Count - 1 - ind - extraBr));
            ans += expr[ind] + "\n\n" + GetSimpleExpression(leftExpr, depth + 1) + "\n\n" + GetSimpleExpression(rightExpr, depth + 1);
            return ans;
        }

        public string GetSimpleExpression()
        {
            List<string> expression = new List<string>();
            int lBraceCount = 0;
            bool havingNeededExpr = false;
            while (true)
            {
                int state = GetLexemState();
                if (state == (int)States.eof) break;
                if (state == (int)States.error) return ErrMsg;
                string lex = GetLexemValue();
                if (state == (int)States.integer || state == (int)States.real || state == (int)States.identifier ||
                    string.Concat(simpleOperSigns).Contains(lex) || string.Concat(braces).Contains(lex))
                {
                    expression.Add(lex);

                    if (string.Concat(simpleOperSigns).Contains(lex))
                    {
                        if (!havingNeededExpr)
                        {
                            ErrMsg = string.Format("({0}, {1}): The operation has no left expression.", LineInd, CharInd);
                            return ErrMsg;
                        }
                        havingNeededExpr = false;
                    }
                    else if (lex == "(")
                    {
                        lBraceCount++;
                        havingNeededExpr = false;
                    }
                    else if (lex == ")")
                    {
                        lBraceCount--;
                        havingNeededExpr = true;
                    }
                    else havingNeededExpr = true;
                }
                else
                    ErrMsg = string.Format("({0}, {1}): Unexpected lexem for simple expressions.", LineInd, CharInd);
            }
            if (!havingNeededExpr)
                ErrMsg = string.Format("({0}, {1}): The operation has no right expression.", LineInd, CharInd);
            if (lBraceCount > 0)
                ErrMsg = string.Format("({0}, {1}): The operation has no right expression.", LineInd, CharInd);
            if (ErrMsg != "") return ErrMsg;
            return GetSimpleExpression(expression, 0);
        }
    }
}