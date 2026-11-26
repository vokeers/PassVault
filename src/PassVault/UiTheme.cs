using System;
using System.Drawing;
using System.Windows.Forms;

namespace PassVault
{
    internal static class UiTheme
    {
        public static readonly Color WindowBackground = Color.FromArgb(245, 245, 247);
        public static readonly Color HeaderBackground = Color.White;
        public static readonly Color HeaderSubtle = Color.FromArgb(110, 110, 115);
        public static readonly Color CardBackground = Color.White;
        public static readonly Color Accent = Color.FromArgb(0, 122, 255);
        public static readonly Color AccentHover = Color.FromArgb(10, 132, 255);
        public static readonly Color Danger = Color.FromArgb(255, 59, 48);
        public static readonly Color Text = Color.FromArgb(29, 29, 31);
        public static readonly Color Muted = Color.FromArgb(134, 134, 139);
        public static readonly Color Border = Color.FromArgb(229, 229, 234);
        public static readonly Color FieldBackground = Color.FromArgb(242, 242, 247);
        public static readonly Color SoftBlue = Color.FromArgb(234, 244, 255);
        public static readonly Color SoftRed = Color.FromArgb(255, 240, 239);
        public static readonly Color SoftGray = Color.FromArgb(242, 242, 247);

        public static Font FontOf(float size, FontStyle style)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }

        public static Label Label(string text, float size, Color color, FontStyle style)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.ForeColor = color;
            label.Font = FontOf(size, style);
            label.BackColor = Color.Transparent;
            label.UseCompatibleTextRendering = false;
            return label;
        }

        public static RoundedTextBox TextBox(bool readOnly)
        {
            RoundedTextBox box = new RoundedTextBox();
            box.ReadOnly = readOnly;
            box.Height = 36;
            return box;
        }

        public static RoundedButton Button(string text, bool primary)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.AutoSize = false;
            button.Height = 38;
            button.MinimumSize = new Size(84, 38);
            button.Font = FontOf(9.5F, FontStyle.Regular);
            button.CornerRadius = 18;
            button.BackColor = primary ? Accent : FieldBackground;
            button.ForeColor = primary ? Color.White : Text;
            button.HoverBackColor = primary ? AccentHover : Color.FromArgb(232, 232, 237);
            button.PressedBackColor = primary ? Color.FromArgb(0, 103, 214) : Color.FromArgb(223, 223, 228);
            button.BorderWidth = 0;
            button.BorderColor = Color.Empty;
            return button;
        }

        public static RoundedButton DangerButton(string text)
        {
            RoundedButton button = Button(text, false);
            button.BackColor = SoftRed;
            button.ForeColor = Danger;
            button.HoverBackColor = Color.FromArgb(255, 227, 225);
            button.PressedBackColor = Color.FromArgb(255, 210, 207);
            return button;
        }

        public static RoundedPanel Card()
        {
            RoundedPanel panel = new RoundedPanel();
            panel.BackColor = CardBackground;
            panel.BorderColor = Border;
            panel.BorderWidth = 1;
            panel.CornerRadius = 18;
            return panel;
        }

        public static void CenterOnScreen(Form form)
        {
            form.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}