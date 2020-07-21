﻿namespace GlitchGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    internal partial class FrmDisplay : Form
	{
        private readonly Stack<string> filenames = new Stack<string>();
        private Bitmap formBitmap;
        private Graphics formGraphics;        

		internal FrmDisplay()
		{
			this.InitializeComponent();
            this.Invalidate();
            RNG.Random = new Random();
            var lastParent = string.Empty;
            var glitches = Actions.GetAllGlitches();
            ToolStripMenuItem menu = null;
            foreach (var glitch in glitches)
            {
                if (glitch.Parent != lastParent)
                {
                    menu = new ToolStripMenuItem(glitch.Parent);
                    menu.Enabled = false;
                    this.MainMenuStrip.Items.Add(menu);
                    lastParent = glitch.Parent;
                }

                var menuItem = new ToolStripMenuItem(glitch.Name);
                menuItem.Click += this.MenuItem_Click;
                menuItem.Tag = glitch.Method;
                menu.DropDownItems.Add(menuItem);
            }

            this.WindowState = FormWindowState.Maximized;
            this.Text = this.MakeVersionNumber();
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            this.formBitmap = ((Converter<Bitmap, Bitmap>)((ToolStripMenuItem)sender).Tag).Invoke(this.formBitmap);
            this.SaveLatestImage();
            this.Text = this.MakeVersionNumber();
        }

        private void FrmDisplay_Paint(object sender, PaintEventArgs e)
		{
            if (this.formBitmap != null)
            {
                e.Graphics.DrawImage(this.formBitmap, new Rectangle(0, 25, this.formBitmap.Width, this.formBitmap.Height));
            }
		}

		private void FrmDisplay_Resize(object sender, EventArgs e)
		{
            this.Invalidate();
        }        

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ofd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "Examples";
            this.ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            this.ofd.FileName = string.Empty;
            this.ofd.ShowDialog();
            if (File.Exists(this.ofd.FileName))
            {
                this.filenames.Clear();
                if (this.formBitmap != null)
                {
                    this.formBitmap.Dispose();
                }

                this.formBitmap = new Bitmap(this.ofd.FileName);
                var filename = Path.GetTempFileName() + ".png";
                this.formBitmap.Save(filename);
                this.filenames.Push(filename);
                this.formGraphics = Graphics.FromImage(this.formBitmap);                
                this.Invalidate();
                foreach (ToolStripMenuItem menu in this.MainMenuStrip.Items)
                {
                    menu.Enabled = true;
                }
                this.UndoToolStripMenuItem.Enabled = false;
            }
        }

        private void RandomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            var glitches = Actions.GetAllGlitches();
            var amount = RNG.Random.Next(3, 7);
            for (int i = 0; i < amount; i++)
            {
                var glitch = glitches[RNG.Random.Next(glitches.Count)].Method;
                this.formBitmap = glitch.Invoke(this.formBitmap);
            }

            this.SaveLatestImage();
            this.Text = this.MakeVersionNumber();
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            this.filenames.Pop();
            this.formBitmap.Dispose();
            var temp = new Bitmap(this.filenames.Peek());
            this.formBitmap = new Bitmap(temp);
            temp.Dispose();
            this.formGraphics = Graphics.FromImage(this.formBitmap);
            this.Invalidate();
            this.UndoToolStripMenuItem.Enabled = this.filenames.Count > 1;
            this.Text = this.MakeVersionNumber();
        }

        private void RandomOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            this.ApplyRandomGlitch(1, isAllowingCompression: true, isSafe:false);
            this.SaveLatestImage();
            this.Text = this.MakeVersionNumber();
        }

        private void RandomMultipleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            this.ApplyRandomGlitch(RNG.Random.Next(3, 8), isAllowingCompression: true, isSafe: false);
            this.SaveLatestImage();
            this.Text = this.MakeVersionNumber();
        }

        private void RandomMultipleNoCompressionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Text = "Working on it...";
            this.ApplyRandomGlitch(RNG.Random.Next(3, 8), isAllowingCompression: false, isSafe: false);
            this.SaveLatestImage();
            this.Text = this.MakeVersionNumber();
        }

        private void ApplyRandomGlitch(int amount, bool isAllowingCompression, bool isSafe, bool isUpdating=true)
        {
            var glitches = Actions.GetAllGlitches();
            do
            {
                var glitch = glitches[RNG.Random.Next(glitches.Count)];
                if (!glitch.Parent.Contains("Compress") || isAllowingCompression)
                {
                    if (!glitch.Parent.Contains("Corrupt") || !isSafe)
                    {
                        Console.WriteLine(glitch.Parent + " > " + glitch.Name);
                        this.formBitmap = glitch.Method.Invoke(this.formBitmap);
                        amount--;
                        if (isUpdating)
                        {
                            this.Invalidate();
                            this.Refresh();
                        }
                    }                    
                }
            }
            while (amount > 0);
        }

        private void RandomToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var amount = 10;
            var collageBitmap = new Bitmap(this.formBitmap.Width * 3, this.formBitmap.Height * 3);
            var collageGraphics = Graphics.FromImage(collageBitmap);
            var folderName = Path.GetTempFileName() + "x" + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(folderName);
            var originalImage = (Bitmap)this.formBitmap.Clone();
            for (int i = 0; i < amount; i++)
            {
                this.Text = "Creating Image " + (i + 1) + " of " + amount.ToString() + "...";
                this.formBitmap = (Bitmap)originalImage.Clone();
                this.formGraphics = Graphics.FromImage(this.formBitmap);
                this.ApplyRandomGlitch(RNG.Random.Next(3, 10), isAllowingCompression: false, isSafe: false);
                if (i < 10)
                {
                    var x = i % 3;
                    var y = (int)(i / 3);
                    collageGraphics.DrawImage(this.formBitmap, new RectangleF(this.formBitmap.Width * x, this.formBitmap.Height * y, this.formBitmap.Width, this.formBitmap.Height));
                }
                this.Invalidate();
                this.Refresh();
                var imageCopy = new Bitmap(this.formBitmap);
                this.formBitmap.Dispose();
                this.formBitmap = imageCopy;
                this.formBitmap.Save(folderName + string.Format("{0:00}.png", i), Glitches.GetEncoder(ImageFormat.Png), null);
                this.formBitmap.Dispose();
                this.formGraphics.Dispose();
                imageCopy.Dispose();
                GC.Collect();
                GC.GetTotalMemory(forceFullCollection: true);
            }

            collageBitmap.Save(folderName + "Collage.png", Glitches.GetEncoder(ImageFormat.Png), null);
            collageBitmap.Dispose();
            collageGraphics.Dispose();

            this.formBitmap = (Bitmap)originalImage.Clone();
            originalImage.Dispose();
            Process.Start(folderName);
            this.Text = MakeVersionNumber();
            this.Invalidate();
            this.Refresh();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sfd.Filter = "Images|*.png;*.jpg";
            sfd.ShowDialog();
            if (sfd.FileName != string.Empty)
            {
                if (sfd.FileName.ToLower().EndsWith("png"))
                {
                    this.formBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                else if(sfd.FileName.ToLower().EndsWith("jpg") || sfd.FileName.ToLower().EndsWith("jpeg"))
                {
                    this.formBitmap.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
            }
        }

        private void SaveLatestImage()
        {
            this.filenames.Push(Path.GetTempFileName() + ".png");
            var imageCopy = new Bitmap(this.formBitmap);
            this.formBitmap.Dispose();
            this.formBitmap = imageCopy;
            this.formBitmap.Save(this.filenames.Peek(), Glitches.GetEncoder(ImageFormat.Png), null);
            this.formBitmap.Dispose();
            this.formBitmap = new Bitmap(this.filenames.Peek());
            this.formGraphics = Graphics.FromImage(this.formBitmap);
            this.Invalidate();
            imageCopy.Dispose();
            this.UndoToolStripMenuItem.Enabled = true;
        }

        private string MakeVersionNumber()
        {
            Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            string ret = Application.ProductName.ToString() + " v" + ver.Major;
            if (ver.Minor != 0 || ver.Build != 0 || ver.Revision != 0)
            {
                ret += "." + ver.Minor;
                if (ver.Build != 0 || ver.Revision != 0)
                {
                    ret += "." + ver.Build;
                    if (ver.Revision != 0)
                    {
                        ret += "." + ver.Revision;
                    }
                }
            }

            return ret;
        }

        private void BrowseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ofd.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "Examples";
            this.ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            this.ofd.FileName = string.Empty;
            this.ofd.ShowDialog();
            if (File.Exists(this.ofd.FileName))
            {
                this.filenames.Clear();
                if (this.formBitmap != null)
                {
                    this.formBitmap.Dispose();
                }

                this.formBitmap = new Bitmap(this.ofd.FileName);
                var filename = Path.GetTempFileName() + ".png";
                this.formBitmap.Save(filename);
                this.filenames.Push(filename);
                this.formGraphics = Graphics.FromImage(this.formBitmap);
                this.Invalidate();
                foreach (ToolStripMenuItem menu in this.MainMenuStrip.Items)
                {
                    menu.Enabled = true;
                }
                this.UndoToolStripMenuItem.Enabled = false;
            }
        } 

        private void SmallGeometricToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomGeometricImage(500, 500, 200);
        }

        private void MediumGeometricToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomGeometricImage(1000, 1000, 300);
        }

        private void LargeGeometricToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomGeometricImage(1920, 1080, 400);
        }

        private void CreateRandomGeometricImage(int width, int height, int max)
        {
            this.formBitmap = new Bitmap(width, height);
            var filename = Path.GetTempFileName() + ".png";
            this.formBitmap.Save(filename);
            this.filenames.Push(filename);
            this.formGraphics = Graphics.FromImage(this.formBitmap);
            this.formGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i < 2000; i++)
            {
                var colour = Color.FromArgb(RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
                int sizeX = RNG.Random.Next(25, max), sizeY = RNG.Random.Next(25, max);
                int x = RNG.Random.Next(width), y = RNG.Random.Next(height);
                switch (RNG.Random.Next(4))
                {
                    case 0:
                        this.formGraphics.FillEllipse(new SolidBrush(colour), x, y, sizeX, sizeY);
                        break;

                    case 1:
                        this.formGraphics.FillRectangle(new SolidBrush(colour), x - sizeX / 2, y - sizeY / 2, sizeX / 2, sizeY / 2);
                        break;

                    case 2:
                        int nx = x + RNG.Random.Next(-max, max), ny = y + RNG.Random.Next(-max, max);
                        this.formGraphics.DrawLine(new Pen(colour, RNG.Random.Next(1, 30)), x, y, nx, ny);
                        break;

                    case 3:
                        var points = new List<Point>();
                        var amount = RNG.Random.Next(3, 6) * 2;
                        for (int p = 0; p < amount; p++)
                        {
                            points.Add(new Point(x + RNG.Random.Next(-max, max), y + RNG.Random.Next(-max, max)));
                        }

                        this.formGraphics.FillPolygon(new SolidBrush(colour), points.ToArray());
                        break;
                }
            }

            this.Invalidate();
            foreach (ToolStripMenuItem menu in this.MainMenuStrip.Items)
            {
                menu.Enabled = true;
            }
            this.UndoToolStripMenuItem.Enabled = false;
        }

        private void SmallImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomImage(500, 500, 0.5f);
        }

        private void MediumImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomImage(1000, 1000, 0.75f);
        }

        private void LargeImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateRandomImage(1920, 1080, 1f);
        }

        private void CreateRandomImage(int width, int height, float multiplier)
        {
            this.formBitmap = new Bitmap(width, height);
            var filename = Path.GetTempFileName() + ".png";
            this.formBitmap.Save(filename);
            this.filenames.Push(filename);
            this.formGraphics = Graphics.FromImage(this.formBitmap);
            this.formGraphics.Clear(Color.Black);

            var sources = Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath) + Path.DirectorySeparatorChar + "Examples", "*.*");

            for (int i = 0; i < 100; i++)
            {
                var source = new Bitmap(sources[RNG.Random.Next(sources.Length)]);
                var sourceW = RNG.Random.Next(200, source.Width);
                var sourceH = RNG.Random.Next(200, source.Height);
                var sourceX = RNG.Random.Next(0, source.Width - sourceW);
                var sourceY = RNG.Random.Next(0, source.Height - sourceH);
                var destinationW = (int)(sourceW * multiplier);
                var destinationH = (int)(sourceH * multiplier);
                var destinationX = RNG.Random.Next(-300, (this.formBitmap.Width + 300) - destinationW);
                var destinationY = RNG.Random.Next(-300, (this.formBitmap.Height + 300) - destinationH);
                this.formGraphics.DrawImage(source, new Rectangle(destinationX, destinationY, destinationW, destinationH), new Rectangle(sourceX, sourceY, sourceW, sourceH), GraphicsUnit.Point);
            }

            this.Invalidate();
            foreach (ToolStripMenuItem menu in this.MainMenuStrip.Items)
            {
                menu.Enabled = true;
            }
            this.UndoToolStripMenuItem.Enabled = false;
        }

        private void CorruptFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ofd.Multiselect = true;
            ofd.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            ofd.FileName = string.Empty;
            ofd.ShowDialog();

            if (ofd.FileNames.Count() > 0)
            {
                var newFolder = Path.GetDirectoryName(ofd.FileNames[0]) + Path.DirectorySeparatorChar + "Glitched " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                Directory.CreateDirectory(newFolder);
                for (int i = 0; i < ofd.FileNames.Count(); i++)
                {
                    this.Text = "Processing " + (i + 1).ToString() + " of " + ofd.FileNames.Count();
                    Console.WriteLine(this.Text);
                    var temp = new Bitmap(ofd.FileNames[i]);
                    this.formBitmap = new Bitmap(temp.Width, temp.Height);
                    this.formGraphics = Graphics.FromImage(this.formBitmap);
                    this.formGraphics.DrawImage(temp, 0, 0, this.formBitmap.Width, this.formBitmap.Height);
                    this.ApplyRandomGlitch(RNG.Random.Next(2, 5), isAllowingCompression: false, isSafe: false, isUpdating: false);
                    this.formBitmap.Save(newFolder + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(ofd.FileNames[i]) + ".png");
                    temp.Dispose();
                    this.formGraphics.Dispose();
                    this.formBitmap.Dispose();                    
                    GC.GetTotalMemory(true);
                }

                Process.Start(newFolder);
            }

            this.Text = MakeVersionNumber();            
        }
    }
}