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
            components = new System.ComponentModel.Container();
            minRefreshTimer = new System.Windows.Forms.Timer(components);
            streamTimer = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // minRefreshTimer
            // 
            minRefreshTimer.Enabled = true;
            minRefreshTimer.Interval = 500;
            minRefreshTimer.Tick += minRefreshTimer_Tick;
            // 
            // streamTimer
            // 
            streamTimer.Enabled = true;
            streamTimer.Interval = 30;
            streamTimer.Tick += streamTimer_Tick;
            // 
            // Terminal
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(12, 12, 12);
            ForeColor = Color.FromArgb(224, 224, 224);
            Margin = new Padding(3, 2, 3, 2);
            Name = "Terminal";
            Size = new Size(818, 525);
            KeyDown += Terminal_KeyDown;
            MouseDown += Terminal_MouseDown;
            MouseMove += Terminal_MouseMove;
            MouseUp += Terminal_MouseUp;
            PreviewKeyDown += Terminal_PreviewKeyDown;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer minRefreshTimer;
        private System.Windows.Forms.Timer streamTimer;
    }
}