namespace FFXI_ME_v2
{
    partial class ExitAndSaveBox
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
            this.components = new System.ComponentModel.Container();
            this.YesButton = new System.Windows.Forms.Button();
            this.NoButton = new System.Windows.Forms.Button();
            this.CancelExitButton = new System.Windows.Forms.Button();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deselectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.selectMacroFilesOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectBooksOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.selectFoldersOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // YesButton
            // 
            this.YesButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.YesButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.YesButton.Location = new System.Drawing.Point(62, 264);
            this.YesButton.Name = "YesButton";
            this.YesButton.Size = new System.Drawing.Size(75, 23);
            this.YesButton.TabIndex = 0;
            this.YesButton.Text = "Save && Exit";
            this.YesButton.UseVisualStyleBackColor = true;
            // 
            // NoButton
            // 
            this.NoButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.NoButton.DialogResult = System.Windows.Forms.DialogResult.No;
            this.NoButton.Location = new System.Drawing.Point(159, 264);
            this.NoButton.Name = "NoButton";
            this.NoButton.Size = new System.Drawing.Size(75, 23);
            this.NoButton.TabIndex = 0;
            this.NoButton.Text = "Just Exit";
            this.NoButton.UseVisualStyleBackColor = true;
            // 
            // CancelExitButton
            // 
            this.CancelExitButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.CancelExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelExitButton.Location = new System.Drawing.Point(256, 264);
            this.CancelExitButton.Name = "CancelExitButton";
            this.CancelExitButton.Size = new System.Drawing.Size(75, 23);
            this.CancelExitButton.TabIndex = 0;
            this.CancelExitButton.Text = "Cancel!";
            this.CancelExitButton.UseVisualStyleBackColor = true;
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.HorizontalScrollbar = true;
            this.checkedListBox1.Location = new System.Drawing.Point(12, 28);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(368, 214);
            this.checkedListBox1.TabIndex = 1;
            this.checkedListBox1.ThreeDCheckBoxes = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectAllToolStripMenuItem,
            this.deselectAllToolStripMenuItem,
            this.toolStripSeparator1,
            this.selectMacroFilesOnlyToolStripMenuItem,
            this.selectBooksOnlyToolStripMenuItem,
            this.selectFoldersOnlyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(185, 142);
            // 
            // selectAllToolStripMenuItem
            // 
            this.selectAllToolStripMenuItem.Image = global::FFXI_ME_v2.Properties.Resources.SelectAll;
            this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
            this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.selectAllToolStripMenuItem.Text = "Select All";
            this.selectAllToolStripMenuItem.Click += new System.EventHandler(this.selectAllToolStripMenuItem_Click);
            // 
            // deselectAllToolStripMenuItem
            // 
            this.deselectAllToolStripMenuItem.Image = global::FFXI_ME_v2.Properties.Resources.DeSelectAll;
            this.deselectAllToolStripMenuItem.Name = "deselectAllToolStripMenuItem";
            this.deselectAllToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.deselectAllToolStripMenuItem.Text = "Deselect All";
            this.deselectAllToolStripMenuItem.Click += new System.EventHandler(this.deselectAllToolStripMenuItem_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(357, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Select which of the following actions you would like to take before exiting?";
            // 
            // selectMacroFilesOnlyToolStripMenuItem
            // 
            this.selectMacroFilesOnlyToolStripMenuItem.Image = global::FFXI_ME_v2.Properties.Resources.Macrofile;
            this.selectMacroFilesOnlyToolStripMenuItem.Name = "selectMacroFilesOnlyToolStripMenuItem";
            this.selectMacroFilesOnlyToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.selectMacroFilesOnlyToolStripMenuItem.Text = "Select Macro Files Only";
            this.selectMacroFilesOnlyToolStripMenuItem.Visible = false;
            this.selectMacroFilesOnlyToolStripMenuItem.Click += new System.EventHandler(this.selectMacroFilesOnlyToolStripMenuItem_Click);
            // 
            // selectBooksOnlyToolStripMenuItem
            // 
            this.selectBooksOnlyToolStripMenuItem.Image = global::FFXI_ME_v2.Properties.Resources.Book_openHS;
            this.selectBooksOnlyToolStripMenuItem.Name = "selectBooksOnlyToolStripMenuItem";
            this.selectBooksOnlyToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.selectBooksOnlyToolStripMenuItem.Text = "Select Books Only";
            this.selectBooksOnlyToolStripMenuItem.Visible = false;
            this.selectBooksOnlyToolStripMenuItem.Click += new System.EventHandler(this.selectBooksOnlyToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(181, 6);
            this.toolStripSeparator1.Visible = false;
            // 
            // selectFoldersOnlyToolStripMenuItem
            // 
            this.selectFoldersOnlyToolStripMenuItem.Image = global::FFXI_ME_v2.Properties.Resources.ClosedFolder;
            this.selectFoldersOnlyToolStripMenuItem.Name = "selectFoldersOnlyToolStripMenuItem";
            this.selectFoldersOnlyToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
            this.selectFoldersOnlyToolStripMenuItem.Text = "Select Folders Only";
            this.selectFoldersOnlyToolStripMenuItem.Visible = false;
            this.selectFoldersOnlyToolStripMenuItem.Click += new System.EventHandler(this.selectFoldersOnlyToolStripMenuItem_Click);
            // 
            // ExitAndSaveBox
            // 
            this.AcceptButton = this.CancelExitButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelExitButton;
            this.ClientSize = new System.Drawing.Size(392, 299);
            this.ControlBox = false;
            this.Controls.Add(this.checkedListBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.NoButton);
            this.Controls.Add(this.YesButton);
            this.Controls.Add(this.CancelExitButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 200);
            this.Name = "ExitAndSaveBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Save before exit?";
            this.TopMost = true;
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button YesButton;
        private System.Windows.Forms.Button CancelExitButton;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deselectAllToolStripMenuItem;
        private System.Windows.Forms.Button NoButton;
        public System.Windows.Forms.ToolStripMenuItem selectMacroFilesOnlyToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem selectBooksOnlyToolStripMenuItem;
        public System.Windows.Forms.ToolStripMenuItem selectFoldersOnlyToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        public System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}