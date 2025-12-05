namespace SeniorProjectCompressionApp
{
    partial class Form1
    {
        // Required designer variable.
        private System.ComponentModel.IContainer components = null;

        // Clean up any resources being used. disposing is true when managed resources should be released.
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        // Required method for Designer support - do not modify with the code editor.
        private void InitializeComponent()
        {
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabCompression = new System.Windows.Forms.TabPage();
            this.btnStartCompression = new System.Windows.Forms.Button();
            this.btnBrowseCompressionOutput = new System.Windows.Forms.Button();
            this.txtCompressionOutput = new System.Windows.Forms.TextBox();
            this.lblCompressionOutput = new System.Windows.Forms.Label();
            this.chkCompressionShowPassword = new System.Windows.Forms.CheckBox();
            this.txtCompressionPassword = new System.Windows.Forms.TextBox();
            this.lblCompressionPassword = new System.Windows.Forms.Label();
            this.cmbCompressionAlgorithm = new System.Windows.Forms.ComboBox();
            this.lblCompressionAlgorithm = new System.Windows.Forms.Label();
            this.btnBrowseCompressionFolder = new System.Windows.Forms.Button();
            this.btnBrowseCompressionFile = new System.Windows.Forms.Button();
            this.txtCompressionInput = new System.Windows.Forms.TextBox();
            this.lblCompressionInput = new System.Windows.Forms.Label();
            this.tabDecompression = new System.Windows.Forms.TabPage();
            this.btnStartDecompression = new System.Windows.Forms.Button();
            this.chkDecompressionShowPassword = new System.Windows.Forms.CheckBox();
            this.txtDecompressionPassword = new System.Windows.Forms.TextBox();
            this.lblDecompressionPassword = new System.Windows.Forms.Label();
            this.btnBrowseDecompressionDestination = new System.Windows.Forms.Button();
            this.txtDecompressionDestination = new System.Windows.Forms.TextBox();
            this.lblDecompressionDestination = new System.Windows.Forms.Label();
            this.btnBrowseDecompressionArchive = new System.Windows.Forms.Button();
            this.txtDecompressionArchive = new System.Windows.Forms.TextBox();
            this.lblDecompressionArchive = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.tabControlMain.SuspendLayout();
            this.tabCompression.SuspendLayout();
            this.tabDecompression.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlMain.Controls.Add(this.tabCompression);
            this.tabControlMain.Controls.Add(this.tabDecompression);
            this.tabControlMain.Location = new System.Drawing.Point(12, 12);
            this.tabControlMain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(680, 393);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabCompression
            // 
            this.tabCompression.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(252)))));
            this.tabCompression.Controls.Add(this.btnStartCompression);
            this.tabCompression.Controls.Add(this.btnBrowseCompressionOutput);
            this.tabCompression.Controls.Add(this.txtCompressionOutput);
            this.tabCompression.Controls.Add(this.lblCompressionOutput);
            this.tabCompression.Controls.Add(this.chkCompressionShowPassword);
            this.tabCompression.Controls.Add(this.txtCompressionPassword);
            this.tabCompression.Controls.Add(this.lblCompressionPassword);
            this.tabCompression.Controls.Add(this.cmbCompressionAlgorithm);
            this.tabCompression.Controls.Add(this.lblCompressionAlgorithm);
            this.tabCompression.Controls.Add(this.btnBrowseCompressionFolder);
            this.tabCompression.Controls.Add(this.btnBrowseCompressionFile);
            this.tabCompression.Controls.Add(this.txtCompressionInput);
            this.tabCompression.Controls.Add(this.lblCompressionInput);
            this.tabCompression.Location = new System.Drawing.Point(4, 26);
            this.tabCompression.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabCompression.Name = "tabCompression";
            this.tabCompression.Padding = new System.Windows.Forms.Padding(18);
            this.tabCompression.Size = new System.Drawing.Size(672, 363);
            this.tabCompression.TabIndex = 0;
            this.tabCompression.Text = "Compression";
            // 
            // btnStartCompression
            // 
            this.btnStartCompression.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnStartCompression.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnStartCompression.FlatAppearance.BorderSize = 0;
            this.btnStartCompression.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(195)))));
            this.btnStartCompression.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(119)))), ((int)(((byte)(205)))));
            this.btnStartCompression.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartCompression.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnStartCompression.ForeColor = System.Drawing.Color.White;
            this.btnStartCompression.Location = new System.Drawing.Point(246, 317);
            this.btnStartCompression.Name = "btnStartCompression";
            this.btnStartCompression.Size = new System.Drawing.Size(180, 38);
            this.btnStartCompression.TabIndex = 12;
            this.btnStartCompression.Text = "Compress";
            this.btnStartCompression.UseVisualStyleBackColor = false;
            this.btnStartCompression.Click += new System.EventHandler(this.btnStartCompression_Click);
            // 
            // btnBrowseCompressionOutput
            // 
            this.btnBrowseCompressionOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCompressionOutput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            this.btnBrowseCompressionOutput.FlatAppearance.BorderSize = 0;
            this.btnBrowseCompressionOutput.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseCompressionOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnBrowseCompressionOutput.Location = new System.Drawing.Point(543, 275);
            this.btnBrowseCompressionOutput.Name = "btnBrowseCompressionOutput";
            this.btnBrowseCompressionOutput.Size = new System.Drawing.Size(100, 32);
            this.btnBrowseCompressionOutput.TabIndex = 11;
            this.btnBrowseCompressionOutput.Text = "Browse...";
            this.btnBrowseCompressionOutput.UseVisualStyleBackColor = false;
            this.btnBrowseCompressionOutput.Click += new System.EventHandler(this.btnBrowseCompressionOutput_Click);
            // 
            // txtCompressionOutput
            // 
            this.txtCompressionOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCompressionOutput.Location = new System.Drawing.Point(24, 280);
            this.txtCompressionOutput.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtCompressionOutput.Name = "txtCompressionOutput";
            this.txtCompressionOutput.Size = new System.Drawing.Size(512, 25);
            this.txtCompressionOutput.TabIndex = 10;
            // 
            // lblCompressionOutput
            // 
            this.lblCompressionOutput.AutoSize = true;
            this.lblCompressionOutput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompressionOutput.Location = new System.Drawing.Point(24, 258);
            this.lblCompressionOutput.Name = "lblCompressionOutput";
            this.lblCompressionOutput.Size = new System.Drawing.Size(140, 19);
            this.lblCompressionOutput.TabIndex = 9;
            this.lblCompressionOutput.Text = "Destination directory:";
            // 
            // chkCompressionShowPassword
            // 
            this.chkCompressionShowPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkCompressionShowPassword.AutoSize = true;
            this.chkCompressionShowPassword.Location = new System.Drawing.Point(548, 207);
            this.chkCompressionShowPassword.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.chkCompressionShowPassword.Name = "chkCompressionShowPassword";
            this.chkCompressionShowPassword.Size = new System.Drawing.Size(61, 23);
            this.chkCompressionShowPassword.TabIndex = 8;
            this.chkCompressionShowPassword.Text = "Show";
            this.chkCompressionShowPassword.UseVisualStyleBackColor = true;
            this.chkCompressionShowPassword.CheckedChanged += new System.EventHandler(this.chkCompressionShowPassword_CheckedChanged);
            // 
            // txtCompressionPassword
            // 
            this.txtCompressionPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCompressionPassword.Location = new System.Drawing.Point(24, 204);
            this.txtCompressionPassword.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtCompressionPassword.Name = "txtCompressionPassword";
            this.txtCompressionPassword.PasswordChar = '*';
            this.txtCompressionPassword.Size = new System.Drawing.Size(512, 25);
            this.txtCompressionPassword.TabIndex = 7;
            // 
            // lblCompressionPassword
            // 
            this.lblCompressionPassword.AutoSize = true;
            this.lblCompressionPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompressionPassword.Location = new System.Drawing.Point(24, 182);
            this.lblCompressionPassword.Name = "lblCompressionPassword";
            this.lblCompressionPassword.Size = new System.Drawing.Size(132, 19);
            this.lblCompressionPassword.TabIndex = 6;
            this.lblCompressionPassword.Text = "Password (optional):";
            // 
            // cmbCompressionAlgorithm
            // 
            this.cmbCompressionAlgorithm.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCompressionAlgorithm.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCompressionAlgorithm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbCompressionAlgorithm.FormattingEnabled = true;
            this.cmbCompressionAlgorithm.Location = new System.Drawing.Point(24, 126);
            this.cmbCompressionAlgorithm.Name = "cmbCompressionAlgorithm";
            this.cmbCompressionAlgorithm.Size = new System.Drawing.Size(512, 25);
            this.cmbCompressionAlgorithm.TabIndex = 5;
            // 
            // lblCompressionAlgorithm
            // 
            this.lblCompressionAlgorithm.AutoSize = true;
            this.lblCompressionAlgorithm.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompressionAlgorithm.Location = new System.Drawing.Point(24, 104);
            this.lblCompressionAlgorithm.Name = "lblCompressionAlgorithm";
            this.lblCompressionAlgorithm.Size = new System.Drawing.Size(145, 19);
            this.lblCompressionAlgorithm.TabIndex = 4;
            this.lblCompressionAlgorithm.Text = "Compression Method:";
            // 
            // btnBrowseCompressionFolder
            // 
            this.btnBrowseCompressionFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCompressionFolder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            this.btnBrowseCompressionFolder.FlatAppearance.BorderSize = 0;
            this.btnBrowseCompressionFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseCompressionFolder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnBrowseCompressionFolder.Location = new System.Drawing.Point(543, 49);
            this.btnBrowseCompressionFolder.Name = "btnBrowseCompressionFolder";
            this.btnBrowseCompressionFolder.Size = new System.Drawing.Size(100, 32);
            this.btnBrowseCompressionFolder.TabIndex = 3;
            this.btnBrowseCompressionFolder.Text = "Browse folder";
            this.btnBrowseCompressionFolder.UseVisualStyleBackColor = false;
            this.btnBrowseCompressionFolder.Click += new System.EventHandler(this.btnBrowseCompressionFolder_Click);
            // 
            // btnBrowseCompressionFile
            // 
            this.btnBrowseCompressionFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCompressionFile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            this.btnBrowseCompressionFile.FlatAppearance.BorderSize = 0;
            this.btnBrowseCompressionFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseCompressionFile.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnBrowseCompressionFile.Location = new System.Drawing.Point(436, 49);
            this.btnBrowseCompressionFile.Name = "btnBrowseCompressionFile";
            this.btnBrowseCompressionFile.Size = new System.Drawing.Size(100, 32);
            this.btnBrowseCompressionFile.TabIndex = 2;
            this.btnBrowseCompressionFile.Text = "Browse file";
            this.btnBrowseCompressionFile.UseVisualStyleBackColor = false;
            this.btnBrowseCompressionFile.Click += new System.EventHandler(this.btnBrowseCompressionFile_Click);
            // 
            // txtCompressionInput
            // 
            this.txtCompressionInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCompressionInput.Location = new System.Drawing.Point(24, 54);
            this.txtCompressionInput.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtCompressionInput.Name = "txtCompressionInput";
            this.txtCompressionInput.Size = new System.Drawing.Size(404, 25);
            this.txtCompressionInput.TabIndex = 1;
            // 
            // lblCompressionInput
            // 
            this.lblCompressionInput.AutoSize = true;
            this.lblCompressionInput.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblCompressionInput.Location = new System.Drawing.Point(24, 32);
            this.lblCompressionInput.Name = "lblCompressionInput";
            this.lblCompressionInput.Size = new System.Drawing.Size(89, 19);
            this.lblCompressionInput.TabIndex = 0;
            this.lblCompressionInput.Text = "Input source:";
            // 
            // tabDecompression
            // 
            this.tabDecompression.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(247)))), ((int)(((byte)(249)))), ((int)(((byte)(252)))));
            this.tabDecompression.Controls.Add(this.btnStartDecompression);
            this.tabDecompression.Controls.Add(this.chkDecompressionShowPassword);
            this.tabDecompression.Controls.Add(this.txtDecompressionPassword);
            this.tabDecompression.Controls.Add(this.lblDecompressionPassword);
            this.tabDecompression.Controls.Add(this.btnBrowseDecompressionDestination);
            this.tabDecompression.Controls.Add(this.txtDecompressionDestination);
            this.tabDecompression.Controls.Add(this.lblDecompressionDestination);
            this.tabDecompression.Controls.Add(this.btnBrowseDecompressionArchive);
            this.tabDecompression.Controls.Add(this.txtDecompressionArchive);
            this.tabDecompression.Controls.Add(this.lblDecompressionArchive);
            this.tabDecompression.Location = new System.Drawing.Point(4, 26);
            this.tabDecompression.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.tabDecompression.Name = "tabDecompression";
            this.tabDecompression.Padding = new System.Windows.Forms.Padding(18);
            this.tabDecompression.Size = new System.Drawing.Size(672, 363);
            this.tabDecompression.TabIndex = 1;
            this.tabDecompression.Text = "Decompression";
            // 
            // btnStartDecompression
            // 
            this.btnStartDecompression.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnStartDecompression.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnStartDecompression.FlatAppearance.BorderSize = 0;
            this.btnStartDecompression.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(110)))), ((int)(((byte)(195)))));
            this.btnStartDecompression.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(119)))), ((int)(((byte)(205)))));
            this.btnStartDecompression.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartDecompression.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnStartDecompression.ForeColor = System.Drawing.Color.White;
            this.btnStartDecompression.Location = new System.Drawing.Point(254, 320);
            this.btnStartDecompression.Name = "btnStartDecompression";
            this.btnStartDecompression.Size = new System.Drawing.Size(180, 38);
            this.btnStartDecompression.TabIndex = 9;
            this.btnStartDecompression.Text = "Decompress";
            this.btnStartDecompression.UseVisualStyleBackColor = false;
            this.btnStartDecompression.Click += new System.EventHandler(this.btnStartDecompression_Click);
            // 
            // chkDecompressionShowPassword
            // 
            this.chkDecompressionShowPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDecompressionShowPassword.AutoSize = true;
            this.chkDecompressionShowPassword.Location = new System.Drawing.Point(564, 202);
            this.chkDecompressionShowPassword.Name = "chkDecompressionShowPassword";
            this.chkDecompressionShowPassword.Size = new System.Drawing.Size(61, 23);
            this.chkDecompressionShowPassword.TabIndex = 8;
            this.chkDecompressionShowPassword.Text = "Show";
            this.chkDecompressionShowPassword.UseVisualStyleBackColor = true;
            this.chkDecompressionShowPassword.CheckedChanged += new System.EventHandler(this.chkDecompressionShowPassword_CheckedChanged);
            // 
            // txtDecompressionPassword
            // 
            this.txtDecompressionPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDecompressionPassword.Location = new System.Drawing.Point(24, 198);
            this.txtDecompressionPassword.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtDecompressionPassword.Name = "txtDecompressionPassword";
            this.txtDecompressionPassword.PasswordChar = '*';
            this.txtDecompressionPassword.Size = new System.Drawing.Size(528, 25);
            this.txtDecompressionPassword.TabIndex = 7;
            // 
            // lblDecompressionPassword
            // 
            this.lblDecompressionPassword.AutoSize = true;
            this.lblDecompressionPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblDecompressionPassword.Location = new System.Drawing.Point(24, 176);
            this.lblDecompressionPassword.Name = "lblDecompressionPassword";
            this.lblDecompressionPassword.Size = new System.Drawing.Size(132, 19);
            this.lblDecompressionPassword.TabIndex = 6;
            this.lblDecompressionPassword.Text = "Password (optional):";
            // 
            // btnBrowseDecompressionDestination
            // 
            this.btnBrowseDecompressionDestination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseDecompressionDestination.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            this.btnBrowseDecompressionDestination.FlatAppearance.BorderSize = 0;
            this.btnBrowseDecompressionDestination.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseDecompressionDestination.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnBrowseDecompressionDestination.Location = new System.Drawing.Point(564, 132);
            this.btnBrowseDecompressionDestination.Name = "btnBrowseDecompressionDestination";
            this.btnBrowseDecompressionDestination.Size = new System.Drawing.Size(100, 32);
            this.btnBrowseDecompressionDestination.TabIndex = 5;
            this.btnBrowseDecompressionDestination.Text = "Browse...";
            this.btnBrowseDecompressionDestination.UseVisualStyleBackColor = false;
            this.btnBrowseDecompressionDestination.Click += new System.EventHandler(this.btnBrowseDecompressionDestination_Click);
            // 
            // txtDecompressionDestination
            // 
            this.txtDecompressionDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDecompressionDestination.Location = new System.Drawing.Point(24, 134);
            this.txtDecompressionDestination.Name = "txtDecompressionDestination";
            this.txtDecompressionDestination.Size = new System.Drawing.Size(528, 25);
            this.txtDecompressionDestination.TabIndex = 4;
            // 
            // lblDecompressionDestination
            // 
            this.lblDecompressionDestination.AutoSize = true;
            this.lblDecompressionDestination.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblDecompressionDestination.Location = new System.Drawing.Point(24, 112);
            this.lblDecompressionDestination.Name = "lblDecompressionDestination";
            this.lblDecompressionDestination.Size = new System.Drawing.Size(140, 19);
            this.lblDecompressionDestination.TabIndex = 3;
            this.lblDecompressionDestination.Text = "Destination directory:";
            // 
            // btnBrowseDecompressionArchive
            // 
            this.btnBrowseDecompressionArchive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseDecompressionArchive.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(240)))), ((int)(((byte)(254)))));
            this.btnBrowseDecompressionArchive.FlatAppearance.BorderSize = 0;
            this.btnBrowseDecompressionArchive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseDecompressionArchive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(136)))), ((int)(((byte)(229)))));
            this.btnBrowseDecompressionArchive.Location = new System.Drawing.Point(564, 52);
            this.btnBrowseDecompressionArchive.Name = "btnBrowseDecompressionArchive";
            this.btnBrowseDecompressionArchive.Size = new System.Drawing.Size(100, 32);
            this.btnBrowseDecompressionArchive.TabIndex = 2;
            this.btnBrowseDecompressionArchive.Text = "Browse...";
            this.btnBrowseDecompressionArchive.UseVisualStyleBackColor = false;
            this.btnBrowseDecompressionArchive.Click += new System.EventHandler(this.btnBrowseDecompressionArchive_Click);
            // 
            // txtDecompressionArchive
            // 
            this.txtDecompressionArchive.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDecompressionArchive.Location = new System.Drawing.Point(24, 54);
            this.txtDecompressionArchive.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtDecompressionArchive.Name = "txtDecompressionArchive";
            this.txtDecompressionArchive.Size = new System.Drawing.Size(528, 25);
            this.txtDecompressionArchive.TabIndex = 1;
            // 
            // lblDecompressionArchive
            // 
            this.lblDecompressionArchive.AutoSize = true;
            this.lblDecompressionArchive.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblDecompressionArchive.Location = new System.Drawing.Point(24, 32);
            this.lblDecompressionArchive.Name = "lblDecompressionArchive";
            this.lblDecompressionArchive.Size = new System.Drawing.Size(57, 19);
            this.lblDecompressionArchive.TabIndex = 0;
            this.lblDecompressionArchive.Text = "Archive:";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 417);
            this.progressBar.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(680, 18);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(11, 438);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.lblStatus.Size = new System.Drawing.Size(680, 30);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Title = "Select file";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "spca";
            this.saveFileDialog.Filter = "Senior Project Archive (*.spca)|*.spca|All files (*.*)|*.*";
            this.saveFileDialog.Title = "Save compressed archive";
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Select folder";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(704, 481);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.tabControlMain);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.MinimumSize = new System.Drawing.Size(720, 520);
            this.Name = "Form1";
            this.ShowIcon = false;
            this.Text = "ByteSqueeze";
            this.tabControlMain.ResumeLayout(false);
            this.tabCompression.ResumeLayout(false);
            this.tabCompression.PerformLayout();
            this.tabDecompression.ResumeLayout(false);
            this.tabDecompression.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabCompression;
        private System.Windows.Forms.TabPage tabDecompression;
        private System.Windows.Forms.Label lblCompressionInput;
        private System.Windows.Forms.TextBox txtCompressionInput;
        private System.Windows.Forms.Button btnBrowseCompressionFile;
        private System.Windows.Forms.Button btnBrowseCompressionFolder;
        private System.Windows.Forms.Label lblCompressionAlgorithm;
        private System.Windows.Forms.ComboBox cmbCompressionAlgorithm;
        private System.Windows.Forms.Label lblCompressionPassword;
        private System.Windows.Forms.TextBox txtCompressionPassword;
        private System.Windows.Forms.CheckBox chkCompressionShowPassword;
        private System.Windows.Forms.Label lblCompressionOutput;
        private System.Windows.Forms.TextBox txtCompressionOutput;
        private System.Windows.Forms.Button btnBrowseCompressionOutput;
        private System.Windows.Forms.Button btnStartCompression;
        private System.Windows.Forms.Label lblDecompressionArchive;
        private System.Windows.Forms.TextBox txtDecompressionArchive;
        private System.Windows.Forms.Button btnBrowseDecompressionArchive;
        private System.Windows.Forms.Label lblDecompressionDestination;
        private System.Windows.Forms.TextBox txtDecompressionDestination;
        private System.Windows.Forms.Button btnBrowseDecompressionDestination;
        private System.Windows.Forms.Label lblDecompressionPassword;
        private System.Windows.Forms.TextBox txtDecompressionPassword;
        private System.Windows.Forms.CheckBox chkDecompressionShowPassword;
        private System.Windows.Forms.Button btnStartDecompression;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    }
}


