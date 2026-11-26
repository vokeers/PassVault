using System;
using System.Drawing;
using System.Windows.Forms;

namespace PassVault
{
    public sealed class PasswordOptionsControl : UserControl
    {
        private CheckBox chkUppercase;
        private CheckBox chkLowercase;
        private CheckBox chkDigits;
        private CheckBox chkSymbols;
        private NumericUpDown numLength;
        private Button btnGenerate;

        public event EventHandler GenerateRequested;

        public PasswordOptionsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            BackColor = Color.FromArgb(248, 250, 254);
            Size = new Size(480, 72);

            chkUppercase = CreateCheckBox("大写 A-Z", 0, 2, true);
            chkLowercase = CreateCheckBox("小写 a-z", 112, 2, true);
            chkDigits = CreateCheckBox("数字 0-9", 224, 2, true);
            chkSymbols = CreateCheckBox("符号 !@#", 336, 2, false);
            Controls.Add(chkUppercase);
            Controls.Add(chkLowercase);
            Controls.Add(chkDigits);
            Controls.Add(chkSymbols);

            Label lblLength = new Label();
            lblLength.Text = "长度";
            lblLength.AutoSize = true;
            lblLength.Location = new Point(2, 40);
            lblLength.ForeColor = UiTheme.Text;
            lblLength.Font = UiTheme.FontOf(9F, FontStyle.Regular);
            Controls.Add(lblLength);

            numLength = new NumericUpDown();
            numLength.Location = new Point(42, 36);
            numLength.Size = new Size(64, 26);
            numLength.Minimum = 6;
            numLength.Maximum = 128;
            numLength.Value = 10;
            numLength.Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            Controls.Add(numLength);

            Label hint = new Label();
            hint.Text = "默认：大小写字母 + 数字，10 位";
            hint.AutoSize = true;
            hint.Location = new Point(122, 40);
            hint.ForeColor = UiTheme.Muted;
            hint.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            Controls.Add(hint);

            btnGenerate = UiTheme.Button("生成随机密码", false);
            btnGenerate.Location = new Point(356, 32);
            btnGenerate.Size = new Size(124, 32);
            btnGenerate.Click += delegate
            {
                EventHandler handler = GenerateRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };
            Controls.Add(btnGenerate);
        }

        private static CheckBox CreateCheckBox(string text, int x, int y, bool isChecked)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.AutoSize = true;
            box.Location = new Point(x, y);
            box.Checked = isChecked;
            box.ForeColor = UiTheme.Text;
            box.Font = UiTheme.FontOf(9F, FontStyle.Regular);
            box.BackColor = Color.Transparent;
            return box;
        }

        public PasswordOptions GetOptions()
        {
            return new PasswordOptions(
                chkUppercase.Checked,
                chkLowercase.Checked,
                chkDigits.Checked,
                chkSymbols.Checked,
                (int)numLength.Value);
        }

        public void SetLength(int length)
        {
            if (length < (int)numLength.Minimum) length = (int)numLength.Minimum;
            if (length > (int)numLength.Maximum) length = (int)numLength.Maximum;
            numLength.Value = length;
        }
    }
}