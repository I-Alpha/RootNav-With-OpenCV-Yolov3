namespace RSMLx
{
    partial class TagListControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tagListView = new System.Windows.Forms.ListView();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.tagListView.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tagListView.Location = new System.Drawing.Point(3, 3);
            this.tagListView.Name = "listView1";
            this.tagListView.Size = new System.Drawing.Size(229, 397);
            this.tagListView.TabIndex = 0;
            this.tagListView.TileSize = new System.Drawing.Size(168, 20);
            this.tagListView.UseCompatibleStateImageBehavior = false;
            this.tagListView.View = System.Windows.Forms.View.Tile;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(3, 436);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(229, 53);
            this.button1.TabIndex = 1;
            this.button1.Text = "Measure";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(50, 406);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(182, 24);
            this.textBox1.TabIndex = 2;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 409);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Filter:";
            // 
            // TagListControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tagListView);
            this.Name = "TagListControl";
            this.Size = new System.Drawing.Size(235, 508);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.ListView tagListView;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;


    }
}
