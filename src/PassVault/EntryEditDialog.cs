using System;
using System.Drawing;
using System.Windows.Forms;

namespace PassVault
{
    public sealed class EntryEditDialog : Form
    {
        private readonly VaultSession session;
        private readonly PasswordEntry existing;
        private RoundedTextBox txtSiteName;
        private RoundedTextBox txtWebsite;
        private RoundedTextBox txtAccount;
        private RoundedTextBox txtPassword;
        private RoundedTextBox txtNote;
        private Button btnShow;
        private Label lblError;
        private Label lblStrength;
        private Button btnSave;
        private PasswordOptionsControl optionsControl;
        private string savedEntryId;

        public string SavedEntryId
        {
            get { return savedEntryId; }
        }

        public EntryEditDialog(VaultSession session, PasswordEntry existing)
        {
            this.session = session;
            this.existing = existing;
            InitializeComponent();

            if (existing == null)
            {
                Text = "新增账号";
                GeneratePassword();
            }
            else
            {
                Text = "编辑账号";
                txtSiteName.Text = existing.SiteName;
                txtWebsite.Text = existing.Website;
                txtAccount.Text = existing.Account;
                txtPassword.Text = existing.Password;
                txtNote.Text = existing.Note;
            }
            UpdateStrength();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(560, 610);
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

            Label title = UiTheme.Label(existing == null ? "新增账号" : "编辑账号", 18F, UiTheme.Text, FontStyle.Bold);
            title.Location = new Point(24, 16);
            header.Controls.Add(title);

            Label subtitle = UiTheme.Label("填写账号信息，密码可手动修改或按选项生成", 8.5F, UiTheme.HeaderSubtle, FontStyle.Regular);
            subtitle.Location = new Point(26, 43);
            header.Controls.Add(subtitle);

            Panel headerLine = new Panel();
            headerLine.Dock = DockStyle.Bottom;
            headerLine.Height = 1;
            headerLine.BackColor = UiTheme.Border;
            header.Controls.Add(headerLine);

            Label lblSiteName = UiTheme.Label("网站名称", 9F, UiTheme.Text, FontStyle.Bold);
            lblSiteName.Location = new Point(28, 82);
            Controls.Add(lblSiteName);

            txtSiteName = UiTheme.TextBox(false);
            txtSiteName.Location = new Point(28, 104);
            txtSiteName.Width = 504;
            Controls.Add(txtSiteName);

            Label lblWebsite = UiTheme.Label("网站地址", 9F, UiTheme.Text, FontStyle.Bold);
            lblWebsite.Location = new Point(28, 142);
            Controls.Add(lblWebsite);

            txtWebsite = UiTheme.TextBox(false);
            txtWebsite.Location = new Point(28, 164);
            txtWebsite.Width = 504;
            Controls.Add(txtWebsite);

            Label lblAccount = UiTheme.Label("账号", 9F, UiTheme.Text, FontStyle.Bold);
            lblAccount.Location = new Point(28, 202);
            Controls.Add(lblAccount);

            txtAccount = UiTheme.TextBox(false);
            txtAccount.Location = new Point(28, 224);
            txtAccount.Width = 504;
            Controls.Add(txtAccount);

            Label lblPassword = UiTheme.Label("密码", 9F, UiTheme.Text, FontStyle.Bold);
            lblPassword.Location = new Point(28, 262);
            Controls.Add(lblPassword);

            txtPassword = UiTheme.TextBox(false);
            txtPassword.Location = new Point(28, 284);
            txtPassword.Width = 390;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += TxtPassword_TextChanged;
            Controls.Add(txtPassword);

            btnShow = UiTheme.Button("显示", false);
            btnShow.Location = new Point(426, 283);
            btnShow.Size = new Size(106, 31);
            btnShow.Click += BtnShow_Click;
            Controls.Add(btnShow);

            lblStrength = new Label();
            lblStrength.AutoSize = false;
            lblStrength.Location = new Point(28, 320);
            lblStrength.Size = new Size(240, 20);
            lblStrength.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            lblStrength.ForeColor = UiTheme.Muted;
            Controls.Add(lblStrength);

            RoundedPanel optionsGroup = UiTheme.Card();
            optionsGroup.Location = new Point(28, 344);
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

            Label lblNote = UiTheme.Label("备注", 9F, UiTheme.Text, FontStyle.Bold);
            lblNote.Location = new Point(28, 452);
            Controls.Add(lblNote);

            txtNote = UiTheme.TextBox(false);
            txtNote.Location = new Point(28, 472);
            txtNote.Width = 504;
            txtNote.Height = 52;
            txtNote.Multiline = true;
            txtNote.ScrollBars = ScrollBars.Vertical;
            txtNote.AcceptsReturn = true;
            Controls.Add(txtNote);

            lblError = new Label();
            lblError.AutoSize = false;
            lblError.Location = new Point(28, 530);
            lblError.Size = new Size(504, 26);
            lblError.ForeColor = UiTheme.Danger;
            lblError.Font = UiTheme.FontOf(8.5F, FontStyle.Regular);
            Controls.Add(lblError);

            Button btnCancel = UiTheme.Button("取消", false);
            btnCancel.Location = new Point(300, 566);
            btnCancel.Size = new Size(112, 36);
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            btnSave = UiTheme.Button("保存", true);
            btnSave.Location = new Point(420, 566);
            btnSave.Size = new Size(112, 36);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ResumeLayout(false);
            PerformLayout();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtSiteName.Focus();
        }

