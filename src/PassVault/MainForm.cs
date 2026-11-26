using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PassVault
{
    public sealed class MainForm : Form
    {
        private const int AutoLockMinutes = 5;
        private const int ClipboardClearSeconds = 30;
        private const int ActionButtonHeight = 28;
        private const int ActionButtonGap = 5;
        private static readonly int[] ActionWidths = new int[] { 42, 62, 62, 42, 42, 42 };
        private static readonly string[] ActionTexts = new string[] { "打开", "复制账号", "复制密码", "编辑", "重置", "删除" };

        private readonly VaultSession session;
        private readonly Timer timer;
        private readonly ActivityFilter activityFilter;
        private ListView listEntries;
        private RoundedTextBox txtSearch;
        private Label lblStatus;
        private Label lblEmpty;
        private RoundedButton btnAdd;
        private RoundedButton btnLock;
        private ImageList rowImageList;
        private Font listHeaderFont;
        private Font actionFont;
        private DateTime lastActivityUtc;
        private DateTime? clipboardClearAt;
        private string clipboardValue;
        private bool clipboardIsPassword;
        private bool locking;

        public event EventHandler LockRequested;

        public MainForm(VaultSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            this.session = session;
            lastActivityUtc = DateTime.UtcNow;
            activityFilter = new ActivityFilter(this);
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;

            InitializeComponent();
            RefreshList(null);
            timer.Start();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "PassVault - 密码库";
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(980, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;
            listHeaderFont = UiTheme.FontOf(8.5F, FontStyle.Bold);
            actionFont = UiTheme.FontOf(8F, FontStyle.Regular);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 86;
            header.BackColor = UiTheme.HeaderBackground;
            Controls.Add(header);

            Label title = UiTheme.Label("PassVault", 22F, UiTheme.Text, FontStyle.Bold);
            title.Location = new Point(24, 16);
            header.Controls.Add(title);

            Label subtitle = UiTheme.Label("本地加密密码库  ·  AES-256  ·  完整性校验", 9F, UiTheme.HeaderSubtle, FontStyle.Regular);
            subtitle.Location = new Point(27, 52);
            header.Controls.Add(subtitle);

            btnLock = UiTheme.Button("锁定", false);
            btnLock.Location = new Point(998, 24);
            btnLock.Size = new Size(96, 36);
            btnLock.CornerRadius = 18;
            btnLock.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLock.Click += BtnLock_Click;
            header.Controls.Add(btnLock);

            Panel headerLine = new Panel();
            headerLine.Dock = DockStyle.Bottom;
            headerLine.Height = 1;
            headerLine.BackColor = UiTheme.Border;
            header.Controls.Add(headerLine);

            RoundedPanel tableCard = UiTheme.Card();
            tableCard.Location = new Point(22, 106);
            tableCard.Size = new Size(1076, 560);
            tableCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(tableCard);

            txtSearch = UiTheme.TextBox(false);
            txtSearch.Location = new Point(20, 18);
            txtSearch.Width = 876;
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            tableCard.Controls.Add(txtSearch);

            btnAdd = UiTheme.Button("+ 新增账号", true);
            btnAdd.Location = new Point(916, 16);
            btnAdd.Size = new Size(140, 38);
            btnAdd.CornerRadius = 19;
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Click += BtnAdd_Click;
            tableCard.Controls.Add(btnAdd);

            listEntries = new ListView();
            listEntries.Location = new Point(20, 72);
            listEntries.Size = new Size(1036, 468);
            listEntries.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            listEntries.View = View.Details;
            listEntries.FullRowSelect = true;
            listEntries.HideSelection = false;
            listEntries.MultiSelect = false;
            listEntries.GridLines = false;
            listEntries.BorderStyle = BorderStyle.None;
            listEntries.Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            listEntries.BackColor = Color.White;
            listEntries.ForeColor = UiTheme.Text;
            listEntries.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listEntries.OwnerDraw = true;
            listEntries.ShowItemToolTips = true;
            listEntries.Columns.Add("网站名称", 120);
            listEntries.Columns.Add("网站地址", 220);
            listEntries.Columns.Add("账号", 130);
            listEntries.Columns.Add("密码", 90);
            listEntries.Columns.Add("备注", 140);
            listEntries.Columns.Add("操作", 336);
            listEntries.DrawColumnHeader += ListEntries_DrawColumnHeader;
            listEntries.DrawItem += ListEntries_DrawItem;
            listEntries.DrawSubItem += ListEntries_DrawSubItem;
            listEntries.MouseClick += ListEntries_MouseClick;
            listEntries.MouseMove += ListEntries_MouseMove;
            listEntries.MouseLeave += ListEntries_MouseLeave;
            listEntries.DoubleClick += ListEntries_DoubleClick;
            tableCard.Controls.Add(listEntries);

            rowImageList = new ImageList();
            rowImageList.ImageSize = new Size(1, 42);
            rowImageList.Images.Add(new Bitmap(1, 42));
            listEntries.SmallImageList = rowImageList;

            lblEmpty = new Label();
            lblEmpty.Location = new Point(20, 72);
            lblEmpty.Size = new Size(1036, 468);
            lblEmpty.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            lblEmpty.Text = "暂无记录，点击右上角“新增账号”开始";
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.ForeColor = UiTheme.Muted;
            lblEmpty.Font = UiTheme.FontOf(10F, FontStyle.Regular);
            lblEmpty.BackColor = Color.Transparent;
            lblEmpty.Visible = false;
            tableCard.Controls.Add(lblEmpty);
            lblEmpty.BringToFront();

            lblStatus = new Label();
            lblStatus.Location = new Point(22, 682);
            lblStatus.Size = new Size(1076, 20);
            lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            lblStatus.ForeColor = UiTheme.Muted;
            lblStatus.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            Controls.Add(lblStatus);

            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Application.AddMessageFilter(activityFilter);
            SendMessage(txtSearch.TextHandle, 0x1501, (IntPtr)1, "搜索名称、网站、账号或备注");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtSearch.Focus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            Application.RemoveMessageFilter(activityFilter);
            ClearOwnedClipboard();
            if (session != null)
            {
                session.Dispose();
            }
            if (rowImageList != null)
            {
                rowImageList.Dispose();
            }
            if (listHeaderFont != null)
            {
                listHeaderFont.Dispose();
            }
            if (actionFont != null)
            {
                actionFont.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (locking)
            {
                return;
            }
            if (DateTime.UtcNow - lastActivityUtc >= TimeSpan.FromMinutes(AutoLockMinutes))
            {
                RequestLock();
                return;
            }
            ClearClipboardIfExpired();
        }

        internal void NotifyActivity()
        {
            if (!locking)
            {
                lastActivityUtc = DateTime.UtcNow;
            }
        }

        private void BtnLock_Click(object sender, EventArgs e)
        {
            RequestLock();
        }

        private void RequestLock()
        {
            if (locking)
            {
                return;
            }
            locking = true;
            timer.Stop();
            CloseOwnedDialogs();
            ClearOwnedClipboard();
            if (session != null)
            {
                session.Dispose();
            }
            EventHandler handler = LockRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void CloseOwnedDialogs()
        {
            List<Form> owned = new List<Form>();
            foreach (Form form in Application.OpenForms)
            {
                if (form != this && form.Owner == this)
                {
                    owned.Add(form);
                }
            }
            for (int i = 0; i < owned.Count; i++)
            {
                try { owned[i].Close(); }
                catch { }
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (EntryEditDialog dialog = new EntryEditDialog(session, null))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SavedEntryId != null)
                {
                    RefreshList(dialog.SavedEntryId);
                    SetStatus("账号已保存并加密写入密码库。");
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshList(GetSelectedEntryId());
        }

        private void ListEntries_DoubleClick(object sender, EventArgs e)
        {
            PasswordEntry entry = GetSelectedEntry();
            if (entry != null)
            {
                OpenWebsite(entry);
            }
        }

        private void OpenWebsite(PasswordEntry entry)
        {
            if (entry == null)
            {
                return;
            }
            try
            {
                string url = UrlHelper.NormalizeWebsite(entry.Website);
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                SetStatus("已在默认浏览器中打开网站。");
            }
            catch (Exception ex)
            {
                ShowError("无法打开网站。\r\n" + ex.Message);
            }
        }

        private void CopyAccount(PasswordEntry entry)
        {
            if (entry == null) return;
            CopyToClipboard(entry.Account, false);
            SetStatus("账号已复制到剪贴板。");
        }

        private void CopyPassword(PasswordEntry entry)
        {
            if (entry == null) return;
            CopyToClipboard(entry.Password, true);
            SetStatus("密码已复制，30 秒后自动清空剪贴板。");
        }

        private void EditEntry(PasswordEntry entry)
        {
            if (entry == null) return;
            using (EntryEditDialog dialog = new EntryEditDialog(session, entry))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshList(dialog.SavedEntryId);
                    SetStatus("修改已保存。");
                }
            }
        }

        private void ResetEntry(PasswordEntry entry)
        {
            if (entry == null) return;
            using (ResetPasswordDialog dialog = new ResetPasswordDialog(session, entry))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshList(entry.Id);
                    CopyToClipboard(dialog.NewPassword, true);
                    SetStatus("密码已重置并复制，30 秒后自动清空剪贴板。请到网站修改为新密码。");
                }
            }
        }

        private void DeleteEntry(PasswordEntry entry)
        {
            if (entry == null) return;

            DialogResult result = MessageBox.Show(
                this,
                "确定删除这条记录吗？\r\n\r\n网站名称：" + DisplayName(entry) + "\r\n网站地址：" + entry.Website + "\r\n账号：" + entry.Account,
                "删除记录",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                session.DeleteEntry(entry.Id);
                RefreshList(null);
                SetStatus("记录已删除。");
            }
            catch (VaultException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception)
            {
                ShowError("删除失败，请检查程序目录权限。");
            }
        }

        private void ListEntries_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ListViewHitTestInfo hit = listEntries.HitTest(e.X, e.Y);
            if (hit.Item == null || hit.SubItem == null || hit.SubItem != hit.Item.SubItems[5])
            {
                return;
            }

            RowAction action = RowAction.None;
            List<ActionHit> hits = GetActionHits(hit.SubItem.Bounds);
            for (int i = 0; i < hits.Count; i++)
            {
                if (hits[i].Bounds.Contains(e.X, e.Y))
                {
                    action = hits[i].Action;
                    break;
                }
            }
            if (action == RowAction.None)
            {
                return;
            }

            hit.Item.Selected = true;
            hit.Item.Focused = true;
            PasswordEntry entry = session.FindEntry(hit.Item.Tag as string);
            ExecuteRowAction(action, entry);
        }

        private void ListEntries_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listEntries.HitTest(e.X, e.Y);
            bool overAction = false;
            if (hit.Item != null && hit.SubItem != null && hit.SubItem == hit.Item.SubItems[5])
            {
                List<ActionHit> hits = GetActionHits(hit.SubItem.Bounds);
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i].Bounds.Contains(e.X, e.Y))
                    {
                        overAction = true;
                        break;
                    }
                }
            }
            listEntries.Cursor = overAction ? Cursors.Hand : Cursors.Default;
        }

        private void ListEntries_MouseLeave(object sender, EventArgs e)
        {
            listEntries.Cursor = Cursors.Default;
        }

        private void ExecuteRowAction(RowAction action, PasswordEntry entry)
        {
            switch (action)
            {
                case RowAction.Open:
                    OpenWebsite(entry);
                    break;
                case RowAction.CopyAccount:
                    CopyAccount(entry);
                    break;
                case RowAction.CopyPassword:
                    CopyPassword(entry);
                    break;
                case RowAction.Edit:
                    EditEntry(entry);
                    break;
                case RowAction.Reset:
                    ResetEntry(entry);
                    break;
                case RowAction.Delete:
                    DeleteEntry(entry);
                    break;
            }
        }

        private void ListEntries_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(248, 248, 250)))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            using (Pen pen = new Pen(UiTheme.Border))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
            Rectangle textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                listHeaderFont,
                textBounds,
                UiTheme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void ListEntries_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Color background = e.Item.Selected ? UiTheme.SoftBlue : Color.White;
            using (SolidBrush brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            using (Pen pen = new Pen(Color.FromArgb(242, 242, 247)))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        private void ListEntries_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 5)
            {
                DrawActionCell(e.Graphics, e.Bounds);
                return;
            }

            string text = e.SubItem.Text;
            if (e.ColumnIndex == 3)
            {
                text = "••••••••";
            }
            else if (e.ColumnIndex == 4)
            {
                text = NormalizeNoteForList(text);
            }

            Rectangle textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                listEntries.Font,
                textBounds,
                UiTheme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawActionCell(Graphics graphics, Rectangle bounds)
        {
            List<ActionHit> hits = GetActionHits(bounds);
            for (int i = 0; i < hits.Count; i++)
            {
                ActionHit hit = hits[i];
                Color fill;
                Color fore;
                switch (hit.Action)
                {
                    case RowAction.Open:
                        fill = UiTheme.SoftBlue;
                        fore = UiTheme.Accent;
                        break;
                    case RowAction.Delete:
                        fill = UiTheme.SoftRed;
                        fore = UiTheme.Danger;
                        break;
                    default:
                        fill = UiTheme.FieldBackground;
                        fore = UiTheme.Text;
                        break;
                }
                DrawPill(graphics, hit.Bounds, ActionTexts[i], fill, fore);
            }
        }

        private void DrawPill(Graphics graphics, Rectangle bounds, string text, Color fill, Color fore)
        {
            using (GraphicsPath path = RoundedGeometry.CreatePath(bounds, 14))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                graphics.FillPath(brush, path);
            }
            TextRenderer.DrawText(
                graphics,
                text,
                actionFont,
                bounds,
                fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static List<ActionHit> GetActionHits(Rectangle bounds)
        {
            List<ActionHit> hits = new List<ActionHit>();
            int total = ActionButtonGap * (ActionWidths.Length - 1);
            for (int i = 0; i < ActionWidths.Length; i++)
            {
                total += ActionWidths[i];
            }

            int x = bounds.Left + Math.Max(4, (bounds.Width - total) / 2);
            int y = bounds.Top + Math.Max(2, (bounds.Height - ActionButtonHeight) / 2);
            for (int i = 0; i < ActionWidths.Length; i++)
            {
                ActionHit hit = new ActionHit();
                hit.Action = (RowAction)(i + 1);
                hit.Bounds = new Rectangle(x, y, ActionWidths[i], ActionButtonHeight);
                hits.Add(hit);
                x += ActionWidths[i] + ActionButtonGap;
            }
            return hits;
        }

        private void RefreshList(string selectId)
        {
            string filter = (txtSearch.Text ?? string.Empty).Trim();
            int count = 0;
            listEntries.BeginUpdate();
            try
            {
                listEntries.Items.Clear();
                IReadOnlyList<PasswordEntry> entries = session.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    PasswordEntry entry = entries[i];
                    if (filter.Length > 0 &&
                        !Contains(entry.SiteName, filter) &&
                        !Contains(entry.Website, filter) &&
                        !Contains(entry.Account, filter) &&
                        !Contains(entry.Note, filter))
                    {
                        continue;
                    }

                    ListViewItem item = new ListViewItem(DisplayName(entry));
                    item.SubItems.Add(entry.Website);
                    item.SubItems.Add(entry.Account);
                    item.SubItems.Add("••••••••");
                    item.SubItems.Add(NormalizeNoteForList(entry.Note));
                    item.SubItems.Add(string.Empty);
                    item.Tag = entry.Id;
                    item.ImageIndex = 0;
                    item.ToolTipText = "网站地址：" + entry.Website +
                                       (string.IsNullOrWhiteSpace(entry.Note) ? string.Empty : "\r\n备注：" + entry.Note);
                    listEntries.Items.Add(item);
                    count++;
                }
            }
            finally
            {
                listEntries.EndUpdate();
            }

            int selectedIndex = -1;
            if (!string.IsNullOrEmpty(selectId))
            {
                for (int i = 0; i < listEntries.Items.Count; i++)
                {
                    if (string.Equals((string)listEntries.Items[i].Tag, selectId, StringComparison.Ordinal))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            if (selectedIndex < 0 && listEntries.Items.Count > 0)
            {
                selectedIndex = 0;
            }

            if (selectedIndex >= 0)
            {
                listEntries.Items[selectedIndex].Selected = true;
                listEntries.Items[selectedIndex].Focused = true;
                listEntries.EnsureVisible(selectedIndex);
            }

            lblEmpty.Visible = listEntries.Items.Count == 0;
            lblEmpty.Text = string.IsNullOrEmpty(filter) ? "暂无记录，点击右上角“新增账号”开始" : "没有匹配的记录";
            if (lblEmpty.Visible)
            {
                lblEmpty.BringToFront();
            }
            else
            {
                listEntries.BringToFront();
            }

            lblStatus.Text = string.Format("已解锁 · 显示 {0} 条记录（共 {1} 条）· 5 分钟无操作自动锁定", count, session.Entries.Count);
        }

        private PasswordEntry GetSelectedEntry()
        {
            if (listEntries.SelectedItems.Count == 0)
            {
                return null;
            }
            string id = listEntries.SelectedItems[0].Tag as string;
            return session.FindEntry(id);
        }

        private string GetSelectedEntryId()
        {
            if (listEntries.SelectedItems.Count == 0)
            {
                return null;
            }
            return listEntries.SelectedItems[0].Tag as string;
        }

        private static string DisplayName(PasswordEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.SiteName))
            {
                return entry.SiteName;
            }
            return entry.Website;
        }

        private static string NormalizeNoteForList(string note)
        {
            if (string.IsNullOrEmpty(note))
            {
                return string.Empty;
            }
            return note.Replace("\r", " ").Replace("\n", " ");
        }

        private static bool Contains(string value, string filter)
        {
            return value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void CopyToClipboard(string text, bool isPassword)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
                clipboardValue = text;
                clipboardIsPassword = isPassword;
                clipboardClearAt = isPassword ? DateTime.UtcNow.AddSeconds(ClipboardClearSeconds) : (DateTime?)null;
            }
            catch (Exception)
            {
                ShowError("无法写入剪贴板，请稍后重试。");
            }
        }

        private void ClearClipboardIfExpired()
        {
            if (!clipboardClearAt.HasValue || DateTime.UtcNow < clipboardClearAt.Value)
            {
                return;
            }
            ClearOwnedClipboard();
        }

        private void ClearOwnedClipboard()
        {
            if (clipboardIsPassword && clipboardValue != null)
            {
                try
                {
                    if (Clipboard.ContainsText() &&
                        string.Equals(Clipboard.GetText(), clipboardValue, StringComparison.Ordinal))
                    {
                        Clipboard.Clear();
                    }
                }
                catch { }
            }
            clipboardClearAt = null;
            clipboardValue = null;
            clipboardIsPassword = false;
        }

        private void SetStatus(string message)
        {
            lblStatus.Text = message;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, "PassVault", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private enum RowAction
        {
            None = 0,
            Open = 1,
            CopyAccount = 2,
            CopyPassword = 3,
            Edit = 4,
            Reset = 5,
            Delete = 6
        }

        private sealed class ActionHit
        {
            public RowAction Action { get; set; }
            public Rectangle Bounds { get; set; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private sealed class ActivityFilter : IMessageFilter
        {
            private readonly MainForm owner;

            public ActivityFilter(MainForm owner)
            {
                this.owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                int msg = m.Msg;
                if (msg == 0x0100 || msg == 0x0104 || msg == 0x0200 ||
                    msg == 0x0201 || msg == 0x0204 || msg == 0x020A || msg == 0x020E)
                {
                    owner.NotifyActivity();
                }
                return false;
            }
        }
    }
}