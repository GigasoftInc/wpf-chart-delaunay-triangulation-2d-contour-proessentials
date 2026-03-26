using System;
using System.IO;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using Gigasoft.ProEssentials;
using Gigasoft.ProEssentials.Enums;

namespace DelaunayTriangulation
{
    /// <summary>
    /// ProEssentials WPF Delaunay Triangulation — Hand Held Sound Meter Readings
    ///
    /// Demonstrates Delaunay triangulation contour rendering using PesgoWpf —
    /// the ProEssentials scientific graph object for continuous numeric X and Y axes.
    ///
    /// Delaunay triangulation connects an irregular scatter of XY points into a
    /// triangulated mesh, then interpolates Z values across the triangles to
    /// produce a smooth contour fill. The result is a continuous colored heatmap
    /// from unstructured survey data — no regular grid required.
    ///
    /// Features:
    ///   - Loads DelaunaySample.txt — 70 space-delimited X Y Z measurement points
    ///   - SGraphPlottingMethod.ContourDelaunay — triangulates the XY plane,
    ///     interpolates Z (dBA), renders as a continuous color fill
    ///   - Graph annotations: SmallDotSolid symbol + Z value label on every point,
    ///     plus a named Pointer annotation at a specific location
    ///   - Custom ContourColors array (7 stops: blue → cyan → green → yellow →
    ///     orange → red → dark blue)
    ///   - Manual Z range clamped to 80–102 dBA via ManualScaleControlZ
    ///   - Contour legend on the right — maps colors to dBA ranges
    ///   - MediumNoBorder QuickStyle
    ///   - PeCustomTrackingDataText event — custom tooltip showing Q/N, PC, dBA
    ///   - Mouse wheel zoom, horizontal and vertical scroll zoom, mouse dragging
    ///
    /// Data model — Delaunay uses a flat, unstructured point list:
    ///   PeData.Subsets = 1       — always 1 subset for Delaunay
    ///   PeData.Points  = 70      — one entry per line in DelaunaySample.txt
    ///   PeData.X[0, p] = col 1   — Q/N (flow coefficient)
    ///   PeData.Y[0, p] = col 2   — PC (pressure coefficient)
    ///   PeData.Z[0, p] = col 3   — dBA (the quantity being contoured)
    ///
    /// Controls:
    ///   Left-click drag   — zoom box  (right-click or 'z' to undo)
    ///   Mouse wheel       — horizontal + vertical zoom
    ///   Mouse drag        — pan
    ///   Right-click       — context menu (export, print, customize, annotations)
    ///   Double-click      — customization dialog
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // -----------------------------------------------------------------------
        // Pesgo1_Loaded — chart initialization
        //
        // Always initialize ProEssentials in the control's Loaded event.
        // Do NOT initialize in the Window's Loaded event — the window fires
        // before the control is fully initialized.
        // -----------------------------------------------------------------------
        void Pesgo1_Loaded(object sender, RoutedEventArgs e)
        {
            // Wire up events before ReinitializeResetImage
            Pesgo1.PeCustomTrackingDataText += Pesgo1_PeCustomTrackingDataText;
            Pesgo1.MouseMove += Pesgo1_MouseMove;

            // =======================================================================
            // Step 1 — Declare data dimensions
            //
            // Delaunay triangulation always uses a single subset (Subsets = 1).
            // Points is the total count of scattered XYZ measurements.
            // =======================================================================
            Pesgo1.PeData.Subsets = 1;
            Pesgo1.PeData.Points  = 70;

            // =======================================================================
            // Step 2 — Load DelaunaySample.txt
            //
            // Space-delimited file: X  Y  Z per line, 70 lines total.
            //   X = Q/N  (flow coefficient, horizontal axis)
            //   Y = PC   (pressure coefficient, vertical axis)
            //   Z = dBA  (sound level — the quantity being contoured)
            //
            // Graph annotations are placed at each data point:
            //   - SmallDotSolid symbol marks the measurement location
            //   - Text label shows the Z (dBA) value at that point
            // An additional Pointer annotation marks a named test spot.
            // =======================================================================
            int nPointCount = 0;

            string[] fileArray = { "", "" };
            try
            {
                fileArray = File.ReadAllLines("DelaunaySample.txt");
            }
            catch
            {
                MessageBox.Show("DelaunaySample.txt not found.\n\nMake sure DelaunaySample.txt is in the same folder as the executable.",
                    "File Not Found", MessageBoxButton.OK);
                Application.Current.Shutdown();
                return;
            }

            for (int i = 0; i < fileArray.Length; i++)
            {
                string line = fileArray[i];
                if (line.Length < 3) continue;

                var columns = line.Split(' ');
                float fX = float.Parse(columns[0], CultureInfo.InvariantCulture.NumberFormat);
                float fY = float.Parse(columns[1], CultureInfo.InvariantCulture.NumberFormat);
                float fZ = float.Parse(columns[2], CultureInfo.InvariantCulture.NumberFormat);

                // Chart data — XYZ positions for Delaunay triangulation
                Pesgo1.PeData.X[0, nPointCount] = fX;
                Pesgo1.PeData.Y[0, nPointCount] = fY;
                Pesgo1.PeData.Z[0, nPointCount] = fZ;

                // Graph annotation — dot symbol at each measurement location
                Pesgo1.PeAnnotation.Graph.X[nPointCount]    = fX;
                Pesgo1.PeAnnotation.Graph.Y[nPointCount]    = fY;
                Pesgo1.PeAnnotation.Graph.Type[nPointCount] = (int)GraphAnnotationType.SmallDotSolid;
                Pesgo1.PeAnnotation.Graph.Color[nPointCount] = Color.FromArgb(155, 0, 0, 0); // optional semi-transparent
                Pesgo1.PeAnnotation.Graph.Text[nPointCount]  = string.Format("{0:##0.0}", fZ);

                nPointCount++;
            }

            // Named pointer annotation at a specific test location
            Pesgo1.PeAnnotation.Graph.X[nPointCount]    = 3.75;
            Pesgo1.PeAnnotation.Graph.Y[nPointCount]    = .45;
            Pesgo1.PeAnnotation.Graph.Type[nPointCount] = (int)GraphAnnotationType.Pointer;
            Pesgo1.PeAnnotation.Graph.Text[nPointCount] = "Test Spot";
            Pesgo1.PeAnnotation.Graph.Color[nPointCount] = Color.FromArgb(255, 55, 255, 55);

            // Annotation display settings
            Pesgo1.PeAnnotation.Show                    = true;
            Pesgo1.PeAnnotation.InFront                 = true;
            Pesgo1.PeAnnotation.Graph.TextSize          = 100;
            Pesgo1.PeAnnotation.Graph.MaxSymbolSize     = MinimumPointSize.Medium;

            // =======================================================================
            // Step 3 — Layout
            // =======================================================================
            Pesgo1.PeConfigure.ImageAdjustLeft = 100;

            // =======================================================================
            // Step 4 — Titles
            // =======================================================================
            Pesgo1.PeString.MainTitle         = "Hand Held Sound Meter Readings [dBA]";
            Pesgo1.PeString.SubTitle          = "CVHF 1300.31 Impeller Diameter: LTO 40214, DGI=6";
            Pesgo1.PeString.MultiSubTitles[0] = "|Contour of DRS Attribute = [1339]|";

            Pesgo1.PeFont.MainTitle.Font = "Arial";
            Pesgo1.PeFont.SubTitle.Font  = "Arial";

            Pesgo1.PeString.YAxisLabel = "PC";
            Pesgo1.PeString.XAxisLabel = "Q/N";

            // =======================================================================
            // Step 5 — Manual Z range
            //
            // Clamps the contour color scale to the known dBA measurement range.
            // Values outside 80–102 still render but are clamped to the nearest
            // color stop — prevents a single outlier from skewing the entire scale.
            // =======================================================================
            Pesgo1.PeGrid.Configure.ManualScaleControlZ = ManualScaleControl.MinMax;
            Pesgo1.PeGrid.Configure.ManualMinZ          = 80.0F;
            Pesgo1.PeGrid.Configure.ManualMaxZ          = 102.0F;

            // =======================================================================
            // Step 6 — Custom contour color array
            //
            // 7 color stops define the gradient from low to high dBA.
            // ContourColorBlends = 2 applies minimal interpolation between stops,
            // producing distinct bands — appropriate for engineering contour maps.
            // ContourColorAlpha = 225 gives slight transparency on the fill.
            //
            // ContourColorBlends must be set BEFORE ContourColorSet.
            // =======================================================================
            Pesgo1.PeColor.ContourColors.Clear(6);
            Pesgo1.PeColor.ContourColors[0] = Color.FromArgb(255, 0,   0,   255); // blue
            Pesgo1.PeColor.ContourColors[1] = Color.FromArgb(255, 17,  211, 214); // cyan
            Pesgo1.PeColor.ContourColors[2] = Color.FromArgb(255, 0,   255, 0);   // green
            Pesgo1.PeColor.ContourColors[3] = Color.FromArgb(255, 255, 255, 0);   // yellow
            Pesgo1.PeColor.ContourColors[4] = Color.FromArgb(255, 245, 181, 5);   // orange
            Pesgo1.PeColor.ContourColors[5] = Color.FromArgb(255, 255, 0,   0);   // red
            Pesgo1.PeColor.ContourColors[6] = Color.FromArgb(255, 0,   55,  155); // dark blue

            Pesgo1.PeColor.ContourColorBlends = 2;
            Pesgo1.PeColor.ContourColorAlpha  = 225;
            Pesgo1.PeColor.ContourColorSet    = ContourColorSet.ContourColors;

            // =======================================================================
            // Step 7 — Axis padding
            // =======================================================================
            Pesgo1.PeGrid.Configure.AutoMinMaxPadding   = 1;
            Pesgo1.PeGrid.Configure.AutoPadBeyondZeroX  = true;

            // =======================================================================
            // Step 8 — Monochrome SubsetShades fallback
            //
            // When the chart is viewed in monochrome mode (print, accessibility),
            // SubsetShades provides grayscale bands to replace the color contours.
            // =======================================================================
            for (int s = 0; s < 30; s++)
                Pesgo1.PeColor.SubsetShades[s] = Color.FromArgb(255,
                    (byte)(50 + (s * 2)),
                    (byte)(50 + (s * 2)),
                    (byte)(50 + (s * 2)));

            // =======================================================================
            // Step 9 — Theme and border
            // =======================================================================
            Pesgo1.PeColor.BitmapGradientMode  = true;
            Pesgo1.PeColor.QuickStyle          = QuickStyle.MediumNoBorder;
            Pesgo1.PeConfigure.BorderTypes     = TABorder.SingleLine;

            // =======================================================================
            // Step 10 — Context menu and dialog configuration
            // =======================================================================
            Pesgo1.PeUserInterface.Menu.PlotMethod          = MenuControl.Hide;
            Pesgo1.PeUserInterface.Menu.DataShadow          = MenuControl.Hide;
            Pesgo1.PeUserInterface.Menu.AnnotationControl   = true;
            Pesgo1.PeUserInterface.Menu.ShowAnnotationText  = MenuControl.Show;
            Pesgo1.PeUserInterface.Menu.ShowAnnotations     = MenuControl.Show;
            Pesgo1.PeUserInterface.Menu.MarkDataPoints      = MenuControl.Hide;
            Pesgo1.PeUserInterface.Dialog.PlotCustomization = false;
            Pesgo1.PeUserInterface.Dialog.Page2             = false;

            // =======================================================================
            // Step 11 — Delaunay triangulation plotting method
            //
            // ContourDelaunay (enum value 25) is the Pesgo plotting method that:
            //   1. Computes the Delaunay triangulation of all XY point positions
            //   2. Interpolates Z (dBA) values across each triangle
            //   3. Renders the result as a smooth, filled contour surface
            // =======================================================================
            Pesgo1.PePlot.Method = SGraphPlottingMethod.ContourDelaunay;

            // =======================================================================
            // Step 12 — Legend
            // =======================================================================
            Pesgo1.PeLegend.Location               = LegendLocation.Right;
            Pesgo1.PeLegend.ContourStyle           = true;
            Pesgo1.PeLegend.ContourLegendPrecision = ContourLegendPrecision.ThreeDecimals;

            // =======================================================================
            // Step 13 — Grid display
            // =======================================================================
            Pesgo1.PeGrid.InFront     = true;
            Pesgo1.PeGrid.LineControl = GridLineControl.Both;
            Pesgo1.PeGrid.Style       = GridStyle.Dot;

            // =======================================================================
            // Step 14 — Image caching
            // =======================================================================
            Pesgo1.PeConfigure.PrepareImages = true;
            Pesgo1.PeConfigure.CacheBmp      = true;

            // =======================================================================
            // Step 15 — Zoom and interaction
            // =======================================================================
            Pesgo1.PeUserInterface.Scrollbar.ScrollingVertZoom         = true;
            Pesgo1.PeUserInterface.Scrollbar.ScrollingHorzZoom         = true;
            Pesgo1.PeUserInterface.Allow.ZoomStyle                     = ZoomStyle.Ro2Not;
            Pesgo1.PeUserInterface.Allow.Zooming                       = AllowZooming.HorzAndVert;
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelFunction        = MouseWheelFunction.HorizontalVerticalZoom;
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelZoomSmoothness  = 2;
            Pesgo1.PeUserInterface.Scrollbar.MouseWheelZoomFactor      = 1.05F;
            Pesgo1.PeUserInterface.Scrollbar.MouseDraggingX            = true;
            Pesgo1.PeUserInterface.Scrollbar.MouseDraggingY            = true;
            Pesgo1.PeGrid.GridBands                                    = false;

            // =======================================================================
            // Step 16 — Disable non-Delaunay plotting methods from context menu
            // =======================================================================
            Pesgo1.PePlot.Allow.ContourLines         = false;
            Pesgo1.PePlot.Allow.ContourColors        = true;
            Pesgo1.PePlot.Allow.ContourColorsShadows = false;
            Pesgo1.PePlot.Allow.Line                 = false;
            Pesgo1.PePlot.Allow.Point                = false;
            Pesgo1.PePlot.Allow.Bar                  = false;
            Pesgo1.PePlot.Allow.Area                 = false;
            Pesgo1.PePlot.Allow.Spline               = false;
            Pesgo1.PePlot.Allow.SplineArea           = false;
            Pesgo1.PePlot.Allow.PointsPlusLine       = false;
            Pesgo1.PePlot.Allow.PointsPlusSpline     = false;
            Pesgo1.PePlot.Allow.BestFitCurve         = false;
            Pesgo1.PePlot.Allow.BestFitLine          = false;
            Pesgo1.PePlot.Allow.Stick                = false;

            // =======================================================================
            // Step 17 — Fonts
            // =======================================================================
            Pesgo1.PeFont.FontSize = Gigasoft.ProEssentials.Enums.FontSize.Large;
            Pesgo1.PeFont.Fixed    = true;

            Pesgo1.PeUserInterface.Dialog.Axis    = false;
            Pesgo1.PeUserInterface.Dialog.Style   = false;
            Pesgo1.PeUserInterface.Dialog.Subsets = false;

            Pesgo1.PeConfigure.TextShadows  = TextShadows.BoldText;
            Pesgo1.PeFont.MainTitle.Bold    = true;
            Pesgo1.PeFont.SubTitle.Bold     = true;
            Pesgo1.PeFont.Label.Bold        = true;
            Pesgo1.PeFont.FontSize          = Gigasoft.ProEssentials.Enums.FontSize.Medium;

            // =======================================================================
            // Step 18 — Export defaults
            // =======================================================================
            Pesgo1.PeSpecial.DpiX                        = 600;
            Pesgo1.PeSpecial.DpiY                        = 600;
            Pesgo1.PeUserInterface.Dialog.ExportSizeDef  = ExportSizeDef.NoSizeOrPixel;
            Pesgo1.PeUserInterface.Dialog.ExportTypeDef  = ExportTypeDef.Png;
            Pesgo1.PeUserInterface.Dialog.ExportDestDef  = ExportDestDef.Clipboard;
            Pesgo1.PeUserInterface.Dialog.ExportUnitXDef = "1280";
            Pesgo1.PeUserInterface.Dialog.ExportUnitYDef = "768";
            Pesgo1.PeUserInterface.Dialog.ExportImageDpi = 300;
            Pesgo1.PeUserInterface.Dialog.AllowEmfExport = false;
            Pesgo1.PeUserInterface.Dialog.AllowWmfExport = false;

            // =======================================================================
            // Step 19 — Cursor and custom tooltip
            //
            // PromptStyle = ZValue: the base tooltip text is the interpolated dBA
            // value at the cursor position.
            //
            // TrackingCustomDataText = true activates the PeCustomTrackingDataText
            // event, allowing the handler below to replace or augment that text
            // with formatted Q/N, PC, and dBA values.
            // =======================================================================
            Pesgo1.PeUserInterface.HotSpot.Data                       = false;
            Pesgo1.PeUserInterface.Cursor.PromptTracking               = true;
            Pesgo1.PeUserInterface.Cursor.PromptStyle                  = CursorPromptStyle.ZValue;
            Pesgo1.PeUserInterface.Cursor.TrackingTooltipTitle         = "dBA";
            Pesgo1.PeUserInterface.Cursor.PromptLocation               = CursorPromptLocation.ToolTip;
            Pesgo1.PeUserInterface.Cursor.TrackingCustomDataText       = true;
            Pesgo1.PeUserInterface.Cursor.Hand                         = (int)Gigasoft.ProEssentials.Enums.MouseCursorStyles.Arrow;
            Pesgo1.PeUserInterface.Cursor.TrackingTooltipMaxWidth      = 300;

            // =======================================================================
            // Step 20 — Anti-aliasing
            // =======================================================================
            Pesgo1.PeConfigure.AntiAliasGraphics = true;
            Pesgo1.PeConfigure.AntiAliasText     = true;

            // =======================================================================
            // Step 21 — Rendering engine
            //
            // Composite2D3D.Foreground renders the Delaunay contour fill via
            // Direct3D, then composites the 2D axis/grid/labels on top —
            // combining GPU performance with crisp 2D text rendering.
            // =======================================================================
            Pesgo1.PeConfigure.Composite2D3D = Composite2D3D.Foreground;
            Pesgo1.PeConfigure.RenderEngine  = RenderEngine.Direct3D;

            Pesgo1.PeFunction.Force3dxNewColors      = true;
            Pesgo1.PeFunction.Force3dxVerticeRebuild = true;

            // ReinitializeResetImage applies all properties and renders.
            // Always call as the final step.
            Pesgo1.PeFunction.ReinitializeResetImage();
            Pesgo1.Invalidate();
        }

