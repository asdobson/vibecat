using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace VibeCat.Controls;

public partial class ResizeGrip : UserControl
{
    public event EventHandler<DragDeltaEventArgs>? DragDelta;

    public ResizeGrip()
    {
        InitializeComponent();
    }

    private void GripThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        DragDelta?.Invoke(this, e);
    }
}