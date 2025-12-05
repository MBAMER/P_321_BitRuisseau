namespace P_Bit_Ruisseau
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
            label1 = new Label();
            panel2 = new Panel();
            panel3 = new Panel();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            button1 = new Button();
            dataGridView1 = new DataGridView();
            Titre = new DataGridViewTextBoxColumn();
            Artiste = new DataGridViewTextBoxColumn();
            Année = new DataGridViewTextBoxColumn();
            Durée = new DataGridViewTextBoxColumn();
            Taille = new DataGridViewTextBoxColumn();
            Featuring = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = SystemColors.ActiveCaptionText;
            label1.Font = new Font("Segoe UI", 18F);
            label1.ForeColor = SystemColors.ButtonFace;
            label1.Location = new Point(662, 9);
            label1.Name = "label1";
            label1.Size = new Size(168, 32);
            label1.TabIndex = 0;
            label1.Text = "P_Bit_Ruisseau";
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ButtonFace;
            panel2.Location = new Point(821, 286);
            panel2.Name = "panel2";
            panel2.Size = new Size(249, 322);
            panel2.TabIndex = 2;
            // 
            // panel3
            // 
            panel3.BackColor = SystemColors.ButtonFace;
            panel3.Location = new Point(1111, 286);
            panel3.Name = "panel3";
            panel3.Size = new Size(249, 322);
            panel3.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(314, 71);
            label2.Name = "label2";
            label2.Size = new Size(38, 15);
            label2.TabIndex = 3;
            label2.Text = "label2";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15F);
            label3.ForeColor = SystemColors.ButtonFace;
            label3.Location = new Point(54, 169);
            label3.Name = "label3";
            label3.Size = new Size(57, 28);
            label3.TabIndex = 4;
            label3.Text = "Local";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15F);
            label4.ForeColor = SystemColors.ButtonFace;
            label4.Location = new Point(821, 255);
            label4.Name = "label4";
            label4.Size = new Size(128, 28);
            label4.TabIndex = 5;
            label4.Text = "Médiathèque";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 15F);
            label5.ForeColor = SystemColors.ButtonFace;
            label5.Location = new Point(1111, 255);
            label5.Name = "label5";
            label5.Size = new Size(223, 28);
            label5.TabIndex = 6;
            label5.Text = "Nom de la médiathèque";
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 15F);
            button1.Location = new Point(54, 29);
            button1.Name = "button1";
            button1.Size = new Size(177, 40);
            button1.TabIndex = 7;
            button1.Text = "Choisir un dossier";
            button1.UseVisualStyleBackColor = true;
            button1.Click += ChoixDossier;
            // 
            // dataGridView1
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Titre, Artiste, Année, Durée, Taille, Featuring });
            dataGridView1.Cursor = Cursors.No;
            dataGridView1.ImeMode = ImeMode.NoControl;
            dataGridView1.Location = new Point(54, 200);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.Size = new Size(602, 408);
            dataGridView1.TabIndex = 8;
            dataGridView1.TabStop = false;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            // 
            // Titre
            // 
            Titre.FillWeight = 137.7295F;
            Titre.HeaderText = "Titre";
            Titre.Name = "Titre";
            // 
            // Artiste
            // 
            Artiste.FillWeight = 300.31073F;
            Artiste.HeaderText = "Artiste";
            Artiste.Name = "Artiste";
            // 
            // Année
            // 
            Année.FillWeight = 282.284546F;
            Année.HeaderText = "Année";
            Année.Name = "Année";
            // 
            // Durée
            // 
            Durée.FillWeight = 254.140228F;
            Durée.HeaderText = "Durée";
            Durée.Name = "Durée";
            // 
            // Taille
            // 
            Taille.FillWeight = 222.5958F;
            Taille.HeaderText = "Taille";
            Taille.Name = "Taille";
            // 
            // Featuring
            // 
            Featuring.FillWeight = 302.938782F;
            Featuring.HeaderText = "Featuring";
            Featuring.Name = "Featuring";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = SystemColors.ActiveCaptionText;
            ClientSize = new Size(1490, 657);
            Controls.Add(dataGridView1);
            Controls.Add(button1);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Panel panel2;
        private Panel panel3;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Button button1;
        protected DataGridView dataGridView1;
        private DataGridViewTextBoxColumn Titre;
        private DataGridViewTextBoxColumn Artiste;
        private DataGridViewTextBoxColumn Année;
        private DataGridViewTextBoxColumn Durée;
        private DataGridViewTextBoxColumn Taille;
        private DataGridViewTextBoxColumn Featuring;
    }
}
