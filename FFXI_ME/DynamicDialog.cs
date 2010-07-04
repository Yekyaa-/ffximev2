using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FFXI_ME_v2
{
    public partial class DynamicDialog : Form
    {
        public String GetSelection()
        {
            if (this.comboBox.Items.Count > 0)//(this.comboBox.Visible == true)
            {
                if (this.comboBox.DropDownStyle == ComboBoxStyle.DropDown)
                    return this.comboBox.Text;
                return this.comboBox.SelectedItem as String;
            }
            else return this.textBox.Text;
        }

        public DynamicDialog(String title, String label, String defaultvalue) 
            : this(title, null, label, defaultvalue, true) { }

        public DynamicDialog(String title, Object[] Items, String label, bool IsEditable) 
            : this(title, Items, label, String.Empty, IsEditable) { }

        public DynamicDialog(String title, Object[] Items, String label)
            : this(title, Items, label, String.Empty, false) { }

        public DynamicDialog(String title, Object[] Items, String label, String defaultvalue, bool IsEditable)
        {
            InitializeComponent();
            this.Text = title;
            if (label != String.Empty)
                this.label.Text = label;
            else this.label.Text = "Take your pick:";
            if (Items != null)
                this.comboBox.Items.AddRange(Items);
            if (IsEditable)
            {
                if (Items == null)
                {
                    // If Items is null, the box is editable
                    // forget the combobox and just go with a TextBox
                    this.comboBox.Visible = false;
                    this.textBox.Visible = true;
                    this.textBox.Text = defaultvalue;
                }
                else
                {
                    this.comboBox.DropDownStyle = ComboBoxStyle.DropDown;
                    if (defaultvalue != String.Empty)
                        this.comboBox.Text = defaultvalue;
                    else
                    {
                        this.comboBox.SelectedIndex = 0;
                    }
                }
            }
            else
            {
                this.comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                if (this.comboBox.Items.Count > 0)
                    this.comboBox.SelectedIndex = 0;
            }
        }
    }
}
