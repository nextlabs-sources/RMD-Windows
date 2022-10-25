using Viewer.upgrade.ui.common.hoops.view;

namespace Viewer.upgrade.ui.common.hoops.commands
{
    /// <summary>
    /// Simple Shadow Mode Command handler
    /// </summary>
    class SimpleShadowModeCommand : BaseCommand
    {
        private bool _enableShadows = false;

        const float opacity = 0.3f;
        const uint resolution = 512;
        const uint blurring = 20;

        public SimpleShadowModeCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            _enableShadows = !_enableShadows;

            // Get the Sprockets control from the _mainBorder
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();

            // Get the Sprockets.View segment to set the shadow settings on
            HPS.SegmentKey viewSeg = ctrl.Canvas.GetFrontView().GetSegmentKey();

            // Only recompute plane when enabling shadows
            if (_enableShadows)
            {
                ctrl.Canvas.GetFrontView().SetSimpleShadow(true);

                // Set opacity in simple shadow color
                HPS.RGBAColor color = new HPS.RGBAColor(0, 0, 0, opacity);
                if (viewSeg.GetVisualEffectsControl().ShowSimpleShadowColor(out color))
                    color.alpha = opacity;

                // Enable/disable shadow and pass in shadow settings
                viewSeg.GetVisualEffectsControl().SetSimpleShadowColor(color);
            }
            else
                ctrl.Canvas.GetFrontView().SetSimpleShadow(false);

            // Trigger update
            ctrl.Canvas.Update();
        }
    }

    /// <summary>
    /// Smooth Mode Command handler
    /// </summary>
    class SmoothModeCommand : BaseCommand
    {
        public SmoothModeCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();
            ctrl.Canvas.GetFrontView().SetRenderingMode(HPS.Rendering.Mode.Phong);

            if (_win.CADModel != null && _win.CADModel.Type() == HPS.Type.DWGCADModel)
                ctrl.Canvas.GetFrontView().GetSegmentKey().GetVisibilityControl().SetLines(true);

            ctrl.Canvas.GetFrontView().Update();
        }
    }

    /// <summary>
    /// Hidden Line Mode Command handler
    /// </summary>
    class HiddenLineModeCommand : BaseCommand
    {
        public HiddenLineModeCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();
            ctrl.Canvas.GetFrontView().SetRenderingMode(HPS.Rendering.Mode.FastHiddenLine);

            if (_win._enableFrameRate)
            {
                //FrameRate and HiddenLine are incompatible. Turn off FrameRate when selecting HiddenLine
                _win.GetSprocketsControl().Canvas.SetFrameRate(0);
                _win._enableFrameRate = false;
                _win.FrameRateButton.IsChecked = false;
            }

            ctrl.Canvas.GetFrontView().Update();
        }
    }

    /// <summary>
    /// Frame Rate Mode Command handler
    /// </summary>
    class FrameRateModeCommand : BaseCommand
    {
        const float frameRate = 20.0f;

        public FrameRateModeCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();

            // Toggle frame rate and set.  Note that 0 disables frame rate.
            _win._enableFrameRate = !_win._enableFrameRate;
            if (_win._enableFrameRate)
            {
                ctrl.Canvas.SetFrameRate(frameRate);
                //FrameRate and HiddenLine are incompatible. When FrameRate is turned on, switch to Smooth if HiddenLine is active
                if (ctrl.Canvas.GetFrontView().GetRenderingMode() == HPS.Rendering.Mode.FastHiddenLine)
                {
                    _win.HiddenLineButton.IsChecked = false;
                    _win.SmoothButton.IsChecked = true;
                    ctrl.Canvas.GetFrontView().SetRenderingMode(HPS.Rendering.Mode.Phong);
                }
            }
            else
                ctrl.Canvas.SetFrameRate(0);

            // Trigger update
            ctrl.Canvas.Update();
        }
    }
}
