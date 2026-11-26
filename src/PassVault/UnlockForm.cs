using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassVault
{
    public sealed class UnlockForm : Form
    {
        private readonly VaultStore store;
        private bool setupMode;
        private bool busy;
        private RoundedTextBox txtPassword;
        private RoundedTextBox txtConfirm;
        private Label lblHeading;
        private Label lblDescription;
        private Label lblConfirm;
        private Label lblError;
        private Label lblModeHint;
        private RoundedButton btnUnlock;
        private MainForm mainForm;

        public UnlockForm(VaultStore store)
        {
            this.store = store;
            setupMode = !store.Exists;
            InitializeComponent();
            ConfigureMode();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "PassVault";
            ClientSize = new Size(560, 548);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = UiTheme.WindowBackground;
            Font = UiTheme.FontOf(9.5F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 112;
            header.BackColor = UiTheme.HeaderBackground;
            Controls.Add(header);

            Label title = UiTheme.Label("PassVault", 26F, UiTheme.Text, FontStyle.Bold);
            title.Location = new Point(40, 22);
            header.Controls.Add(title);

            Label subtitle = UiTheme.Label("本地加密密码库", 10.5F, UiTheme.HeaderSubtle, FontStyle.Regular);
            subtitle.Location = new Point(43, 66);
            header.Controls.Add(subtitle);

            Label badge = UiTheme.Label("离线 · 便携 · 加密", 9F, UiTheme.Accent, FontStyle.Regular);
            badge.Location = new Point(390, 34);
            header.Controls.Add(badge);

            Panel headerLine = new Panel();
            headerLine.Dock = DockStyle.Bottom;
            headerLine.Height = 1;
            headerLine.BackColor = UiTheme.Border;
            header.Controls.Add(headerLine);

            RoundedPanel card = UiTheme.Card();
            card.Location = new Point(40, 140);
            card.Size = new Size(480, 356);
            Controls.Add(card);

            lblHeading = UiTheme.Label("解锁密码库", 20F, UiTheme.Text, FontStyle.Bold);
            lblHeading.Location = new Point(32, 28);
            card.Controls.Add(lblHeading);

            lblDescription = UiTheme.Label("输入主密码后进入密码库。", 9.5F, UiTheme.Muted, FontStyle.Regular);
            lblDescription.Location = new Point(34, 66);
            card.Controls.Add(lblDescription);

            Label lblPassword = UiTheme.Label("主密码", 9F, UiTheme.Text, FontStyle.Bold);
            lblPassword.Location = new Point(34, 104);
            card.Controls.Add(lblPassword);

            txtPassword = UiTheme.TextBox(false);
            txtPassword.Location = new Point(34, 126);
            txtPassword.Width = 412;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.MaxLength = 256;
            card.Controls.Add(txtPassword);

            lblConfirm = UiTheme.Label("确认主密码", 9F, UiTheme.Text, FontStyle.Bold);
            lblConfirm.Location = new Point(34, 176);
            card.Controls.Add(lblConfirm);

            txtConfirm = UiTheme.TextBox(false);
            txtConfirm.Location = new Point(34, 198);
            txtConfirm.Width = 412;
            txtConfirm.UseSystemPasswordChar = true;
            txtConfirm.MaxLength = 256;
            card.Controls.Add(txtConfirm);

            lblError = new Label();
            lblError.AutoSize = false;
            lblError.Location = new Point(34, 236);
            lblError.Size = new Size(412, 40);
            lblError.ForeColor = UiTheme.Danger;
            lblError.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            card.Controls.Add(lblError);

            btnUnlock = UiTheme.Button("解锁", true);
            btnUnlock.Location = new Point(34, 284);
            btnUnlock.Size = new Size(412, 42);
            btnUnlock.CornerRadius = 21;
            btnUnlock.Click += BtnUnlock_Click;
            card.Controls.Add(btnUnlock);

            lblModeHint = new Label();
            lblModeHint.AutoSize = false;
            lblModeHint.Location = new Point(40, 514);
            lblModeHint.Size = new Size(390, 20);
            lblModeHint.ForeColor = UiTheme.Muted;
            lblModeHint.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            lblModeHint.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(lblModeHint);

            Label version = UiTheme.Label("PassVault v1.3.0", 8F, UiTheme.Muted, FontStyle.Regular);
            version.Location = new Point(448, 514);
            Controls.Add(version);

            AcceptButton = btnUnlock;
            ResumeLayout(false);
            PerformLayout();
        }

        private void ConfigureMode()
        {
            setupMode = !store.Exists;
            lblError.Text = string.Empty;
            txtPassword.Clear();
            txtConfirm.Clear();

            if (setupMode)
            {
                lblHeading.Text = "创建主密码";
                lblDescription.Text = "首次使用，请设置用于加密密码库的主密码。";
                btnUnlock.Text = "创建并进入";
                lblConfirm.Visible = true;
                txtConfirm.Visible = true;
                lblError.Location = new Point(34, 236);
                btnUnlock.Location = new Point(34, 284);
                lblModeHint.Text = "主密码至少 6 位，且必须包含字母和数字。建议 12 位以上。";
                lblModeHint.ForeColor = UiTheme.Danger;
                Text = "PassVault - 创建密码库";
            }
            else
            {
                lblHeading.Text = "解锁密码库";
                lblDescription.Text = "输入主密码后进入密码库。";
                btnUnlock.Text = "解锁";
                lblConfirm.Visible = false;
                txtConfirm.Visible = false;
                lblError.Location = new Point(34, 176);
                btnUnlock.Location = new Point(34, 224);
                lblModeHint.Text = "密码库文件：vault.pvault（保存在程序目录）";
                lblModeHint.ForeColor = UiTheme.Muted;
                Text = "PassVault - 解锁";
            }

            txtPassword.Focus();
        }

        private async void BtnUnlock_Click(object sender, EventArgs e)
        {
            if (busy)
            {
                return;
            }

            lblError.Text = string.Empty;
            string password = txtPassword.Text;
            if (setupMode)
            {
                if (!string.Equals(password, txtConfirm.Text, StringComparison.Ordinal))
                {
                    ShowError("两次输入的主密码不一致。");
                    return;
                }
                try
                {
                    MasterPasswordPolicy.Validate(password);
                }
                catch (VaultException ex)
                {
                    ShowError(ex.Message);
                    return;
                }
            }
            else if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入主密码。");
                return;
            }

            busy = true;
            btnUnlock.Enabled = false;
            string originalButtonText = btnUnlock.Text;
            btnUnlock.Text = setupMode ? "正在创建…" : "正在解锁…";
            Cursor previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            VaultSession session = null;
            string errorMessage = null;

            try
            {
                bool isSetup = setupMode;
                session = await Task.Run(delegate
                {
                    return isSetup ? store.Create(password) : store.Unlock(password);
                });
            }
            catch (VaultException ex)
            {
                errorMessage = ex.Message;
            }
            catch (Exception)
            {
                errorMessage = "操作失败，请检查密码库文件是否完整。";
            }
            finally
            {
                Cursor = previousCursor;
                btnUnlock.Enabled = true;
                btnUnlock.Text = originalButtonText;
                busy = false;
                txtPassword.Clear();
                txtConfirm.Clear();
            }

            if (IsDisposed)
            {
                if (session != null)
                {
                    session.Dispose();
                }
                return;
            }

            if (errorMessage != null)
            {
                ShowError(errorMessage);
                return;
            }
            if (session != null)
            {
                OpenMain(session);
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
        }

        private void OpenMain(VaultSession session)
        {
            Hide();
            mainForm = new MainForm(session);
            mainForm.LockRequested += MainForm_LockRequested;
            mainForm.FormClosed += MainForm_FormClosed;
            mainForm.Show();
            mainForm.Activate();
        }

        private void MainForm_LockRequested(object sender, EventArgs e)
        {
            MainForm form = mainForm;
            mainForm = null;
            if (form != null)
            {
                form.LockRequested -= MainForm_LockRequested;
                form.FormClosed -= MainForm_FormClosed;
                form.Dispose();
            }

            Show();
            WindowState = FormWindowState.Normal;
            ConfigureMode();
            Activate();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mainForm != null)
            {
                mainForm.LockRequested -= MainForm_LockRequested;
                mainForm.FormClosed -= MainForm_FormClosed;
                mainForm = null;
            }
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtPassword.Focus();
        }
    }
}