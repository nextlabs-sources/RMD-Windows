using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.render.hoops.ThreeDView;

namespace Viewer.hoops.Commands
{
    /// <summary>
    /// Orbit command handler.
    /// </summary>
    class OrbitCommand : BaseCommand
    {
        public OrbitCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new HPS.OrbitOperator(HPS.MouseButtons.ButtonLeft()));
            _win.GetSprocketsControl().Focus();
        }
    }

    /// <summary>
    /// Pan Command handler
    /// </summary>
    class PanCommand : BaseCommand
    {
        public PanCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new HPS.PanOperator(HPS.MouseButtons.ButtonLeft()));
            _win.GetSprocketsControl().Focus();
        }
    }

    /// <summary>
    /// Zoom Area Command handler
    /// </summary>
    class ZoomAreaCommand : BaseCommand
    {
        public ZoomAreaCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new HPS.ZoomBoxOperator(HPS.MouseButtons.ButtonLeft()));
            _win.GetSprocketsControl().Focus();
        }
    }

    /// <summary>
    /// Fly Command handler
    /// </summary>
    class FlyCommand : BaseCommand
    {
        public FlyCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new HPS.FlyOperator());
            _win.GetSprocketsControl().Focus();
        }
    }

    /// <summary>
    /// Home command handler
    /// </summary>
    class HomeCommand : BaseCommand
    {
        public HomeCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            try
            {
                if (_win.CADModel != null)
                    _win.AttachViewWithSmoothTransition(_win.CADModel.ActivateDefaultCapture().FitWorld());
                else if (_win.DefaultCamera != null)
                    _win.GetSprocketsControl().Canvas.GetFrontView().SmoothTransition(_win.DefaultCamera);
            }
            catch (HPS.InvalidSpecificationException)
            {
                //SmoothTransition() can throw if there is no model attached
            }
        }
    }

    /// <summary>
    /// Zoom Fit Command handler
    /// </summary>
    class ZoomFitCommand : BaseCommand
    {
        public ZoomFitCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();
            HPS.View frontView = ctrl.Canvas.GetFrontView();
            HPS.CameraKit fitWorldCamera;
            frontView.ComputeFitWorldCamera(out fitWorldCamera);
            frontView.SmoothTransition(fitWorldCamera);
        }
    }

    /// <summary>
    /// Point Select Command handler
    /// </summary>
    class PointSelectCommand : BaseCommand
    {
        public PointSelectCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new SandboxHighlightOperator(_win));
            _win.GetSprocketsControl().Focus();
        }
    }

    /// <summary>
    /// Area Select Command handler
    /// </summary>
    class AreaSelectCommand : BaseCommand
    {
        public AreaSelectCommand(ThreeDViewer win) : base(win) { }

        public override void Execute(object parameter)
        {
            HPS.View view = _win.GetSprocketsControl().Canvas.GetFrontView();
            view.GetOperatorControl().Pop();
            view.GetOperatorControl().Push(new HPS.HighlightAreaOperator(HPS.MouseButtons.ButtonLeft()));
            _win.GetSprocketsControl().Focus();
        }
    }

    // -----------------------------------------

    /// <summary>
    /// New command handler: mainly used to create one default scene (Canvas background).
    /// </summary>
    class NewCommand : BaseCommand
    {
        public NewCommand(ThreeDViewer win):base(win) { }

        public override void Execute(object parameter)
        {
            SprocketsWPFControl ctrl = _win.GetSprocketsControl();

            // Restore scene defaults
            ctrl.Canvas.GetWindowKey().GetHighlightControl().UnhighlightEverything();
            _win.CreateNewModel();
            _win.SetupSceneDefaults();

            // Triggers update
            ctrl.Canvas.Update();
        }
    }
}
