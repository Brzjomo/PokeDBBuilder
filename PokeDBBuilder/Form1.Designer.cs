namespace PokeDBBuilder
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            TB_1 = new TabPage();
            BTN_Gen = new Button();
            label2 = new Label();
            label1 = new Label();
            BTN_Open_F2 = new Button();
            LLB_File_2 = new LinkLabel();
            BTN_Open_F1 = new Button();
            LLB_File_1 = new LinkLabel();
            tabControl1.SuspendLayout();
            TB_1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(TB_1);
            tabControl1.Location = new Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(468, 257);
            tabControl1.TabIndex = 0;
            // 
            // TB_1
            // 
            TB_1.Controls.Add(BTN_Gen);
            TB_1.Controls.Add(label2);
            TB_1.Controls.Add(label1);
            TB_1.Controls.Add(BTN_Open_F2);
            TB_1.Controls.Add(LLB_File_2);
            TB_1.Controls.Add(BTN_Open_F1);
            TB_1.Controls.Add(LLB_File_1);
            TB_1.Location = new Point(4, 30);
            TB_1.Name = "TB_1";
            TB_1.Padding = new Padding(3);
            TB_1.Size = new Size(460, 223);
            TB_1.TabIndex = 0;
            TB_1.Text = "数据库生成";
            TB_1.UseVisualStyleBackColor = true;
            // 
            // BTN_Gen
            // 
            BTN_Gen.Location = new Point(29, 126);
            BTN_Gen.Name = "BTN_Gen";
            BTN_Gen.Size = new Size(404, 77);
            BTN_Gen.TabIndex = 6;
            BTN_Gen.Text = "生成";
            BTN_Gen.UseVisualStyleBackColor = true;
            BTN_Gen.Click += BTN_Gen_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(29, 77);
            label2.Name = "label2";
            label2.Size = new Size(126, 21);
            label2.TabIndex = 5;
            label2.Text = "宝可梦其他数据:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(29, 28);
            label1.Name = "label1";
            label1.Size = new Size(126, 21);
            label1.TabIndex = 4;
            label1.Text = "宝可梦名称数据:";
            // 
            // BTN_Open_F2
            // 
            BTN_Open_F2.Location = new Point(328, 70);
            BTN_Open_F2.Name = "BTN_Open_F2";
            BTN_Open_F2.Size = new Size(105, 34);
            BTN_Open_F2.TabIndex = 3;
            BTN_Open_F2.Text = "打开";
            BTN_Open_F2.UseVisualStyleBackColor = true;
            BTN_Open_F2.Click += BTN_Open_F2_Click;
            // 
            // LLB_File_2
            // 
            LLB_File_2.ActiveLinkColor = Color.Red;
            LLB_File_2.LinkColor = Color.FromArgb(255, 128, 0);
            LLB_File_2.Location = new Point(161, 77);
            LLB_File_2.Name = "LLB_File_2";
            LLB_File_2.Size = new Size(151, 21);
            LLB_File_2.TabIndex = 2;
            LLB_File_2.TabStop = true;
            LLB_File_2.Text = "未打开";
            LLB_File_2.LinkClicked += LLB_File_2_LinkClicked;
            // 
            // BTN_Open_F1
            // 
            BTN_Open_F1.Location = new Point(328, 21);
            BTN_Open_F1.Name = "BTN_Open_F1";
            BTN_Open_F1.Size = new Size(105, 34);
            BTN_Open_F1.TabIndex = 1;
            BTN_Open_F1.Text = "打开";
            BTN_Open_F1.UseVisualStyleBackColor = true;
            BTN_Open_F1.Click += BTN_Open_F1_Click;
            // 
            // LLB_File_1
            // 
            LLB_File_1.ActiveLinkColor = Color.Red;
            LLB_File_1.LinkColor = Color.FromArgb(255, 128, 0);
            LLB_File_1.Location = new Point(161, 28);
            LLB_File_1.Name = "LLB_File_1";
            LLB_File_1.Size = new Size(151, 21);
            LLB_File_1.TabIndex = 0;
            LLB_File_1.TabStop = true;
            LLB_File_1.Text = "未打开";
            LLB_File_1.LinkClicked += LLB_File_1_LinkClicked;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(494, 282);
            Controls.Add(tabControl1);
            Font = new Font("Microsoft YaHei UI", 12F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(4);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PokeDBBuilder";
            tabControl1.ResumeLayout(false);
            TB_1.ResumeLayout(false);
            TB_1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage TB_1;
        private Button BTN_Open_F1;
        private LinkLabel LLB_File_1;
        private Button BTN_Open_F2;
        private LinkLabel LLB_File_2;
        private Label label2;
        private Label label1;
        private Button BTN_Gen;
    }
}
