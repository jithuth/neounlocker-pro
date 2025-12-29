namespace NEOUnlocker.Client.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            
            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 750);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.Text = "NEOUnlocker Pro - Router Unlock Tool";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            
            // Initialize all controls
            InitializeControls();
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // Port Selection Controls
        private System.Windows.Forms.GroupBox grpPortSelection;
        private System.Windows.Forms.ComboBox cmbPorts;
        private System.Windows.Forms.Button btnScanPorts;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblPortStatus;

        // Step 1 Controls
        private System.Windows.Forms.GroupBox grpStep1;
        private System.Windows.Forms.TextBox txtManufacturer;
        private System.Windows.Forms.TextBox txtModel;
        private System.Windows.Forms.TextBox txtIMEI;
        private System.Windows.Forms.TextBox txtFirmware;
        private System.Windows.Forms.TextBox txtLockStatus;
        private System.Windows.Forms.Button btnReadProperties;
        private System.Windows.Forms.Label lblStep1Status;

        // Step 2 Controls
        private System.Windows.Forms.GroupBox grpStep2;
        private System.Windows.Forms.Label lblStep2Instructions;
        private System.Windows.Forms.Button btnDetectFastboot;
        private System.Windows.Forms.TextBox txtDeviceSerial;
        private System.Windows.Forms.Label lblStep2Status;
        private System.Windows.Forms.Label lblCountdown;

        // Step 3 Controls
        private System.Windows.Forms.GroupBox grpStep3;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblOperation;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.Button btnStartUnlock;
        private System.Windows.Forms.Label lblStep3Status;

        // Log Panel
        private System.Windows.Forms.GroupBox grpLogs;
        private System.Windows.Forms.RichTextBox txtLogs;

        // Status Bar
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblCurrentStep;
        private System.Windows.Forms.ToolStripProgressBar progressStatusBar;
        private System.Windows.Forms.ToolStripStatusLabel lblElapsedTime;

        private void InitializeControls()
        {
            this.SuspendLayout();

            // Port Selection Group
            this.grpPortSelection = new System.Windows.Forms.GroupBox();
            this.grpPortSelection.Text = "Port Selection";
            this.grpPortSelection.Location = new System.Drawing.Point(12, 12);
            this.grpPortSelection.Size = new System.Drawing.Size(976, 80);
            this.grpPortSelection.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            this.cmbPorts = new System.Windows.Forms.ComboBox();
            this.cmbPorts.Location = new System.Drawing.Point(15, 30);
            this.cmbPorts.Size = new System.Drawing.Size(150, 24);
            this.cmbPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;

            this.btnScanPorts = new System.Windows.Forms.Button();
            this.btnScanPorts.Text = "Scan Ports";
            this.btnScanPorts.Location = new System.Drawing.Point(180, 28);
            this.btnScanPorts.Size = new System.Drawing.Size(100, 28);
            this.btnScanPorts.Click += BtnScanPorts_Click;

            this.btnConnect = new System.Windows.Forms.Button();
            this.btnConnect.Text = "Connect";
            this.btnConnect.Location = new System.Drawing.Point(295, 28);
            this.btnConnect.Size = new System.Drawing.Size(100, 28);
            this.btnConnect.Click += BtnConnect_Click;

            this.lblPortStatus = new System.Windows.Forms.Label();
            this.lblPortStatus.Text = "Status: Disconnected";
            this.lblPortStatus.Location = new System.Drawing.Point(410, 33);
            this.lblPortStatus.Size = new System.Drawing.Size(200, 20);
            this.lblPortStatus.ForeColor = System.Drawing.Color.Gray;

            this.grpPortSelection.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.cmbPorts, this.btnScanPorts, this.btnConnect, this.lblPortStatus
            });

            // Step 1 Group
            this.grpStep1 = new System.Windows.Forms.GroupBox();
            this.grpStep1.Text = "Step 1: Read Router Properties";
            this.grpStep1.Location = new System.Drawing.Point(12, 98);
            this.grpStep1.Size = new System.Drawing.Size(976, 140);
            this.grpStep1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpStep1.Enabled = false;

            var lblManufacturer = new System.Windows.Forms.Label();
            lblManufacturer.Text = "Manufacturer:";
            lblManufacturer.Location = new System.Drawing.Point(15, 30);
            lblManufacturer.Size = new System.Drawing.Size(100, 20);

            this.txtManufacturer = new System.Windows.Forms.TextBox();
            this.txtManufacturer.Location = new System.Drawing.Point(120, 27);
            this.txtManufacturer.Size = new System.Drawing.Size(200, 22);
            this.txtManufacturer.ReadOnly = true;

            var lblModel = new System.Windows.Forms.Label();
            lblModel.Text = "Model:";
            lblModel.Location = new System.Drawing.Point(340, 30);
            lblModel.Size = new System.Drawing.Size(50, 20);

            this.txtModel = new System.Windows.Forms.TextBox();
            this.txtModel.Location = new System.Drawing.Point(400, 27);
            this.txtModel.Size = new System.Drawing.Size(200, 22);
            this.txtModel.ReadOnly = true;

            var lblIMEI = new System.Windows.Forms.Label();
            lblIMEI.Text = "IMEI:";
            lblIMEI.Location = new System.Drawing.Point(15, 62);
            lblIMEI.Size = new System.Drawing.Size(100, 20);

            this.txtIMEI = new System.Windows.Forms.TextBox();
            this.txtIMEI.Location = new System.Drawing.Point(120, 59);
            this.txtIMEI.Size = new System.Drawing.Size(200, 22);
            this.txtIMEI.ReadOnly = true;

            var lblFirmware = new System.Windows.Forms.Label();
            lblFirmware.Text = "Firmware:";
            lblFirmware.Location = new System.Drawing.Point(340, 62);
            lblFirmware.Size = new System.Drawing.Size(70, 20);

            this.txtFirmware = new System.Windows.Forms.TextBox();
            this.txtFirmware.Location = new System.Drawing.Point(400, 59);
            this.txtFirmware.Size = new System.Drawing.Size(200, 22);
            this.txtFirmware.ReadOnly = true;

            var lblLockStatus = new System.Windows.Forms.Label();
            lblLockStatus.Text = "Lock Status:";
            lblLockStatus.Location = new System.Drawing.Point(15, 94);
            lblLockStatus.Size = new System.Drawing.Size(100, 20);

            this.txtLockStatus = new System.Windows.Forms.TextBox();
            this.txtLockStatus.Location = new System.Drawing.Point(120, 91);
            this.txtLockStatus.Size = new System.Drawing.Size(200, 22);
            this.txtLockStatus.ReadOnly = true;

            this.btnReadProperties = new System.Windows.Forms.Button();
            this.btnReadProperties.Text = "Read Properties";
            this.btnReadProperties.Location = new System.Drawing.Point(640, 60);
            this.btnReadProperties.Size = new System.Drawing.Size(120, 35);
            this.btnReadProperties.Click += BtnReadProperties_Click;

            this.lblStep1Status = new System.Windows.Forms.Label();
            this.lblStep1Status.Text = "Ready";
            this.lblStep1Status.Location = new System.Drawing.Point(780, 68);
            this.lblStep1Status.Size = new System.Drawing.Size(180, 20);

            this.grpStep1.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblManufacturer, this.txtManufacturer, lblModel, this.txtModel,
                lblIMEI, this.txtIMEI, lblFirmware, this.txtFirmware,
                lblLockStatus, this.txtLockStatus, this.btnReadProperties, this.lblStep1Status
            });

            // Step 2 Group
            this.grpStep2 = new System.Windows.Forms.GroupBox();
            this.grpStep2.Text = "Step 2: Switch to Fastboot Mode";
            this.grpStep2.Location = new System.Drawing.Point(12, 244);
            this.grpStep2.Size = new System.Drawing.Size(976, 100);
            this.grpStep2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpStep2.Enabled = false;

            this.lblStep2Instructions = new System.Windows.Forms.Label();
            this.lblStep2Instructions.Text = "Please switch your device to Fastboot mode and connect via Huawei Download port.";
            this.lblStep2Instructions.Location = new System.Drawing.Point(15, 25);
            this.lblStep2Instructions.Size = new System.Drawing.Size(950, 20);
            this.lblStep2Instructions.Font = new System.Drawing.Font(this.lblStep2Instructions.Font, System.Drawing.FontStyle.Bold);

            this.btnDetectFastboot = new System.Windows.Forms.Button();
            this.btnDetectFastboot.Text = "Detect Fastboot Device";
            this.btnDetectFastboot.Location = new System.Drawing.Point(15, 55);
            this.btnDetectFastboot.Size = new System.Drawing.Size(160, 30);
            this.btnDetectFastboot.Click += BtnDetectFastboot_Click;

            var lblSerial = new System.Windows.Forms.Label();
            lblSerial.Text = "Device Serial:";
            lblSerial.Location = new System.Drawing.Point(190, 62);
            lblSerial.Size = new System.Drawing.Size(90, 20);

            this.txtDeviceSerial = new System.Windows.Forms.TextBox();
            this.txtDeviceSerial.Location = new System.Drawing.Point(285, 59);
            this.txtDeviceSerial.Size = new System.Drawing.Size(200, 22);
            this.txtDeviceSerial.ReadOnly = true;

            this.lblStep2Status = new System.Windows.Forms.Label();
            this.lblStep2Status.Text = "Waiting";
            this.lblStep2Status.Location = new System.Drawing.Point(500, 62);
            this.lblStep2Status.Size = new System.Drawing.Size(150, 20);

            this.lblCountdown = new System.Windows.Forms.Label();
            this.lblCountdown.Text = "";
            this.lblCountdown.Location = new System.Drawing.Point(665, 62);
            this.lblCountdown.Size = new System.Drawing.Size(200, 20);

            this.grpStep2.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lblStep2Instructions, this.btnDetectFastboot, lblSerial,
                this.txtDeviceSerial, this.lblStep2Status, this.lblCountdown
            });

            // Step 3 Group
            this.grpStep3 = new System.Windows.Forms.GroupBox();
            this.grpStep3.Text = "Step 3: Unlock Device";
            this.grpStep3.Location = new System.Drawing.Point(12, 350);
            this.grpStep3.Size = new System.Drawing.Size(976, 100);
            this.grpStep3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.grpStep3.Enabled = false;

            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressBar.Location = new System.Drawing.Point(15, 30);
            this.progressBar.Size = new System.Drawing.Size(600, 23);

            this.lblOperation = new System.Windows.Forms.Label();
            this.lblOperation.Text = "Ready";
            this.lblOperation.Location = new System.Drawing.Point(15, 60);
            this.lblOperation.Size = new System.Drawing.Size(400, 20);

            this.lblSpeed = new System.Windows.Forms.Label();
            this.lblSpeed.Text = "";
            this.lblSpeed.Location = new System.Drawing.Point(425, 60);
            this.lblSpeed.Size = new System.Drawing.Size(190, 20);

            this.btnStartUnlock = new System.Windows.Forms.Button();
            this.btnStartUnlock.Text = "Start Unlock";
            this.btnStartUnlock.Location = new System.Drawing.Point(640, 30);
            this.btnStartUnlock.Size = new System.Drawing.Size(120, 35);
            this.btnStartUnlock.Click += BtnStartUnlock_Click;

            this.lblStep3Status = new System.Windows.Forms.Label();
            this.lblStep3Status.Text = "Ready";
            this.lblStep3Status.Location = new System.Drawing.Point(780, 38);
            this.lblStep3Status.Size = new System.Drawing.Size(180, 20);

            this.grpStep3.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.progressBar, this.lblOperation, this.lblSpeed,
                this.btnStartUnlock, this.lblStep3Status
            });

            // Log Group
            this.grpLogs = new System.Windows.Forms.GroupBox();
            this.grpLogs.Text = "Progress Log";
            this.grpLogs.Location = new System.Drawing.Point(12, 456);
            this.grpLogs.Size = new System.Drawing.Size(976, 230);
            this.grpLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | 
                                  System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            this.txtLogs = new System.Windows.Forms.RichTextBox();
            this.txtLogs.Location = new System.Drawing.Point(10, 20);
            this.txtLogs.Size = new System.Drawing.Size(956, 200);
            this.txtLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | 
                                  System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.txtLogs.ReadOnly = true;
            this.txtLogs.BackColor = System.Drawing.Color.White;
            this.txtLogs.Font = new System.Drawing.Font("Consolas", 9F);

            this.grpLogs.Controls.Add(this.txtLogs);

            // Status Strip
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusStrip.Location = new System.Drawing.Point(0, 692);
            this.statusStrip.Size = new System.Drawing.Size(1000, 26);

            this.lblCurrentStep = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblCurrentStep.Text = "Current Step: None";
            this.lblCurrentStep.Size = new System.Drawing.Size(200, 21);

            this.progressStatusBar = new System.Windows.Forms.ToolStripProgressBar();
            this.progressStatusBar.Size = new System.Drawing.Size(200, 20);

            this.lblElapsedTime = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblElapsedTime.Text = "Time: 00:00:00";
            this.lblElapsedTime.Size = new System.Drawing.Size(150, 21);
            this.lblElapsedTime.Spring = true;
            this.lblElapsedTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblCurrentStep, this.progressStatusBar, this.lblElapsedTime
            });

            // Add all to form
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.grpPortSelection, this.grpStep1, this.grpStep2, 
                this.grpStep3, this.grpLogs, this.statusStrip
            });
        }
    }
}
