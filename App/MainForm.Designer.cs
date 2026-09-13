namespace tarkov_settings
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.layoutTablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.TogglePreviewButton = new System.Windows.Forms.Button();
            this.ProfileGroupBox = new System.Windows.Forms.GroupBox();
            this.RenameProfileButton = new System.Windows.Forms.Button();
            this.profile_t1 = new System.Windows.Forms.Label();
            this.ProfileEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.ProfileComboBox = new System.Windows.Forms.ComboBox();
            this.DeleteProfileButton = new System.Windows.Forms.Button();
            this.TargetProcessTextBox = new System.Windows.Forms.TextBox();
            this.AddProfileButton = new System.Windows.Forms.Button();
            this.StabilizerGroup = new System.Windows.Forms.GroupBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.WhiteStabilizerLabel = new System.Windows.Forms.Label();
            this.WhiteStabilizerText = new System.Windows.Forms.TextBox();
            this.WhiteStabilizerBar = new System.Windows.Forms.TrackBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.DynamicAdaptiveCheckBox = new System.Windows.Forms.CheckBox();
            this.BlackStabilizerLabel = new System.Windows.Forms.Label();
            this.BlackStabilizerText = new System.Windows.Forms.TextBox();
            this.BlackStabilizerBar = new System.Windows.Forms.TrackBar();
            this.minimizeStartCheckBox = new System.Windows.Forms.CheckBox();
            this.DisplayCombo = new System.Windows.Forms.ComboBox();
            this.DVLGroupBox = new System.Windows.Forms.GroupBox();
            this.DVLPanel = new System.Windows.Forms.Panel();
            this.DVLLabel = new System.Windows.Forms.Label();
            this.DVLBar = new System.Windows.Forms.TrackBar();
            this.DVLText = new System.Windows.Forms.TextBox();
            this.colorGroupBox = new System.Windows.Forms.GroupBox();
            this.colorTablePanel = new System.Windows.Forms.TableLayoutPanel();
            this.brightnessPanel = new System.Windows.Forms.Panel();
            this.BrightnessBar = new System.Windows.Forms.TrackBar();
            this.BrightnessLabel = new System.Windows.Forms.Label();
            this.BrightnessText = new System.Windows.Forms.TextBox();
            this.contrastPanel = new System.Windows.Forms.Panel();
            this.ContrastBar = new System.Windows.Forms.TrackBar();
            this.ContrastText = new System.Windows.Forms.TextBox();
            this.ContrastLabel = new System.Windows.Forms.Label();
            this.gammaPanel = new System.Windows.Forms.Panel();
            this.GammaText = new System.Windows.Forms.TextBox();
            this.GammaBar = new System.Windows.Forms.TrackBar();
            this.GammaLabel = new System.Windows.Forms.Label();
            this.PreviewGroupBox = new System.Windows.Forms.GroupBox();
            this.LutGraphPictureBox = new System.Windows.Forms.PictureBox();
            this.PreviewPictureBox = new System.Windows.Forms.PictureBox();
            this.CompareOriginalCheckBox = new System.Windows.Forms.CheckBox();
            this.SampleImageComboBox = new System.Windows.Forms.ComboBox();
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.enableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.brightnessToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.contrastToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.gammaToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.dvlToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.layoutTablePanel.SuspendLayout();
            this.ColorPanel.SuspendLayout();
            this.ProfileGroupBox.SuspendLayout();
            this.StabilizerGroup.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WhiteStabilizerBar)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BlackStabilizerBar)).BeginInit();
            this.DVLGroupBox.SuspendLayout();
            this.DVLPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DVLBar)).BeginInit();
            this.colorGroupBox.SuspendLayout();
            this.colorTablePanel.SuspendLayout();
            this.brightnessPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BrightnessBar)).BeginInit();
            this.contrastPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ContrastBar)).BeginInit();
            this.gammaPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GammaBar)).BeginInit();
            this.PreviewGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LutGraphPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).BeginInit();
            this.trayMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutTablePanel
            // 
            this.layoutTablePanel.ColumnCount = 2;
            this.layoutTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.layoutTablePanel.Controls.Add(this.ColorPanel, 0, 0);
            this.layoutTablePanel.Controls.Add(this.PreviewGroupBox, 1, 0);
            this.layoutTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutTablePanel.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.layoutTablePanel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.layoutTablePanel.Location = new System.Drawing.Point(0, 0);
            this.layoutTablePanel.Name = "layoutTablePanel";
            this.layoutTablePanel.RowCount = 1;
            this.layoutTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutTablePanel.Size = new System.Drawing.Size(1393, 615);
            this.layoutTablePanel.TabIndex = 0;
            this.layoutTablePanel.Paint += new System.Windows.Forms.PaintEventHandler(this.layoutTablePanel_Paint);
            // 
            // ColorPanel
            // 
            this.ColorPanel.Controls.Add(this.TogglePreviewButton);
            this.ColorPanel.Controls.Add(this.ProfileGroupBox);
            this.ColorPanel.Controls.Add(this.StabilizerGroup);
            this.ColorPanel.Controls.Add(this.minimizeStartCheckBox);
            this.ColorPanel.Controls.Add(this.DisplayCombo);
            this.ColorPanel.Controls.Add(this.DVLGroupBox);
            this.ColorPanel.Controls.Add(this.colorGroupBox);
            this.ColorPanel.Location = new System.Drawing.Point(3, 3);
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Size = new System.Drawing.Size(652, 609);
            this.ColorPanel.TabIndex = 2;
            // 
            // TogglePreviewButton
            // 
            this.TogglePreviewButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.TogglePreviewButton.Location = new System.Drawing.Point(505, 489);
            this.TogglePreviewButton.Name = "TogglePreviewButton";
            this.TogglePreviewButton.Size = new System.Drawing.Size(75, 23);
            this.TogglePreviewButton.TabIndex = 21;
            this.TogglePreviewButton.Text = "▶ 미리보기";
            this.TogglePreviewButton.UseVisualStyleBackColor = true;
            // 
            // ProfileGroupBox
            // 
            this.ProfileGroupBox.Controls.Add(this.RenameProfileButton);
            this.ProfileGroupBox.Controls.Add(this.profile_t1);
            this.ProfileGroupBox.Controls.Add(this.ProfileEnabledCheckBox);
            this.ProfileGroupBox.Controls.Add(this.ProfileComboBox);
            this.ProfileGroupBox.Controls.Add(this.DeleteProfileButton);
            this.ProfileGroupBox.Controls.Add(this.TargetProcessTextBox);
            this.ProfileGroupBox.Controls.Add(this.AddProfileButton);
            this.ProfileGroupBox.Location = new System.Drawing.Point(3, 7);
            this.ProfileGroupBox.Name = "ProfileGroupBox";
            this.ProfileGroupBox.Size = new System.Drawing.Size(638, 82);
            this.ProfileGroupBox.TabIndex = 0;
            this.ProfileGroupBox.TabStop = false;
            this.ProfileGroupBox.Text = "Profile";
            this.ProfileGroupBox.Enter += new System.EventHandler(this.groupBox1_Enter_1);
            // 
            // RenameProfileButton
            // 
            this.RenameProfileButton.Font = new System.Drawing.Font("Consolas", 10F);
            this.RenameProfileButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.RenameProfileButton.Location = new System.Drawing.Point(93, 49);
            this.RenameProfileButton.Name = "RenameProfileButton";
            this.RenameProfileButton.Size = new System.Drawing.Size(88, 23);
            this.RenameProfileButton.TabIndex = 25;
            this.RenameProfileButton.Text = "이름 수정";
            this.RenameProfileButton.UseVisualStyleBackColor = true;
            this.RenameProfileButton.Click += new System.EventHandler(this.RenameProfileButton_Click);
            // 
            // profile_t1
            // 
            this.profile_t1.AutoSize = true;
            this.profile_t1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.profile_t1.Location = new System.Drawing.Point(281, 29);
            this.profile_t1.Name = "profile_t1";
            this.profile_t1.Size = new System.Drawing.Size(321, 14);
            this.profile_t1.TabIndex = 20;
            this.profile_t1.Text = ".exe를 뺀 영문 프로세스명을 입력하세요 (쉼표로 복수 입력 가능)";
            this.profile_t1.Click += new System.EventHandler(this.label1_Click_1);
            // 
            // ProfileEnabledCheckBox
            // 
            this.ProfileEnabledCheckBox.AutoSize = true;
            this.ProfileEnabledCheckBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.ProfileEnabledCheckBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ProfileEnabledCheckBox.Location = new System.Drawing.Point(6, 51);
            this.ProfileEnabledCheckBox.Name = "ProfileEnabledCheckBox";
            this.ProfileEnabledCheckBox.Size = new System.Drawing.Size(85, 21);
            this.ProfileEnabledCheckBox.TabIndex = 24;
            this.ProfileEnabledCheckBox.Text = "자동 활성화";
            this.ProfileEnabledCheckBox.UseVisualStyleBackColor = true;
            this.ProfileEnabledCheckBox.CheckedChanged += new System.EventHandler(this.ProfileEnabledCheckBox_CheckedChanged);
            // 
            // ProfileComboBox
            // 
            this.ProfileComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ProfileComboBox.FormattingEnabled = true;
            this.ProfileComboBox.Location = new System.Drawing.Point(6, 20);
            this.ProfileComboBox.Name = "ProfileComboBox";
            this.ProfileComboBox.Size = new System.Drawing.Size(175, 22);
            this.ProfileComboBox.TabIndex = 2;
            this.ProfileComboBox.SelectedIndexChanged += new System.EventHandler(this.ProfileComboBox_SelectedIndexChanged);
            // 
            // DeleteProfileButton
            // 
            this.DeleteProfileButton.Font = new System.Drawing.Font("Consolas", 10F);
            this.DeleteProfileButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DeleteProfileButton.Location = new System.Drawing.Point(187, 49);
            this.DeleteProfileButton.Name = "DeleteProfileButton";
            this.DeleteProfileButton.Size = new System.Drawing.Size(88, 23);
            this.DeleteProfileButton.TabIndex = 22;
            this.DeleteProfileButton.Text = "프로필 삭제";
            this.DeleteProfileButton.UseVisualStyleBackColor = true;
            this.DeleteProfileButton.Click += new System.EventHandler(this.DeleteProfileButton_Click);
            // 
            // TargetProcessTextBox
            // 
            this.TargetProcessTextBox.Location = new System.Drawing.Point(281, 49);
            this.TargetProcessTextBox.Name = "TargetProcessTextBox";
            this.TargetProcessTextBox.Size = new System.Drawing.Size(351, 22);
            this.TargetProcessTextBox.TabIndex = 23;
            this.TargetProcessTextBox.TextChanged += new System.EventHandler(this.TargetProcessTextBox_TextChanged);
            // 
            // AddProfileButton
            // 
            this.AddProfileButton.Font = new System.Drawing.Font("Consolas", 10F);
            this.AddProfileButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.AddProfileButton.Location = new System.Drawing.Point(187, 20);
            this.AddProfileButton.Name = "AddProfileButton";
            this.AddProfileButton.Size = new System.Drawing.Size(88, 23);
            this.AddProfileButton.TabIndex = 21;
            this.AddProfileButton.Text = "새 프로필";
            this.AddProfileButton.UseVisualStyleBackColor = true;
            this.AddProfileButton.Click += new System.EventHandler(this.AddProfileButton_Click);
            // 
            // StabilizerGroup
            // 
            this.StabilizerGroup.Controls.Add(this.panel2);
            this.StabilizerGroup.Controls.Add(this.panel1);
            this.StabilizerGroup.Location = new System.Drawing.Point(3, 409);
            this.StabilizerGroup.Name = "StabilizerGroup";
            this.StabilizerGroup.Size = new System.Drawing.Size(490, 197);
            this.StabilizerGroup.TabIndex = 20;
            this.StabilizerGroup.TabStop = false;
            this.StabilizerGroup.Text = "Stabilizer";
            this.StabilizerGroup.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.WhiteStabilizerLabel);
            this.panel2.Controls.Add(this.WhiteStabilizerText);
            this.panel2.Controls.Add(this.WhiteStabilizerBar);
            this.panel2.Location = new System.Drawing.Point(6, 109);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(481, 82);
            this.panel2.TabIndex = 27;
            // 
            // WhiteStabilizerLabel
            // 
            this.WhiteStabilizerLabel.AutoSize = true;
            this.WhiteStabilizerLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.WhiteStabilizerLabel.Location = new System.Drawing.Point(20, 16);
            this.WhiteStabilizerLabel.Name = "WhiteStabilizerLabel";
            this.WhiteStabilizerLabel.Size = new System.Drawing.Size(119, 14);
            this.WhiteStabilizerLabel.TabIndex = 18;
            this.WhiteStabilizerLabel.Text = "White Stabilizer";
            this.WhiteStabilizerLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // WhiteStabilizerText
            // 
            this.WhiteStabilizerText.Location = new System.Drawing.Point(424, 33);
            this.WhiteStabilizerText.Name = "WhiteStabilizerText";
            this.WhiteStabilizerText.Size = new System.Drawing.Size(41, 22);
            this.WhiteStabilizerText.TabIndex = 19;
            this.WhiteStabilizerText.Text = "0";
            this.WhiteStabilizerText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // WhiteStabilizerBar
            // 
            this.WhiteStabilizerBar.Location = new System.Drawing.Point(13, 33);
            this.WhiteStabilizerBar.Maximum = 100;
            this.WhiteStabilizerBar.Name = "WhiteStabilizerBar";
            this.WhiteStabilizerBar.Size = new System.Drawing.Size(397, 45);
            this.WhiteStabilizerBar.TabIndex = 17;
            this.WhiteStabilizerBar.TickFrequency = 10;
            this.WhiteStabilizerBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.DynamicAdaptiveCheckBox);
            this.panel1.Controls.Add(this.BlackStabilizerLabel);
            this.panel1.Controls.Add(this.BlackStabilizerText);
            this.panel1.Controls.Add(this.BlackStabilizerBar);
            this.panel1.Location = new System.Drawing.Point(6, 21);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(481, 82);
            this.panel1.TabIndex = 26;
            // 
            // DynamicAdaptiveCheckBox
            // 
            this.DynamicAdaptiveCheckBox.AutoSize = true;
            this.DynamicAdaptiveCheckBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DynamicAdaptiveCheckBox.Location = new System.Drawing.Point(285, 12);
            this.DynamicAdaptiveCheckBox.Name = "DynamicAdaptiveCheckBox";
            this.DynamicAdaptiveCheckBox.Size = new System.Drawing.Size(180, 18);
            this.DynamicAdaptiveCheckBox.TabIndex = 20;
            this.DynamicAdaptiveCheckBox.Text = "Dynamic Adaptive Boost";
            this.DynamicAdaptiveCheckBox.UseVisualStyleBackColor = true;
            this.DynamicAdaptiveCheckBox.CheckedChanged += new System.EventHandler(this.DynamicAdaptiveCheckBox_CheckedChanged);
            // 
            // BlackStabilizerLabel
            // 
            this.BlackStabilizerLabel.AutoSize = true;
            this.BlackStabilizerLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BlackStabilizerLabel.Location = new System.Drawing.Point(20, 16);
            this.BlackStabilizerLabel.Name = "BlackStabilizerLabel";
            this.BlackStabilizerLabel.Size = new System.Drawing.Size(119, 14);
            this.BlackStabilizerLabel.TabIndex = 18;
            this.BlackStabilizerLabel.Text = "Black Stabilizer";
            this.BlackStabilizerLabel.Click += new System.EventHandler(this.label1_Click);
            this.BlackStabilizerLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // BlackStabilizerText
            // 
            this.BlackStabilizerText.Location = new System.Drawing.Point(424, 33);
            this.BlackStabilizerText.Name = "BlackStabilizerText";
            this.BlackStabilizerText.Size = new System.Drawing.Size(41, 22);
            this.BlackStabilizerText.TabIndex = 19;
            this.BlackStabilizerText.Text = "0";
            this.BlackStabilizerText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // BlackStabilizerBar
            // 
            this.BlackStabilizerBar.Location = new System.Drawing.Point(13, 33);
            this.BlackStabilizerBar.Maximum = 100;
            this.BlackStabilizerBar.Name = "BlackStabilizerBar";
            this.BlackStabilizerBar.Size = new System.Drawing.Size(397, 45);
            this.BlackStabilizerBar.TabIndex = 17;
            this.BlackStabilizerBar.TickFrequency = 10;
            this.BlackStabilizerBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // minimizeStartCheckBox
            // 
            this.minimizeStartCheckBox.AutoSize = true;
            this.minimizeStartCheckBox.BackColor = System.Drawing.Color.Transparent;
            this.minimizeStartCheckBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.minimizeStartCheckBox.Location = new System.Drawing.Point(505, 442);
            this.minimizeStartCheckBox.Name = "minimizeStartCheckBox";
            this.minimizeStartCheckBox.Size = new System.Drawing.Size(138, 32);
            this.minimizeStartCheckBox.TabIndex = 16;
            this.minimizeStartCheckBox.Text = "Minimize to Tray\r\non Start";
            this.minimizeStartCheckBox.UseVisualStyleBackColor = false;
            this.minimizeStartCheckBox.CheckedChanged += new System.EventHandler(this.CheckOnMinimizeToTray);
            // 
            // DisplayCombo
            // 
            this.DisplayCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DisplayCombo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.DisplayCombo.FormattingEnabled = true;
            this.DisplayCombo.Location = new System.Drawing.Point(505, 409);
            this.DisplayCombo.Name = "DisplayCombo";
            this.DisplayCombo.Size = new System.Drawing.Size(139, 22);
            this.DisplayCombo.TabIndex = 15;
            this.DisplayCombo.SelectedValueChanged += new System.EventHandler(this.DisplayCombo_SelectedValueChanged);
            // 
            // DVLGroupBox
            // 
            this.DVLGroupBox.Controls.Add(this.DVLPanel);
            this.DVLGroupBox.Location = new System.Drawing.Point(502, 96);
            this.DVLGroupBox.Name = "DVLGroupBox";
            this.DVLGroupBox.Size = new System.Drawing.Size(145, 307);
            this.DVLGroupBox.TabIndex = 13;
            this.DVLGroupBox.TabStop = false;
            this.DVLGroupBox.Text = "DVL";
            // 
            // DVLPanel
            // 
            this.DVLPanel.Controls.Add(this.DVLLabel);
            this.DVLPanel.Controls.Add(this.DVLBar);
            this.DVLPanel.Controls.Add(this.DVLText);
            this.DVLPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DVLPanel.Location = new System.Drawing.Point(3, 18);
            this.DVLPanel.Name = "DVLPanel";
            this.DVLPanel.Size = new System.Drawing.Size(139, 286);
            this.DVLPanel.TabIndex = 0;
            // 
            // DVLLabel
            // 
            this.DVLLabel.AutoSize = true;
            this.DVLLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.DVLLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.DVLLabel.Location = new System.Drawing.Point(13, 11);
            this.DVLLabel.Name = "DVLLabel";
            this.DVLLabel.Size = new System.Drawing.Size(119, 28);
            this.DVLLabel.TabIndex = 10;
            this.DVLLabel.Text = "Digital Vibrance\r\n(Saturation)\r\n";
            this.DVLLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.DVLLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // DVLBar
            // 
            this.DVLBar.Location = new System.Drawing.Point(56, 42);
            this.DVLBar.Maximum = 63;
            this.DVLBar.Name = "DVLBar";
            this.DVLBar.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.DVLBar.Size = new System.Drawing.Size(45, 184);
            this.DVLBar.TabIndex = 9;
            this.DVLBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.DVLBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // DVLText
            // 
            this.DVLText.Location = new System.Drawing.Point(46, 232);
            this.DVLText.Name = "DVLText";
            this.DVLText.ReadOnly = true;
            this.DVLText.Size = new System.Drawing.Size(41, 22);
            this.DVLText.TabIndex = 11;
            this.DVLText.Text = "0";
            this.DVLText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // colorGroupBox
            // 
            this.colorGroupBox.Controls.Add(this.colorTablePanel);
            this.colorGroupBox.Location = new System.Drawing.Point(3, 96);
            this.colorGroupBox.Name = "colorGroupBox";
            this.colorGroupBox.Size = new System.Drawing.Size(490, 307);
            this.colorGroupBox.TabIndex = 12;
            this.colorGroupBox.TabStop = false;
            this.colorGroupBox.Text = "Color";
            // 
            // colorTablePanel
            // 
            this.colorTablePanel.ColumnCount = 1;
            this.colorTablePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.colorTablePanel.Controls.Add(this.brightnessPanel, 0, 0);
            this.colorTablePanel.Controls.Add(this.contrastPanel, 0, 1);
            this.colorTablePanel.Controls.Add(this.gammaPanel, 0, 2);
            this.colorTablePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.colorTablePanel.Location = new System.Drawing.Point(3, 18);
            this.colorTablePanel.Name = "colorTablePanel";
            this.colorTablePanel.RowCount = 3;
            this.colorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.colorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.colorTablePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.colorTablePanel.Size = new System.Drawing.Size(484, 286);
            this.colorTablePanel.TabIndex = 1;
            // 
            // brightnessPanel
            // 
            this.brightnessPanel.Controls.Add(this.BrightnessBar);
            this.brightnessPanel.Controls.Add(this.BrightnessLabel);
            this.brightnessPanel.Controls.Add(this.BrightnessText);
            this.brightnessPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.brightnessPanel.Location = new System.Drawing.Point(3, 3);
            this.brightnessPanel.Name = "brightnessPanel";
            this.brightnessPanel.Size = new System.Drawing.Size(478, 89);
            this.brightnessPanel.TabIndex = 0;
            // 
            // BrightnessBar
            // 
            this.BrightnessBar.Location = new System.Drawing.Point(13, 27);
            this.BrightnessBar.Maximum = 100;
            this.BrightnessBar.Minimum = -100;
            this.BrightnessBar.Name = "BrightnessBar";
            this.BrightnessBar.Size = new System.Drawing.Size(397, 45);
            this.BrightnessBar.TabIndex = 18;
            this.BrightnessBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.BrightnessBar.Value = 50;
            this.BrightnessBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // BrightnessLabel
            // 
            this.BrightnessLabel.AutoSize = true;
            this.BrightnessLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BrightnessLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BrightnessLabel.Location = new System.Drawing.Point(20, 10);
            this.BrightnessLabel.Name = "BrightnessLabel";
            this.BrightnessLabel.Size = new System.Drawing.Size(77, 14);
            this.BrightnessLabel.TabIndex = 21;
            this.BrightnessLabel.Text = "Brightness";
            this.BrightnessLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // BrightnessText
            // 
            this.BrightnessText.Location = new System.Drawing.Point(424, 27);
            this.BrightnessText.Name = "BrightnessText";
            this.BrightnessText.ReadOnly = true;
            this.BrightnessText.Size = new System.Drawing.Size(41, 22);
            this.BrightnessText.TabIndex = 24;
            this.BrightnessText.Text = "0.50";
            this.BrightnessText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // contrastPanel
            // 
            this.contrastPanel.Controls.Add(this.ContrastBar);
            this.contrastPanel.Controls.Add(this.ContrastText);
            this.contrastPanel.Controls.Add(this.ContrastLabel);
            this.contrastPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contrastPanel.Location = new System.Drawing.Point(3, 98);
            this.contrastPanel.Name = "contrastPanel";
            this.contrastPanel.Size = new System.Drawing.Size(478, 89);
            this.contrastPanel.TabIndex = 1;
            // 
            // ContrastBar
            // 
            this.ContrastBar.Location = new System.Drawing.Point(13, 39);
            this.ContrastBar.Maximum = 100;
            this.ContrastBar.Minimum = -100;
            this.ContrastBar.Name = "ContrastBar";
            this.ContrastBar.Size = new System.Drawing.Size(397, 45);
            this.ContrastBar.TabIndex = 19;
            this.ContrastBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.ContrastBar.Value = 50;
            this.ContrastBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // ContrastText
            // 
            this.ContrastText.Location = new System.Drawing.Point(424, 39);
            this.ContrastText.Name = "ContrastText";
            this.ContrastText.ReadOnly = true;
            this.ContrastText.Size = new System.Drawing.Size(41, 22);
            this.ContrastText.TabIndex = 25;
            this.ContrastText.Text = "0.50";
            this.ContrastText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ContrastLabel
            // 
            this.ContrastLabel.AutoSize = true;
            this.ContrastLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ContrastLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ContrastLabel.Location = new System.Drawing.Point(20, 22);
            this.ContrastLabel.Name = "ContrastLabel";
            this.ContrastLabel.Size = new System.Drawing.Size(63, 14);
            this.ContrastLabel.TabIndex = 22;
            this.ContrastLabel.Text = "Contrast";
            this.ContrastLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // gammaPanel
            // 
            this.gammaPanel.Controls.Add(this.GammaText);
            this.gammaPanel.Controls.Add(this.GammaBar);
            this.gammaPanel.Controls.Add(this.GammaLabel);
            this.gammaPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gammaPanel.Location = new System.Drawing.Point(3, 193);
            this.gammaPanel.Name = "gammaPanel";
            this.gammaPanel.Size = new System.Drawing.Size(478, 90);
            this.gammaPanel.TabIndex = 2;
            // 
            // GammaText
            // 
            this.GammaText.Location = new System.Drawing.Point(424, 40);
            this.GammaText.Name = "GammaText";
            this.GammaText.ReadOnly = true;
            this.GammaText.Size = new System.Drawing.Size(41, 22);
            this.GammaText.TabIndex = 26;
            this.GammaText.Text = "1.00";
            this.GammaText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // GammaBar
            // 
            this.GammaBar.Location = new System.Drawing.Point(13, 40);
            this.GammaBar.Maximum = 280;
            this.GammaBar.Minimum = 40;
            this.GammaBar.Name = "GammaBar";
            this.GammaBar.Size = new System.Drawing.Size(397, 45);
            this.GammaBar.TabIndex = 20;
            this.GammaBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.GammaBar.Value = 100;
            this.GammaBar.ValueChanged += new System.EventHandler(this.TrackBar_ValueChanged);
            // 
            // GammaLabel
            // 
            this.GammaLabel.AutoSize = true;
            this.GammaLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.GammaLabel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.GammaLabel.Location = new System.Drawing.Point(20, 23);
            this.GammaLabel.Name = "GammaLabel";
            this.GammaLabel.Size = new System.Drawing.Size(42, 14);
            this.GammaLabel.TabIndex = 23;
            this.GammaLabel.Text = "Gamma";
            this.GammaLabel.DoubleClick += new System.EventHandler(this.ColorLabel_DClick);
            // 
            // PreviewGroupBox
            // 
            this.PreviewGroupBox.Controls.Add(this.LutGraphPictureBox);
            this.PreviewGroupBox.Controls.Add(this.PreviewPictureBox);
            this.PreviewGroupBox.Controls.Add(this.CompareOriginalCheckBox);
            this.PreviewGroupBox.Controls.Add(this.SampleImageComboBox);
            this.PreviewGroupBox.Location = new System.Drawing.Point(661, 3);
            this.PreviewGroupBox.Name = "PreviewGroupBox";
            this.PreviewGroupBox.Size = new System.Drawing.Size(725, 609);
            this.PreviewGroupBox.TabIndex = 3;
            this.PreviewGroupBox.TabStop = false;
            this.PreviewGroupBox.Text = "Preview";
            // 
            // LutGraphPictureBox
            // 
            this.LutGraphPictureBox.BackColor = System.Drawing.Color.Black;
            this.LutGraphPictureBox.Location = new System.Drawing.Point(15, 453);
            this.LutGraphPictureBox.Name = "LutGraphPictureBox";
            this.LutGraphPictureBox.Size = new System.Drawing.Size(150, 150);
            this.LutGraphPictureBox.TabIndex = 3;
            this.LutGraphPictureBox.TabStop = false;
            // 
            // PreviewPictureBox
            // 
            this.PreviewPictureBox.Location = new System.Drawing.Point(6, 56);
            this.PreviewPictureBox.Name = "PreviewPictureBox";
            this.PreviewPictureBox.Size = new System.Drawing.Size(711, 391);
            this.PreviewPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PreviewPictureBox.TabIndex = 2;
            this.PreviewPictureBox.TabStop = false;
            // 
            // CompareOriginalCheckBox
            // 
            this.CompareOriginalCheckBox.AutoSize = true;
            this.CompareOriginalCheckBox.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.CompareOriginalCheckBox.Location = new System.Drawing.Point(238, 23);
            this.CompareOriginalCheckBox.Name = "CompareOriginalCheckBox";
            this.CompareOriginalCheckBox.Size = new System.Drawing.Size(44, 18);
            this.CompareOriginalCheckBox.TabIndex = 1;
            this.CompareOriginalCheckBox.Text = "원본";
            this.CompareOriginalCheckBox.UseVisualStyleBackColor = true;
            this.CompareOriginalCheckBox.CheckedChanged += new System.EventHandler(this.CompareOriginalCheckBox_CheckedChanged);
            // 
            // SampleImageComboBox
            // 
            this.SampleImageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SampleImageComboBox.FormattingEnabled = true;
            this.SampleImageComboBox.Location = new System.Drawing.Point(6, 21);
            this.SampleImageComboBox.Name = "SampleImageComboBox";
            this.SampleImageComboBox.Size = new System.Drawing.Size(217, 22);
            this.SampleImageComboBox.TabIndex = 0;
            this.SampleImageComboBox.SelectedIndexChanged += new System.EventHandler(this.SampleImageComboBox_SelectedIndexChanged);
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayMenuStrip;
            this.trayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("trayIcon.Icon")));
            this.trayIcon.Text = "Tarkov Settings";
            this.trayIcon.Visible = true;
            this.trayIcon.DoubleClick += new System.EventHandler(this.ShowForm);
            // 
            // trayMenuStrip
            // 
            this.trayMenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.trayMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableToolStripMenuItem,
            this.showToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.trayMenuStrip.Name = "trayMenuStrip";
            this.trayMenuStrip.Size = new System.Drawing.Size(110, 70);
            // 
            // enableToolStripMenuItem
            // 
            this.enableToolStripMenuItem.Checked = true;
            this.enableToolStripMenuItem.CheckOnClick = true;
            this.enableToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.enableToolStripMenuItem.Name = "enableToolStripMenuItem";
            this.enableToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.enableToolStripMenuItem.Text = "Enable";
            // 
            // showToolStripMenuItem
            // 
            this.showToolStripMenuItem.Name = "showToolStripMenuItem";
            this.showToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.showToolStripMenuItem.Text = "Show";
            this.showToolStripMenuItem.Click += new System.EventHandler(this.ShowForm);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitFormClicked);
            // 
            // brightnessToolTip
            // 
            this.brightnessToolTip.IsBalloon = true;
            this.brightnessToolTip.ShowAlways = true;
            this.brightnessToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.brightnessToolTip.ToolTipTitle = "Brightness";
            // 
            // contrastToolTip
            // 
            this.contrastToolTip.IsBalloon = true;
            this.contrastToolTip.ShowAlways = true;
            this.contrastToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            this.contrastToolTip.ToolTipTitle = "Contrast";
            // 
            // gammaToolTip
            // 
            this.gammaToolTip.IsBalloon = true;
            this.gammaToolTip.ShowAlways = true;
            this.gammaToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.gammaToolTip.ToolTipTitle = "Gamma";
            // 
            // dvlToolTip
            // 
            this.dvlToolTip.IsBalloon = true;
            this.dvlToolTip.ShowAlways = true;
            this.dvlToolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.dvlToolTip.ToolTipTitle = "Saturation";
            // 
            // MainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1393, 615);
            this.Controls.Add(this.layoutTablePanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Tarkov Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.layoutTablePanel.ResumeLayout(false);
            this.ColorPanel.ResumeLayout(false);
            this.ColorPanel.PerformLayout();
            this.ProfileGroupBox.ResumeLayout(false);
            this.ProfileGroupBox.PerformLayout();
            this.StabilizerGroup.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WhiteStabilizerBar)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BlackStabilizerBar)).EndInit();
            this.DVLGroupBox.ResumeLayout(false);
            this.DVLPanel.ResumeLayout(false);
            this.DVLPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DVLBar)).EndInit();
            this.colorGroupBox.ResumeLayout(false);
            this.colorTablePanel.ResumeLayout(false);
            this.brightnessPanel.ResumeLayout(false);
            this.brightnessPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BrightnessBar)).EndInit();
            this.contrastPanel.ResumeLayout(false);
            this.contrastPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ContrastBar)).EndInit();
            this.gammaPanel.ResumeLayout(false);
            this.gammaPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.GammaBar)).EndInit();
            this.PreviewGroupBox.ResumeLayout(false);
            this.PreviewGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.LutGraphPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PreviewPictureBox)).EndInit();
            this.trayMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel layoutTablePanel;
        private System.Windows.Forms.Panel ColorPanel;
        
        

        private System.Windows.Forms.GroupBox colorGroupBox;
        private System.Windows.Forms.TextBox DVLText;
        private System.Windows.Forms.Label DVLLabel;
        private System.Windows.Forms.TrackBar DVLBar;
        private System.Windows.Forms.TextBox GammaText;
        private System.Windows.Forms.TextBox ContrastText;
        private System.Windows.Forms.TextBox BrightnessText;
        private System.Windows.Forms.Label GammaLabel;
        private System.Windows.Forms.Label ContrastLabel;
        private System.Windows.Forms.Label BrightnessLabel;
        private System.Windows.Forms.TrackBar GammaBar;
        private System.Windows.Forms.TrackBar ContrastBar;
        private System.Windows.Forms.TrackBar BrightnessBar;
        private System.Windows.Forms.TableLayoutPanel colorTablePanel;
        private System.Windows.Forms.Panel brightnessPanel;
        private System.Windows.Forms.Panel contrastPanel;
        private System.Windows.Forms.Panel gammaPanel;
        private System.Windows.Forms.GroupBox DVLGroupBox;
        private System.Windows.Forms.Panel DVLPanel;
        private System.Windows.Forms.ComboBox DisplayCombo;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem enableToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.CheckBox minimizeStartCheckBox;
        private System.Windows.Forms.ToolTip dvlToolTip;
        private System.Windows.Forms.ToolTip brightnessToolTip;
        private System.Windows.Forms.ToolTip contrastToolTip;
        private System.Windows.Forms.ToolTip gammaToolTip;
        private System.Windows.Forms.Label BlackStabilizerLabel;
        private System.Windows.Forms.TrackBar BlackStabilizerBar;
        private System.Windows.Forms.GroupBox StabilizerGroup;
        private System.Windows.Forms.TextBox BlackStabilizerText;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox DynamicAdaptiveCheckBox;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label WhiteStabilizerLabel;
        private System.Windows.Forms.TextBox WhiteStabilizerText;
        private System.Windows.Forms.TrackBar WhiteStabilizerBar;
        private System.Windows.Forms.ComboBox ProfileComboBox;
        private System.Windows.Forms.CheckBox ProfileEnabledCheckBox;
        private System.Windows.Forms.TextBox TargetProcessTextBox;
        private System.Windows.Forms.Button DeleteProfileButton;
        private System.Windows.Forms.Button AddProfileButton;
        private System.Windows.Forms.GroupBox ProfileGroupBox;
        private System.Windows.Forms.Label profile_t1;
        private System.Windows.Forms.Button RenameProfileButton;
        private System.Windows.Forms.GroupBox PreviewGroupBox;
        private System.Windows.Forms.PictureBox PreviewPictureBox;
        private System.Windows.Forms.CheckBox CompareOriginalCheckBox;
        private System.Windows.Forms.ComboBox SampleImageComboBox;
        private System.Windows.Forms.Button TogglePreviewButton;
        private System.Windows.Forms.PictureBox LutGraphPictureBox;
    }
}

