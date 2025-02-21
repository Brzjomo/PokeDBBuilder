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
            label3 = new Label();
            BTN_Open_F3 = new Button();
            LLB_File_3 = new LinkLabel();
            BTN_Gen = new Button();
            label2 = new Label();
            label1 = new Label();
            BTN_Open_F2 = new Button();
            LLB_File_2 = new LinkLabel();
            BTN_Open_F1 = new Button();
            LLB_File_1 = new LinkLabel();
            TB_2 = new TabPage();
            TB_Info = new TextBox();
            button1 = new Button();
            label4 = new Label();
            BTN_Open_F4 = new Button();
            LLB_File_4 = new LinkLabel();
            tabControl1.SuspendLayout();
            TB_1.SuspendLayout();
            TB_2.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(TB_1);
            tabControl1.Controls.Add(TB_2);
            tabControl1.Location = new Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(468, 373);
            tabControl1.TabIndex = 0;
            // 
            // TB_1
            // 
            TB_1.Controls.Add(label4);
            TB_1.Controls.Add(BTN_Open_F4);
            TB_1.Controls.Add(LLB_File_4);
            TB_1.Controls.Add(label3);
            TB_1.Controls.Add(BTN_Open_F3);
            TB_1.Controls.Add(LLB_File_3);
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
            TB_1.Size = new Size(460, 339);
            TB_1.TabIndex = 0;
            TB_1.Text = "从txt生成";
            TB_1.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(29, 75);
            label3.Name = "label3";
            label3.Size = new Size(122, 21);
            label3.TabIndex = 9;
            label3.Text = "Mega名称数据:";
            // 
            // BTN_Open_F3
            // 
            BTN_Open_F3.Location = new Point(328, 68);
            BTN_Open_F3.Name = "BTN_Open_F3";
            BTN_Open_F3.Size = new Size(105, 34);
            BTN_Open_F3.TabIndex = 8;
            BTN_Open_F3.Text = "打开";
            BTN_Open_F3.UseVisualStyleBackColor = true;
            BTN_Open_F3.Click += BTN_Open_F3_Click;
            // 
            // LLB_File_3
            // 
            LLB_File_3.ActiveLinkColor = Color.Red;
            LLB_File_3.LinkColor = Color.FromArgb(255, 128, 0);
            LLB_File_3.Location = new Point(161, 75);
            LLB_File_3.Name = "LLB_File_3";
            LLB_File_3.Size = new Size(151, 21);
            LLB_File_3.TabIndex = 7;
            LLB_File_3.TabStop = true;
            LLB_File_3.Text = "未打开";
            LLB_File_3.LinkClicked += LLB_File_3_LinkClicked;
            // 
            // BTN_Gen
            // 
            BTN_Gen.Location = new Point(29, 233);
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
            label2.Location = new Point(29, 123);
            label2.Name = "label2";
            label2.Size = new Size(126, 21);
            label2.TabIndex = 5;
            label2.Text = "宝可梦个体数据:";
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
            BTN_Open_F2.Location = new Point(328, 116);
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
            LLB_File_2.Location = new Point(161, 123);
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
            // TB_2
            // 
            TB_2.Controls.Add(TB_Info);
            TB_2.Controls.Add(button1);
            TB_2.Location = new Point(4, 26);
            TB_2.Name = "TB_2";
            TB_2.Padding = new Padding(3);
            TB_2.Size = new Size(460, 270);
            TB_2.TabIndex = 1;
            TB_2.Text = "从网络生成";
            TB_2.UseVisualStyleBackColor = true;
            // 
            // TB_Info
            // 
            TB_Info.Location = new Point(6, 6);
            TB_Info.Multiline = true;
            TB_Info.Name = "TB_Info";
            TB_Info.ReadOnly = true;
            TB_Info.ScrollBars = ScrollBars.Vertical;
            TB_Info.Size = new Size(448, 198);
            TB_Info.TabIndex = 1;
            // 
            // button1
            // 
            button1.Location = new Point(304, 210);
            button1.Name = "button1";
            button1.Size = new Size(150, 50);
            button1.TabIndex = 0;
            button1.Text = "生成";
            button1.UseVisualStyleBackColor = true;
            button1.Click += Button1_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(29, 171);
            label4.Name = "label4";
            label4.Size = new Size(126, 21);
            label4.TabIndex = 12;
            label4.Text = "宝可梦进化数据:";
            // 
            // BTN_Open_F4
            // 
            BTN_Open_F4.Location = new Point(328, 164);
            BTN_Open_F4.Name = "BTN_Open_F4";
            BTN_Open_F4.Size = new Size(105, 34);
            BTN_Open_F4.TabIndex = 11;
            BTN_Open_F4.Text = "打开";
            BTN_Open_F4.UseVisualStyleBackColor = true;
            BTN_Open_F4.Click += BTN_Open_F4_Click;
            // 
            // LLB_File_4
            // 
            LLB_File_4.ActiveLinkColor = Color.Red;
            LLB_File_4.LinkColor = Color.FromArgb(255, 128, 0);
            LLB_File_4.Location = new Point(161, 171);
            LLB_File_4.Name = "LLB_File_4";
            LLB_File_4.Size = new Size(151, 21);
            LLB_File_4.TabIndex = 10;
            LLB_File_4.TabStop = true;
            LLB_File_4.Text = "未打开";
            LLB_File_4.LinkClicked += LLB_File_4_LinkClicked;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(494, 397);
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
            TB_2.ResumeLayout(false);
            TB_2.PerformLayout();
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
        private Label label3;
        private Button BTN_Open_F3;
        private LinkLabel LLB_File_3;
        private TabPage TB_2;
        private Button button1;
        private TextBox TB_Info;
        private Label label4;
        private Button BTN_Open_F4;
        private LinkLabel LLB_File_4;
    }
}
