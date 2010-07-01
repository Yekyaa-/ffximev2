namespace FFXI_ME_v2
{
    partial class OptionsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDialog));
            this.comboBoxProgLanguage = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBoxExplorerView = new System.Windows.Forms.CheckBox();
            this.checkBoxFolderAsRoot = new System.Windows.Forms.CheckBox();
            this.checkBoxIncludeHeader = new System.Windows.Forms.CheckBox();
            this.numericUpDownMaxMenuItems = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxShowBlankBooks = new System.Windows.Forms.CheckBox();
            this.checkBoxMinimize = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkBoxItems = new System.Windows.Forms.CheckBox();
            this.checkBoxKeyItems = new System.Windows.Forms.CheckBox();
            this.checkBoxAtPhrases = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonApply = new System.Windows.Forms.Button();
            this.toolTipForOptions = new System.Windows.Forms.ToolTip(this.components);
            this.comboBoxEnterKeyOption = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxMenuItems)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxProgLanguage
            // 
            this.comboBoxProgLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxProgLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxProgLanguage.Enabled = false;
            this.comboBoxProgLanguage.FormattingEnabled = true;
            this.comboBoxProgLanguage.Items.AddRange(new object[] {
            "Japanese",
            "English",
            "German",
            "French"});
            this.comboBoxProgLanguage.Location = new System.Drawing.Point(210, 37);
            this.comboBoxProgLanguage.Name = "comboBoxProgLanguage";
            this.comboBoxProgLanguage.Size = new System.Drawing.Size(139, 21);
            this.comboBoxProgLanguage.TabIndex = 0;
            this.toolTipForOptions.SetToolTip(this.comboBoxProgLanguage, "Sets the default language for text in the program.");
            this.comboBoxProgLanguage.SelectedValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 45);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Program Language :";
            this.toolTipForOptions.SetToolTip(this.label1, "Which language do you speak best?\r\n(Not Implemented)");
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Items.AddRange(new object[] {
            "Japanese",
            "English",
            "German",
            "French",
            "All (Long Load)"});
            this.comboBoxLanguage.Location = new System.Drawing.Point(210, 13);
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.Size = new System.Drawing.Size(139, 21);
            this.comboBoxLanguage.TabIndex = 2;
            this.toolTipForOptions.SetToolTip(this.comboBoxLanguage, "Sets what language will be used when\r\nfirst loading auto-translate phrases.");
            this.comboBoxLanguage.SelectedValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(169, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Auto-Translate Phrase Language :";
            this.toolTipForOptions.SetToolTip(this.label2, "Requires restart, but will load this\r\nlanguage on next startup.");
            // 
            // checkBoxExplorerView
            // 
            this.checkBoxExplorerView.AutoSize = true;
            this.checkBoxExplorerView.Location = new System.Drawing.Point(19, 20);
            this.checkBoxExplorerView.Name = "checkBoxExplorerView";
            this.checkBoxExplorerView.Size = new System.Drawing.Size(112, 17);
            this.checkBoxExplorerView.TabIndex = 4;
            this.checkBoxExplorerView.Text = "Use Explorer View";
            this.toolTipForOptions.SetToolTip(this.checkBoxExplorerView, "Do you like folder view when\r\nusing Windows Explorer?");
            this.checkBoxExplorerView.UseVisualStyleBackColor = true;
            this.checkBoxExplorerView.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // checkBoxFolderAsRoot
            // 
            this.checkBoxFolderAsRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxFolderAsRoot.AutoSize = true;
            this.checkBoxFolderAsRoot.Location = new System.Drawing.Point(224, 19);
            this.checkBoxFolderAsRoot.Name = "checkBoxFolderAsRoot";
            this.checkBoxFolderAsRoot.Size = new System.Drawing.Size(118, 17);
            this.checkBoxFolderAsRoot.TabIndex = 5;
            this.checkBoxFolderAsRoot.Text = "Use Folder As Root";
            this.toolTipForOptions.SetToolTip(this.checkBoxFolderAsRoot, "When opening the main or template folders,\r\nthis option won\'t bother you with see" +
                    "ing\r\nthe Desktop or My Documents folders.");
            this.checkBoxFolderAsRoot.UseVisualStyleBackColor = true;
            this.checkBoxFolderAsRoot.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // checkBoxIncludeHeader
            // 
            this.checkBoxIncludeHeader.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxIncludeHeader.AutoSize = true;
            this.checkBoxIncludeHeader.Location = new System.Drawing.Point(19, 18);
            this.checkBoxIncludeHeader.Name = "checkBoxIncludeHeader";
            this.checkBoxIncludeHeader.Size = new System.Drawing.Size(99, 17);
            this.checkBoxIncludeHeader.TabIndex = 6;
            this.checkBoxIncludeHeader.Text = "Include Header";
            this.toolTipForOptions.SetToolTip(this.checkBoxIncludeHeader, "Check this to display headers on all context menus.");
            this.checkBoxIncludeHeader.UseVisualStyleBackColor = true;
            this.checkBoxIncludeHeader.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // numericUpDownMaxMenuItems
            // 
            this.numericUpDownMaxMenuItems.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.numericUpDownMaxMenuItems.Location = new System.Drawing.Point(304, 15);
            this.numericUpDownMaxMenuItems.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDownMaxMenuItems.Name = "numericUpDownMaxMenuItems";
            this.numericUpDownMaxMenuItems.Size = new System.Drawing.Size(45, 20);
            this.numericUpDownMaxMenuItems.TabIndex = 7;
            this.numericUpDownMaxMenuItems.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.toolTipForOptions.SetToolTip(this.numericUpDownMaxMenuItems, "Maximum number of menu items allowed\r\nbefore categorizing the TAB menu.");
            this.numericUpDownMaxMenuItems.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numericUpDownMaxMenuItems.ValueChanged += new System.EventHandler(this.ValueChanged);
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(178, 19);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Maximum Menu Items";
            this.toolTipForOptions.SetToolTip(this.label3, "Maximum number of menu items allowed\r\nbefore categorizing the TAB menu.");
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.comboBoxEnterKeyOption);
            this.groupBox1.Controls.Add(this.checkBoxShowBlankBooks);
            this.groupBox1.Controls.Add(this.checkBoxMinimize);
            this.groupBox1.Controls.Add(this.comboBoxLanguage);
            this.groupBox1.Controls.Add(this.comboBoxProgLanguage);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.groupBox4);
            this.groupBox1.Location = new System.Drawing.Point(4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(367, 177);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Program Specific";
            // 
            // checkBoxShowBlankBooks
            // 
            this.checkBoxShowBlankBooks.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxShowBlankBooks.AutoSize = true;
            this.checkBoxShowBlankBooks.Location = new System.Drawing.Point(224, 71);
            this.checkBoxShowBlankBooks.Name = "checkBoxShowBlankBooks";
            this.checkBoxShowBlankBooks.Size = new System.Drawing.Size(116, 17);
            this.checkBoxShowBlankBooks.TabIndex = 9;
            this.checkBoxShowBlankBooks.Text = "Show Blank Books";
            this.toolTipForOptions.SetToolTip(this.checkBoxShowBlankBooks, "If checked, reloading all macrofiles will\r\ncause blank books to be ignored.");
            this.checkBoxShowBlankBooks.UseVisualStyleBackColor = true;
            this.checkBoxShowBlankBooks.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // checkBoxMinimize
            // 
            this.checkBoxMinimize.AutoSize = true;
            this.checkBoxMinimize.Location = new System.Drawing.Point(19, 71);
            this.checkBoxMinimize.Name = "checkBoxMinimize";
            this.checkBoxMinimize.Size = new System.Drawing.Size(106, 17);
            this.checkBoxMinimize.TabIndex = 8;
            this.checkBoxMinimize.Text = "Minimize To Tray";
            this.toolTipForOptions.SetToolTip(this.checkBoxMinimize, "If you check this, minimize will send the program\r\nto the system tray.  Unchecked" +
                    " it stays in the taskbar.");
            this.checkBoxMinimize.UseVisualStyleBackColor = true;
            this.checkBoxMinimize.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.checkBoxItems);
            this.groupBox4.Controls.Add(this.checkBoxKeyItems);
            this.groupBox4.Controls.Add(this.checkBoxAtPhrases);
            this.groupBox4.Location = new System.Drawing.Point(6, 126);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(355, 45);
            this.groupBox4.TabIndex = 7;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Load";
            // 
            // checkBoxItems
            // 
            this.checkBoxItems.AutoSize = true;
            this.checkBoxItems.Location = new System.Drawing.Point(13, 16);
            this.checkBoxItems.Name = "checkBoxItems";
            this.checkBoxItems.Size = new System.Drawing.Size(51, 17);
            this.checkBoxItems.TabIndex = 4;
            this.checkBoxItems.Text = "Items";
            this.toolTipForOptions.SetToolTip(this.checkBoxItems, "Load Item Information On Program Load.\r\nIf you have a slow load time, unchecking " +
                    "this might help.");
            this.checkBoxItems.UseVisualStyleBackColor = true;
            this.checkBoxItems.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // checkBoxKeyItems
            // 
            this.checkBoxKeyItems.AutoSize = true;
            this.checkBoxKeyItems.Location = new System.Drawing.Point(107, 16);
            this.checkBoxKeyItems.Name = "checkBoxKeyItems";
            this.checkBoxKeyItems.Size = new System.Drawing.Size(72, 17);
            this.checkBoxKeyItems.TabIndex = 5;
            this.checkBoxKeyItems.Text = "Key Items";
            this.toolTipForOptions.SetToolTip(this.checkBoxKeyItems, "Load Key Item Information On Program Load.");
            this.checkBoxKeyItems.UseVisualStyleBackColor = true;
            this.checkBoxKeyItems.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // checkBoxAtPhrases
            // 
            this.checkBoxAtPhrases.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxAtPhrases.AutoSize = true;
            this.checkBoxAtPhrases.Location = new System.Drawing.Point(218, 9);
            this.checkBoxAtPhrases.Name = "checkBoxAtPhrases";
            this.checkBoxAtPhrases.Size = new System.Drawing.Size(95, 30);
            this.checkBoxAtPhrases.TabIndex = 6;
            this.checkBoxAtPhrases.Text = "Auto-Translate\r\nPhrases";
            this.checkBoxAtPhrases.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.toolTipForOptions.SetToolTip(this.checkBoxAtPhrases, "Load Auto-Translate Phrase Information On Program Load.");
            this.checkBoxAtPhrases.UseVisualStyleBackColor = true;
            this.checkBoxAtPhrases.CheckedChanged += new System.EventHandler(this.ValueChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.checkBoxExplorerView);
            this.groupBox2.Controls.Add(this.checkBoxFolderAsRoot);
            this.groupBox2.Location = new System.Drawing.Point(4, 187);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(367, 43);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "When Opening Folders (Invoking Windows Explorer)";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.numericUpDownMaxMenuItems);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.checkBoxIncludeHeader);
            this.groupBox3.Location = new System.Drawing.Point(4, 236);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(367, 41);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Context Menu Specific";
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(23, 283);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 12;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(150, 283);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 13;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonApply
            // 
            this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApply.Enabled = false;
            this.buttonApply.Location = new System.Drawing.Point(277, 283);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 23);
            this.buttonApply.TabIndex = 14;
            this.buttonApply.Text = "Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // comboBoxEnterKeyOption
            // 
            this.comboBoxEnterKeyOption.FormattingEnabled = true;
            this.comboBoxEnterKeyOption.Items.AddRange(new object[] {
            "Next Line",
            "Insert Line",
            "Safe Insert"});
            this.comboBoxEnterKeyOption.Location = new System.Drawing.Point(210, 95);
            this.comboBoxEnterKeyOption.Name = "comboBoxEnterKeyOption";
            this.comboBoxEnterKeyOption.Size = new System.Drawing.Size(139, 21);
            this.comboBoxEnterKeyOption.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 103);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(149, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Enter keypress when editing : ";
            // 
            // OptionsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(374, 318);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.OptionsDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxMenuItems)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ComboBox comboBoxProgLanguage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.ComboBox comboBoxLanguage;
        public System.Windows.Forms.CheckBox checkBoxExplorerView;
        public System.Windows.Forms.CheckBox checkBoxFolderAsRoot;
        public System.Windows.Forms.CheckBox checkBoxIncludeHeader;
        public System.Windows.Forms.NumericUpDown numericUpDownMaxMenuItems;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.ToolTip toolTipForOptions;
        private System.Windows.Forms.CheckBox checkBoxItems;
        private System.Windows.Forms.CheckBox checkBoxAtPhrases;
        private System.Windows.Forms.CheckBox checkBoxKeyItems;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox checkBoxMinimize;
        private System.Windows.Forms.CheckBox checkBoxShowBlankBooks;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxEnterKeyOption;


    }
}