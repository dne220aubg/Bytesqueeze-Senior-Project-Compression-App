using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using SeniorProjectCompressionApp.Compression;
using SeniorProjectCompressionApp.Compression.Algorithms;
using SeniorProjectCompressionApp.IO;
using SeniorProjectCompressionApp.Security;
using SeniorProjectCompressionApp.Models;
using SeniorProjectCompressionApp.Services;

namespace SeniorProjectCompressionApp
{
    // Windows Forms UI that drives compression and decompression workflows.
    public partial class Form1 : Form
    {
        private readonly ICompressionAlgorithmRegistry _registry;
        private readonly IFileSystemService _fileSystem;
        private readonly ICompressionOrchestrator _orchestrator;
        private string _currentOperation = "Ready";

        // Initializes the form, dependency graph, and UI controls.
        public Form1()
        {
            InitializeComponent();
            _fileSystem = new FileSystemService();

            ICompressionAlgorithm[] algorithms = new ICompressionAlgorithm[]
            {
                new DeflateRfc1951Algorithm(CompressionLevel.Fast),
                new DeflateRfc1951Algorithm(CompressionLevel.Normal),
                new DeflateRfc1951Algorithm(CompressionLevel.Best)
            };

            _registry = new CompressionAlgorithmRegistry(algorithms);

            IEncryptionService encryptionService = new AesEncryptionService();

            _orchestrator = new CompressionOrchestrator(_registry, _fileSystem, encryptionService);

            InitializeUi();
        
            this.Click += Background_Click;
            tabCompression.Click += Background_Click;
            tabDecompression.Click += Background_Click;
            
            // Prevent tab switching during operations
            tabControlMain.Selecting += TabControlMain_Selecting;
        }

        private void TabControlMain_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (_isOperationRunning)
            {
                e.Cancel = true;
            }
        }

        private void Background_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

