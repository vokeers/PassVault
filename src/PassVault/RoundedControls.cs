using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PassVault
{
    internal static class RoundedGeometry
    {
        public static GraphicsPath CreatePath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }
            int diameter = Math.Min(Math.Min(radius * 2, bounds.Width), bounds.Height);
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public sealed class RoundedPanel : Panel
    {
        private int cornerRadius = 18;
        private Color borderColor = Color.FromArgb(229, 229, 234);
        private int borderWidth = 1;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        public int BorderWidth
        {
            get { return borderWidth; }
            set { borderWidth = Math.Max(0, value); Invalidate(); }
        }

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = RoundedGeometry.CreatePath(new Rectangle(0, 0, Width, Height), cornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = Parent != null ? Parent.BackColor : UiTheme.WindowBackground;
            e.Graphics.Clear(parentColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedGeometry.CreatePath(bounds, cornerRadius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
                if (borderWidth > 0)
                {
                    using (Pen pen = new Pen(borderColor, borderWidth))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }
    }

    public sealed class RoundedButton : Button
    {
        private int cornerRadius = 18;
        private Color hoverBackColor = Color.Empty;
        private Color pressedBackColor = Color.Empty;
        private Color borderColor = Color.Empty;
        private int borderWidth = 0;
        private bool hovering;
        private bool pressing;

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
        }

        public Color HoverBackColor
        {
            get { return hoverBackColor; }
            set { hoverBackColor = value; Invalidate(); }
        }

        public Color PressedBackColor
        {
            get { return pressedBackColor; }
            set { pressedBackColor = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        public int BorderWidth
        {
            get { return borderWidth; }
            set { borderWidth = Math.Max(0, value); Invalidate(); }
        }

        public RoundedButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            BackColor = UiTheme.FieldBackground;
            ForeColor = UiTheme.Text;
            Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = RoundedGeometry.CreatePath(new Rectangle(0, 0, Width, Height), cornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            pressing = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            pressing = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressing = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = Parent != null ? Parent.BackColor : UiTheme.WindowBackground;
            pevent.Graphics.Clear(parentColor);

            Color fill = BackColor;
            if (!Enabled)
            {
                fill = Color.FromArgb(230, 230, 235);
            }
            else if (pressing && pressedBackColor != Color.Empty)
            {
                fill = pressedBackColor;
            }
            else if (hovering && hoverBackColor != Color.Empty)
            {
                fill = hoverBackColor;
            }

            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedGeometry.CreatePath(bounds, cornerRadius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                pevent.Graphics.FillPath(brush, path);
                if (borderWidth > 0 && borderColor != Color.Empty)
                {
                    using (Pen pen = new Pen(borderColor, borderWidth))
                    {
                        pevent.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                Font,
                bounds,
                Enabled ? ForeColor : UiTheme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public sealed class RoundedTextBox : Panel
    {
        private readonly TextBox inner;
        private bool readOnlyField;
        private bool focusedField;
        private int cornerRadius = 10;

        public new event EventHandler TextChanged
        {
            add { inner.TextChanged += value; }
            remove { inner.TextChanged -= value; }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set { cornerRadius = Math.Max(0, value); UpdateRegion(); Invalidate(); }
        }

        public IntPtr TextHandle
        {
            get { return inner.Handle; }
        }

        public override string Text
        {
            get { return inner.Text; }
            set { inner.Text = value ?? string.Empty; }
        }

        public bool ReadOnly
        {
            get { return readOnlyField; }
            set
            {
                readOnlyField = value;
                inner.ReadOnly = value;
                BackColor = value ? Color.FromArgb(245, 245, 247) : UiTheme.FieldBackground;
                inner.BackColor = BackColor;
                Invalidate();
            }
        }

        public bool UseSystemPasswordChar
        {
            get { return inner.UseSystemPasswordChar; }
            set { inner.UseSystemPasswordChar = value; }
        }

        public int MaxLength
        {
            get { return inner.MaxLength; }
            set { inner.MaxLength = value; }
        }

        public bool Multiline
        {
            get { return inner.Multiline; }
            set
            {
                inner.Multiline = value;
                if (value)
                {
                    inner.ScrollBars = ScrollBars.Vertical;
                    inner.AcceptsReturn = true;
                }
                LayoutInner();
            }
        }

        public ScrollBars ScrollBars
        {
            get { return inner.ScrollBars; }
            set { inner.ScrollBars = value; }
        }

        public bool AcceptsReturn
        {
            get { return inner.AcceptsReturn; }
            set { inner.AcceptsReturn = value; }
        }

        public new Font Font
        {
            get { return inner.Font; }
            set { inner.Font = value; LayoutInner(); }
        }

        public new Color ForeColor
        {
            get { return inner.ForeColor; }
            set { inner.ForeColor = value; }
        }

        public int TextLength
        {
            get { return inner.TextLength; }
        }

        public int SelectionStart
        {
            get { return inner.SelectionStart; }
            set { inner.SelectionStart = value; }
        }

        public int SelectionLength
        {
            get { return inner.SelectionLength; }
            set { inner.SelectionLength = value; }
        }

        public RoundedTextBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = UiTheme.FieldBackground;
            Height = 36;
            Padding = new Padding(12, 7, 12, 7);

            inner = new TextBox();
            inner.BorderStyle = BorderStyle.None;
            inner.BackColor = BackColor;
            inner.ForeColor = UiTheme.Text;
            inner.Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            inner.TabStop = true;
            inner.Enter += delegate { focusedField = true; Invalidate(); };
            inner.Leave += delegate { focusedField = false; Invalidate(); };
            Controls.Add(inner);
            UpdateRegion();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateRegion();
            LayoutInner();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            inner.Enabled = Enabled;
        }

        public new bool Focus()
        {
            return inner.Focus();
        }

        public void SelectAll()
        {
            inner.SelectAll();
        }

        public void Clear()
        {
            inner.Clear();
        }

        private void LayoutInner()
        {
            if (inner == null || Width <= 0 || Height <= 0)
            {
                return;
            }

            int horizontal = 12;
            int vertical = 7;
            if (inner.Multiline)
            {
                inner.Bounds = new Rectangle(horizontal, vertical, Math.Max(10, Width - horizontal * 2), Math.Max(10, Height - vertical * 2));
            }
            else
            {
                int innerHeight = Math.Max(18, inner.PreferredHeight);
                int top = Math.Max(0, (Height - innerHeight) / 2);
                inner.Bounds = new Rectangle(horizontal, top, Math.Max(10, Width - horizontal * 2), innerHeight);
            }
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            using (GraphicsPath path = RoundedGeometry.CreatePath(new Rectangle(0, 0, Width, Height), cornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = Parent != null ? Parent.BackColor : UiTheme.WindowBackground;
            e.Graphics.Clear(parentColor);
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedGeometry.CreatePath(bounds, cornerRadius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
                Color border = focusedField ? UiTheme.Accent : UiTheme.Border;
                using (Pen pen = new Pen(border, focusedField ? 1.5F : 1F))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }
}