namespace ApplicationLogger {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.btnRunAs = new System.Windows.Forms.Button();
			this.ckbAutoScroll = new System.Windows.Forms.CheckBox();
			this.btnClearLog = new System.Windows.Forms.Button();
			this.lsvLog = new System.Windows.Forms.ListView();
			this.imageList = new System.Windows.Forms.ImageList(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.labelApplication = new System.Windows.Forms.Label();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnRunAs);
			this.groupBox1.Controls.Add(this.ckbAutoScroll);
			this.groupBox1.Controls.Add(this.btnClearLog);
			this.groupBox1.Controls.Add(this.lsvLog);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.labelApplication);
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox1.Location = new System.Drawing.Point(10, 10);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(10);
			this.groupBox1.Size = new System.Drawing.Size(1837, 390);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Application information";
			// 
			// btnRunAs
			// 
			this.btnRunAs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRunAs.Location = new System.Drawing.Point(1294, 44);
			this.btnRunAs.Name = "btnRunAs";
			this.btnRunAs.Size = new System.Drawing.Size(150, 31);
			this.btnRunAs.TabIndex = 4;
			this.btnRunAs.Text = "&Run as Administrator";
			this.btnRunAs.UseVisualStyleBackColor = true;
			this.btnRunAs.Click += new System.EventHandler(this.btnRunAs_Click);
			// 
			// ckbAutoScroll
			// 
			this.ckbAutoScroll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ckbAutoScroll.AutoSize = true;
			this.ckbAutoScroll.Location = new System.Drawing.Point(1521, 50);
			this.ckbAutoScroll.Name = "ckbAutoScroll";
			this.ckbAutoScroll.Size = new System.Drawing.Size(98, 21);
			this.ckbAutoScroll.TabIndex = 4;
			this.ckbAutoScroll.Text = "&Auto Scroll";
			this.ckbAutoScroll.UseVisualStyleBackColor = true;
			this.ckbAutoScroll.CheckedChanged += new System.EventHandler(this.CkbAutoScroll_CheckedChanged);
			// 
			// btnClearLog
			// 
			this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClearLog.Location = new System.Drawing.Point(1677, 44);
			this.btnClearLog.Name = "btnClearLog";
			this.btnClearLog.Size = new System.Drawing.Size(132, 31);
			this.btnClearLog.TabIndex = 3;
			this.btnClearLog.Text = "&Clear Log";
			this.btnClearLog.UseVisualStyleBackColor = true;
			this.btnClearLog.Click += new System.EventHandler(this.BtnClearLog_Click);
			// 
			// lsvLog
			// 
			this.lsvLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.lsvLog.HideSelection = false;
			this.lsvLog.LargeImageList = this.imageList;
			this.lsvLog.Location = new System.Drawing.Point(14, 87);
			this.lsvLog.Name = "lsvLog";
			this.lsvLog.Size = new System.Drawing.Size(1795, 288);
			this.lsvLog.SmallImageList = this.imageList;
			this.lsvLog.TabIndex = 2;
			this.lsvLog.UseCompatibleStateImageBehavior = false;
			this.lsvLog.DoubleClick += new System.EventHandler(this.LsvLog_DoubleClick);
			// 
			// imageList
			// 
			this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.imageList.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList.Images.SetKeyName(0, "Empty");
			this.imageList.Images.SetKeyName(1, "Ascending");
			this.imageList.Images.SetKeyName(2, "Descending");
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(17, 67);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(36, 17);
			this.label1.TabIndex = 1;
			this.label1.Text = "Log:";
			// 
			// labelApplication
			// 
			this.labelApplication.AutoSize = true;
			this.labelApplication.Location = new System.Drawing.Point(17, 33);
			this.labelApplication.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelApplication.Name = "labelApplication";
			this.labelApplication.Size = new System.Drawing.Size(171, 17);
			this.labelApplication.TabIndex = 0;
			this.labelApplication.Text = "Current Application name:";
			// 
			// notifyIcon
			// 
			this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
			this.notifyIcon.Visible = true;
			this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.onDoubleClickNotificationIcon);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1857, 410);
			this.Controls.Add(this.groupBox1);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "MainForm";
			this.Padding = new System.Windows.Forms.Padding(10);
			this.Text = "Application Logger";
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onFormClosing);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.onFormClosed);
			this.Load += new System.EventHandler(this.onFormLoad);
			this.Resize += new System.EventHandler(this.onResize);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label labelApplication;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ListView lsvLog;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnClearLog;
		private System.Windows.Forms.ImageList imageList;
		private System.Windows.Forms.CheckBox ckbAutoScroll;
		private System.Windows.Forms.Button btnRunAs;
	}
}

