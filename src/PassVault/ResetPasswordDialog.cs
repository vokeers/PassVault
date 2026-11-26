using System;
using System.Drawing;
using System.Windows.Forms;

namespace PassVault
{
    public sealed class ResetPasswordDialog : Form
    {
        private readonly VaultSession session;
        private readonly PasswordEntry entry;
        private readonly Timer clipboardTimer;
        private RoundedTextBox txtPassword;
        private Label lblError;
        private Button btnConfirm;
        private PasswordOptionsControl optionsControl;
        private string copiedValue;
        private DateTime copiedAtUtc;

        public string NewPassword
        {
            get { return txtPassword.Text; }
        }

        public ResetPasswordDialog(VaultSession session, PasswordEntry entry)
        {
            this.session = session;
            this.entry = entry;
            clipboardTimer = new Timer();
            clipboardTimer.Interval = 1000;
            clipboardTimer.Tick += ClipboardTimer_Tick;
            clipboardTimer.Start();
            InitializeComponent();
            GenerateNewPassword();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(560, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 66;
            header.BackColor = UiTheme.HeaderBackground;
            Controls.Add(header);

            Label title = UiTheme.Label("重置随机密码", 18F, UiTheme.Text, FontStyle.Bold);
            title.Location = new Point(24, 16);
            header.Controls.Add(title);

            Label subtitle = UiTheme.Label("确认后会更新本地密码库中的记录", 8.5F, UiTheme.HeaderSubtle, FontStyle.Regular);
            subtitle.Location = new Point(26, 43);
            header.Controls.Add(subtitle);

            Panel headerLine = new Panel();
            headerLine.Dock = DockStyle.Bottom;
            headerLine.Height = 1;
            headerLine.BackColor = UiTheme.Border;
            header.Controls.Add(headerLine);

            Label lblSite = UiTheme.Label("记录", 9F, UiTheme.Muted, FontStyle.Bold);
            lblSite.Location = new Point(28, 82);
            Controls.Add(lblSite);

            Label lblTarget = new Label();
            lblTarget.AutoSize = false;
            lblTarget.Location = new Point(28, 102);
            lblTarget.Size = new Size(504, 24);
            lblTarget.ForeColor = UiTheme.Text;
            lblTarget.Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            string site = string.IsNullOrWhiteSpace(entry.SiteName) ? entry.Website : entry.SiteName;
            lblTarget.Text = site + "  /  " + entry.Account;
            Controls.Add(lblTarget);

            Label lblPassword = UiTheme.Label("新的随机密码", 9F, UiTheme.Text, FontStyle.Bold);
            lblPassword.Location = new Point(28, 138);
            Controls.Add(lblPassword);

            txtPassword = UiTheme.TextBox(true);
            txtPassword.Location = new Point(28, 160);
            txtPassword.Width = 368;
            txtPassword.Font = UiTheme.FontOf(11F, FontStyle.Regular);
            Controls.Add(txtPassword);

            Button btnCopy = UiTheme.Button("复制新密码", false);
            btnCopy.Location = new Point(406, 159);
            btnCopy.Size = new Size(126, 31);
            btnCopy.Click += BtnCopy_Click;
            Controls.Add(btnCopy);

            RoundedPanel optionsGroup = UiTheme.Card();
            optionsGroup.Location = new Point(28, 208);
            optionsGroup.Size = new Size(504, 100);
            optionsGroup.CornerRadius = 14;
            Controls.Add(optionsGroup);

            Label lblOptions = UiTheme.Label("随机密码选项", 9F, UiTheme.Text, FontStyle.Bold);
            lblOptions.Location = new Point(14, 8);
            optionsGroup.Controls.Add(lblOptions);

            optionsControl = new PasswordOptionsControl();
            optionsControl.Location = new Point(12, 26);
            optionsControl.Size = new Size(480, 70);
            optionsControl.GenerateRequested += OptionsControl_GenerateRequested;
            optionsGroup.Controls.Add(optionsControl);

            Label warning = new Label();
            warning.AutoSize = false;
            warning.Location = new Point(28, 318);
            warning.Size = new Size(504, 34);
            warning.ForeColor = UiTheme.Danger;
            warning.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            warning.Text = "注意：这里只会修改密码库，不会自动修改网站上的密码。";
            Controls.Add(warning);

            lblError = new Label();
            lblError.AutoSize = false;
            lblError.Location = new Point(28, 352);
            lblError.Size = new Size(504, 24);
            lblError.ForeColor = UiTheme.Danger;
            lblError.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            Controls.Add(lblError);

            Button btnCancel = UiTheme.Button("取消", false);
            btnCancel.Location = new Point(300, 404);
            btnCancel.Size = new Size(112, 36);
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            btnConfirm = UiTheme.Button("确认重置", true);
            btnConfirm.Location = new Point(420, 404);
            btnConfirm.Size = new Size(112, 36);
            btnConfirm.Click += BtnConfirm_Click;
            Controls.Add(btnConfirm);

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;
            ResumeLayout(false);
            PerformLayout();
        }

        private void GenerateNewPassword()
        {
            lblError.Text = string.Empty;
            try
            {
                PasswordOptions options = optionsControl.GetOptions();
                txtPassword.Text = session.GenerateUniquePassword(options, entry.Id);
                txtPassword.SelectionStart = txtPassword.TextLength;
                txtPassword.SelectionLength = 0;
            }
            catch (VaultException ex)
            {
                lblError.Text = ex.Message;
            }
            catch (Exception)
            {
                lblError.Text = "生成密码失败，请检查选项后重试。";
            }
        }

        private void OptionsControl_GenerateRequested(object sender, EventArgs e)
        {
            GenerateNewPassword();
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txtPassword.Text);
                copiedValue = txtPassword.Text;
                copiedAtUtc = DateTime.UtcNow;
                lblError.ForeColor = UiTheme.Muted;
                lblError.Text = "已复制，将在 30 秒后自动清空剪贴板。";
            }
            catch
            {
                lblError.ForeColor = UiTheme.Danger;
                lblError.Text = "无法写入剪贴板，请手动复制。";
            }
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            if (copiedValue == null)
            {
                return;
            }
            if (DateTime.UtcNow - copiedAtUtc < TimeSpan.FromSeconds(30))
            {
                return;
            }
            ClearOwnedClipboard();
        }

        private void ClearOwnedClipboard()
        {
            if (copiedValue != null)
            {
                try
                {
                    if (Clipboard.ContainsText() &&
                        string.Equals(Clipboard.GetText(), copiedValue, StringComparison.Ordinal))
                    {
                        Clipboard.Clear();
                    }
                }
                catch { }
            }
            copiedValue = null;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            lblError.ForeColor = UiTheme.Danger;
            lblError.Text = string.Empty;
            btnConfirm.Enabled = false;
            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                session.ResetPassword(entry.Id, txtPassword.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (DuplicatePasswordException ex)
            {
                lblError.Text = ex.Message;
            }
            catch (VaultException ex)
            {
                lblError.Text = ex.Message;
            }
            catch (Exception)
            {
                lblError.Text = "重置失败，请检查程序目录权限和磁盘空间。";
            }
            finally
            {
                Cursor = previous;
                btnConfirm.Enabled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clipboardTimer.Stop();
            clipboardTimer.Dispose();
            ClearOwnedClipboard();
            base.OnFormClosing(e);
        }
    }
}