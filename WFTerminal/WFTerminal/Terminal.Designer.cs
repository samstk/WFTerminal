namespace WFTerminal
{
    partial class Terminal
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
            this.components = new System.ComponentModel.Container();
            this.minRefreshTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // minRefreshTimer
            // 
            this.minRefreshTimer.Enabled = true;
            this.minRefreshTimer.Interval = 500;
            this.minRefreshTimer.Tick += new System.EventHandler(this.minRefreshTimer_Tick);
            // 
            // Terminal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(12)))), ((int)(((byte)(12)))), ((int)(((byte)(12)))));
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.Name = "Terminal";
            this.Size = new System.Drawing.Size(935, 700);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Terminal_KeyDown);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Terminal_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Terminal_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Terminal_MouseUp);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Terminal_PreviewKeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer minRefreshTimer;
    }
}