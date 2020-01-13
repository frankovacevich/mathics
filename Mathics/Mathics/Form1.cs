using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mathics
{
    
    public partial class Form1 : Form
    {
        public Form1() { InitializeComponent(); }

        // =========================================================================================
        // IMPORTANT CLASSES
        // =========================================================================================

        public class Settings
        {
            // We create a new class for variables to make it easier to serialize
            public class SerializableVariable { public double Value; public string Name; }

            // This is a general class to serialize all data into a single file easily
            public class SerialSetting
            {
                public List<Entry> History;
                public List<SerializableVariable> VariablesList;
            }

            // SAVE DATA METHOD
            public void SaveData(List<Entry> History, Dictionary<string, double> Variables)
            {
                SerialSetting SS = new SerialSetting();
                SS.History = History;
                SS.VariablesList = DictToList(Variables);

                System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(SerialSetting));
                System.IO.FileStream file = System.IO.File.Create("info.dat");
                writer.Serialize(file, SS);
            }

            // LOAD DATA METHOD
            public (List<Entry> History, Dictionary<string, double> Variables) LoadData()
            {
                SerialSetting SS = new SerialSetting();
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(SerialSetting));
                System.IO.StreamReader file = new System.IO.StreamReader("info.dat");
                SS = (SerialSetting)reader.Deserialize(file);
                file.Close();
                return (SS.History,ListToDict(SS.VariablesList));
            }
            
            // HELPER FUNCTION TO CONVERT DICT TO LIST
            private List<SerializableVariable> DictToList(Dictionary<string,double> dict)
            {
                List<SerializableVariable> list = new List<SerializableVariable>();
                foreach(string key in dict.Keys)
                    list.Add(new SerializableVariable() { Name = key, Value = dict[key] });
                return list;
            }

            // INVERSE HELPER FUNCTION TO CONVERT LIST TO DICT
            private Dictionary<string,double> ListToDict(List<SerializableVariable> list)
            {
                Dictionary<string, double> dict = new Dictionary<string, double>();
                foreach(SerializableVariable var in list)
                    dict[var.Name] = var.Value;
                return dict;
            }

        }

        public class Entry
        {
            public int ID;
            public String Text1; // user input
            public String Text2; // result
        }

        // =========================================================================================
        // DEFINE GLOBALS
        // =========================================================================================
        private int CurrentID = 1;

        // Create a history of entries (searcheable with the up and down arrows)
        public List<Entry> History = new List<Entry>();
        int HistoryNav = 0;

        // Initialize settings and evaluator
        Settings st = new Settings();
        Evaluator ev = new Evaluator();

        // Create a string array to store function names (populated at form load)
        public string[] FunctionNames;
        public string[] KeyWords = { "clear","precision","help","about","test","print" };

        // =========================================================================================
        // ON START (FORM LOAD) AND ON END (FORM CLOSING)
        // =========================================================================================
        private void Form1_Load(object sender, EventArgs e)
        {
            // Load Function names using reflection from the Functions class from evaluator
            // Do this to avoid naming a new variable with the name of a predefined function
            Type myType = typeof(Functions);
            MethodInfo[] MethodInfos = myType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            FunctionNames = new string[MethodInfos.Length];
            for (int i = 0; i < FunctionNames.Length; i++) { FunctionNames[i] = MethodInfos[i].Name; }
            

            // Load variables and history from file
            if (System.IO.File.Exists("info.dat"))
            {
                // Load location and size
                this.Location = new Point(Properties.Settings.Default.Location.X, Properties.Settings.Default.Location.Y);
                this.Size = Properties.Settings.Default.Size;

                // Check how much time it takes to load
                DateTime startTime;
                startTime = DateTime.Now;

                // Load variables and history
                var data = st.LoadData();
                ev.Variables = data.Variables;
                History.AddRange(data.History);

                // Populate panel with controls from history
                foreach (Entry ent in data.History)
                {
                    AddNewEntryControl(ent.Text1, ent.Text2, ent.ID);
                    if (ent.ID >= CurrentID) CurrentID = ent.ID + 1;
                }

                // If it's taking too much time to load -> ask user to perform a 'clear'
                double total_time = ((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds;
                if (total_time > 7000)
                {
                    if(MessageBox.Show("Mathics is taking too much time to load. Do you want to perform a 'clear' to help it load faster?", "Mathics",MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        panel2.Controls.Clear();
                        History.Clear();
                        ev.ClearVariable();
                        CurrentID = 1;
                    }
                }

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Location = new Point(this.Location.X, this.Location.Y);
            Properties.Settings.Default.Size = new Size(this.Size.Width, this.Size.Height);
            Properties.Settings.Default.Save();

            st.SaveData(History, ev.Variables);
        }

        // =========================================================================================
        // RICH TEXT BOX KEY PRESS (ENTER)
        // =========================================================================================
        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 13) return;
            e.Handled = true;
            string input = richTextBox1.Text.Replace("\n", "");
            string input_l = input.ToLower();
            richTextBox1.Text = "";

            // a) Empty Input
            if (input_l == "") return;

            // b) Keyword 'clear'
            if (input_l == "clear")
            {
                panel2.Controls.Clear();
                History.Clear();
                ev.ClearVariable();
                CurrentID = 1;
                return;
            }

            // c) Keyword 'about' or 'help'
            if (input_l == "about" || input_l == "help")
            {
                System.Diagnostics.Process.Start("https://github.com/frankovacevich/mathics");
                richTextBox1.Text = "";
                return;
            }

            // d) Keyword 'precision'
            if (input_l.StartsWith("precision="))
            {
                int p = 0;
                if (int.TryParse(input_l.Split('=')[1], out p))
                {
                    if(p<0 || p > 10)
                    {
                        MessageBox.Show("Precision must be between 0 and 10", "Mathics", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }

                    Properties.Settings.Default.Precision = p;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Precision set to " + p.ToString() + " decimal places", "Mathics", MessageBoxButtons.OK);
                }
                else
                {
                    MessageBox.Show("Invalid syntax (use precision=2 for example)", "Mathics", MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
                }
                return;
            }

            // e) Keyword 'test'
            if(input_l == "test")
            {
                double test_result = TestTime();
                MessageBox.Show("Performance test result: " + test_result.ToString() + " milliseconds on average for each evaluation (type 'help' to learn more)", "Mathics", MessageBoxButtons.OK);
                return;
            }
        
            // f) Evaluate expression
            string result = "";
            string expr = input.Replace(" ", "");

            try
            {
                // Define these two strings to store the result in a variable. 'ans' is the default variable
                string varExpr = expr;
                string varName = "";

                // If the expression contains a '=', then it's a varaible to be stored. Check for syntax errors
                if (expr.Contains("="))
                {
                    string[] splittedExpr = expr.Split('=');
                    if (splittedExpr.Length > 2) throw new System.InvalidOperationException("Incorrect syntax (use only one '=')");
                    if (splittedExpr[0].Length == 0) throw new System.InvalidOperationException("Incorrect syntax (variable name empty)");
                    varExpr = splittedExpr[1];
                    varName = splittedExpr[0];
                }

                // Set name to 'ans' as default
                if (varName == "")
                    varName = "ans";

                // Check if the name of the variable is not colliding with an important keyword
                double temp_double;
                if (FunctionNames.Contains(varName) || KeyWords.Contains(varName) || varName == "π" || varName == "e" ||
                    varName.Contains("+") || varName.Contains("-") || varName.Contains("*") ||
                    varName.Contains("/") || varName.Contains("!") || varName.Contains("^") ||
                    varName.Contains("(") || varName.Contains(")") || varName.Contains(",") ||
                    varName.Contains(".") || varName.Contains("$") || double.TryParse(varName, out temp_double))
                    throw new System.InvalidOperationException("Invalid name for variable");

                // Evaluate expression, round the result to set precision and convert it to string
                double x = ev.EvaluateExpression(varExpr);
                result = Math.Round(x,Properties.Settings.Default.Precision).ToString();
                
                // Add variable to evaluator
                ev.AddVariable(varName, x);
            }
            catch (Exception ex)
            {
                // If there is an error, the entry is added with the error message as result
                result = ex.Message;
            }

            // Add new entry and update histroy
            AddNewEntryControl(expr, result, CurrentID);
            AddNewEntryHistory(expr, result, CurrentID);
            CurrentID++;
        }

        // =========================================================================================
        // RICH TEXT BOX KEY PRESS (DOWN OR UP) FOR HISTORY NAVIGATION
        // =========================================================================================
        private void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13) { e.Handled = true; }

            // Key up
            if (e.KeyValue == 38)
            {
                try
                {
                    e.Handled = true;
                    if (History.Count == 0) return;
                    if (HistoryNav < History.Count)
                        HistoryNav++;

                    richTextBox1.Text = History[History.Count - HistoryNav].Text1;
                    richTextBox1.Select(richTextBox1.Text.Length, 0);
                }
                catch (Exception ex)
                {

                }
            }

            // Key down
            else if (e.KeyValue == 40)
            {
                try
                {
                    e.Handled = true;
                    if (History.Count == 0) return;
                    if (HistoryNav > 1)
                        HistoryNav--;

                    richTextBox1.Text = History[History.Count - HistoryNav].Text1;
                    richTextBox1.Select(richTextBox1.Text.Length, 0);
                }
                catch (Exception ex)
                {

                }
            }
        }

        // =========================================================================================
        // RICH TEXT BOX TEXT CHANGED (FORMAT INPUT COLORING MATCHING PARENTHESIS AND VARIABLES)
        // =========================================================================================
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length == 0) { HistoryNav = 0; return; }

            // SAVE CURRENT POSITION
            int position = richTextBox1.SelectionStart;

            // COLOR MATCHING PARENTHESIS
            if (richTextBox1.SelectionStart > 0)
            {
                string lastchar = richTextBox1.Text.Substring(position - 1, 1);
                if (lastchar == ")")
                {
                    ColorSelectionRTB("\\(", Color.Black);
                    ColorSelectionRTB("\\)", Color.Black);

                    int counter = 0;
                    string aux = "";
                    for (int j = 1; j < position + 1; j++)
                    {
                        aux = richTextBox1.Text.Substring(position - j, 1);
                        if (aux == "(")
                        {
                            counter--;
                            if (counter == 0)
                            {
                                richTextBox1.Select(position - j, 1);
                                richTextBox1.SelectionColor = Color.IndianRed;
                                richTextBox1.Select(position - 1, 1);
                                richTextBox1.SelectionColor = Color.IndianRed;
                                richTextBox1.Select(position, 0);
                                return;
                            }
                        }
                        else if (aux == ")")
                        {
                            counter++;
                        }
                    }
                    richTextBox1.Select(position, 0);
                    return;
                }
            }

            // ADD SPECIAL CHARS
            richTextBox1.Text = richTextBox1.Text.Replace("pi", "π")
                .Replace("lambda", "λ").Replace("xi", "ξ").Replace("chi", "χ")
                .Replace("delta", "δ").Replace("Gamma", "Γ").Replace("Delta", "Δ")
                .Replace("Omega", "Ω").Replace("alpha", "α").Replace("beta", "β")
                .Replace("gamma", "γ").Replace("zeta", "ζ")
                .Replace("theta", "θ").Replace("eta", "η").Replace("mu", "μ")
                .Replace("nu", "ν").Replace("sigma", "σ").Replace("tau", "τ")
                .Replace("phi", "ϕ").Replace("psi", "ψ").Replace("omega", "ω")
                .Replace("_1", "₁").Replace("_2", "₂").Replace("_3", "₃")
                .Replace("_4", "₄").Replace("_5", "₄").Replace("_6", "₄")
                .Replace("_7", "₇").Replace("_8", "₈").Replace("_9", "₉")
                .Replace("heart", "♥").Replace("club", "♣").Replace("spade", "♠")
                .Replace("diamond", "♦").Replace("smile", "☺");

            // RETURN COLOR TO NORMAL (ONCE THE PARENTHESIS HAS BEEN COLORED, REMOVE COLOR)
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = Color.Black;

            // COLOR VARIABLES
            foreach (string variable in ev.Variables.Keys)
                ColorSelectionRTB(variable, Color.LimeGreen);

            // COLOR FUNCTIONS
            foreach (string function in FunctionNames)
                ColorSelectionRTB(function, Color.CornflowerBlue);
            foreach (string keyword in KeyWords)
                ColorSelectionRTB(keyword, Color.CornflowerBlue);

            // RETURN CURSOR TO ORIGINAL POSITION
            richTextBox1.Select(position, 0);
            richTextBox1.SelectionColor = Color.Black;
        }


        // =========================================================================================
        // EDIT HISTORY
        // =========================================================================================
        private void AddNewEntryHistory(string text1, string text2, int id)
        {
            Entry newEntry = new Entry() { ID = id, Text1 = text1, Text2 = text2 };
            History.Add(newEntry);
            HistoryNav = 0;
        }

        // =========================================================================================
        // ADD CONTROL TO PANEL
        // =========================================================================================
        private void AddNewEntryControl(string text1, string text2, int id)
        {
            UserControl1 newEntry = new UserControl1();
            newEntry.setText1(text1);
            newEntry.setText2(text2);
            newEntry.setTextLeft((id % 100).ToString());

            newEntry.Dock = DockStyle.Top;
            newEntry.Size = new System.Drawing.Size(panel2.Width, 46);
            newEntry.Location = new System.Drawing.Point(0, 0);

            newEntry.Name = "Panel" + id.ToString();
            newEntry.Father = this;
            newEntry.ID = id;

            panel2.Controls.Add(newEntry);
            panel2.VerticalScroll.Value = 0;
        }
        
        // =========================================================================================
        // HELPER FUNCTION TO CHANGE THE COLOR OF ALL MATHCHING STRINGS ON THE RICH TEXT BOX
        // =========================================================================================
        void ColorSelectionRTB(string text, Color color)
        {
            System.Text.RegularExpressions.MatchCollection RegExp;
            System.Text.RegularExpressions.Regex.Escape("(");
            RegExp = System.Text.RegularExpressions.Regex.Matches(richTextBox1.Text, text);

            foreach (System.Text.RegularExpressions.Match RegExpMatch in RegExp)
            {
                richTextBox1.Select(RegExpMatch.Index, RegExpMatch.Length);
                richTextBox1.SelectionColor = color;
            }
        }

        // =========================================================================================
        // HELPER FUNCTION TO TEST PREFORMANCE
        // =========================================================================================
        private double TestTime()
        {
            DateTime startTime;
            startTime = DateTime.Now;

            for(int i=0; i<2000; i++)
            {
                ev.EvaluateExpression("1+5-4*8/2+9-8/7+9*8*7*6*5*4*3*2*1*3.1415926*0.001-2.788*698.258774125/5.2569874+1");
                ev.EvaluateExpression("ln(ln(ln(sqrt(3.25648)+59874/sqrt(45874))))*sin(ln(99/7))^2+fix(ln(77))!");
                ev.EvaluateExpression("tanh(cosh(8))*sin(9)+1-88/7+exp(4)+e*e*e/(e*e*e)+9874562.2547854^0.2");
                ev.EvaluateExpression("sin(asin(0.5))+cos(acos(0.5))+asin(sin(0.5))+acos(cos(0.5))+tan(atan(0.9))");
                ev.EvaluateExpression("fix(round(1.11,2))*4.99*0.2*5+5-9-9/8-9/7-9/6-9/5-9/4-9/3-9/2-9/1+sin(3.1415926)^2");    
            }

            double total_time = ((TimeSpan)(DateTime.Now - startTime)).TotalMilliseconds;
            return (total_time/(2000*5));
        }
    }
}
