// Etiket Yazdır - termal etiket yazıcısı için metin/görsel çıktı programı
// Hazırlayan: Önsel Özden
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Etiket Yazdır")]
[assembly: System.Reflection.AssemblyProduct("Etiket Yazdır")]
[assembly: System.Reflection.AssemblyCompany("Önsel Özden")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace EtiketYazdir
{
    class Ayarlar
    {
        public string Yazici = "";
        public int GenislikMm = 85;
        public bool SabitBoy = false;
        public decimal BoyMm = 50;
        public decimal KenarMm = 2;
        public decimal KaydirmaMm = 0;
        public bool KagitGonder = true;
        public int Dpi = 203;
        public bool Koyu = false;
        public string Font = "Arial";
        public decimal Boyut = 14;
        public bool Kalin = true;
        public bool OtoBoyut = true;
        public int Hiza = 1; // 0 sol 1 orta 2 sağ
        public bool GorselAltta = false;
        public bool Sigdir = true;
        public bool Tarama = true;

        static string Yol
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EtiketYazdir", "ayarlar.ini");
            }
        }

        public static Ayarlar Yukle()
        {
            Ayarlar a = new Ayarlar();
            try
            {
                if (!File.Exists(Yol)) return a;
                Dictionary<string, string> d = new Dictionary<string, string>();
                foreach (string l in File.ReadAllLines(Yol))
                {
                    int i = l.IndexOf('=');
                    if (i > 0) d[l.Substring(0, i)] = l.Substring(i + 1);
                }
                CultureInfo c = CultureInfo.InvariantCulture;
                string v;
                if (d.TryGetValue("Yazici", out v)) a.Yazici = v;
                if (d.TryGetValue("GenislikMm", out v)) a.GenislikMm = int.Parse(v);
                if (d.TryGetValue("SabitBoy", out v)) a.SabitBoy = v == "1";
                if (d.TryGetValue("BoyMm", out v)) a.BoyMm = decimal.Parse(v, c);
                if (d.TryGetValue("KenarMm", out v)) a.KenarMm = decimal.Parse(v, c);
                if (d.TryGetValue("KaydirmaMm", out v)) a.KaydirmaMm = decimal.Parse(v, c);
                if (d.TryGetValue("KagitGonder", out v)) a.KagitGonder = v == "1";
                if (d.TryGetValue("Dpi", out v)) a.Dpi = int.Parse(v);
                if (d.TryGetValue("Koyu", out v)) a.Koyu = v == "1";
                if (d.TryGetValue("Font", out v)) a.Font = v;
                if (d.TryGetValue("Boyut", out v)) a.Boyut = decimal.Parse(v, c);
                if (d.TryGetValue("Kalin", out v)) a.Kalin = v == "1";
                if (d.TryGetValue("OtoBoyut", out v)) a.OtoBoyut = v == "1";
                if (d.TryGetValue("Hiza", out v)) a.Hiza = int.Parse(v);
                if (d.TryGetValue("GorselAltta", out v)) a.GorselAltta = v == "1";
                if (d.TryGetValue("Sigdir", out v)) a.Sigdir = v == "1";
                if (d.TryGetValue("Tarama", out v)) a.Tarama = v == "1";
            }
            catch { }
            return a;
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Yol));
                CultureInfo c = CultureInfo.InvariantCulture;
                string[] l = {
                    "Yazici=" + Yazici, "GenislikMm=" + GenislikMm, "SabitBoy=" + (SabitBoy ? 1 : 0),
                    "BoyMm=" + BoyMm.ToString(c), "KenarMm=" + KenarMm.ToString(c),
                    "KaydirmaMm=" + KaydirmaMm.ToString(c), "KagitGonder=" + (KagitGonder ? 1 : 0),
                    "Dpi=" + Dpi, "Koyu=" + (Koyu ? 1 : 0), "Font=" + Font, "Boyut=" + Boyut.ToString(c),
                    "Kalin=" + (Kalin ? 1 : 0), "OtoBoyut=" + (OtoBoyut ? 1 : 0),"Hiza=" + Hiza, "GorselAltta=" + (GorselAltta ? 1 : 0),
                    "Sigdir=" + (Sigdir ? 1 : 0), "Tarama=" + (Tarama ? 1 : 0)
                };
                File.WriteAllLines(Yol, l);
            }
            catch { }
        }
    }

    static class Cizim
    {
        public static int Px(double mm, int dpi) { return Math.Max(1, (int)Math.Round(mm / 25.4 * dpi)); }

        // Gri tonlamaya çevirir; tarama=true ise Floyd-Steinberg, değilse eşik
        public static Bitmap SiyahBeyaz(Bitmap src, bool tarama)
        {
            int w = src.Width, h = src.Height;
            Bitmap b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.White);
                g.DrawImage(src, 0, 0, w, h);
            }
            Rectangle r = new Rectangle(0, 0, w, h);
            BitmapData bd = b.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = bd.Stride;
            byte[] p = new byte[stride * h];
            Marshal.Copy(bd.Scan0, p, 0, p.Length);
            float[] gray = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * stride + x * 4;
                    gray[y * w + x] = p[i + 2] * 0.299f + p[i + 1] * 0.587f + p[i] * 0.114f;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int k = y * w + x;
                    float o = gray[k];
                    float n = o < (tarama ? 128 : 140) ? 0 : 255;
                    if (tarama)
                    {
                        float e = o - n;
                        if (x + 1 < w) gray[k + 1] += e * 7 / 16;
                        if (y + 1 < h)
                        {
                            if (x > 0) gray[k + w - 1] += e * 3 / 16;
                            gray[k + w] += e * 5 / 16;
                            if (x + 1 < w) gray[k + w + 1] += e * 1 / 16;
                        }
                    }
                    byte v = (byte)n;
                    int i = y * stride + x * 4;
                    p[i] = v; p[i + 1] = v; p[i + 2] = v; p[i + 3] = 255;
                }
            Marshal.Copy(p, 0, bd.Scan0, p.Length);
            b.UnlockBits(bd);
            return b;
        }
    }

    class AnaForm : Form
    {
        Ayarlar ay;
        Bitmap gorsel;
        Bitmap sonuc;
        int sonucDpi = 203;
        Dictionary<string, int> dpiOnbellek = new Dictionary<string, int>();
        Timer zamanlayici = new Timer();

        TextBox txt;
        ComboBox cbFont;
        NumericUpDown nBoyut, nKopya;
        CheckBox chKalin, chSigdir, chTarama, chOto;
        float sonBoyut;
        RadioButton rSol, rOrta, rSag, rUst, rAlt;
        Label lblGorsel;
        PictureBox onizleme;
        ToolStripStatusLabel durum;
        MenuStrip menu;

        public AnaForm()
        {
            ay = Ayarlar.Yukle();
            Text = "Etiket Yazdır - Önsel";
            Font = new Font("Segoe UI", 9.5f);
            Size = new Size(1040, 680);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Arayuz();
            TemaUygula();
            zamanlayici.Interval = 250;
            zamanlayici.Tick += delegate { zamanlayici.Stop(); Yenile(); };
            KeyDown += Form_KeyDown;
            FormClosing += delegate { Topla(); ay.Kaydet(); };
            Shown += delegate { Yenile(); };
        }

        // ---------- arayüz ----------
        void Arayuz()
        {
            menu = new MenuStrip();
            ToolStripMenuItem dosya = new ToolStripMenuItem("Dosya");
            dosya.DropDownItems.Add("Görsel aç...", null, delegate { GorselAc(); });
            dosya.DropDownItems.Add("Görsel yapıştır (Ctrl+V)", null, delegate { GorselYapistir(true); });
            dosya.DropDownItems.Add("Önizlemeyi PNG kaydet...", null, delegate { PngKaydet(); });
            dosya.DropDownItems.Add(new ToolStripSeparator());
            dosya.DropDownItems.Add("Yazdır (Ctrl+P)", null, delegate { Yazdir(); });
            dosya.DropDownItems.Add(new ToolStripSeparator());
            dosya.DropDownItems.Add("Çıkış", null, delegate { Close(); });
            menu.Items.Add(dosya);
            menu.Items.Add(new ToolStripMenuItem("Ayarlar", null, delegate { AyarlarAc(); }));
            menu.Items.Add(new ToolStripMenuItem("Hakkında", null, delegate
            {
                MessageBox.Show(this, "Etiket Yazdır v1.0\n\nTermal etiket yazıcıları için\nmetin ve görsel çıktı programı.\n\nHazırlayan: Önsel Özden",
                    "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }));

            StatusStrip ss = new StatusStrip();
            durum = new ToolStripStatusLabel();
            durum.Spring = true;
            durum.TextAlign = ContentAlignment.MiddleLeft;
            ss.Items.Add(durum);

            TableLayoutPanel ana = new TableLayoutPanel();
            ana.Dock = DockStyle.Fill;
            ana.Padding = new Padding(10);
            ana.ColumnCount = 2;
            ana.RowCount = 1;
            ana.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            ana.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // sol panel
            TableLayoutPanel sol = new TableLayoutPanel();
            sol.Dock = DockStyle.Fill;
            sol.ColumnCount = 1;
            sol.Margin = new Padding(0, 0, 10, 0);
            sol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            for (int i = 0; i < 7; i++) sol.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            sol.Controls.Add(Baslik("Metin"));
            txt = new TextBox();
            txt.Multiline = true;
            txt.AcceptsReturn = true;
            txt.ScrollBars = ScrollBars.Vertical;
            txt.Dock = DockStyle.Fill;
            txt.Font = new Font("Segoe UI", 12f);
            txt.TextChanged += delegate { Planla(); };
            sol.Controls.Add(txt);

            FlowLayoutPanel f1 = Akis();
            f1.Controls.Add(Etiket("Yazı tipi"));
            cbFont = new ComboBox();
            cbFont.DropDownStyle = ComboBoxStyle.DropDownList;
            cbFont.Width = 170;
            foreach (FontFamily ff in FontFamily.Families) cbFont.Items.Add(ff.Name);
            cbFont.SelectedItem = ay.Font;
            if (cbFont.SelectedIndex < 0) cbFont.SelectedItem = "Arial";
            if (cbFont.SelectedIndex < 0 && cbFont.Items.Count > 0) cbFont.SelectedIndex = 0;
            cbFont.SelectedIndexChanged += delegate { Planla(); };
            f1.Controls.Add(cbFont);
            f1.Controls.Add(Etiket("Boyut (pt)"));
            nBoyut = new NumericUpDown();
            nBoyut.Minimum = 4; nBoyut.Maximum = 200; nBoyut.Width = 60;
            nBoyut.Value = Math.Max(4, Math.Min(200, ay.Boyut));
            nBoyut.ValueChanged += delegate { Planla(); };
            f1.Controls.Add(nBoyut);
            chKalin = Kutu("Kalın", ay.Kalin);
            f1.Controls.Add(chKalin);
            chOto = Kutu("Otomatik boyut (satıra yay)", ay.OtoBoyut);
            chOto.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            nBoyut.Enabled = !ay.OtoBoyut;
            chOto.CheckedChanged += delegate { nBoyut.Enabled = !chOto.Checked; };
            f1.Controls.Add(chOto);
            sol.Controls.Add(f1);

            FlowLayoutPanel f2 = Akis();
            f2.Controls.Add(Etiket("Hizalama"));
            rSol = Radyo("Sol", ay.Hiza == 0); rOrta = Radyo("Orta", ay.Hiza == 1); rSag = Radyo("Sağ", ay.Hiza == 2);
            f2.Controls.Add(rSol); f2.Controls.Add(rOrta); f2.Controls.Add(rSag);
            sol.Controls.Add(f2);

            sol.Controls.Add(Baslik("Görsel"));
            FlowLayoutPanel f3 = Akis();
            f3.Controls.Add(Dugme("Aç...", delegate { GorselAc(); }));
            f3.Controls.Add(Dugme("Yapıştır", delegate { GorselYapistir(true); }));
            f3.Controls.Add(Dugme("Döndür ⟳", delegate
            {
                if (gorsel != null) { gorsel.RotateFlip(RotateFlipType.Rotate90FlipNone); GorselEtiket("Görsel"); Planla(); }
            }));
            f3.Controls.Add(Dugme("Kaldır", delegate { GorselKaldir(); }));
            lblGorsel = Etiket("Görsel yok");
            f3.Controls.Add(lblGorsel);
            sol.Controls.Add(f3);

            // Görsel konum radyoları ayrı panelde (hizalama gruplarıyla karışmasın)
            FlowLayoutPanel f4 = Akis();
            f4.Controls.Add(Etiket("Konum"));
            Panel grp = new Panel();
            grp.AutoSize = true;
            grp.Margin = new Padding(0);
            rUst = Radyo("Metnin üstü", !ay.GorselAltta);
            rAlt = Radyo("Metnin altı", ay.GorselAltta);
            rUst.Location = new Point(0, 0);
            rAlt.Location = new Point(rUst.PreferredSize.Width + 8, 0);
            grp.Controls.Add(rUst); grp.Controls.Add(rAlt);
            f4.Controls.Add(grp);
            chSigdir = Kutu("Genişliğe sığdır", ay.Sigdir);
            chTarama = Kutu("Fotoğraf (tarama)", ay.Tarama);
            f4.Controls.Add(chSigdir); f4.Controls.Add(chTarama);
            sol.Controls.Add(f4);

            FlowLayoutPanel f5 = Akis();
            f5.Controls.Add(Etiket("Kopya"));
            nKopya = new NumericUpDown();
            nKopya.Minimum = 1; nKopya.Maximum = 999; nKopya.Width = 60;
            f5.Controls.Add(nKopya);
            f5.Controls.Add(Dugme("Temizle", delegate { txt.Clear(); GorselKaldir(); }));
            Button yaz = Dugme("🖨  YAZDIR", delegate { Yazdir(); });
            yaz.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            yaz.Size = new Size(150, 40);
            yaz.Tag = "vurgu";
            f5.Controls.Add(yaz);
            sol.Controls.Add(f5);

            // sağ panel
            TableLayoutPanel sag = new TableLayoutPanel();
            sag.Dock = DockStyle.Fill;
            sag.ColumnCount = 1;
            sag.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sag.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sag.Controls.Add(Baslik("Önizleme"));
            onizleme = new PictureBox();
            onizleme.Dock = DockStyle.Fill;
            onizleme.SizeMode = PictureBoxSizeMode.CenterImage;
            onizleme.Resize += delegate { OnizlemeGoster(); };
            sag.Controls.Add(onizleme);

            ana.Controls.Add(sol, 0, 0);
            ana.Controls.Add(sag, 1, 0);

            Controls.Add(ana);
            Controls.Add(ss);
            Controls.Add(menu);
            MainMenuStrip = menu;
        }

        Label Baslik(string s)
        {
            Label l = new Label();
            l.Text = s; l.AutoSize = true;
            l.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            l.Margin = new Padding(0, 6, 0, 4);
            return l;
        }
        Label Etiket(string s)
        {
            Label l = new Label();
            l.Text = s; l.AutoSize = true;
            l.Margin = new Padding(3, 7, 3, 3);
            return l;
        }
        FlowLayoutPanel Akis()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.AutoSize = true; f.Dock = DockStyle.Fill; f.WrapContents = true;
            f.Margin = new Padding(0, 2, 0, 2);
            return f;
        }
        CheckBox Kutu(string s, bool v)
        {
            CheckBox c = new CheckBox();
            c.Text = s; c.Checked = v; c.AutoSize = true;
            c.Margin = new Padding(8, 6, 3, 3);
            c.CheckedChanged += delegate { Planla(); };
            return c;
        }
        RadioButton Radyo(string s, bool v)
        {
            RadioButton r = new RadioButton();
            r.Text = s; r.Checked = v; r.AutoSize = true;
            r.Margin = new Padding(6, 6, 3, 3);
            r.CheckedChanged += delegate { Planla(); };
            return r;
        }
        Button Dugme(string s, EventHandler h)
        {
            Button b = new Button();
            b.Text = s; b.AutoSize = true; b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Padding = new Padding(6, 3, 6, 3);
            b.Click += h;
            return b;
        }

        void TemaUygula()
        {
            Color bg = ay.Koyu ? Color.FromArgb(30, 30, 30) : Color.FromArgb(243, 243, 243);
            Color fg = ay.Koyu ? Color.FromArgb(238, 238, 238) : Color.FromArgb(17, 17, 17);
            Color alan = ay.Koyu ? Color.FromArgb(43, 43, 43) : Color.White;
            Color btn = ay.Koyu ? Color.FromArgb(58, 58, 58) : Color.FromArgb(225, 225, 225);
            Color vurgu = ay.Koyu ? Color.FromArgb(61, 139, 253) : Color.FromArgb(10, 100, 200);
            Color tuval = ay.Koyu ? Color.FromArgb(18, 18, 18) : Color.FromArgb(217, 217, 217);
            Boya(this, bg, fg, alan, btn, vurgu);
            onizleme.BackColor = tuval;
            menu.BackColor = bg; menu.ForeColor = fg;
            menu.Renderer = new ToolStripProfessionalRenderer(new Renkler(bg, btn));
            foreach (ToolStripItem it in menu.Items)
            {
                it.ForeColor = fg;
                ToolStripMenuItem mi = it as ToolStripMenuItem;
                if (mi != null) foreach (ToolStripItem s in mi.DropDownItems) { s.BackColor = bg; s.ForeColor = fg; }
            }
        }

        public static void Boya(Control k, Color bg, Color fg, Color alan, Color btn, Color vurgu)
        {
            k.BackColor = bg; k.ForeColor = fg;
            foreach (Control c in k.Controls)
            {
                if (c is TextBox || c is ComboBox || c is NumericUpDown) { c.BackColor = alan; c.ForeColor = fg; }
                else if (c is Button)
                {
                    bool v = (c.Tag as string) == "vurgu";
                    c.BackColor = v ? vurgu : btn; c.ForeColor = v ? Color.White : fg;
                }
                else if (c is StatusStrip || c is MenuStrip) { c.BackColor = bg; c.ForeColor = fg; }
                else Boya(c, bg, fg, alan, btn, vurgu);
            }
        }

        class Renkler : ProfessionalColorTable
        {
            Color b, s;
            public Renkler(Color bg, Color sec) { b = bg; s = sec; }
            public override Color MenuItemSelected { get { return s; } }
            public override Color MenuItemSelectedGradientBegin { get { return s; } }
            public override Color MenuItemSelectedGradientEnd { get { return s; } }
            public override Color MenuItemPressedGradientBegin { get { return s; } }
            public override Color MenuItemPressedGradientEnd { get { return s; } }
            public override Color MenuItemBorder { get { return s; } }
            public override Color MenuBorder { get { return s; } }
            public override Color ToolStripDropDownBackground { get { return b; } }
            public override Color ImageMarginGradientBegin { get { return b; } }
            public override Color ImageMarginGradientMiddle { get { return b; } }
            public override Color ImageMarginGradientEnd { get { return b; } }
            public override Color MenuStripGradientBegin { get { return b; } }
            public override Color MenuStripGradientEnd { get { return b; } }
        }

        // ---------- olaylar ----------
        void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                if (GorselYapistir(false)) { e.Handled = true; e.SuppressKeyPress = true; }
            }
            else if (e.Control && e.KeyCode == Keys.P) { e.Handled = true; e.SuppressKeyPress = true; Yazdir(); }
        }

        void Planla() { zamanlayici.Stop(); zamanlayici.Start(); }

        void GorselEtiket(string ad)
        {
            lblGorsel.Text = gorsel == null ? "Görsel yok" : ad + "  (" + gorsel.Width + "×" + gorsel.Height + ")";
        }

        void GorselAyarla(Image im, string ad)
        {
            Bitmap b = new Bitmap(im.Width, im.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.White); g.DrawImage(im, 0, 0, im.Width, im.Height); }
            if (gorsel != null) gorsel.Dispose();
            gorsel = b;
            GorselEtiket(ad);
            Planla();
        }

        void GorselAc()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Filter = "Görseller|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|Tümü|*.*";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                try { using (Image im = Image.FromFile(d.FileName)) GorselAyarla(im, Path.GetFileName(d.FileName)); }
                catch (Exception ex) { MessageBox.Show(this, "Görsel açılamadı:\n" + ex.Message, "Etiket Yazdır"); }
            }
        }

        bool GorselYapistir(bool uyar)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    using (Image im = Clipboard.GetImage()) GorselAyarla(im, "Pano");
                    return true;
                }
                if (Clipboard.ContainsFileDropList())
                {
                    foreach (string p in Clipboard.GetFileDropList())
                    {
                        try { using (Image im = Image.FromFile(p)) GorselAyarla(im, Path.GetFileName(p)); return true; }
                        catch { }
                    }
                }
            }
            catch { }
            if (uyar) MessageBox.Show(this, "Panoda görsel yok.", "Etiket Yazdır");
            return false;
        }

        void GorselKaldir()
        {
            if (gorsel != null) { gorsel.Dispose(); gorsel = null; }
            GorselEtiket("");
            Planla();
        }

        // ---------- çizim ----------
        int GecerliDpi()
        {
            if (!string.IsNullOrEmpty(ay.Yazici))
            {
                int d;
                if (!dpiOnbellek.TryGetValue(ay.Yazici, out d))
                {
                    d = 0;
                    try
                    {
                        PrinterSettings ps = new PrinterSettings();
                        ps.PrinterName = ay.Yazici;
                        if (ps.IsValid)
                        {
                            d = ps.DefaultPageSettings.PrinterResolution.X;
                            if (d <= 0)
                                foreach (PrinterResolution r in ps.PrinterResolutions)
                                    if (r.X > d) d = r.X;
                        }
                    }
                    catch { }
                    dpiOnbellek[ay.Yazici] = d;
                }
                if (d >= 100 && d <= 1200) return d;
            }
            return ay.Dpi > 0 ? ay.Dpi : 203;
        }

        Bitmap Olustur(out int dpi, out bool tasma)
        {
            dpi = GecerliDpi();
            int W = Cizim.Px(ay.GenislikMm, dpi);
            int m = Cizim.Px((double)ay.KenarMm, dpi);
            if (ay.KenarMm == 0) m = 0;
            int ic = Math.Max(10, W - 2 * m);
            int bosluk = Cizim.Px(1.5, dpi);
            List<Bitmap> bloklar = new List<Bitmap>();

            Bitmap gb = null;
            if (gorsel != null)
            {
                int gw = gorsel.Width, gh = gorsel.Height;
                if (chSigdir.Checked || gw > ic) { gh = Math.Max(1, (int)((long)gh * ic / gw)); gw = ic; }
                using (Bitmap t = new Bitmap(gw, gh))
                {
                    using (Graphics g = Graphics.FromImage(t))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.Clear(Color.White);
                        g.DrawImage(gorsel, 0, 0, gw, gh);
                    }
                    gb = Cizim.SiyahBeyaz(t, chTarama.Checked);
                }
            }

            Bitmap tb = null;
            if (txt.Text.Trim().Length > 0)
            {
                FontStyle st = chKalin.Checked ? FontStyle.Bold : FontStyle.Regular;
                string fad = cbFont.SelectedItem as string ?? "Arial";
                try { using (new Font(fad, 10f, st)) { } } catch { fad = "Arial"; }
                float pt = (float)nBoyut.Value;
                bool oto = chOto.Checked;
                if (oto)
                {
                    int maxH = int.MaxValue;
                    if (ay.SabitBoy)
                        maxH = Cizim.Px((double)ay.BoyMm, dpi) - 2 * m - (gb != null ? gb.Height + bosluk : 0);
                    pt = OtoBoyut(txt.Text, fad, st, dpi, ic, maxH);
                }
                sonBoyut = pt;
                Font f = new Font(fad, pt, st, GraphicsUnit.Point);
                using (f)
                using (StringFormat sf = oto ? (StringFormat)StringFormat.GenericTypographic.Clone() : new StringFormat())
                {
                    if (oto) sf.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
                    sf.Alignment = rSol.Checked ? StringAlignment.Near : (rSag.Checked ? StringAlignment.Far : StringAlignment.Center);
                    int h;
                    using (Bitmap olc = new Bitmap(1, 1))
                    {
                        olc.SetResolution(dpi, dpi);
                        using (Graphics g = Graphics.FromImage(olc))
                        {
                            g.PageUnit = GraphicsUnit.Pixel;
                            h = (int)Math.Ceiling(g.MeasureString(txt.Text, f, ic, sf).Height) + 2;
                        }
                    }
                    using (Bitmap t = new Bitmap(ic, Math.Max(1, h)))
                    {
                        t.SetResolution(dpi, dpi);
                        using (Graphics g = Graphics.FromImage(t))
                        {
                            g.PageUnit = GraphicsUnit.Pixel;
                            g.Clear(Color.White);
                            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                            g.DrawString(txt.Text, f, Brushes.Black, new RectangleF(0, 0, ic, h), sf);
                        }
                        tb = Cizim.SiyahBeyaz(t, false);
                    }
                }
            }

            if (rAlt.Checked) { if (tb != null) bloklar.Add(tb); if (gb != null) bloklar.Add(gb); }
            else { if (gb != null) bloklar.Add(gb); if (tb != null) bloklar.Add(tb); }

            int icerik = 0;
            foreach (Bitmap b in bloklar) icerik += b.Height;
            icerik += bosluk * Math.Max(0, bloklar.Count - 1);

            int H, y;
            if (ay.SabitBoy)
            {
                H = Cizim.Px((double)ay.BoyMm, dpi);
                y = Math.Max(m, (H - icerik) / 2);
                tasma = icerik + 2 * m > H;
            }
            else
            {
                H = Math.Max(Cizim.Px(10, dpi), icerik + 2 * m);
                y = m;
                tasma = false;
            }

            Bitmap sayfa = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            sayfa.SetResolution(dpi, dpi);
            using (Graphics g = Graphics.FromImage(sayfa))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                foreach (Bitmap b in bloklar)
                {
                    int x = m + (ic - b.Width) / 2;
                    g.DrawImage(b, new Rectangle(x, y, b.Width, b.Height));
                    y += b.Height + bosluk;
                    b.Dispose();
                }
            }
            return sayfa;
        }

        // Satırların hepsi genişliğe (ve sabit boyda yüksekliğe) sığacak en büyük punto
        static float OtoBoyut(string text, string fad, FontStyle st, int dpi, int ic, int maxH)
        {
            string[] satirlar = text.Replace("\r", "").Split('\n');
            using (Bitmap olc = new Bitmap(1, 1))
            using (StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                olc.SetResolution(dpi, dpi);
                using (Graphics g = Graphics.FromImage(olc))
                {
                    g.PageUnit = GraphicsUnit.Pixel;
                    g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    float lo = 4f, hi = 400f;
                    for (int i = 0; i < 14; i++)
                    {
                        float mid = (lo + hi) / 2;
                        bool sigar = true;
                        using (Font f = new Font(fad, mid, st, GraphicsUnit.Point))
                        {
                            foreach (string s in satirlar)
                                if (s.Trim().Length > 0 && g.MeasureString(s.TrimEnd(), f, PointF.Empty, sf).Width > ic - 2)
                                { sigar = false; break; }
                            if (sigar && maxH != int.MaxValue)
                                sigar = g.MeasureString(text, f, new SizeF(ic, 100000), sf).Height + 4 <= maxH;
                        }
                        if (sigar) lo = mid; else hi = mid;
                    }
                    return (float)Math.Floor(lo * 2) / 2;
                }
            }
        }

        void Yenile()
        {
            try
            {
                bool tasma;
                Bitmap yeni = Olustur(out sonucDpi, out tasma);
                if (sonuc != null) sonuc.Dispose();
                sonuc = yeni;
                double hmm = sonuc.Height * 25.4 / sonucDpi;
                string s = "Etiket: " + ay.GenislikMm + " × " + Math.Round(hmm) + " mm   |   " + sonucDpi + " DPI   |   Yazıcı: " +
                           (string.IsNullOrEmpty(ay.Yazici) ? "(seçilmedi - Ayarlar)" : ay.Yazici);
                if (chOto.Checked && txt.Text.Trim().Length > 0) s += "   |   Yazı: " + sonBoyut.ToString("0.#") + " pt";
                if (tasma) s += "   |   ⚠ İçerik sabit etiket boyuna sığmıyor";
                durum.Text = s;
                OnizlemeGoster();
            }
            catch (Exception ex) { durum.Text = "Hata: " + ex.Message; }
        }

        void OnizlemeGoster()
        {
            if (sonuc == null || onizleme.Width < 20 || onizleme.Height < 20) return;
            double k = Math.Min((onizleme.Width - 30.0) / sonuc.Width, (onizleme.Height - 30.0) / sonuc.Height);
            k = Math.Max(0.05, Math.Min(k, 3.0));
            int w = Math.Max(1, (int)(sonuc.Width * k)), h = Math.Max(1, (int)(sonuc.Height * k));
            Bitmap b = new Bitmap(w + 2, h + 2);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Gray);
                g.InterpolationMode = k < 1 ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(sonuc, new Rectangle(1, 1, w, h));
            }
            Image eski = onizleme.Image;
            onizleme.Image = b;
            if (eski != null) eski.Dispose();
        }

        // ---------- yazdırma ----------
        void Yazdir()
        {
            if (string.IsNullOrEmpty(ay.Yazici))
            {
                MessageBox.Show(this, "Önce Ayarlar'dan yazıcı seçin.", "Etiket Yazdır");
                AyarlarAc();
                return;
            }
            if (txt.Text.Trim().Length == 0 && gorsel == null)
            {
                MessageBox.Show(this, "Yazdırılacak bir şey yok.", "Etiket Yazdır");
                return;
            }
            Yenile();
            if (sonuc == null) return;

            double wMm = ay.GenislikMm;
            double hMm = sonuc.Height * 25.4 / sonucDpi;
            double kayMm = (double)ay.KaydirmaMm;
            Bitmap resim = sonuc;
            try
            {
                Cursor = Cursors.WaitCursor;
                using (PrintDocument pd = new PrintDocument())
                {
                    pd.DocumentName = "Etiket";
                    pd.PrinterSettings.PrinterName = ay.Yazici;
                    if (!pd.PrinterSettings.IsValid) throw new Exception("Yazıcı bulunamadı: " + ay.Yazici);
                    pd.PrinterSettings.Copies = 1;
                    pd.PrintController = new StandardPrintController();
                    pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                    pd.DefaultPageSettings.Landscape = false;
                    if (ay.KagitGonder)
                    {
                        // yüzde bir inç
                        int pw = (int)Math.Round((wMm + kayMm) / 25.4 * 100);
                        int ph = (int)Math.Round(hMm / 25.4 * 100);
                        pd.DefaultPageSettings.PaperSize = new PaperSize("Etiket", pw, ph);
                    }
                    int kalan = (int)nKopya.Value;
                    pd.PrintPage += delegate (object s, PrintPageEventArgs e)
                    {
                        Graphics g = e.Graphics;
                        g.PageUnit = GraphicsUnit.Millimeter;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        float x = (float)kayMm;
                        if (!pd.PrinterSettings.PrintToFile)
                        {
                            // sürücünün basılamayan kenarını telafi et
                            x -= e.PageSettings.HardMarginX * 0.254f;
                        }
                        g.DrawImage(resim, new RectangleF(x, -e.PageSettings.HardMarginY * 0.254f, (float)wMm, (float)hMm));
                        kalan--;
                        e.HasMorePages = kalan > 0;
                    };
                    pd.Print();
                }
                durum.Text = "✔ " + nKopya.Value + " adet yazıcıya gönderildi (" + ay.Yazici + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Yazdırılamadı:\n" + ex.Message, "Etiket Yazdır", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        void PngKaydet()
        {
            if (sonuc == null) return;
            using (SaveFileDialog d = new SaveFileDialog())
            {
                d.Filter = "PNG|*.png";
                d.FileName = "etiket.png";
                if (d.ShowDialog(this) == DialogResult.OK) sonuc.Save(d.FileName, ImageFormat.Png);
            }
        }

        void Topla()
        {
            ay.Font = cbFont.SelectedItem as string ?? "Arial";
            ay.Boyut = nBoyut.Value;
            ay.Kalin = chKalin.Checked;
            ay.OtoBoyut = chOto.Checked;
            ay.Hiza = rSol.Checked ? 0 : (rSag.Checked ? 2 : 1);
            ay.GorselAltta = rAlt.Checked;
            ay.Sigdir = chSigdir.Checked;
            ay.Tarama = chTarama.Checked;
        }

        // ---------- ayarlar penceresi ----------
        void AyarlarAc()
        {
            using (AyarForm f = new AyarForm(ay))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    Topla();
                    ay.Kaydet();
                    TemaUygula();
                    Yenile();
                }
            }
        }
    }

    class AyarForm : Form
    {
        Ayarlar ay;
        ComboBox cbYazici, cbDpi;
        RadioButton r55, r85, rOto, rSabit, rAcik, rKoyu;
        NumericUpDown nBoy, nKenar, nKay;
        CheckBox chKagit;

        public AyarForm(Ayarlar a)
        {
            ay = a;
            Text = "Ayarlar";
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            TableLayoutPanel t = new TableLayoutPanel();
            t.AutoSize = true;
            t.Padding = new Padding(14);
            t.ColumnCount = 2;
            t.Dock = DockStyle.Fill;

            cbYazici = new ComboBox();
            cbYazici.DropDownStyle = ComboBoxStyle.DropDownList;
            cbYazici.Width = 300;
            string varsayilan = "";
            try { varsayilan = new PrinterSettings().PrinterName; } catch { }
            foreach (string p in PrinterSettings.InstalledPrinters) cbYazici.Items.Add(p);
            cbYazici.SelectedItem = string.IsNullOrEmpty(a.Yazici) ? varsayilan : a.Yazici;
            Satir(t, "Yazıcı", cbYazici);

            r55 = new RadioButton(); r55.Text = "55 mm"; r55.AutoSize = true; r55.Checked = a.GenislikMm == 55;
            r85 = new RadioButton(); r85.Text = "85 mm"; r85.AutoSize = true; r85.Checked = a.GenislikMm != 55;
            Satir(t, "Etiket genişliği", Grup(r55, r85));

            rOto = new RadioButton(); rOto.Text = "Otomatik (içeriğe göre)"; rOto.AutoSize = true; rOto.Checked = !a.SabitBoy;
            rSabit = new RadioButton(); rSabit.Text = "Sabit:"; rSabit.AutoSize = true; rSabit.Checked = a.SabitBoy;
            nBoy = Sayi(10, 1000, 1, 0, a.BoyMm);
            Label mm = new Label(); mm.Text = "mm"; mm.AutoSize = true; mm.Margin = new Padding(3, 7, 3, 3);
            Satir(t, "Etiket boyu", Grup(rOto, rSabit, nBoy, mm));

            nKenar = Sayi(0, 20, 0.5m, 1, a.KenarMm);
            Satir(t, "Kenar boşluğu (mm)", nKenar);
            nKay = Sayi(0, 40, 0.5m, 1, a.KaydirmaMm);
            Satir(t, "Yatay kaydırma (mm)", nKay);

            cbDpi = new ComboBox();
            cbDpi.DropDownStyle = ComboBoxStyle.DropDownList;
            cbDpi.Items.Add("203"); cbDpi.Items.Add("300");
            cbDpi.SelectedItem = a.Dpi == 300 ? "300" : "203";
            cbDpi.Width = 70;
            Satir(t, "Varsayılan DPI", cbDpi);

            chKagit = new CheckBox(); chKagit.Text = "Kağıt boyutunu yazıcı sürücüsüne gönder";
            chKagit.AutoSize = true; chKagit.Checked = a.KagitGonder;
            Satir(t, "", chKagit);

            rAcik = new RadioButton(); rAcik.Text = "Açık"; rAcik.AutoSize = true; rAcik.Checked = !a.Koyu;
            rKoyu = new RadioButton(); rKoyu.Text = "Koyu"; rKoyu.AutoSize = true; rKoyu.Checked = a.Koyu;
            Satir(t, "Tema", Grup(rAcik, rKoyu));

            FlowLayoutPanel b = new FlowLayoutPanel();
            b.AutoSize = true; b.FlowDirection = FlowDirection.RightToLeft; b.Dock = DockStyle.Fill;
            b.Margin = new Padding(0, 12, 0, 0);
            Button iptal = new Button(); iptal.Text = "İptal"; iptal.DialogResult = DialogResult.Cancel; iptal.AutoSize = true;
            Button kaydet = new Button(); kaydet.Text = "Kaydet"; kaydet.AutoSize = true; kaydet.Tag = "vurgu";
            kaydet.Click += Kaydet;
            b.Controls.Add(iptal); b.Controls.Add(kaydet);
            t.Controls.Add(b, 0, t.RowCount);
            t.SetColumnSpan(b, 2);
            AcceptButton = kaydet; CancelButton = iptal;

            Controls.Add(t);

            Color bg = a.Koyu ? Color.FromArgb(30, 30, 30) : Color.FromArgb(243, 243, 243);
            Color fg = a.Koyu ? Color.FromArgb(238, 238, 238) : Color.FromArgb(17, 17, 17);
            Color alan = a.Koyu ? Color.FromArgb(43, 43, 43) : Color.White;
            Color btn = a.Koyu ? Color.FromArgb(58, 58, 58) : Color.FromArgb(225, 225, 225);
            Color vurgu = a.Koyu ? Color.FromArgb(61, 139, 253) : Color.FromArgb(10, 100, 200);
            AnaForm.Boya(this, bg, fg, alan, btn, vurgu);
            foreach (Control c in new Control[] { iptal, kaydet })
            {
                ((Button)c).FlatStyle = FlatStyle.Flat;
                ((Button)c).FlatAppearance.BorderSize = 0;
                c.Padding = new Padding(10, 3, 10, 3);
            }
        }

        static NumericUpDown Sayi(decimal min, decimal max, decimal art, int ond, decimal v)
        {
            NumericUpDown n = new NumericUpDown();
            n.Minimum = min; n.Maximum = max; n.Increment = art; n.DecimalPlaces = ond; n.Width = 70;
            n.Value = Math.Max(min, Math.Min(max, v));
            return n;
        }

        static FlowLayoutPanel Grup(params Control[] cs)
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.AutoSize = true; f.WrapContents = false; f.Margin = new Padding(0);
            foreach (Control c in cs) { c.Margin = new Padding(3, 5, 8, 3); f.Controls.Add(c); }
            return f;
        }

        static void Satir(TableLayoutPanel t, string ad, Control c)
        {
            int r = t.RowCount;
            Label l = new Label(); l.Text = ad; l.AutoSize = true; l.Margin = new Padding(3, 8, 12, 3);
            t.Controls.Add(l, 0, r);
            t.Controls.Add(c, 1, r);
            t.RowCount = r + 1;
        }

        void Kaydet(object s, EventArgs e)
        {
            ay.Yazici = cbYazici.SelectedItem as string ?? "";
            ay.GenislikMm = r55.Checked ? 55 : 85;
            ay.SabitBoy = rSabit.Checked;
            ay.BoyMm = nBoy.Value;
            ay.KenarMm = nKenar.Value;
            ay.KaydirmaMm = nKay.Value;
            ay.Dpi = int.Parse((string)cbDpi.SelectedItem);
            ay.KagitGonder = chKagit.Checked;
            ay.Koyu = rKoyu.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AnaForm());
        }
    }
}
