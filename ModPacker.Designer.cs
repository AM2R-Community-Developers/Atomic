namespace AM2R_ModPacker
{
    partial class ModPacker
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModPacker));
            this.panel1 = new System.Windows.Forms.Panel();
            this.CreateLabel = new System.Windows.Forms.Label();
            this.CreateButton = new System.Windows.Forms.Button();
            this.ModLabel = new System.Windows.Forms.Label();
            this.ModButton = new System.Windows.Forms.Button();
            this.OriginalLabel = new System.Windows.Forms.Label();
            this.OriginalButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.AuthorTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.APKLabel = new System.Windows.Forms.Label();
            this.APKButton = new System.Windows.Forms.Button();
            this.AndroidCheckBox = new System.Windows.Forms.CheckBox();
            this.YYCCheckBox = new System.Windows.Forms.CheckBox();
            this.SaveCheckBox = new System.Windows.Forms.CheckBox();
            this.MusicCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.CreateLabel);
            this.panel1.Controls.Add(this.CreateButton);
            this.panel1.Controls.Add(this.ModLabel);
            this.panel1.Controls.Add(this.ModButton);
            this.panel1.Controls.Add(this.OriginalLabel);
            this.panel1.Controls.Add(this.OriginalButton);
            this.panel1.Location = new System.Drawing.Point(12, 166);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(291, 86);
            this.panel1.TabIndex = 0;
            // 
            // CreateLabel
            // 
            this.CreateLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.CreateLabel.Location = new System.Drawing.Point(5, 71);
            this.CreateLabel.Name = "CreateLabel";
            this.CreateLabel.Size = new System.Drawing.Size(283, 13);
            this.CreateLabel.TabIndex = 6;
            this.CreateLabel.Text = "Mod package created!";
            this.CreateLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.CreateLabel.Visible = false;
            // 
            // CreateButton
            // 
            this.CreateButton.Enabled = false;
            this.CreateButton.Location = new System.Drawing.Point(3, 45);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(285, 23);
            this.CreateButton.TabIndex = 5;
            this.CreateButton.Text = "Create mod package";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // ModLabel
            // 
            this.ModLabel.AutoSize = true;
            this.ModLabel.Location = new System.Drawing.Point(201, 29);
            this.ModLabel.Name = "ModLabel";
            this.ModLabel.Size = new System.Drawing.Size(66, 13);
            this.ModLabel.TabIndex = 4;
            this.ModLabel.Text = "Mod loaded!";
            this.ModLabel.Visible = false;
            // 
            // ModButton
            // 
            this.ModButton.Location = new System.Drawing.Point(177, 3);
            this.ModButton.Name = "ModButton";
            this.ModButton.Size = new System.Drawing.Size(111, 23);
            this.ModButton.TabIndex = 3;
            this.ModButton.Text = "Load modded game";
            this.ModButton.UseVisualStyleBackColor = true;
            this.ModButton.Click += new System.EventHandler(this.ModButton_Click);
            // 
            // OriginalLabel
            // 
            this.OriginalLabel.AutoSize = true;
            this.OriginalLabel.Location = new System.Drawing.Point(28, 29);
            this.OriginalLabel.Name = "OriginalLabel";
            this.OriginalLabel.Size = new System.Drawing.Size(60, 13);
            this.OriginalLabel.TabIndex = 2;
            this.OriginalLabel.Text = "1.1 loaded!";
            this.OriginalLabel.Visible = false;
            // 
            // OriginalButton
            // 
            this.OriginalButton.Location = new System.Drawing.Point(3, 3);
            this.OriginalButton.Name = "OriginalButton";
            this.OriginalButton.Size = new System.Drawing.Size(111, 23);
            this.OriginalButton.TabIndex = 1;
            this.OriginalButton.Text = "Load 1.1";
            this.OriginalButton.UseVisualStyleBackColor = true;
            this.OriginalButton.Click += new System.EventHandler(this.OriginalButton_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.AuthorTextBox);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.NameTextBox);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Location = new System.Drawing.Point(12, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(291, 51);
            this.panel2.TabIndex = 1;
            // 
            // AuthorTextBox
            // 
            this.AuthorTextBox.Location = new System.Drawing.Point(63, 28);
            this.AuthorTextBox.MaxLength = 30;
            this.AuthorTextBox.Name = "AuthorTextBox";
            this.AuthorTextBox.Size = new System.Drawing.Size(225, 20);
            this.AuthorTextBox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Author:";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(63, 6);
            this.NameTextBox.MaxLength = 30;
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(225, 20);
            this.NameTextBox.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Mod name:";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.APKLabel);
            this.panel3.Controls.Add(this.APKButton);
            this.panel3.Controls.Add(this.AndroidCheckBox);
            this.panel3.Controls.Add(this.YYCCheckBox);
            this.panel3.Controls.Add(this.SaveCheckBox);
            this.panel3.Controls.Add(this.MusicCheckBox);
            this.panel3.Location = new System.Drawing.Point(12, 57);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(291, 111);
            this.panel3.TabIndex = 2;
            // 
            // APKLabel
            // 
            this.APKLabel.AutoSize = true;
            this.APKLabel.Location = new System.Drawing.Point(176, 90);
            this.APKLabel.Name = "APKLabel";
            this.APKLabel.Size = new System.Drawing.Size(112, 13);
            this.APKLabel.TabIndex = 7;
            this.APKLabel.Text = "Modded APK Loaded!";
            this.APKLabel.Visible = false;
            // 
            // APKButton
            // 
            this.APKButton.Enabled = false;
            this.APKButton.Location = new System.Drawing.Point(3, 85);
            this.APKButton.Name = "APKButton";
            this.APKButton.Size = new System.Drawing.Size(143, 23);
            this.APKButton.TabIndex = 6;
            this.APKButton.Text = "Load modded Android APK";
            this.APKButton.UseVisualStyleBackColor = true;
            this.APKButton.Click += new System.EventHandler(this.APKButton_Click);
            // 
            // AndroidCheckBox
            // 
            this.AndroidCheckBox.AutoSize = true;
            this.AndroidCheckBox.Enabled = false;
            this.AndroidCheckBox.Location = new System.Drawing.Point(4, 66);
            this.AndroidCheckBox.Name = "AndroidCheckBox";
            this.AndroidCheckBox.Size = new System.Drawing.Size(107, 17);
            this.AndroidCheckBox.TabIndex = 5;
            this.AndroidCheckBox.Text = "Supports Android";
            this.AndroidCheckBox.UseVisualStyleBackColor = true;
            this.AndroidCheckBox.CheckedChanged += new System.EventHandler(this.AndroidCheckBox_CheckedChanged);
            // 
            // YYCCheckBox
            // 
            this.YYCCheckBox.AutoSize = true;
            this.YYCCheckBox.Location = new System.Drawing.Point(4, 45);
            this.YYCCheckBox.Name = "YYCCheckBox";
            this.YYCCheckBox.Size = new System.Drawing.Size(140, 17);
            this.YYCCheckBox.TabIndex = 4;
            this.YYCCheckBox.Text = "Uses the YoYo Compiler";
            this.YYCCheckBox.UseVisualStyleBackColor = true;
            // 
            // SaveCheckBox
            // 
            this.SaveCheckBox.AutoSize = true;
            this.SaveCheckBox.Location = new System.Drawing.Point(4, 24);
            this.SaveCheckBox.Name = "SaveCheckBox";
            this.SaveCheckBox.Size = new System.Drawing.Size(163, 17);
            this.SaveCheckBox.TabIndex = 3;
            this.SaveCheckBox.Text = "Uses custom savedata folder";
            this.SaveCheckBox.UseVisualStyleBackColor = true;
            // 
            // MusicCheckBox
            // 
            this.MusicCheckBox.AutoSize = true;
            this.MusicCheckBox.Location = new System.Drawing.Point(4, 3);
            this.MusicCheckBox.Margin = new System.Windows.Forms.Padding(2, 3, 3, 3);
            this.MusicCheckBox.Name = "MusicCheckBox";
            this.MusicCheckBox.Size = new System.Drawing.Size(117, 17);
            this.MusicCheckBox.TabIndex = 2;
            this.MusicCheckBox.Text = "Uses custom music";
            this.MusicCheckBox.UseVisualStyleBackColor = true;
            // 
            // ModPacker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(315, 258);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ModPacker";
            this.Text = "AM2R ModPacker";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label OriginalLabel;
        private System.Windows.Forms.Button OriginalButton;
        private System.Windows.Forms.Label ModLabel;
        private System.Windows.Forms.Button ModButton;
        private System.Windows.Forms.Button CreateButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox AuthorTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button APKButton;
        private System.Windows.Forms.CheckBox AndroidCheckBox;
        private System.Windows.Forms.CheckBox YYCCheckBox;
        private System.Windows.Forms.CheckBox SaveCheckBox;
        private System.Windows.Forms.CheckBox MusicCheckBox;
        private System.Windows.Forms.Label APKLabel;
        private System.Windows.Forms.Label CreateLabel;
    }
}

