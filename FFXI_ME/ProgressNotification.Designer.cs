namespace FFXI_ME_v2
{
    partial class ProgressNotification
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.notifyBar = new System.Windows.Forms.ProgressBar();
            this.notifyStopButton = new System.Windows.Forms.Button();
            this.notifyLabel = new System.Windows.Forms.Label();
            this.countLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // notifyBar
            // 
            this.notifyBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.notifyBar.Location = new System.Drawing.Point(13, 13);
            this.notifyBar.Name = "notifyBar";
            this.notifyBar.Size = new System.Drawing.Size(289, 23);
            this.notifyBar.Step = 1;
            this.notifyBar.TabIndex = 0;
            // 
            // notifyStopButton
            // 
            this.notifyStopButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.notifyStopButton.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.notifyStopButton.Location = new System.Drawing.Point(120, 125);
            this.notifyStopButton.Name = "notifyStopButton";
            this.notifyStopButton.Size = new System.Drawing.Size(75, 23);
            this.notifyStopButton.TabIndex = 1;
            this.notifyStopButton.Text = "Stop";
            this.notifyStopButton.UseVisualStyleBackColor = true;
            // 
            // notifyLabel
            // 
            this.notifyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.notifyLabel.Location = new System.Drawing.Point(13, 43);
            this.notifyLabel.Name = "notifyLabel";
            this.notifyLabel.Size = new System.Drawing.Size(289, 79);
            this.notifyLabel.TabIndex = 2;
            this.notifyLabel.Text = "notifyLabel";
            this.notifyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // countLabel
            // 
            this.countLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.countLabel.Location = new System.Drawing.Point(202, 130);
            this.countLabel.Name = "countLabel";
            this.countLabel.Size = new System.Drawing.Size(100, 23);
            this.countLabel.TabIndex = 3;
            this.countLabel.Text = "10000/10000";
            this.countLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            // 
            // ProgressNotification
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(314, 167);
            this.ControlBox = false;
            this.Controls.Add(this.countLabel);
            this.Controls.Add(this.notifyLabel);
            this.Controls.Add(this.notifyStopButton);
            this.Controls.Add(this.notifyBar);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(640, 480);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(275, 175);
            this.Name = "ProgressNotification";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ProgressNotification";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar notifyBar;
        private System.Windows.Forms.Button notifyStopButton;
        private System.Windows.Forms.Label notifyLabel;
        private System.Windows.Forms.Label countLabel;
    }
}