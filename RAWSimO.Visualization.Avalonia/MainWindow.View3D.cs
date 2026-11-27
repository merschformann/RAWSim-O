using System;
using System.Threading.Tasks;
using Aquarium;
using Aquarium.Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RAWSimO.Visualization.Avalonia.Rendering;

namespace RAWSimO.Visualization.Avalonia
{
    /// <summary>
    /// The 3D view (all tiers, rendered with Aquarium).
    /// </summary>
    public partial class MainWindow
    {
        private Scene3D _scene3D;
        private bool _animating3D;
        private bool _view3DInitialized;

        private bool Is3D => Toggle3D.IsChecked == true;

        private void InitView3D()
        {
            View3D.Picked += hit => View.Select(_scene3D?.GetObject(hit));
            View3D.GlInitialized += (info, error) => Dispatcher.UIThread.Post(() =>
            {
                if (error != null)
                {
                    Text3DError.Text = "The 3D view is not available: " + error.Message;
                    Text3DError.IsVisible = Is3D;
                    Log("3D view: " + error.Message);
                }
                else
                    Log("3D view: " + info);
            });
        }

        private void Toggle3D_Changed(object sender, RoutedEventArgs e)
        {
            if (View3D == null) return; // Not initialized yet
            if (!_view3DInitialized)
            {
                InitView3D();
                _view3DInitialized = true;
            }
            View.IsVisible = !Is3D;
            View3D.IsVisible = Is3D;
            Text3DError.IsVisible = Is3D && View3D.Error != null;
            if (Is3D)
            {
                bool fit = _scene3D == null;
                Update3D(force: true);
                if (fit)
                    View3D.FitToScene();
                StartAnimation3D();
            }
        }

        /// <summary>
        /// Marks the 3D scene as outdated (new instance or detail level), it is rebuilt when shown.
        /// </summary>
        private void Reset3D()
        {
            _scene3D = null;
            if (View3D != null && Is3D)
            {
                Update3D(force: true);
                View3D.FitToScene();
            }
        }

        private void Update3D(bool force)
        {
            if (_instance == null || _runner == null || !Is3D) return;
            if (_scene3D == null)
            {
                _scene3D = new Scene3D(_instance, (DetailLevel)(ComboDetail.SelectedItem ?? DetailLevel.Debug));
                View3D.Scene = _scene3D.Scene;
                force = true;
            }
            SceneSnapshot snapshot = _runner.Snapshot;
            if (snapshot != null && (force || snapshot.Version != _scene3D.Version))
                _scene3D.Update(snapshot, View.Options, View.SelectedObject);
        }

        private void StartAnimation3D()
        {
            if (_animating3D) return;
            _animating3D = true;
            void OnFrame(TimeSpan _)
            {
                if (!Is3D || !IsVisible)
                {
                    _animating3D = false;
                    return;
                }
                Update3D(force: false);
                TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
            }
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
        }

        private async Task Export3D(string file)
        {
            Screenshot shot = await View3D.CaptureAsync().WaitAsync(TimeSpan.FromSeconds(10));
            using var bitmap = new WriteableBitmap(new global::Avalonia.PixelSize(shot.Width, shot.Height), new global::Avalonia.Vector(96, 96),
                global::Avalonia.Platform.PixelFormat.Rgba8888, global::Avalonia.Platform.AlphaFormat.Unpremul);
            using (var fb = bitmap.Lock())
                for (int y = 0; y < shot.Height; y++)
                    System.Runtime.InteropServices.Marshal.Copy(shot.Rgba, y * shot.Width * 4, fb.Address + y * fb.RowBytes, shot.Width * 4);
            bitmap.Save(file, PngBitmapEncoderOptions.Default);
        }
    }
}
