using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mathics
{
    public partial class UserControl1 : UserControl
    {
        public UserControl1() { InitializeComponent(); }

        // =========================================================================================
        // VARIABLES
        // =========================================================================================
        public int ID = 0;
        public Form1 Father;

        // =========================================================================================
        // USEFUL METHODS
        // =========================================================================================
        public void setText1(String text) { textBox1.Text = text; }
        public void setText2(String text) { textBox2.Text = text; }
        public void setTextLeft(String text) { label1.Text = text; }

        // =========================================================================================
        // DELETE (CROSS) BUTTON
        // =========================================================================================
        private void label2_MouseEnter(object sender, EventArgs e) { label2.ForeColor = Color.DarkGray; }
        private void label2_MouseLeave(object sender, EventArgs e) { label2.ForeColor = Color.White; }

        private void label2_Click(object sender, EventArgs e)
        {
            Father.panel2.Controls.RemoveByKey(this.Name);
            Father.History.Remove(Father.History.Find(x => x.ID == ID));
        }

        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Father.richTextBox1.Text = textBox1.Text;
            Father.richTextBox1.Focus();
            Father.richTextBox1.Select(Father.richTextBox1.Text.Length, 0);
        }
    }
}
