namespace PaddleOCR.TotalStation
{
    partial class Test
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
            button1 = new Button();
            button2 = new Button();
            RealPlayWnd = new PictureBox();
            button3 = new Button();
            button4 = new Button();
            button5 = new Button();
            button6 = new Button();
            pictureBox1 = new PictureBox();
            pictureBox2 = new PictureBox();
            richTextBoxInfo = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)RealPlayWnd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(1228, 12);
            button1.Name = "button1";
            button1.Size = new Size(123, 53);
            button1.TabIndex = 0;
            button1.Text = "开始";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(1228, 71);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 1;
            button2.Text = "扫描";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // RealPlayWnd
            // 
            RealPlayWnd.BackColor = SystemColors.AppWorkspace;
            RealPlayWnd.Location = new Point(11, 12);
            RealPlayWnd.Margin = new Padding(2, 3, 2, 3);
            RealPlayWnd.Name = "RealPlayWnd";
            RealPlayWnd.Size = new Size(1200, 675);
            RealPlayWnd.SizeMode = PictureBoxSizeMode.StretchImage;
            RealPlayWnd.TabIndex = 5;
            RealPlayWnd.TabStop = false;
            // 
            // button3
            // 
            button3.Location = new Point(1228, 652);
            button3.Name = "button3";
            button3.Size = new Size(92, 35);
            button3.TabIndex = 6;
            button3.Text = "云台控制";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button4
            // 
            button4.Location = new Point(1228, 100);
            button4.Name = "button4";
            button4.Size = new Size(75, 23);
            button4.TabIndex = 7;
            button4.Text = "相机寻找模型中心点";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // button5
            // 
            button5.Location = new Point(1228, 129);
            button5.Name = "button5";
            button5.Size = new Size(75, 23);
            button5.TabIndex = 8;
            button5.Text = "激光寻找模型中i先弄点";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // button6
            // 
            button6.Location = new Point(1228, 158);
            button6.Name = "button6";
            button6.Size = new Size(75, 23);
            button6.TabIndex = 9;
            button6.Text = "测试按钮";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = SystemColors.AppWorkspace;
            pictureBox1.Location = new Point(11, 707);
            pictureBox1.Margin = new Padding(2, 3, 2, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(522, 273);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 45;
            pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.BackColor = SystemColors.AppWorkspace;
            pictureBox2.Location = new Point(575, 707);
            pictureBox2.Margin = new Padding(2, 3, 2, 3);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(522, 273);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.TabIndex = 46;
            pictureBox2.TabStop = false;
            // 
            // richTextBoxInfo
            // 
            richTextBoxInfo.Location = new Point(1228, 187);
            richTextBoxInfo.Name = "richTextBoxInfo";
            richTextBoxInfo.Size = new Size(365, 459);
            richTextBoxInfo.TabIndex = 47;
            richTextBoxInfo.Text = "";
            // 
            // Test
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1605, 992);
            Controls.Add(richTextBoxInfo);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            Controls.Add(button6);
            Controls.Add(button5);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(RealPlayWnd);
            Controls.Add(button2);
            Controls.Add(button1);
            Name = "Test";
            Text = "Test";
            ((System.ComponentModel.ISupportInitialize)RealPlayWnd).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button button2;
        private PictureBox RealPlayWnd;
        private Button button3;
        private Button button4;
        private Button button5;
        private Button button6;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private RichTextBox richTextBoxInfo;
    }
}