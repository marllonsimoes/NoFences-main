using NoFences.Model;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NoFences.View.Fences.Handlers
{
    internal class PictureFenceHandler : IFenceHandler
    {
        private Form _window;
        private FenceInfo _fenceInfo;
        private Timer _timer;
        private string _currentPicture;
        private string[] validExtensionsForSlideshow = new string[] { ".jpg", ".png", ".gif", ".jpeg", ".bpm" };

        public bool CanHandle(string fenceType)
        {
            return fenceType == EntryType.Pictures.ToString();
        }

        public void Initialize(Form window)
        {
            _window = window;
            _fenceInfo = ((FenceWindow)window).fenceInfo;

            SetNextSlidePicture();
            if (_fenceInfo.Items.Count > 1)
            {
                _timer = new Timer();
                _timer.Interval = _fenceInfo.Interval;
                _timer.Tick += Timer_Tick;
                _timer.Enabled = true;
                _timer.Start();
            }
        }

        public void Paint(PaintEventArgs e)
        {
            if (_currentPicture != null && _currentPicture.Length > 0)
            {
                Image img = Image.FromFile(_currentPicture);
                ExifRotate(img);

                var ratioX = (double)_window.Width / img.Width;
                var ratioY = (double)_window.Height / img.Height;
                var ratio = Math.Min(ratioX, ratioY);

                var newWidth = (int)(img.Width * ratio);
                var newHeight = (int)(img.Height * ratio);
                var newX = (_window.Width - newWidth) / 2;
                var newY = (_window.Height - newHeight) / 2;

                e.Graphics.DrawImage(img, newX, newY + _fenceInfo.TitleHeight, newWidth, newHeight);
                img.Dispose();
            }
        }

        private void SetNextSlidePicture()
        {
            if (_fenceInfo.Items.Count != 0)
            {
                var nextPic = _fenceInfo.Items.Count > 1 ? new Random().Next(0, _fenceInfo.Items.Count() - 1) : 0;
                _currentPicture = _fenceInfo.Items.ElementAt(nextPic);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            SetNextSlidePicture();
            _window.Refresh();
        }

        private readonly int exifOrientationID = 0x112; //274
        private void ExifRotate(Image img)
        {
            if (!img.PropertyIdList.Contains(exifOrientationID))
                return;

            var prop = img.GetPropertyItem(exifOrientationID);
            int val = BitConverter.ToUInt16(prop.Value, 0);
            var rot = RotateFlipType.RotateNoneFlipNone;

            if (val == 3 || val == 4)
                rot = RotateFlipType.Rotate180FlipNone;
            else if (val == 5 || val == 6)
                rot = RotateFlipType.Rotate90FlipNone;
            else if (val == 7 || val == 8)
                rot = RotateFlipType.Rotate270FlipNone;

            if (val == 2 || val == 4 || val == 5 || val == 7)
                rot |= RotateFlipType.RotateNoneFlipX;

            if (rot != RotateFlipType.RotateNoneFlipNone)
            {
                img.RotateFlip(rot);
                img.RemovePropertyItem(exifOrientationID);
            }
        }

        public void MouseClick(object sender, EventArgs e)
        {
            
        }

        public void MouseDoubleClick(object sender, EventArgs e)
        {
            
        }

        public void MouseLeave(object sender, EventArgs e)
        {
            
        }

        public void Click(object sender, EventArgs e)
        {
            
        }

        public void MouseEnter(object sender, EventArgs e)
        {
           
        }

        public void MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        public void DragDrop(object sender, DragEventArgs e)
        {
        }

        public void DragEnter(object sender, DragEventArgs e)
        {
            var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (dropped.Length > 1)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            foreach (var file in dropped)
            {
                var fileLowercase = Path.GetExtension(file).ToLower();
                if (!File.Exists(file) ||
                    !validExtensionsForSlideshow.Contains(fileLowercase))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
            }
            e.Effect = DragDropEffects.Move;
        }

        public void KeyUp(object sender, KeyEventArgs e)
        {
        }
    }
}