        // Populates combo boxes, resets progress state, and displays a ready status.
        private void InitializeUi()
        {
            cmbCompressionAlgorithm.Items.Clear();
            foreach (ICompressionAlgorithm algorithm in _registry.GetAlgorithms())
            {
                cmbCompressionAlgorithm.Items.Add(algorithm.Name);
            }

            if (cmbCompressionAlgorithm.Items.Count > 0)
            // Automatically select "Normal" compression algorithm when the app starts
            {
                int normalIndex = cmbCompressionAlgorithm.Items.IndexOf("Normal");
                if (normalIndex >= 0)
                {
                    cmbCompressionAlgorithm.SelectedIndex = normalIndex;
                }
                else
                {
                    cmbCompressionAlgorithm.SelectedIndex = 0;
                }
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            lblStatus.Text = "Ready";
        }

        // Allows the user to choose a single file for compression.
        private void btnBrowseCompressionFile_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtCompressionInput.Text = openFileDialog.FileName;
                EnsureCompressionOutputPath();
            }
        }

        // Allows the user to choose a folder for compression.
        private void btnBrowseCompressionFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtCompressionInput.Text = folderBrowserDialog.SelectedPath;
                EnsureCompressionOutputPath();
            }
        }

        // Chooses where the compressed archive should be saved.
        private void btnBrowseCompressionOutput_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtCompressionInput.Text))
            {
                string defaultName = GetSuggestedArchiveName(txtCompressionInput.Text.Trim());
                saveFileDialog.FileName = defaultName + CompressionOrchestrator.DefaultArchiveExtension;
            }

            if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtCompressionOutput.Text = saveFileDialog.FileName;
            }
        }

        // Allows the user to choose an archive for decompression.
        private void btnBrowseDecompressionArchive_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "Senior Project Archive (*.spca)|*.spca|All files (*.*)|*.*";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtDecompressionArchive.Text = openFileDialog.FileName;
                EnsureDecompressionDestination();
            }
        }

        // Allows the user to select the decompression destination folder.
        private void btnBrowseDecompressionDestination_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                txtDecompressionDestination.Text = folderBrowserDialog.SelectedPath;
            }
        }

        // Validates inputs and begins the asynchronous compression workflow.
        private async void btnStartCompression_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                return;
            }

            // Guard against double-clicking or cross-thread activation
            if (!btnStartCompression.Enabled) return;

            string inputPath = txtCompressionInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                MessageBox.Show(this, "Please select a file or folder to compress.", "Missing input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                MessageBox.Show(this, "The selected input path does not exist.", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? algorithmName = cmbCompressionAlgorithm.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(algorithmName))
            {
                MessageBox.Show(this, "Please select a compression algorithm.", "Missing algorithm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? password;
            if (string.IsNullOrWhiteSpace(txtCompressionPassword.Text))
            {
                password = null;
            }
            else
            {
                password = txtCompressionPassword.Text;
            }

            string? outputPath;
            if (string.IsNullOrWhiteSpace(txtCompressionOutput.Text))
            {
                outputPath = null;
            }
            else
            {
                outputPath = txtCompressionOutput.Text.Trim();
            }

            await ExecuteOperationAsync(
                (token) => _orchestrator.CompressAsync(
                    inputPath,
                    algorithmName!,
                    password,
                    outputPath,
                    new Progress<double>(UpdateProgress),
                    token),
                "Compressing...",
                summary =>
                {
                    txtCompressionOutput.Text = summary.OutputPath;
                    return $"Compression completed: {summary.OutputPath}";
                },
                "Compression failed.",
                ShowCompressionSummary,
                isCompression: true);
        }

        // Validates inputs and begins the asynchronous decompression workflow.
        private async void btnStartDecompression_Click(object sender, EventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                return;
            }

            // Guard against double-clicking or cross-thread activation
            if (!btnStartDecompression.Enabled) return;

            string archivePath = txtDecompressionArchive.Text.Trim();
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                MessageBox.Show(this, "Please select a valid archive file to decompress.", "Missing archive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string destination = txtDecompressionDestination.Text.Trim();
            if (string.IsNullOrWhiteSpace(destination))
            {
                EnsureDecompressionDestination();
                destination = txtDecompressionDestination.Text.Trim();
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                MessageBox.Show(this, "Please specify a destination folder.", "Missing destination", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? password;
            if (string.IsNullOrWhiteSpace(txtDecompressionPassword.Text))
            {
                password = null;
            }
            else
            {
                password = txtDecompressionPassword.Text;
            }

            await ExecuteOperationAsync(
                (token) => _orchestrator.DecompressAsync(
                    archivePath,
                    destination,
                    password,
                    new Progress<double>(UpdateProgress),
                    token),
                "Decompressing...",
                summary =>
                {
                    txtDecompressionDestination.Text = summary.DestinationPath;
                    return $"Decompression completed: {summary.DestinationPath}";
                },
                "Decompression failed.",
                ShowDecompressionSummary,
                isCompression: false);
        }

        // Toggles password visibility for the compression tab.
        private void chkCompressionShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (chkCompressionShowPassword.Checked)
            {
                txtCompressionPassword.PasswordChar = '\0';
            }
            else
            {
                txtCompressionPassword.PasswordChar = '●';
            }
        }

        // Toggles password visibility for the decompression tab.
        private void chkDecompressionShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDecompressionShowPassword.Checked)
            {
                txtDecompressionPassword.PasswordChar = '\0';
            }
            else
            {
                txtDecompressionPassword.PasswordChar = '●';
            }
        }

        private CancellationTokenSource? _cancellationTokenSource;

        // Executes a long-running operation while updating the UI with progress and status text.
        private async Task ExecuteOperationAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            string startMessage,
            Func<T, string> successMessageFactory,
            string failureMessage,
            Action<T>? afterSuccess = null,
            bool isCompression = true)
        {
            // Setup Cancellation
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                SetUiState(true, isCompression);
                _currentOperation = startMessage.TrimEnd('.');
                UpdateProgress(0);
                lblStatus.Text = startMessage;

                T result = await operation(token).ConfigureAwait(true);

                UpdateProgress(1.0);
                lblStatus.Text = successMessageFactory(result);
                afterSuccess?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                UpdateProgress(0); // Reset progress FIRST
                lblStatus.Text = "Operation cancelled."; // THEN set text so it sticks
                MessageBox.Show(this, "The operation was cancelled by the user.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = failureMessage;
                MessageBox.Show(this, ex.Message, failureMessage, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                _currentOperation = "Ready";
                SetUiState(false, isCompression);
            }
        }

        // Color Constants
        private static readonly System.Drawing.Color BlueNormal = System.Drawing.Color.FromArgb(30, 136, 229);
        private static readonly System.Drawing.Color BlueHover = System.Drawing.Color.FromArgb(24, 119, 205);
        private static readonly System.Drawing.Color BlueDown = System.Drawing.Color.FromArgb(13, 110, 195);

        private static readonly System.Drawing.Color RedNormal = System.Drawing.Color.Red;
        private static readonly System.Drawing.Color RedHover = System.Drawing.Color.FromArgb(200, 0, 0);
        private static readonly System.Drawing.Color RedDown = System.Drawing.Color.FromArgb(139, 0, 0);

        private bool _isOperationRunning = false;

        // Toggles UI state between "Running" and "Ready".
        private void SetUiState(bool isRunning, bool isCompression)
        {
            _isOperationRunning = isRunning;

            // Always enable the progress bar
            progressBar.Enabled = true;

            tabControlMain.Enabled = true; // Always enabled to allow interaction with active tab

            if (isRunning)
            {
                if (isCompression)
                {
                    // Disable Decompression Tab Controls
                    ToggleControls(tabDecompression, false);
                    btnStartDecompression.Enabled = false; // Explicitly disable
                    
                    // Disable Compression Tab Controls EXCEPT the Start/Cancel button
                    ToggleControls(tabCompression, false, btnStartCompression);
                    
                    // Style the button as Cancel
                    StyleButton(btnStartCompression, "Cancel", RedNormal, RedHover, RedDown);
                    btnStartCompression.Enabled = true;
                }
                else
                {
                    // Disable Compression Tab Controls
                    ToggleControls(tabCompression, false);
                    btnStartCompression.Enabled = false; // Explicitly disable

                    // Disable Decompression Tab Controls EXCEPT the Start/Cancel button
                    ToggleControls(tabDecompression, false, btnStartDecompression);

                    // Style the button as Cancel
                    StyleButton(btnStartDecompression, "Cancel", RedNormal, RedHover, RedDown);
                    btnStartDecompression.Enabled = true;
                }
            }
            else
            {
                // Re-enable everything
                ToggleControls(tabCompression, true);
                ToggleControls(tabDecompression, true);

                // Reset Buttons
                StyleButton(btnStartCompression, "Compress", BlueNormal, BlueHover, BlueDown);
                btnStartCompression.Enabled = true;

                StyleButton(btnStartDecompression, "Decompress", BlueNormal, BlueHover, BlueDown);
                btnStartDecompression.Enabled = true;
            }
        }

        private void StyleButton(Button btn, string text, System.Drawing.Color normal, System.Drawing.Color hover, System.Drawing.Color down)
        {
            btn.Text = text;
            btn.BackColor = normal;
            btn.ForeColor = System.Drawing.Color.White;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = down;
            btn.UseVisualStyleBackColor = false;
        }

        private void ToggleControls(Control parent, bool enabled, Control? excludedControl = null)
        {
            foreach (Control c in parent.Controls)
            {
                if (c == excludedControl) continue;
                c.Enabled = enabled;
            }
        }

        // Displays a dialog summarizing the results of a compression run.
        private void ShowCompressionSummary(CompressionSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            string encryptionText;
            if (summary.WasEncrypted)
            {
                encryptionText = "Yes";
            }
            else
            {
                encryptionText = "No";
            }

            string message =
                "Compression completed successfully." + Environment.NewLine + Environment.NewLine +
                $"Algorithm: {summary.AlgorithmName}" + Environment.NewLine +
                $"Encrypted: {encryptionText}" + Environment.NewLine +
                $"Files Compressed: {summary.CompressedFileCount}" + Environment.NewLine +
                $"Original Size: {FormatBytes(summary.OriginalBytes)}" + Environment.NewLine +
                $"Archive Size: {FormatBytes(summary.ArchiveBytes)}" + Environment.NewLine +
                $"Compression Ratio: {FormatRatio(summary.CompressionRatio)}" + Environment.NewLine +
                $"Algorithm Time: {summary.ElapsedMilliseconds} ms";

            MessageBox.Show(this, message, "Compression Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Displays a dialog summarizing the results of a decompression run.
        private void ShowDecompressionSummary(DecompressionSummary summary)
        {
            if (summary == null)
            {
                return;
            }

            string encryptionStatus;
            if (summary.WasEncrypted)
            {
                encryptionStatus = "Yes";
            }
            else
            {
                encryptionStatus = "No";
            }

            string message =
                "Decompression completed successfully." + Environment.NewLine + Environment.NewLine +
                $"Algorithm: {summary.AlgorithmName}" + Environment.NewLine +
                $"Encrypted: {encryptionStatus}" + Environment.NewLine +
                $"Files Restored: {summary.RestoredFileCount}" + Environment.NewLine +
                $"Archive Size: {FormatBytes(summary.ArchiveBytes)}" + Environment.NewLine +
                $"Restored Size: {FormatBytes(summary.RestoredBytes)}" + Environment.NewLine +
                $"Expansion Ratio: {FormatRatio(summary.ExpansionRatio)}" + Environment.NewLine +
                $"Algorithm Time: {summary.ElapsedMilliseconds} ms";

            MessageBox.Show(this, message, "Decompression Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Converts a byte count into a human-readable string.
        private static string FormatBytes(long bytes)
        {
            if (bytes < 0)
            {
                bytes = 0;
            }

            string[] units = { "bytes", "KB", "MB", "GB", "TB", "PB" };
            double value = bytes;
            int unitIndex = 0;

            while (unitIndex < units.Length - 1 && value >= 1024)
            {
                value /= 1024;
                unitIndex++;
            }

            if (unitIndex == 0)
            {
                return $"{bytes} {units[unitIndex]}";
            }

            return $"{value:0.##} {units[unitIndex]}";
        }

        // Formats a ratio as a percentage with a single decimal place.
        private static string FormatRatio(double ratio)
        {
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
            {
                return "N/A";
            }

            if (ratio < 0)
            {
                ratio = 0;
            }

            return ratio.ToString("P1", CultureInfo.CurrentCulture);
        }

        // Updates the progress bar and status label with the supplied progress value.
        private void UpdateProgress(double value)
        {
            int percent = (int)Math.Round(value * 100);
            percent = Math.Min(progressBar.Maximum, Math.Max(progressBar.Minimum, percent));
            progressBar.Value = percent;
            lblStatus.Text = $"{_currentOperation} ({percent}%)";
        }



        // Suggests a default archive path based on the current compression input.
        private void EnsureCompressionOutputPath()
        {
            if (string.IsNullOrWhiteSpace(txtCompressionInput.Text))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtCompressionOutput.Text))
            {
                return;
            }

            try
            {
                txtCompressionOutput.Text = _fileSystem.GetSafeOutputPath(
                    txtCompressionInput.Text.Trim(),
                    CompressionOrchestrator.DefaultArchiveExtension);
            }
            catch
            {
                // Ignore failures when generating default output path.
            }
        }

        // Suggests a default extraction directory relative to the selected archive.
        private void EnsureDecompressionDestination()
        {
            if (string.IsNullOrWhiteSpace(txtDecompressionArchive.Text))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtDecompressionDestination.Text))
            {
                return;
            }

            try
            {
                string archiveDirectory = Path.GetDirectoryName(txtDecompressionArchive.Text.Trim()) ?? string.Empty;
                if (!string.IsNullOrEmpty(archiveDirectory))
                {
                    txtDecompressionDestination.Text = archiveDirectory;
                }
            }
            catch
            {
                // Ignore failures when suggesting destination.
            }
        }

        // Chooses a readable archive name based on the input file or directory.
        private static string GetSuggestedArchiveName(string path)
        {
            if (Directory.Exists(path))
            {
                return new DirectoryInfo(path).Name;
            }

            if (File.Exists(path))
            {
                return Path.GetFileNameWithoutExtension(path);
            }

            return "archive";
        }
    }
}






