using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;

namespace AoOniOnlineAssetBundleDecrypt
{
    public partial class MainForm : Form
    {
        private TextBox txtInput;
        private TextBox txtOutput;
        private Button btnBrowseInput;
        private Button btnBrowseOutput;
        private Button btnStart;
        private ProgressBar progressBar;
        private Label lblStatus;
        private CheckBox chkOverwrite;

        public MainForm()
        {
            this.Text = "Ao Oni Online Asset Decrypt Tool";
            this.Size = new Size(680, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Enable Drag & Drop on the Form
            this.AllowDrop = true;
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            // Input Folder
            Label lblInput = new Label { Text = "Input Folder:", Location = new Point(20, 25), AutoSize = true };
            txtInput = new TextBox { Location = new Point(120, 22), Width = 420, AllowDrop = true };
            txtInput.DragEnter += Control_DragEnter;
            txtInput.DragDrop += (s, e) => HandleDropToTextBox(txtInput, e);

            btnBrowseInput = new Button { Text = "Browse...", Location = new Point(550, 20), Width = 80 };
            btnBrowseInput.Click += (s, e) => BrowseFolder(txtInput);

            // Output Folder
            Label lblOutput = new Label { Text = "Output Folder:", Location = new Point(20, 65), AutoSize = true };
            txtOutput = new TextBox { Location = new Point(120, 62), Width = 420, AllowDrop = true };
            txtOutput.DragEnter += Control_DragEnter;
            txtOutput.DragDrop += (s, e) => HandleDropToTextBox(txtOutput, e);

            btnBrowseOutput = new Button { Text = "Browse...", Location = new Point(550, 60), Width = 80 };
            btnBrowseOutput.Click += (s, e) => BrowseFolder(txtOutput);

            // Options
            chkOverwrite = new CheckBox
            {
                Text = "Overwrite existing files",
                Location = new Point(120, 100),
                AutoSize = true,
                Checked = true
            };

            // Start Button
            btnStart = new Button
            {
                Text = "Start Decryption",
                Location = new Point(250, 140),
                Size = new Size(150, 35),
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 190),
                Width = 610,
                Height = 22,
                Style = ProgressBarStyle.Continuous
            };

            // Status Label
            lblStatus = new Label
            {
                Text = "Ready (Drag and drop folders here)",
                Location = new Point(20, 220),
                AutoSize = true
            };

            this.Controls.AddRange(new Control[]
            {
                lblInput, txtInput, btnBrowseInput,
                lblOutput, txtOutput, btnBrowseOutput,
                chkOverwrite, btnStart, progressBar, lblStatus
            });
        }

        #region Drag and Drop Logic

        private void MainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                string droppedPath = files[0];
                string folderPath = Directory.Exists(droppedPath) ? droppedPath : (Path.GetDirectoryName(droppedPath) ?? droppedPath);

                if (string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    txtInput.Text = folderPath;
                }
                else
                {
                    txtOutput.Text = folderPath;
                }
            }
        }

        private void Control_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void HandleDropToTextBox(TextBox target, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                string droppedPath = files[0];
                target.Text = Directory.Exists(droppedPath) ? droppedPath : (Path.GetDirectoryName(droppedPath) ?? droppedPath);
            }
        }

        #endregion

        private void BrowseFolder(TextBox target)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Folder";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            string inputDir = txtInput.Text.Trim();
            string outputDir = txtOutput.Text.Trim();

            if (string.IsNullOrEmpty(inputDir) || !Directory.Exists(inputDir))
            {
                MessageBox.Show("Please select a valid input folder!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(outputDir))
            {
                MessageBox.Show("Please select an output folder!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            btnBrowseInput.Enabled = false;
            btnBrowseOutput.Enabled = false;
            progressBar.Value = 0;
            lblStatus.Text = "Scanning files...";

            try
            {
                string[] files = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);
                int total = files.Length;
                int processed = 0;
                int success = 0;
                int skipped = 0;
                int failed = 0;

                progressBar.Maximum = Math.Max(total, 1);

                await Task.Run(() =>
                {
                    foreach (string filePath in files)
                    {
                        try
                        {
                            string fileName = Path.GetFileName(filePath);
                            string relativePath = Path.GetRelativePath(inputDir, filePath);
                            string outputPath = Path.Combine(outputDir, relativePath);
                            string? outDir = Path.GetDirectoryName(outputPath);

                            if (!string.IsNullOrEmpty(outDir))
                            {
                                Directory.CreateDirectory(outDir);
                            }

                            // Skip if file already exists and overwrite is unticked
                            if (File.Exists(outputPath) && !chkOverwrite.Checked)
                            {
                                skipped++;
                            }
                            // If the filename contains "GRMap", directly copy it without decrypting
                            else if (fileName.IndexOf("GRMap", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                File.Copy(filePath, outputPath, overwrite: true);
                                success++;
                            }
                            else
                            {
                                byte[] data = File.ReadAllBytes(filePath);
                                if (data.Length > 0)
                                {
                                    // Dynamic Key calculation = first byte XOR 0x55 (for 'U' in UnityFS)
                                    byte key = (byte)(data[0] ^ 0x55);

                                    // XOR decrypt only the first 0x30 bytes
                                    int xorCount = Math.Min(0x30, data.Length);
                                    for (int i = 0; i < xorCount; i++)
                                    {
                                        data[i] ^= key;
                                    }
                                }

                                File.WriteAllBytes(outputPath, data);
                                success++;
                            }
                        }
                        catch
                        {
                            failed++;
                        }

                        processed++;
                        UpdateProgress(processed, total, success, skipped, failed);
                    }
                });

                lblStatus.Text = $"Completed! Total: {total}, Success: {success}, Skipped: {skipped}, Failed: {failed}";
                MessageBox.Show($"Processing finished!\n\nTotal files: {total}\nSuccess: {success}\nSkipped: {skipped}\nFailed: {failed}",
                    "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "An error occurred";
            }
            finally
            {
                btnStart.Enabled = true;
                btnBrowseInput.Enabled = true;
                btnBrowseOutput.Enabled = true;
            }
        }

        private void UpdateProgress(int processed, int total, int success, int skipped, int failed)
        {
            this.Invoke(() =>
            {
                progressBar.Value = Math.Min(processed, progressBar.Maximum);
                lblStatus.Text = $"Processing... {processed}/{total} (Success: {success}, Skipped: {skipped}, Failed: {failed})";
            });
        }
    }
}