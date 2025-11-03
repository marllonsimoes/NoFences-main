using System;
using System.Drawing;
using System.Windows.Forms;

namespace NoFences.View.Fences.Handlers
{
    public interface IFenceHandler
    {
        void Click(object sender, EventArgs e);
        void DragDrop(object sender, DragEventArgs e);
        void DragEnter(object sender, DragEventArgs e);
        void Initialize(Form window);
        void KeyUp(object sender, KeyEventArgs e);
        void MouseClick(object sender, EventArgs e);
        void MouseDoubleClick(object sender, EventArgs e);
        void MouseEnter(object sender, EventArgs e);
        void MouseLeave(object sender, EventArgs e);
        void MouseMove(object sender, MouseEventArgs e);
        void Paint(PaintEventArgs e);
    }
}
