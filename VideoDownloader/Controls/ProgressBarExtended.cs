using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoDownloader.Core;

namespace VideoDownloader.Controls
{
    public partial class ProgressBarExtended : ProgressBar
    {
        private double _preciseValue = 0;
        private string _speed = string.Empty;
        private string _eta = string.Empty;

        public Color BaseBackColor;

        public double PreciseValue
        {
            get => _preciseValue;
            set
            {
                _preciseValue = Math.Clamp(value, Minimum, Maximum);
                int intVal = (int)_preciseValue;
                if (intVal >= Minimum && intVal <= Maximum)
                    base.Value = intVal;

                this.Invalidate(); // Wymusza przerysowanie
            }
        }

        public string Speed
        { 
            get => _speed;
            set => _speed = value;
        }

        public string ETA
        {
            get => _eta;
            set => _eta = value;
        }

        public ProgressBarExtended()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint |
                  ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            BaseBackColor = BackColor;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;

            Graphics g = e.Graphics;
            Rectangle rect = ClientRectangle;

            ProgressBarRenderer.DrawHorizontalBar(g, rect);

            // Używamy PreciseValue do obliczenia szerokości paska
            double percentage = Maximum > Minimum ? (PreciseValue - Minimum) / (Maximum - Minimum) : 0;
            int chunkWidth = (int)(rect.Width * percentage);

            if (chunkWidth > 0)
            {
                Rectangle chunksRect = new Rectangle(0, 0, chunkWidth, rect.Height);
                ProgressBarRenderer.DrawHorizontalChunks(g, chunksRect);
            }

            List<string> progressInfo = new List<string>();
            progressInfo.Add(string.Format(Localization.T("Progress: {0:F1} %"), PreciseValue));
            if (!string.IsNullOrEmpty(Speed))
                progressInfo.Add(string.Format(Localization.T("Speed: {0}"), Speed));
            if (!string.IsNullOrEmpty(ETA))
                progressInfo.Add(string.Format(Localization.T("ETA: {0}"), ETA));
            string progressInfoText = string.Join("\t\t", progressInfo);

            using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(progressInfoText, font);
                float x = (rect.Width - textSize.Width) / 2;
                float y = (rect.Height - textSize.Height) / 2;

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.DrawString(progressInfoText, font, Brushes.Black, x, y);
            }
        }
    }
}