        private void BtnShow_Click(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
            btnShow.Text = txtPassword.UseSystemPasswordChar ? "显示" : "隐藏";
        }

        private void OptionsControl_GenerateRequested(object sender, EventArgs e)
        {
            GeneratePassword();
        }

        private void GeneratePassword()
        {
            lblError.Text = string.Empty;
            try
            {
                PasswordOptions options = optionsControl.GetOptions();
                string exclude = existing == null ? null : existing.Id;
                txtPassword.Text = session.GenerateUniquePassword(options, exclude);
                txtPassword.UseSystemPasswordChar = false;
                btnShow.Text = "隐藏";
                txtPassword.Focus();
                txtPassword.SelectAll();
                UpdateStrength();
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

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            UpdateStrength();
        }

        private void UpdateStrength()
        {
            string password = txtPassword.Text ?? string.Empty;
            int score = 0;
            if (password.Length >= 8) score++;
            if (password.Length >= 10) score++;
            if (password.Length >= 12) score++;
            if (password.Length >= 16) score++;
            if (HasUpper(password) && HasLower(password)) score++;
            if (HasDigit(password)) score++;
            if (HasSymbol(password)) score++;

            if (password.Length == 0)
            {
                lblStrength.Text = string.Empty;
            }
            else if (score <= 3)
            {
                lblStrength.Text = "密码强度：弱";
                lblStrength.ForeColor = UiTheme.Danger;
            }
            else if (score <= 5)
            {
                lblStrength.Text = "密码强度：中";
                lblStrength.ForeColor = Color.FromArgb(185, 123, 25);
            }
            else
            {
                lblStrength.Text = "密码强度：强";
                lblStrength.ForeColor = Color.FromArgb(30, 130, 76);
            }
        }

        private static bool HasUpper(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsUpper(value[i])) return true;
            }
            return false;
        }

        private static bool HasLower(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLower(value[i])) return true;
            }
            return false;
        }

        private static bool HasDigit(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i])) return true;
            }
            return false;
        }

        private static bool HasSymbol(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i])) return true;
            }
            return false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            lblError.Text = string.Empty;
            btnSave.Enabled = false;
            Cursor previous = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                if (existing == null)
                {
                    PasswordEntry entry = session.AddEntry(
                        txtSiteName.Text,
                        txtWebsite.Text,
                        txtAccount.Text,
                        txtPassword.Text,
                        txtNote.Text);
                    savedEntryId = entry.Id;
                }
                else
                {
                    session.UpdateEntry(
                        existing.Id,
                        txtSiteName.Text,
                        txtWebsite.Text,
                        txtAccount.Text,
                        txtPassword.Text,
                        txtNote.Text);
                    savedEntryId = existing.Id;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (DuplicatePasswordException ex)
            {
                lblError.Text = ex.Message;
                txtPassword.Focus();
                txtPassword.SelectAll();
            }
            catch (VaultException ex)
            {
                lblError.Text = ex.Message;
            }
            catch (Exception)
            {
                lblError.Text = "保存失败，请检查程序目录权限和磁盘空间。";
            }
            finally
            {
                Cursor = previous;
                btnSave.Enabled = true;
            }
        }
    }
}