        // -----------------------------------------------------------------------
        // Pesgo1_PeCustomTrackingDataText
        //
        // Fires as the mouse moves over the contour surface.
        // Replaces the default tooltip text with a formatted multi-line
        // readout showing the cursor's Q/N (X), PC (Y), and dBA (Z) values.
        //
        // CursorValueX and CursorValueY return the data-coordinate position
        // of the cursor within the chart's grid at the time of the event.
        // The Z value is interpolated by ProEssentials from the Delaunay mesh.
        // -----------------------------------------------------------------------
        private void Pesgo1_PeCustomTrackingDataText(object sender,
            Gigasoft.ProEssentials.EventArg.CustomTrackingDataTextEventArgs e)
        {
            double dX = Pesgo1.PeUserInterface.Cursor.CursorValueX;
            double dY = Pesgo1.PeUserInterface.Cursor.CursorValueY;

            e.TrackingText = string.Format(
                "Q/N:  {0:0.0000}\nPC:   {1:0.0000}\ndBA:  {2}",
                dX, dY, e.TrackingText);
        }


        // -----------------------------------------------------------------------
        // Pesgo1_MouseMove
        //
        // Updates the window title bar with the interpolated Z (dBA) value at
        // the current cursor position — mirrors the demo app title bar behavior.
        //
        // When HotSpot.Data = false (our setting), CursorValueZ returns the
        // Delaunay-interpolated dBA value at the mouse location.
        // -----------------------------------------------------------------------
        private void Pesgo1_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point pt = Pesgo1.PeUserInterface.Cursor.LastMouseMove;
            System.Windows.Rect rect = Pesgo1.PeFunction.GetRectGraph();

            if (rect.Contains(pt))
            {
                if (Pesgo1.PeUserInterface.HotSpot.Data == false)
                {
                    this.Title = "Interpolated Z: " + string.Format("{0:0.000}", Pesgo1.PeUserInterface.Cursor.CursorValueZ);
                }
                else
                {
                    Gigasoft.ProEssentials.Structs.HotSpotData ds = Pesgo1.PeFunction.GetHotSpot();
                    if (ds.Type == Gigasoft.ProEssentials.Enums.HotSpotType.DataPoint)
                    {
                        this.Title = "Z Data: " + Pesgo1.PeData.Z[ds.Data1, ds.Data2].ToString();
                    }
                }
            }
        }

        // -----------------------------------------------------------------------
        // Window_Closing
        // -----------------------------------------------------------------------
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
