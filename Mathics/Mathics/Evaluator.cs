// =============================================================================================
// EVALUATOR CLASS
//
// This class provides a simple script to evaluate mathematical expressions on C# passed as
// strings. It can be included on any project and the predefined functions can be changed
// at will easily.
//
//      Usage:
//      =====
//      Evaluator ev = new Evaluator();
//      string expression = "2+2";
//      double result = ev.EvaluateExpression(expression);
//
// The evaluator uses the Shunting Yard Algorithm, taking advantage of the right-polish notation.
//
// IMPORTANT: Make sure the system's decimal separator is a dot ('.'). If it's a comma it can bring
// problems, since arguments in functions with multiple arguments are separated with commas.
// IMPORTANT: Spaces are omitted.
// IMPORTANT: If a new variable is defined with the same name of a function, the variable will
// override the function.
// IMPORTANT: 'π' (pi) and 'e' are by default defined as their corresponding values. You can change
// the predefined variables too.
//
// If there is an error in the expression, it will throw an error. Possible error messages are:
// - Invalid character '$'
// - Input empty (when the evaluated expression is an empty string)
// - Mismatched parenthesis
// - Invalid expression
// - Unknown variable or function
// - Factorial (!) works only with integers
//
// Public methods:
// - AddVariable(name,value) -> add a new variable to use on the evaluation
// - ClearVariable(name) -> clear one or all variables
// - EvaluateExpression(expression) -> evaluate expression (string) and return result (double)
//
// Francisco Kovacevich. 2020. Open license (feel free to use it as you want).
// =============================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mathics
{
    // =========================================================================================
    // PREDEFINED FUNCTIONS
    // You can edit and add more functions at will.
    // =========================================================================================
    class Functions
    {
        public double sin(double x) { return Math.Sin(x); }
        public double cos(double x) { return Math.Cos(x); }
        public double tan(double x) { return Math.Tan(x); }
        public double csc(double x) { return 1 / Math.Sin(x); }
        public double sec(double x) { return 1 / Math.Cos(x); }
        public double cot(double x) { return 1 / Math.Tan(x); }
        public double sinh(double x) { return Math.Sinh(x); }
        public double cosh(double x) { return Math.Cosh(x); }
        public double tanh(double x) { return Math.Tanh(x); }
        public double asin(double x) { return Math.Asin(x); }
        public double acos(double x) { return Math.Acos(x); }
        public double atan(double x) { return Math.Atan(x); }
        public double atan2(double x, double y) { return Math.Atan2(x, y); }
        public double exp(double x) { return Math.Exp(x); }
        public double log(double x, double y) { return Math.Log(x) / Math.Log(y); }
        public double ln(double x) { return Math.Log(x); }
        public double abs(double x) { return Math.Abs(x); }
        public double sqrt(double x) { return Math.Sqrt(x); }
        public double round(double x, double y) { return Math.Round(x, Convert.ToInt32(y)); }
        public double fix(double x) { return Math.Truncate(x); }
        public double sign(double x) { return Math.Sign(x); }
        public double ceiling(double x) { return Math.Ceiling(x); }
        public double floor(double x) { return Math.Floor(x); }
        public double mod(double x, double y) { return Math.IEEERemainder(x, y); }
    }
    
    // =========================================================================================
    // MAIN CLASS
    // =========================================================================================
    class Evaluator
    {
        // Token types
        private enum TkenType { Function, Number, OpenParenthesis, ClosingParenthesis, Operator };

        // Token class
        private class Tken
        {
            public TkenType Type = 0;
            public string Name = "";
            public double Value = 0;
        }

        // Define list of variables as a dictionary
        public Dictionary<string, double> Variables = new Dictionary<string, double>();
        
        // Method to add a new variable to the list of variables
        public void AddVariable( string name, double value)
        {
            Variables[name] = value;
        }

        // Method to remove a variable from the list of variables
        public void ClearVariable(string name = "")
        {
            if (name == "") { Variables.Clear(); }
            if (Variables.ContainsKey(name)) { Variables.Remove(name); }

        }

        // Returns the importance of the token as in PEMDAS
        private int GetPrecedence(string token)  
        {
            if (token == "(" || token == ")") return 5;
            if (token == "+" || token == "-") return 4;
            if (token == "*" || token == "/") return 3;
            if (token == "^" || token == "++" || token == "--") return 2;
            if (token == "!") return 1;
            return 0;
        }

        /*
        // =====================================================================================
        // HELPER FUNCTIONS FOR DEBUGGING
        // =====================================================================================
        private void DisplayStack(List<Tken> Stack)
        {
            string msg = "";
            foreach (Tken s in Stack)
                msg += s.Value + "\t" + s.Name + "\t" + s.Type + "\n";
            MessageBox.Show(msg);
        }
        private void DisplayList(List<string> Stack)
        {
            string msg = "";
            foreach (string s in Stack)
                msg += s + "\n";
            MessageBox.Show(msg);
        }
        */

        // =====================================================================================
        // MAIN FUNCTION (EVAULATE)
        // Call this function to evaluate an expression (string) and get a ressult (double)
        // 
        // The main function is implemented in three parts:
        // 1. Tokenize
        // 2. Reorder
        // 3. Reduce
        //
        // The Tokenize function recieves the input and returns a stack (list) of tokens
        // (at this point, tokens are represented as strings, and not yet as Tken class).
        // Each token is an individual element of the mathematical expression. Thus,
        // tokens can be functions, numbers, parenthesis or operators (+,-,*,/,^,!).
        //
        // The Reorder function takes the TokenStack from Tokenize and orders it according to
        // the right polish notation. To do this, tokens are sorted by type and exchanged
        // between a Temp stack and an Output stack.
        //
        // The Reduce function reduces de Output stack by performing operations according to
        // the right polish notation, and then outputs the result.
        // =====================================================================================
        public double EvaluateExpression(string expression)
        {
            if (expression.Contains("$")) throw new System.InvalidOperationException("Invalid character '$'");
            expression = expression.Replace(" ", "");
            return ReduceStack(Reorder(Tokenize(expression)));
        }

        // =====================================================================================
        // PART 1. TOKENIZE
        // Takes an expression as string and populates the TokenStack (List of string)
        // =====================================================================================
        private List<string> Tokenize(string input)
        {
            List<string> TokenStack = new List<string>();

            if (input == "") throw new System.InvalidOperationException("Input empty");
            input = input.Replace(" ", "").Replace("\n", "");

            // Identify main tokens with the "$" symbol
            string[] MainTokens = { "+", "-", "*", "/", "(", ")", "^", "!", "," };
            foreach (string token in MainTokens)
                input = input.Replace(token, "$" + token + "$");

            // Replace '-' with a '+' and a unary minus ('--')
            input = input.Replace("$+$$-$", "$-$");
            if (input.StartsWith("$-$")) input = "$--$" + input.Substring(3);
            if (input.StartsWith("$+$")) input = input.Substring(3);
            input = input.Replace("$-$", "$+$$--$");


            // Speparate string
            string[] Tokens = input.Split('$');
            TokenStack.AddRange(Tokens);

            // Remove markers "$" and empty tokens
            while (TokenStack.Contains("$") || TokenStack.Contains(""))
            {
                TokenStack.Remove("$");
                TokenStack.Remove("");
            }

            // Identify if a + or - is unary
            string prevtoken = "";
            for (int i = 0; i < TokenStack.Count; i++)
            {
                string token = TokenStack[i];
                if (token == "+" || token == "-")
                    if (prevtoken == "*" || prevtoken == "/" || prevtoken == "^" || prevtoken == "(")
                        TokenStack[i] = TokenStack[i] + TokenStack[i]; // unary plus is represented as '++' and unary minus as '--'
                
                prevtoken = token;
            }
            
            // Note: at this point, tokens have been added to the TokenStack as strings, not as Tken
            return TokenStack;
        }

        // =====================================================================================
        // PART 2. REORDER
        // Order the TokenStack according to the Right Polish Notation
        // =====================================================================================
        private List<Tken> Reorder(List<String> TokenStack)
        {
            List<Tken> TempStack = new List<Tken>();
            List<Tken> OutputStack = new List<Tken>();

            // Walk through the TokenStack and identify the type of each token (tokens in the TokenStack are still strings).
            foreach (string token in TokenStack)
            {
                Tken newToken = new Tken();

                // a) Number -> Add to Output
                double t = 0;
                if (double.TryParse(token, out t))
                {
                    newToken.Type = TkenType.Number;
                    newToken.Value = t;
                    OutputStack.Add(newToken);
                }

                // b) Operator -> Deplete TempStack until we find a function or a token of lower precedence
                else if (token == "+" || token == "-" || token == "*" || token == "/" || token == "^" || token == "!" || token == "--" || token == "++")
                {
                    while ((TempStack.Count > 0 && TempStack[TempStack.Count - 1].Type == TkenType.Function) || (TempStack.Count > 0 && GetPrecedence(TempStack[TempStack.Count - 1].Name) < GetPrecedence(token)))
                    {
                        OutputStack.Add(TempStack[TempStack.Count - 1]);
                        TempStack.RemoveAt(TempStack.Count - 1);
                    }

                    newToken.Type = TkenType.Operator;
                    newToken.Name = token;
                    TempStack.Add(newToken);
                }

                else if (token == ",")
                {
                    continue;
                }

                // c) Opening parenthesis -> Add to Temp
                else if (token == "(")
                {
                    newToken.Type = TkenType.OpenParenthesis;
                    newToken.Name = "(";
                    TempStack.Add(newToken);
                }

                // d) Closing parenthesis -> Deplete TempStack until we find an opening parenthesis
                else if (token == ")")
                {
                    if (TempStack.Count == 0)
                        throw new System.InvalidOperationException("Mismatched parenthesis");
                    while (TempStack[TempStack.Count - 1].Type != TkenType.OpenParenthesis)
                    {
                        OutputStack.Add(TempStack[TempStack.Count - 1]);
                        TempStack.RemoveAt(TempStack.Count - 1);
                        if (TempStack.Count == 0)
                            throw new System.InvalidOperationException("Mismatched parenthesis");
                    }
                    TempStack.RemoveAt(TempStack.Count - 1);
                }

                // e) Variable -> Find variable value and add it to Output
                else if (Variables.ContainsKey(token))
                {
                    newToken.Type = TkenType.Number;
                    newToken.Value = Variables[token];
                    OutputStack.Add(newToken);
                }
                else if (token == "e")
                {
                    newToken.Type = TkenType.Number;
                    newToken.Value = Math.E;
                    OutputStack.Add(newToken);
                }
                else if (token == "π")
                {
                    newToken.Type = TkenType.Number;
                    newToken.Value = Math.PI;
                    OutputStack.Add(newToken);
                }

                // f) Function -> Add to Temp
                else
                {
                    newToken.Type = TkenType.Function;
                    newToken.Name = token;
                    TempStack.Add(newToken);
                }
            }

            if (TempStack.Count > 0 && (TempStack[TempStack.Count - 1].Name == "(" || TempStack[TempStack.Count - 1].Name == ")"))
                throw new System.InvalidOperationException("Mismatched parenthesis");

            // Deplete remaining of the Temp stack
            OutputStack.AddRange(TempStack.ToArray().Reverse());

            // Rreturn Output
            return OutputStack;
        }

        // =====================================================================================
        // PART 3. REDUCE
        // Walk through the TokenStack and perform operations according to the Right Polish Not.
        // =====================================================================================
        private double ReduceStack(List<Tken> Stack)
        {
            Functions F = new Functions();

            int n = 0;
            while (true)
            {
                if (Stack.Count == 0) throw new System.InvalidOperationException("Invalid expression");
                if (Stack[n].Type == TkenType.Number) n++;

                else if (Stack[n].Type == TkenType.Function)
                {
                    Type thisType = F.GetType();
                    MethodInfo func = thisType.GetMethod(Stack[n].Name);
                    if (func == null) throw new System.InvalidOperationException("Unkown variable or function (" + Stack[n].Name + ")");

                    int param_count = func.GetParameters().Length;

                    Object[] param = new Object[param_count];
                    for (int j = 0; j < param_count; j++)
                    {
                        if (n < j + 1) throw new System.InvalidOperationException("Invalid expression");
                        param[j] = Stack[n - j - 1].Value;
                    }

                    Stack[n].Value = Convert.ToDouble(func.Invoke(F, param));
                    Stack[n].Type = TkenType.Number;

                    for (int j = 0; j < param_count; j++)
                    {
                        Stack.RemoveAt(n - 1);
                        n = n - 1;
                    }

                }

                else if (Stack[n].Type == TkenType.Operator)
                {
                    if (Stack[n].Name == "++") Stack[n].Value = Stack[n - 1].Value;
                    if (Stack[n].Name == "--") Stack[n].Value = -Stack[n - 1].Value;
                    if (Stack[n].Name == "!")
                    {
                        if (Stack[n - 1].Value != Math.Floor(Stack[n - 1].Value)) throw new System.InvalidOperationException("Factorial (!) works only with integers");
                        Stack[n].Value = 1;
                        for (int j = 1; j <= Stack[n - 1].Value; j++)
                            Stack[n].Value = Stack[n].Value * j;
                    }
                    if (Stack[n].Name == "+") Stack[n].Value = Stack[n - 2].Value + Stack[n - 1].Value;
                    if (Stack[n].Name == "-") Stack[n].Value = Stack[n - 2].Value - Stack[n - 1].Value;
                    if (Stack[n].Name == "*") Stack[n].Value = Stack[n - 2].Value * Stack[n - 1].Value;
                    if (Stack[n].Name == "/") Stack[n].Value = Stack[n - 2].Value / Stack[n - 1].Value;
                    if (Stack[n].Name == "^") Stack[n].Value = Math.Pow(Stack[n - 2].Value, Stack[n - 1].Value);

                    Stack[n].Type = TkenType.Number;
                    if (Stack[n].Name == "++" || Stack[n].Name == "--" || Stack[n].Name == "!")
                    {
                        Stack.RemoveAt(n - 1);
                        n = n - 1;
                    }
                    else
                    {
                        Stack.RemoveAt(n - 1);
                        Stack.RemoveAt(n - 2);
                        n = n - 2;
                    }
                }
                if (Stack.Count == 1) break;
            }
            return Stack[0].Value;
        }


    }
}
