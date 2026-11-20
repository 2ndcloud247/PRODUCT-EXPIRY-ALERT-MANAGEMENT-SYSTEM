using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ExpiryAlertSystem.UI
{
    /// <summary>
    /// Draws the application logo (a calendar with an alert bell badge) in code,
    /// so no image files are needed.
    /// </summary>
    public static class AppIcon
    {
        private static Icon _icon;

        public static Bitmap Bitmap(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                float s = size / 64f;

                // calendar body
                using (var body = Rounded(new RectangleF(4 * s, 10 * s, 50 * s, 48 * s), 7 * s))
                using (var white = new SolidBrush(Color.White))
                    g.FillPath(white, body);
                using (var top = Rounded(new RectangleF(4 * s, 10 * s, 50 * s, 14 * s), 7 * s))
                using (var teal = new SolidBrush(Theme.Primary))
                    g.FillPath(teal, top);
                using (var teal = new SolidBrush(Theme.Primary))
                    g.FillRectangle(teal, 4 * s, 18 * s, 50 * s, 6 * s);

                // rings
                using (var ring = new SolidBrush(Theme.Sidebar))
                {
                    g.FillRectangle(ring, 15 * s, 5 * s, 5 * s, 11 * s);
                    g.FillRectangle(ring, 38 * s, 5 * s, 5 * s, 11 * s);
                }

                // date grid
                using (var dot = new SolidBrush(Color.FromArgb(176, 190, 197)))
                    for (int r = 0; r < 3; r++)
                        for (int c = 0; c < 4; c++)
                            g.FillRectangle(dot, (10 + c * 10) * s, (29 + r * 9) * s, 6 * s, 5 * s);

                // alert badge
                using (var red = new SolidBrush(Theme.ExpiredColor))
                    g.FillEllipse(red, 36 * s, 36 * s, 27 * s, 27 * s);
                using (var f = new Font("Segoe UI", 15 * s, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var w = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("!", f, w, new RectangleF(36 * s, 37 * s, 27 * s, 27 * s), sf);
                }
            }
            return bmp;
        }

        public static Icon Get()
        {
            if (_icon == null)
            {
                using (var bmp = Bitmap(64))
                    _icon = Icon.FromHandle(bmp.GetHicon());
            }
            return _icon;
        }

        private static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
