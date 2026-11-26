using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Hardware.RobotControl
{
    public class LineFollower
    {
        public LineFollower(RobotCtrl robotControl)
        {
            Threshold = 0.2;
            InvertImage = true;
            EqualizeImage = false;
            _robotControl = robotControl;
        }

        #region Parameters

        /// <summary>
        /// The active robot control using this line-follower.
        /// </summary>
        private RobotCtrl _robotControl;

        /// <summary>
        /// The line detection mode in use.
        /// </summary>
        private LineDetectionMode _mode = LineDetectionMode.Contours;
        /// <summary>
        /// The line detection mode in use.
        /// </summary>
        public LineDetectionMode Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
            }
        }
        /// <summary>
        /// The threshold-value (percentage of pixels) above which a block is assumed to contain a line.
        /// </summary>
        public double Threshold { get; set; }
        /// <summary>
        /// Indicates whether the image shall be equalized before being processed.
        /// </summary>
        public bool EqualizeImage { get; set; }
        /// <summary>
        /// Indicates whether the image will get inverted.
        /// </summary>
        public bool InvertImage { get; set; }
        /// <summary>
        /// The number of erode image passes.
        /// </summary>
        private int _erodeImagePasses = 5;
        /// <summary>
        /// The number of erode image passes. (Minimum is 1 and Maximum is 6)
        /// </summary>
        public int ErodeImagePasses
        {
            get { return _erodeImagePasses; }
            set
            {
                _erodeImagePasses = Math.Max(value, 1);
                _erodeImagePasses = Math.Min(_erodeImagePasses, 6);
            }
        }
        /// <summary>
        /// Number of dilate image passes.
        /// </summary>
        private int _dilateImagePasses = 0;
        /// <summary>
        /// Number of dilate image passes. (Minimum is 0 and Maximum is 6)
        /// </summary>
        public int DilateImagePasses
        {
            get { return _dilateImagePasses; }
            set
            {
                _dilateImagePasses = Math.Max(value, 0);
                _dilateImagePasses = Math.Min(_dilateImagePasses, 6);
            }
        }
        /// <summary>
        /// The size of the kernel area when smoothing by using the median. (Use 0 to deactivate smoothing or use an odd number smaller than 50)
        /// </summary>
        private int _medianImageSmoothingSize = 0;
        /// <summary>
        /// The size of the kernel area when smoothing by using the median. (Use 0 to deactivate smoothing or use an odd number smaller than 55)
        /// </summary>
        public int MedianImageSmoothingSize
        {
            get { return _medianImageSmoothingSize; }
            set
            {
                // Set to 0 if lower or zero to indicate deactivated smoothing
                if (value <= 0)
                { _medianImageSmoothingSize = 0; return; }
                // Only odd numbers
                if (value % 2 != 1)
                    value++;
                // Set it
                _medianImageSmoothingSize = value;
            }
        }
        /// <summary>
        /// The sigma values for bilateral image smoothing. (Use 0 to deactivate the smoothing operation)
        /// </summary>
        private int _bilateralImageSmoothingSigmas = 0;
        /// <summary>
        /// The sigma values for bilateral image smoothing. (Use 0 to deactivate the smoothing operation)
        /// </summary>
        public int BilateralImageSmoothingSigmas
        {
            get { return _bilateralImageSmoothingSigmas; }
            set
            {
                // Set to 0 if lower or zero to indicate deactivated smoothing
                if (value <= 0)
                { _bilateralImageSmoothingSigmas = 0; return; }
                // Set it
                _bilateralImageSmoothingSigmas = value;
            }
        }
        /// <summary>
        /// Number of closing iterations done. (0 indicates no closing at all.)
        /// </summary>
        private int _closingIterations = 0;
        /// <summary>
        /// Number of closing iterations done. (0 indicates no closing at all.)
        /// </summary>
        public int ClosingIterations
        {
            get { return _closingIterations; }
            set
            {
                // Set to 0 if lower or zero to indicate deactivated closing
                if (value <= 0)
                { _closingIterations = 0; return; }
                // Set it
                _closingIterations = value;
            }
        }
        /// <summary>
        /// Indicates whether a more detailed anylization of the contours is done.
        /// </summary>
        public bool ContourPostProcessing { get; set; }
        /// <summary>
        /// Defines the size of the view area.
        /// </summary>
        private Rectangle _viewArea;
        /// <summary>
        /// Defines the size of the view area.
        /// </summary>
        private Rectangle ViewArea
        {
            get { return _viewArea; }
            set { _viewArea = value; }
        }
        /// <summary>
        /// Checks whether the view area was initialized.
        /// </summary>
        /// <returns><code>true</code> if the view area was initialized, <code>false</code> otherwise.</returns>
        private bool IsViewAreaInit() { return !(_viewArea.X == 0 && _viewArea.Y == 0 && _viewArea.Width == 0 && _viewArea.Height == 0); }
        /// <summary>
        ///  The width of the blocks.
        /// </summary>
        public int BlockWidth
        {
            get { return ViewArea.Width; }
            set
            {
                if (_originalFrameDimensions != null)
                {
                    ViewArea = new Rectangle(
                        (_originalFrameDimensions.Width - value) / 2,
                        (_originalFrameDimensions.Height - ViewArea.Height) / 2,
                        value,
                        ViewArea.Height);
                    InitializeBlocks(_originalFrameDimensions);
                }
            }
        }
        /// <summary>
        /// The height of the blocks.
        /// </summary>
        public int BlockHeight
        {
            get { return ViewArea.Height; }
            set
            {
                if (_originalFrameDimensions != null)
                {
                    ViewArea = new Rectangle(
                        (_originalFrameDimensions.Width - ViewArea.Width) / 2,
                        (_originalFrameDimensions.Height - value) / 2,
                        ViewArea.Width,
                        value);
                    InitializeBlocks(_originalFrameDimensions);
                }
            }
        }
        /// <summary>
        /// Number of vertical blocks.
        /// </summary>
        private int _blockCountVertical = 2;
        /// <summary>
        /// Number of horizontal blocks.
        /// </summary>
        private int _blockCountHorizontal = 5;

        /// <summary>
        /// The left border of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        private double _focusedLineSectionLeftBorder = 0.2;
        /// <summary>
        /// The right border of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        private double _focusedLineSectionRightBorder = 0.8;
        /// <summary>
        /// The upper border of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        private double _focusedLineSectionTopBorder = 0.35;
        /// <summary>
        /// The lower border of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        private double _focusedLineSectionBottomBorder = 0.1;
        /// <summary>
        /// The left offset of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        public double FocusedLineSectionLeftOffset { get { return _focusedLineSectionLeftBorder; } set { _focusedLineSectionLeftBorder = value; } }
        /// <summary>
        /// The right offset of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        public double FocusedLineSectionRightOffset { get { return 1 - _focusedLineSectionRightBorder; } set { _focusedLineSectionRightBorder = 1 - value; } }
        /// <summary>
        /// The upper offset of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        public double FocusedLineSectionTopOffset { get { return 1 - _focusedLineSectionTopBorder; } set { _focusedLineSectionTopBorder = 1 - value; } }
        /// <summary>
        /// The lower offset of the section within which a line is recognized as focussed. The value is relative to the region of interest used by image processing.
        /// </summary>
        public double FocusedLineSectionBottomOffset { get { return _focusedLineSectionBottomBorder; } set { _focusedLineSectionBottomBorder = value; } }

        #endregion

        #region Image processing fields

        /// <summary>
        /// The result list of line detection.
        /// </summary>
        internal LineRecognitionResult Result;
        /// <summary>
        /// The rectangles defining the blocks.
        /// </summary>
        private Rectangle[][] _rectangles;
        /// <summary>
        /// The first set of points marking the boundings of the rectangles.
        /// </summary>
        private Point[][] _rectanglePoints1;
        /// <summary>
        /// The size of the rectangles.
        /// </summary>
        private Size[][] _rectanglePoints2;
        /// <summary>
        /// The dimensions of the camera image.
        /// </summary>
        private Rectangle _originalFrameDimensions;
        /// <summary>
        /// The width of the camera image.
        /// </summary>
        public int OriginalFrameWidth { get { return _originalFrameDimensions != null ? _originalFrameDimensions.Width : 0; } }
        /// <summary>
        /// The height of the camera image.
        /// </summary>
        public int OriginalFrameHeight { get { return _originalFrameDimensions != null ? _originalFrameDimensions.Height : 0; } }
        /// <summary>
        /// The lower limit for filtering the image.
        /// </summary>
        private Hsv _lowerRangeLimit = new Hsv();
        /// <summary>
        /// The upper limit for filtering the image.
        /// </summary>
        private Hsv _upperRangeLimit = new Hsv();

        #endregion

        #region Performance measuring

        /// <summary>
        /// The number of images processed overall.
        /// </summary>
        private int _imagesProcessed;
        /// <summary>
        /// The number of images processed on the last poll.
        /// </summary>
        private int _imagesProcessedLastPoll;
        /// <summary>
        /// The last time we measured the FPS.
        /// </summary>
        private DateTime _lastFPSMeasurement;
        /// <summary>
        /// The time after which the FPS counter is refreshed.
        /// </summary>
        private TimeSpan _fpsMeasurementDelay = TimeSpan.FromSeconds(1);
        /// <summary>
        /// The current FPS processed.
        /// </summary>
        public double CurrentFPS { get; private set; }
        /// <summary>
        /// The number of images processed overall.
        /// </summary>
        public int ImagesProcessedOverall { get { return _imagesProcessed; } }
        /// <summary>
        /// The aggregated time spent in image processing.
        /// </summary>
        private TimeSpan _timeSpentInIP = TimeSpan.Zero;
        /// <summary>
        /// The time of the last performance measurement.
        /// </summary>
        private DateTime _lastIPMeasurement;
        /// <summary>
        /// The time after which the IP performance is re-evaluated.
        /// </summary>
        private TimeSpan _ipMeasurementDelay = TimeSpan.FromSeconds(1);
        /// <summary>
        /// The time spent in image processing as a percentage of the real-time elapsed during 3 s.
        /// </summary>
        public double CurrentTimeSpentInImageProcessing { get; private set; }
        /// <summary>
        /// Number of hough frames processed with a recognized line.
        /// </summary>
        private int _houghFramesWithLine = 0;
        /// <summary>
        /// Number of hough frames processed at whole.
        /// </summary>
        private int _houghFramesWhole = 0;
        /// <summary>
        /// The fraction of successful line detections in terms of processed frames while in hough mode.
        /// </summary>
        public double CurrentHoughAccuracy { get { return _houghFramesWhole != 0 ? (double)_houghFramesWithLine / (double)_houghFramesWhole : 0; } }
        #endregion

        #region Camera Improvement
        /// <summary>
        /// The average HSV-color from the automatic camera improvement.
        /// </summary>
        private Hsv _overallAvgLineColor;
        /// <summary>
        /// Indicates if the areas for error logging are drawn.
        /// </summary>
        public bool DrawAreasForErrorLog = false;
        /// <summary>
        /// Indicates if the error log for camera improvement is active.
        /// </summary>
        public bool EnableErrorLogForCameraImprovement = false;
        /// <summary>
        /// Size of the line in the processed frame.
        /// </summary>
        public int LineWidth = 100;
        /// <summary>
        /// Size of the margin around the LineWidth.
        /// </summary>
        public int LineWidthMargin = 25;
        /// <summary>
        /// The delay between two error logs for camera improvement.
        /// </summary>
        public int ErrorLogCameraImprovementDelay = 5;
        /// <summary>
        /// Time of the last error log for camera improvement.
        /// </summary>
        private DateTime _lastErrorLogCameraImprovement;
        /// <summary>
        /// Id of the current error log entry.
        /// </summary>
        public int ErrorLogId = 0;
        #endregion

        /// <summary>
        /// Checks whether the given point lies within the area considered the center.
        /// <remarks>The function assumes that a potential line was detected at all.</remarks>
        /// </summary>
        /// <param name="x">The x-value of the point.</param>
        /// <param name="y">The y-value of the point.</param>
        /// <returns><code>true</code> if the point is within the given area, <code>false</code> otherwise.</returns>
        private bool IsCenterInFocus(double x, double y)
        {
            // Transform to focus area relative coordinates
            y = 1 - y;
            // Check
            return
                // See whether x is in focus
                ((Mode == LineDetectionMode.Blocks && _blockCountHorizontal <= 2) || // When in block mode and only 2 or less blocks its always centered if there is any
                _focusedLineSectionLeftBorder <= x && x <= _focusedLineSectionRightBorder) && // See whether the coordinate is within the focus area
                                                                                            // See whether y is in focus
                ((Mode == LineDetectionMode.Blocks && _blockCountVertical <= 2) || // When in block mode and only 2 or less blocks its always centered if there is any
                _focusedLineSectionBottomBorder <= y && y <= _focusedLineSectionTopBorder); // See whether the coordinate is within the focus area

        }

        /// <summary>
        /// Initializes the sizes of the blocks.
        /// </summary>
        /// <param name="originalFrameDimensions">The size of the original frame.</param>
        private void InitializeBlocks(Rectangle originalFrameDimensions)
        {
            // Remember frame dimensions
            _originalFrameDimensions = originalFrameDimensions;
            // Initialize focus area
            if (!IsViewAreaInit())
            {
                int focusAreaWidth = _originalFrameDimensions.Width - 200; int focusAreaHeight = _originalFrameDimensions.Height - 200;
                ViewArea = new Rectangle(
                    (_originalFrameDimensions.Width - focusAreaWidth) / 2,
                    (_originalFrameDimensions.Height - focusAreaHeight) / 2,
                    focusAreaWidth,
                    focusAreaHeight);
            }
            // Generate rectangles of proper dimension
            Point topLeft = new Point((originalFrameDimensions.Width - ViewArea.Width) / 2, (originalFrameDimensions.Height - ViewArea.Height) / 2);
            Size blockSize = new Size(ViewArea.Width / _blockCountHorizontal, ViewArea.Height / _blockCountVertical);
            _rectangles = new Rectangle[_blockCountVertical][];
            _rectanglePoints1 = new Point[_blockCountVertical][];
            _rectanglePoints2 = new Size[_blockCountVertical][];
            Result = new LineRecognitionResult() { AnyLine = false, Blocks = new LineFollowerBlock[_blockCountVertical][], ProminentBlock = new LineFollowerBlock() };
            for (int vertIndex = 0; vertIndex < _blockCountVertical; vertIndex++)
            {
                _rectangles[vertIndex] = new Rectangle[_blockCountHorizontal];
                _rectanglePoints1[vertIndex] = new Point[_blockCountHorizontal];
                _rectanglePoints2[vertIndex] = new Size[_blockCountHorizontal];
                Result.Blocks[vertIndex] = new LineFollowerBlock[_blockCountHorizontal];
                for (int horizIndex = 0; horizIndex < _blockCountHorizontal; horizIndex++)
                {
                    _rectangles[vertIndex][horizIndex] =
                        new Rectangle(topLeft.X + horizIndex * blockSize.Width, topLeft.Y + vertIndex * blockSize.Height, blockSize.Width, blockSize.Height);
                    _rectanglePoints1[vertIndex][horizIndex] =
                        new Point(_rectangles[vertIndex][horizIndex].X, _rectangles[vertIndex][horizIndex].Y);
                    _rectanglePoints2[vertIndex][horizIndex] =
                        new Size(_rectangles[vertIndex][horizIndex].Width, _rectangles[vertIndex][horizIndex].Height);
                    Result.Blocks[vertIndex][horizIndex].X =
                        (_rectangles[vertIndex][horizIndex].X + 0.5 * _rectangles[vertIndex][horizIndex].Width) / originalFrameDimensions.Width;
                    Result.Blocks[vertIndex][horizIndex].Y =
                        (_rectangles[vertIndex][horizIndex].Y + 0.5 * _rectangles[vertIndex][horizIndex].Height) / originalFrameDimensions.Height;
                    Result.Blocks[vertIndex][horizIndex].CenterInPixels =
                        new Point()
                        {
                            X = _rectanglePoints1[vertIndex][horizIndex].X + _rectanglePoints2[vertIndex][horizIndex].Width / 2,
                            Y = _rectanglePoints1[vertIndex][horizIndex].Y + _rectanglePoints2[vertIndex][horizIndex].Height / 2
                        };

                }
            }
        }
        /// <summary>
        /// Sets the size of the blocks.
        /// </summary>
        /// <param name="bottomOffset">The relative offset from the bottom.</param>
        /// <param name="topOffset">The relative offset from the top.</param>
        /// <param name="leftOffset">The relative offset from the left.</param>
        /// <param name="rightOffset">The relative offset from the right.</param>
        /// <param name="horizontalCount">Number of horizontal boxes.</param>
        /// <param name="verticalCount">Number of vertical boxes.</param>
        public void SetBlockParams(double bottomOffset, double topOffset, double leftOffset, double rightOffset, int horizontalCount, int verticalCount)
        {
            if (_originalFrameDimensions != null)
            {
                ViewArea = new Rectangle(
                    (int)(_originalFrameDimensions.Width * leftOffset),
                    (int)(_originalFrameDimensions.Height * topOffset),
                    (int)(_originalFrameDimensions.Width * (1 - rightOffset - leftOffset)),
                    (int)(_originalFrameDimensions.Height * (1 - bottomOffset - topOffset)));
                _blockCountHorizontal = horizontalCount;
                _blockCountVertical = verticalCount;
                InitializeBlocks(_originalFrameDimensions);
            }
        }

        //Using a "liked" sensor array the processor can calculate the error.
        //For line following anything between 6 to 10 sensors (depending on your robot) will be sufficient. 
        private void CheckBlocks(Image<Gray, Byte>[][] blockList)
        {
            // Calc value of the blocks
            int[][] blockResults = blockList.AsParallel().Select(b => b.AsParallel().Select(inner => CheckSingleBlockCV(inner)).ToArray()).ToArray();

            // Re-init result list and mark blocks above threshold
            bool anyLine = false; int maxValue = 0; int maxVertIndex = 0; int maxHorizIndex = 0;
            for (int vertIndex = 0; vertIndex < blockList.Length; vertIndex++)
                for (int horizIndex = 0; horizIndex < blockList[vertIndex].Length; horizIndex++)
                {
                    if ((blockResults[vertIndex][horizIndex] != 0))
                    {
                        Result.Blocks[vertIndex][horizIndex].Value = 2;
                        anyLine = true;
                        if (blockResults[vertIndex][horizIndex] > maxValue)
                        {
                            maxValue = blockResults[vertIndex][horizIndex];
                            maxVertIndex = vertIndex; maxHorizIndex = horizIndex;
                        }
                    }
                    else
                        Result.Blocks[vertIndex][horizIndex].Value = 0;
                }

            // Mark the maximal value of the block list
            if (anyLine)
            {
                Result.Blocks[maxVertIndex][maxHorizIndex].Value = 1;
                Result.ProminentBlock = Result.Blocks[maxVertIndex][maxHorizIndex];
                if (IsCenterInFocus(Result.ProminentBlock.X, Result.ProminentBlock.Y))
                    Result.LineInFocus = true;
                else
                    Result.LineInFocus = false;
                Result.AnyLine = true;
            }
            else
            {
                Result.AnyLine = false;
                Result.LineInFocus = false;
            }
        }

        /// <summary>
        /// Checks the value of the given block.
        /// </summary>
        /// <param name="grayBlock">The block to check.</param>
        /// <returns>The value of the block if it is above the threshold.</returns>
        private int CheckSingleBlockCV(Image<Gray, Byte> grayBlock)
        {
            // Get maximal value
            int maxValue = grayBlock.Width * grayBlock.Height;
            // Calculate block's value
            int value = maxValue - grayBlock.CountNonzero()[0];
            // Return result (if above threshold)
            if (value > Threshold * maxValue) return value;
            else return 0;
        }
        /// <summary>
        /// Checks the value of the given block.
        /// </summary>
        /// <param name="grayBlock">The block to check.</param>
        /// <returns>The value of the block if it is above the threshold.</returns>
        private int CheckSingleBlockSelf(Image<Gray, Byte> grayBlock)
        {
            // Define a step value to skip many pixels for performance sakes (we probably do not need to look at all pixels of the block to determine whether there is a line or not)
            int pixelSkip = 3;
            // Init threshold and value
            int threshold = (int)((grayBlock.Width / pixelSkip) * (grayBlock.Height / pixelSkip) * Threshold);
            int value = 0;
            // Analyze columns
            for (int v = 0; v < grayBlock.Height; v += pixelSkip)
                // Analyze lines
                for (int u = 0; u < grayBlock.Width; u += pixelSkip)
                    // Get pixel color (fast way)
                    if (grayBlock.Data[v, u, 0] == 0) // white:255, black:0
                        value++;
            // Return the resulting value (if above threshold)
            if (value > threshold) return value;
            else return 0;
        }

        /// <summary>
        /// Attempts to detect lines in the given image.
        /// </summary>
        /// <param name="originalFrame">The image to process. Colored blocks are painted on the image to give user feedback.</param>
        /// <param name="linesDetected">The result of the line detection.</param>
        /// <param name="grayImage">The resulting gray image used for line recognition.</param>
        /// <param name="lowerLimit">The lower limit to filter the image by.</param>
        /// <param name="upperLimit">The upper limit to filter the image by.</param>
        public void DetectLine(ref Image<Bgr, Byte> originalFrame, ref Image<Gray, Byte> grayImage, Hsv lowerLimit, Hsv upperLimit)
        {
            if (originalFrame != null)
            {
                // Measure time spent in this routine
                DateTime before = DateTime.Now;

                // Init the rectangles for the blocks once
                if (!IsViewAreaInit() || _originalFrameDimensions == null || _originalFrameDimensions.Width != originalFrame.Width || _originalFrameDimensions.Height != originalFrame.Height)
                    InitializeBlocks(new Rectangle(0, 0, originalFrame.Width, originalFrame.Height));

                // Measure FPS
                _imagesProcessed++;
                if (_lastFPSMeasurement == null)
                    _lastFPSMeasurement = DateTime.Now;
                else
                {
                    DateTime now = DateTime.Now;
                    TimeSpan delay = now - _lastFPSMeasurement;
                    if (delay > _fpsMeasurementDelay)
                    {
                        CurrentFPS = (_imagesProcessed - _imagesProcessedLastPoll) / delay.TotalSeconds;
                        _imagesProcessedLastPoll = _imagesProcessed;
                        _lastFPSMeasurement = now;
                    }
                }

                // Init working image
                Image<Gray, Byte> grayImageWork = null;

                #region Preprocessing
                // >>> Preprocessing image
                if (Mode == LineDetectionMode.Blocks || Mode == LineDetectionMode.Contours || Mode == LineDetectionMode.RevisedContour)
                {
                    // >>> Convert the image to HSV
                    Image<Hsv, Byte> hsv = originalFrame.Convert<Hsv, byte>();
                    // >>> Smooth the image using bilateral
                    if (_bilateralImageSmoothingSigmas > 0)
                        hsv = hsv.SmoothBilatral(5, _bilateralImageSmoothingSigmas, _bilateralImageSmoothingSigmas);
                    // >>> Filter the image
                    if (lowerLimit.Hue > upperLimit.Hue || lowerLimit.Satuation > upperLimit.Satuation || lowerLimit.Value > upperLimit.Value)
                    {
                        // Filter first pass
                        if (lowerLimit.Hue > upperLimit.Hue) { _lowerRangeLimit.Hue = 0; _upperRangeLimit.Hue = lowerLimit.Hue; }
                        else { _lowerRangeLimit.Hue = lowerLimit.Hue; _upperRangeLimit.Hue = upperLimit.Hue; }
                        if (lowerLimit.Satuation > upperLimit.Satuation) { _lowerRangeLimit.Satuation = 0; _upperRangeLimit.Satuation = lowerLimit.Satuation; }
                        else { _lowerRangeLimit.Satuation = lowerLimit.Satuation; _upperRangeLimit.Satuation = upperLimit.Satuation; }
                        if (lowerLimit.Value > upperLimit.Value) { _lowerRangeLimit.Value = 0; _upperRangeLimit.Value = lowerLimit.Value; }
                        else { _lowerRangeLimit.Value = lowerLimit.Value; _upperRangeLimit.Value = upperLimit.Value; }
                        Image<Gray, byte> firstPass = hsv.InRange(_lowerRangeLimit, _upperRangeLimit);
                        // Filter second pass
                        if (lowerLimit.Hue > upperLimit.Hue) { _lowerRangeLimit.Hue = upperLimit.Hue; _upperRangeLimit.Hue = 180; }
                        else { _lowerRangeLimit.Hue = lowerLimit.Hue; _upperRangeLimit.Hue = upperLimit.Hue; }
                        if (lowerLimit.Satuation > upperLimit.Satuation) { _lowerRangeLimit.Satuation = upperLimit.Satuation; _upperRangeLimit.Satuation = 255; }
                        else { _lowerRangeLimit.Satuation = lowerLimit.Satuation; _upperRangeLimit.Satuation = upperLimit.Satuation; }
                        if (lowerLimit.Value > upperLimit.Value) { _lowerRangeLimit.Value = upperLimit.Value; _upperRangeLimit.Value = 255; }
                        else { _lowerRangeLimit.Value = lowerLimit.Value; _upperRangeLimit.Value = upperLimit.Value; }
                        Image<Gray, byte> secondPass = hsv.InRange(_lowerRangeLimit, _upperRangeLimit);
                        // Combine
                        grayImageWork = firstPass.And(secondPass);
                    }
                    else
                    {
                        grayImageWork = hsv.InRange(lowerLimit, upperLimit);
                    }
                    // >>> Using morphological operations to reduce noise
                    // Equalize image
                    if (EqualizeImage)
                        CvInvoke.EqualizeHist(grayImageWork, grayImageWork);
                    // >>> Smooth the image using median
                    if (_medianImageSmoothingSize > 0)
                        grayImageWork = grayImageWork.SmoothMedian(_medianImageSmoothingSize);
                    // Erode image
                    if (_erodeImagePasses > 0)
                        grayImageWork = grayImageWork.Erode(_erodeImagePasses);
                    if (_dilateImagePasses > 0)
                        grayImageWork = grayImageWork.Dilate(_dilateImagePasses);
                    // Invert image
                    if (InvertImage)
                        grayImageWork = grayImageWork.Not();
                    // Closing
                    if (_closingIterations > 0)
                    {
                        //StructuringElementEx element = new StructuringElementEx(3, 3, 1, 1, Emgu.CV.CvEnum.ElementShape.Rectangle);
                        //grayImageWork = grayImageWork.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Close, null /* TODO specify kernel */, null /* TODO specify anchor */, _closingIterations, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
                    }
                }
                #endregion

                // >>> Main processing
                switch (Mode)
                {
                    case LineDetectionMode.Blocks:
                        {
                            #region Block mode processing

                            // >>> Build blocks
                            Image<Gray, Byte>[][] grayBlocks = _rectangles.Select(rows => { return rows.Select(rect => { grayImageWork.ROI = rect; return grayImageWork.Copy(); }).ToArray(); }).ToArray();
                            // Reset ROI of gray image to view it afterwards
                            grayImageWork.ROI = _originalFrameDimensions;

                            // >>> Analyze position of the line
                            CheckBlocks(grayBlocks);

                            #endregion
                        }
                        break;
                    case LineDetectionMode.Hough:
                        {
                            #region Hough mode processing

                            // Convert and filter the image
                            grayImageWork = originalFrame.Convert<Hsv, byte>().InRange(lowerLimit, upperLimit);
                            // Smooth the image
                            grayImageWork = grayImageWork.SmoothGaussian(75);
                            // Use canny edge detection to get all edges in the image
                            grayImageWork = grayImageWork.Canny(15, 50);
                            // Use hough transformation to explicitly detect the lines in the image
                            var houghLines = grayImageWork.HoughLinesBinary(3, Math.PI / 180, 100, 170, 25);
                            // Draw the lines on the image for visual feedback
                            var color = new Bgr(255, 255, 0);
                            bool lineDetected = false;
                            foreach (var line in houghLines)
                            {
                                foreach (var segLine in line.Where(l => Math.Abs(l.Direction.Y) > Math.Abs(l.Direction.X)))
                                {
                                    originalFrame.Draw(segLine, color, 3);
                                    lineDetected = true;
                                }
                            }
                            // Estimate position of the line we are following
                            if (lineDetected)
                            {
                                // TODO use clustering to better determine center
                                // Calculate estimated middle of the line
                                float midY = originalFrame.Height / 2.0f;
                                float avgXAtMidY = houghLines.SelectMany(l => l).Where(l => Math.Abs(l.Direction.Y) > Math.Abs(l.Direction.X)).Average(l =>
                                {
                                    // If the line is completely vertical just return x
                                    if (l.Direction.X == 0)
                                    {
                                        return l.P1.X;
                                    }
                                    else
                                    {
                                        // Calculate intersection with horizontal line in the middle of the original frame
                                        float m = l.Direction.Y / l.Direction.X;
                                        float n = l.P1.Y - (m * l.P1.X);
                                        float x = (midY - n) / m;
                                        return x;
                                    }
                                });
                                // Give visual feedback about the gravity center of the lines
                                var colorCircle = new MCvScalar(0, 255, 0);
                                CvInvoke.Circle(originalFrame, new Point((int)avgXAtMidY, (int)midY), 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                _houghFramesWithLine++;
                                // Indicate position of the line center
                                Result.ProminentBlock = new LineFollowerBlock()
                                {
                                    X = avgXAtMidY / _originalFrameDimensions.Width,
                                    Y = midY / _originalFrameDimensions.Height,
                                    CenterInPixels = new Point((int)avgXAtMidY, (int)midY)
                                };
                                Result.AnyLine = true;
                                if (IsCenterInFocus(Result.ProminentBlock.X, Result.ProminentBlock.Y))
                                    Result.LineInFocus = true;
                                else
                                    Result.LineInFocus = false;
                            }
                            else
                            {
                                Result.AnyLine = false;
                                Result.LineInFocus = false;
                            }
                            // Log processed frames
                            _houghFramesWhole++;

                            #endregion
                        }
                        break;
                    case LineDetectionMode.Contours:
                        {
                            #region Contour mode processing

                            // Prepare.
                            grayImageWork._Not();
                            Image<Gray, byte> grayImageRoi = new Image<Gray, byte>(ViewArea.Width, ViewArea.Height);
                            grayImageRoi = grayImageWork.Copy(ViewArea);
                            Image<Gray, byte> grayImageTestNew = new Image<Gray, byte>(grayImageWork.Width, grayImageWork.Height);

                            // Filter.
                            grayImageTestNew.ROI = ViewArea;
                            CvInvoke.cvCopy(grayImageRoi, grayImageTestNew, IntPtr.Zero);
                            grayImageTestNew.ROI = new Rectangle(0, 0, originalFrame.Width, originalFrame.Height);
                            // See if there is a line at all
                            bool anyLine = false;
                            double tallestBBoxX = -1; double tallestBBoxY = -1; int tallestBoxWidth = -1; int tallestBoxHeight = -1; Rectangle tallestBoxRect = Rectangle.Empty;

                            // Extract the contours
                            IEnumerable<Point> tallestContour = null;
                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                CvInvoke.FindContours(grayImageTestNew, contours, null, Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                                for (int i = 0; i < contours.Size; i++)
                                {
                                    VectorOfPoint currentContour = new VectorOfPoint();
                                    CvInvoke.ApproxPolyDP(contours[i], currentContour, CvInvoke.ArcLength(contours[i], true) * 0.015, true);
                                    Rectangle boundingrectangle = CvInvoke.BoundingRectangle(currentContour);

                                    if (boundingrectangle.Width * boundingrectangle.Height > 100 &&
                                        boundingrectangle.Height > 50)
                                    {
                                        // Draw contours
                                        CvInvoke.DrawContours(originalFrame, contours, i, new MCvScalar(0, 255, 255), 2);
                                        originalFrame.Draw(boundingrectangle, new Bgr(0, 255, 0), 1);

                                        // Get bounding rectangle
                                        Rectangle boundRect = boundingrectangle;

                                        // Keep track of biggest bounding box
                                        if (boundRect.Height > tallestBoxHeight)
                                        {
                                            tallestContour = currentContour.ToArray();
                                            tallestBoxWidth = boundRect.Width;
                                            tallestBoxHeight = boundRect.Height;
                                            tallestBoxRect = boundRect;
                                        }

                                        // Indicate that we found a possible line
                                        anyLine = true;
                                    }
                                }
                            }

                            // Approximate line center
                            if (anyLine)
                            {
                                var colorCircle = new MCvScalar(0, 0, 255);
                                if (ContourPostProcessing)
                                {
                                    // Draw the center point of the rect.
                                    Point leftMost = tallestContour.OrderByDescending(p => p.Y).ThenBy(p => p.X).Take(2).OrderBy(p => p.X).First();
                                    Point rightMost = tallestContour.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).Take(2).OrderByDescending(p => p.X).First();
                                    CvInvoke.Circle(originalFrame, leftMost, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    CvInvoke.Circle(originalFrame, rightMost, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    // Approximate center
                                    tallestBBoxX = leftMost.X + (rightMost.X - leftMost.X) / 2; tallestBBoxY = leftMost.Y + (rightMost.Y - leftMost.Y) / 2;
                                }
                                else
                                {
                                    // Draw the center point of the rect.
                                    Point pt = new Point()
                                    {
                                        X = tallestBoxRect.X + tallestBoxRect.Width / 2,
                                        Y = tallestBoxRect.Y + tallestBoxRect.Height / 2
                                    };
                                    CvInvoke.Circle(originalFrame, pt, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    // Approximate center
                                    tallestBBoxX = tallestBoxRect.X + tallestBoxRect.Width / 2;
                                    tallestBBoxY = tallestBoxRect.Y + tallestBoxRect.Height / 2;
                                }
                            }

                            // Submit result
                            if (anyLine)
                            {
                                Result.ProminentBlock = new LineFollowerBlock()
                                {
                                    X = tallestBBoxX / _originalFrameDimensions.Width,
                                    Y = tallestBBoxY / _originalFrameDimensions.Height,
                                    CenterInPixels = new Point((int)tallestBBoxX, (int)tallestBBoxY)
                                };
                                Result.AnyLine = anyLine;
                                Result.LineInFocus = tallestBoxRect.Width > 20 && tallestBoxRect.Height > 50 && IsCenterInFocus(Result.ProminentBlock.X, Result.ProminentBlock.Y);
                            }
                            else
                            {
                                Result.AnyLine = anyLine;
                                Result.LineInFocus = false;
                            }

                            #endregion
                        }
                        break;
                    case LineDetectionMode.RevisedContour:
                        {
                            #region Revised contour mode processing

                            // Prepare.
                            grayImageWork._Not();
                            Image<Gray, byte> grayImageRoi = new Image<Gray, byte>(ViewArea.Width, ViewArea.Height);
                            grayImageRoi = grayImageWork.Copy(ViewArea);
                            Image<Gray, byte> grayImageTestNew = new Image<Gray, byte>(grayImageWork.Width, grayImageWork.Height);

                            // Filter.
                            grayImageTestNew.ROI = ViewArea;
                            CvInvoke.cvCopy(grayImageRoi, grayImageTestNew, IntPtr.Zero);
                            grayImageTestNew.ROI = new Rectangle(0, 0, originalFrame.Width, originalFrame.Height);
                            // See if there is a line at all
                            bool anyLine = false;
                            double tallestBBoxX = -1; double tallestBBoxY = -1; int tallestBoxWidth = -1; int tallestBoxHeight = -1; Rectangle tallestBoxRect = Rectangle.Empty;

                            // Extract the contours
                            IEnumerable<Point> tallestContour = null;
                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                CvInvoke.FindContours(grayImageTestNew, contours, null, Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                                for (int i = 0; i < contours.Size; i++)
                                {
                                    VectorOfPoint currentContour = new VectorOfPoint();
                                    CvInvoke.ApproxPolyDP(contours[i], currentContour, CvInvoke.ArcLength(contours[i], true) * 0.015, true);
                                    Rectangle boundingrectangle = CvInvoke.BoundingRectangle(currentContour);

                                    if (boundingrectangle.Width * boundingrectangle.Height > 100 &&
                                        boundingrectangle.Height > 50)
                                    {
                                        // Draw contours
                                        CvInvoke.DrawContours(originalFrame, contours, i, new MCvScalar(0, 255, 255), 2);
                                        originalFrame.Draw(boundingrectangle, new Bgr(0, 255, 0), 1);

                                        // Keep track of biggest bounding box
                                        if (boundingrectangle.Height > tallestBoxHeight)
                                        {
                                            tallestContour = currentContour.ToArray();
                                            tallestBoxWidth = boundingrectangle.Width;
                                            tallestBoxHeight = boundingrectangle.Height;
                                            tallestBoxRect = boundingrectangle;
                                        }

                                        // Indicate that we found a possible line
                                        anyLine = true;
                                    }
                                }
                            }

                            // Approximate line center
                            if (anyLine)
                            {
                                var colorCircle = new MCvScalar(0, 0, 255);
                                if (ContourPostProcessing)
                                {
                                    // Draw the center point of the rect.
                                    Point leftMost = tallestContour.OrderByDescending(p => p.Y).ThenBy(p => p.X).Take(2).OrderBy(p => p.X).First();
                                    Point rightMost = tallestContour.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).Take(2).OrderByDescending(p => p.X).First();
                                    CvInvoke.Circle(originalFrame, leftMost, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    CvInvoke.Circle(originalFrame, rightMost, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    // Approximate center
                                    tallestBBoxX = leftMost.X + (rightMost.X - leftMost.X) / 2; tallestBBoxY = leftMost.Y + (rightMost.Y - leftMost.Y) / 2;
                                }
                                else
                                {
                                    // Draw the center point of the rect.
                                    Point pt = new Point()
                                    {
                                        X = tallestBoxRect.X + tallestBoxRect.Width / 2,
                                        Y = tallestBoxRect.Y + tallestBoxRect.Height / 2
                                    };
                                    CvInvoke.Circle(originalFrame, pt, 10, colorCircle, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                                    // Approximate center
                                    tallestBBoxX = tallestBoxRect.X + tallestBoxRect.Width / 2;
                                    tallestBBoxY = tallestBoxRect.Y + tallestBoxRect.Height / 2;
                                }
                            }

                            // Submit result
                            if (anyLine)
                            {
                                Result.ProminentBlock = new LineFollowerBlock()
                                {
                                    X = tallestBBoxX / _originalFrameDimensions.Width,
                                    Y = tallestBBoxY / _originalFrameDimensions.Height,
                                    CenterInPixels = new Point((int)tallestBBoxX, (int)tallestBBoxY)
                                };
                                Result.AnyLine = anyLine;
                                Result.LineInFocus = tallestBoxRect.Width > 20 && tallestBoxRect.Height > 50 && IsCenterInFocus(Result.ProminentBlock.X, Result.ProminentBlock.Y);
                            }
                            else
                            {
                                Result.AnyLine = anyLine;
                                Result.LineInFocus = false;
                            }

                            #endregion
                        }
                        break;
                    default:
                        break;
                }

                // >>> Draw crosshair and ROI for orientation
                var colorCrosshair = new MCvScalar(0, 0, 0); int crossharWidth = 20; int crosshairThickness = 1;
                // Draw testpoints for camera improvement
                DrawTestpoints(originalFrame, new MCvScalar(255, 255, 255), false);
                // Draw crosshair
                CvInvoke.Line(originalFrame,
                    new Point((_originalFrameDimensions.Width / 2) - crossharWidth, _originalFrameDimensions.Height / 2 - crossharWidth),
                    new Point((_originalFrameDimensions.Width / 2) + crossharWidth, _originalFrameDimensions.Height / 2 + crossharWidth),
                    colorCrosshair, crosshairThickness, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                CvInvoke.Line(originalFrame,
                    new Point(_originalFrameDimensions.Width / 2 + crossharWidth, (_originalFrameDimensions.Height / 2) - crossharWidth),
                    new Point(_originalFrameDimensions.Width / 2 - crossharWidth, (_originalFrameDimensions.Height / 2) + crossharWidth),
                    colorCrosshair, crosshairThickness, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                // Draw ROI area
                CvInvoke.Rectangle(originalFrame, ViewArea, colorCrosshair, crosshairThickness, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                // Draw focus area
                int focusAreaX = (int)(_originalFrameDimensions.X + _originalFrameDimensions.Width * _focusedLineSectionLeftBorder);
                int focusAreaY = (int)(_originalFrameDimensions.Y + _originalFrameDimensions.Height - (_originalFrameDimensions.Height * _focusedLineSectionTopBorder));
                CvInvoke.Rectangle(originalFrame, new Rectangle(
                    new Point(focusAreaX, focusAreaY),
                    new Size(
                        (int)(_originalFrameDimensions.Width * _focusedLineSectionRightBorder) - focusAreaX,
                        (int)(_originalFrameDimensions.Height - (_originalFrameDimensions.Height * _focusedLineSectionBottomBorder) - focusAreaY))),
                    colorCrosshair, crosshairThickness, Emgu.CV.CvEnum.LineType.AntiAlias, 0);

                // >>> Draw the resulting blocks for visual feedback
                if (Mode == LineDetectionMode.Blocks)
                {
                    for (int vertIndex = 0; vertIndex < _blockCountVertical; vertIndex++)
                        for (int horizIndex = 0; horizIndex < _blockCountHorizontal; horizIndex++)
                        {
                            if (Result.Blocks[vertIndex][horizIndex].Value == 1)
                                // Draw green rectangle for detected line
                                CvInvoke.Rectangle(originalFrame, new Rectangle(_rectanglePoints1[vertIndex][horizIndex], _rectanglePoints2[vertIndex][horizIndex]), new MCvScalar(0, 255, 0), 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                            else if (Result.Blocks[vertIndex][horizIndex].Value == 2)
                                // Draw yellow rectangle for block with value above threshold
                                CvInvoke.Rectangle(originalFrame, new Rectangle(_rectanglePoints1[vertIndex][horizIndex], _rectanglePoints2[vertIndex][horizIndex]), new MCvScalar(0, 255, 255), 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                            else // 1
                                // Draw red rectangle for block with value below threshold
                                CvInvoke.Rectangle(originalFrame, new Rectangle(_rectanglePoints1[vertIndex][horizIndex], _rectanglePoints2[vertIndex][horizIndex]), new MCvScalar(0, 0, 255), 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                        }
                }
                // Give visual feedback about the assumed position of the line
                if (Result.AnyLine)
                {
                    var colorLineCenter = Result.LineInFocus ? new MCvScalar(0, 255, 0) : new MCvScalar(0, 255, 255);
                    CvInvoke.Circle(originalFrame, new Point((int)Result.ProminentBlock.CenterInPixels.X, (int)Result.ProminentBlock.CenterInPixels.Y), 10, colorLineCenter, 5, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                }

                // Log the camera improvement error
                LogCameraImprovementError(grayImageWork);
                // Draw the areas for error logging
                DrawCameraImprovementErrorLogAreas(grayImageWork);

                // >>> Return the result for display purpose
                grayImage = grayImageWork;

                // Measure time spent in this routine
                _timeSpentInIP += DateTime.Now - before;
                if (_lastIPMeasurement == null)
                    _lastIPMeasurement = DateTime.Now;
                else
                {
                    DateTime now = DateTime.Now;
                    TimeSpan delay = now - _lastIPMeasurement;
                    if (delay > _ipMeasurementDelay)
                    {
                        CurrentTimeSpentInImageProcessing = _timeSpentInIP.TotalSeconds / delay.TotalSeconds;
                        _timeSpentInIP = TimeSpan.Zero;
                        _lastIPMeasurement = now;
                    }
                }
            }
        }

        #region Camera improvement
        /// <summary>
        /// Resets the overall global line color detected so far.
        /// </summary>
        public void ResetCameraImprovement()
        {
            // Resets the global average line color
            _overallAvgLineColor = new Hsv();
        }

        /// <summary>
        /// Returns the list of testpoints for camera improvement.
        /// </summary>
        /// <param name="frame">The frame to explore.</param>
        /// <returns>List of rectangles (testpoints).</returns>
        private List<Rectangle> getTestpointList(Image<Bgr, byte> frame)
        {
            List<Rectangle> lst = new List<Rectangle>();
            // Top of the frame
            lst.Add(new System.Drawing.Rectangle(frame.Width / 2 - 15, 50, 30, 30));
            // Center of the frame
            lst.Add(new System.Drawing.Rectangle(frame.Width / 2 - 15, frame.Height / 2 - 15, 30, 30));
            // Bottom of the frame
            lst.Add(new System.Drawing.Rectangle(frame.Width / 2 - 15, frame.Height - 80, 30, 30));

            return lst;
        }

        /// <summary>
        /// Improves the filter settings for line detection.
        /// </summary>
        /// <param name="originalFrame">The original frame from the camera.</param>
        /// <param name="processedFrame">The processed frame.</param>
        /// <returns>The Hsv value for the filter settings.</returns>
        public Hsv ImproveCamera(Image<Bgr, byte> originalFrame, Image<Gray, byte> processedFrame)
        {
            List<Hsv> pointsHsv = new List<Hsv>();
            List<Lab> pointsLab = new List<Lab>();
            Image<Bgr, byte> currentFrame = originalFrame;

            foreach (Rectangle rect in getTestpointList(currentFrame))
            {
                pointsHsv.Add(GetAvgHsvFromFrameByRectangle(currentFrame, rect));
                pointsLab.Add(GetAvgLabFromFrameByRectangle(currentFrame, rect));
            }

            // Gets the average HSV-Value from all testpoints
            Hsv avgHsvValue = GetAvgHsvByList(pointsHsv, pointsLab);

            // Sets the global average line color
            if (_overallAvgLineColor.Equals(new Hsv()))
                _overallAvgLineColor = avgHsvValue;
            else
                _overallAvgLineColor = GetAvgHsvByList(new List<Hsv>() { avgHsvValue, _overallAvgLineColor }, null);

            return avgHsvValue;
        }

        /// <summary>
        /// Draw testpoints for camera improvement.
        /// </summary>
        /// <param name="frame">The frame to draw in.</param>
        /// <param name="filled">Draw filled rectangle.</param>
        private void DrawTestpoints(Image<Bgr, byte> frame, MCvScalar color, bool filled)
        {
            foreach (Rectangle rect in getTestpointList(frame))
            {
                int thickness = filled ? -1 : 1;
                CvInvoke.Rectangle(frame, rect, color, thickness, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
            }
        }

        /// <summary>
        /// Gets the average Hsv value from a list of Hsv values.
        /// </summary>
        /// <param name="hsvValues">List of Hsv values.</param>
        /// <param name="labValues">List of Lab values.</param>
        /// <returns>The average Hsv value from the list.</returns>
        private Hsv GetAvgHsvByList(List<Hsv> hsvValues, List<Lab> labValues)
        {
            // Gets the average HSV-Value from a list of HSV-Values
            if (!_overallAvgLineColor.Equals(new Hsv()) && labValues != null)
            {
                Lab overallLineColorLab = ConvertHsvToLab(_overallAvgLineColor);
                List<Hsv> ignoreColors = new List<Hsv>();

                foreach (Hsv hsv in hsvValues)
                {
                    // Proof, if the current color is similar to the global average line color
                    double deltaE = GetColorSimilarity(overallLineColorLab, ConvertHsvToLab(hsv));
                    if (deltaE > _robotControl.ColorSimilarityDeltaE)
                        ignoreColors.Add(hsv);
                }

                // Removes colors, which are not similar to the global line color
                foreach (Hsv ignore in ignoreColors)
                    hsvValues.Remove(ignore);
            }

            // Calculates the HSV-Values
            Hsv avgHsvValue = _overallAvgLineColor;
            if (hsvValues.Count > 0)
            {
                avgHsvValue = new Hsv();
                avgHsvValue.Hue = hsvValues.Average(hsv => hsv.Hue);
                avgHsvValue.Satuation = hsvValues.Average(hsv => hsv.Satuation);
                avgHsvValue.Value = hsvValues.Average(hsv => hsv.Value);
            }
            return avgHsvValue;
        }

        /// <summary>
        /// Converts Hsv to Lab.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>The Lab value for the given Hsv value.</returns>
        private Lab ConvertHsvToLab(Hsv color)
        {
            // Converts a HSV color to a LAB color
            MCvScalar scalar = new MCvScalar();
            Lab labValue = new Lab();

            Image<Hsv, Byte> currentColorHsv = new Image<Hsv, byte>(1, 1, color);
            Image<Bgr, Byte> currentColorBgr = new Image<Bgr, byte>(1, 1);
            Image<Lab, Byte> currentColorLab = new Image<Lab, byte>(1, 1);

            Emgu.CV.CvInvoke.CvtColor(currentColorHsv, currentColorBgr, Emgu.CV.CvEnum.ColorConversion.Hsv2Bgr);
            Emgu.CV.CvInvoke.CvtColor(currentColorBgr, currentColorLab, Emgu.CV.CvEnum.ColorConversion.Bgr2Lab);

            currentColorLab.Convert<Lab, byte>().AvgSdv(out labValue, out scalar);

            return labValue;
        }

        /// <summary>
        /// Gets the average Hsv value from a rectangle cut out of the given frame.
        /// </summary>
        /// <param name="frame">The frame to cut the rectangle out.</param>
        /// <param name="rectangle">The rectangle to cut out.</param>
        /// <returns>The average Hsv value from the specific rectangle.</returns>
        private Hsv GetAvgHsvFromFrameByRectangle(Image<Bgr, byte> frame, System.Drawing.Rectangle rectangle)
        {
            // Gets the average HSV-Value from a rectagle of the original camera frame
            MCvScalar scalar = new MCvScalar();
            Hsv hsvValue = new Hsv();
            Image<Bgr, Byte> currentFrame = frame.Copy(rectangle);
            Image<Hsv, Byte> hsvFrame = new Image<Hsv, byte>(currentFrame.Width, currentFrame.Height);
            Emgu.CV.CvInvoke.CvtColor(currentFrame, hsvFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);
            currentFrame.Convert<Hsv, byte>().AvgSdv(out hsvValue, out scalar);

            return hsvValue;
        }

        /// <summary>
        /// Gets the average Lab value from a rectangle cut out of the given frame.
        /// </summary>
        /// <param name="frame">The frame to cut the rectangle out.</param>
        /// <param name="rectangle">The rectangle to cut out.</param>
        /// <returns>The average Lab value from the specific rectangle.</returns>
        private Lab GetAvgLabFromFrameByRectangle(Image<Bgr, byte> frame, System.Drawing.Rectangle rectangle)
        {
            // Gets the average LAB-Value from a rectagle of the original camera frame
            MCvScalar scalar = new MCvScalar();
            Lab labValue = new Lab();
            Image<Bgr, Byte> currentFrame = frame.Copy(rectangle);
            Image<Lab, Byte> labFrame = new Image<Lab, byte>(currentFrame.Width, currentFrame.Height);
            Emgu.CV.CvInvoke.CvtColor(currentFrame, labFrame, Emgu.CV.CvEnum.ColorConversion.Bgr2Lab);
            labFrame.Convert<Lab, byte>().AvgSdv(out labValue, out scalar);

            return labValue;
        }

        /// <summary>
        /// Gets the similarity of two colors. The lower the value, the more similar are the color.
        /// </summary>
        /// <param name="colorA">The first color as Lab.</param>
        /// <param name="colorB">The second color as Lab.</param>
        /// <returns>Delta e value for similarity.</returns>
        private double GetColorSimilarity(Lab colorA, Lab colorB)
        {
            // Calculates the similarity of two Lab colors
            return Math.Sqrt(Math.Pow(colorA.X - colorB.X, 2) + Math.Pow(colorA.Y - colorB.Y, 2) + Math.Pow(colorA.Z - colorB.Z, 2));
        }

        /// <summary>
        /// Logs the error score.
        /// </summary>
        /// <param name="grayImage">The processed frame.</param>
        public void LogCameraImprovementError(Image<Gray, byte> grayImage)
        {
            if (DateTime.Now - _lastErrorLogCameraImprovement > TimeSpan.FromSeconds(ErrorLogCameraImprovementDelay))
            {
                if (EnableErrorLogForCameraImprovement)
                {
                    string dictionaryName = "_CameraImprovementLog";
                    string dictionaryNameScreens = "screens";

                    if (!System.IO.Directory.Exists(System.IO.Path.Combine(dictionaryName, dictionaryNameScreens)))
                        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dictionaryName, dictionaryNameScreens));

                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(dictionaryName, "cameraImprovementLog.txt"), true))
                    {
                        double errorScore = GetErrorScore(grayImage);
                        sw.WriteLine(ErrorLogId + ";" + DateTime.Now.ToShortDateString() + ";" + DateTime.Now.ToLongTimeString() + ";" + errorScore + ";" + Math.Pow(errorScore, 2));
                        grayImage.Save(System.IO.Path.Combine(dictionaryName, dictionaryNameScreens, ErrorLogId + ".png"));
                        ErrorLogId++;
                    }
                }

                _lastErrorLogCameraImprovement = DateTime.Now;
            }
        }

        /// <summary>
        /// Draws the areas for error logging for camera improvement.
        /// </summary>
        /// <param name="grayImage">Processed image frame.</param>
        public void DrawCameraImprovementErrorLogAreas(Image<Gray, byte> grayImage)
        {
            if (DrawAreasForErrorLog)
            {
                int halfLineWidth = LineWidth / 2;
                MCvScalar color = new MCvScalar(100, 0, 0);
                Rectangle rect;

                // Left bar
                rect = new System.Drawing.Rectangle(grayImage.Width / 2 - halfLineWidth - LineWidthMargin, 0, LineWidthMargin, grayImage.Height);
                CvInvoke.Rectangle(grayImage, rect, color, -1, Emgu.CV.CvEnum.LineType.AntiAlias, 0);

                // Right bar
                rect = new System.Drawing.Rectangle(grayImage.Width / 2 + halfLineWidth, 0, LineWidthMargin, grayImage.Height);
                CvInvoke.Rectangle(grayImage, rect, color, -1, Emgu.CV.CvEnum.LineType.AntiAlias, 0);

                //// Left space
                //rect = new System.Drawing.Rectangle(0, 0, grayImage.Width / 2 - halfLineWidth - LineWidthMargin, grayImage.Height);
                //CvInvoke.Rectangle(grayImage, rect, color, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);

                //// Middle (the line)
                //rect = new System.Drawing.Rectangle(grayImage.Width / 2 - halfLineWidth, 0, LineWidth, grayImage.Height);
                //CvInvoke.Rectangle(grayImage, rect, color, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);

                //// Right space
                //rect = new System.Drawing.Rectangle(grayImage.Width / 2 + halfLineWidth + LineWidthMargin, 0, grayImage.Width - (grayImage.Width / 2 + halfLineWidth + LineWidthMargin), grayImage.Height);
                //CvInvoke.Rectangle(grayImage, rect, color, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
            }
        }

        /// <summary>
        /// Calculates the error of line detection. The lower, the better.
        /// </summary>
        /// <param name="grayImage">The processed frame.</param>
        /// <returns>The Error of line detection.</returns>
        private double GetErrorScore(Image<Gray, byte> grayImage)
        {
            int halfLineWidth = LineWidth / 2;

            // Left space
            double whiteL = GetWhitePixelPercentage(grayImage, new System.Drawing.Rectangle(0, 0, grayImage.Width / 2 - halfLineWidth - LineWidthMargin, grayImage.Height));
            // Middle (the line)
            double whiteC = GetWhitePixelPercentage(grayImage, new System.Drawing.Rectangle(grayImage.Width / 2 - halfLineWidth, 0, LineWidth, grayImage.Height));
            // Right space
            double whiteR = GetWhitePixelPercentage(grayImage, new System.Drawing.Rectangle(grayImage.Width / 2 + halfLineWidth + LineWidthMargin, 0, grayImage.Width - (grayImage.Width / 2 + halfLineWidth + LineWidthMargin), grayImage.Height));

            return (whiteL + whiteR) / 4 + (1 - whiteC) / 2;
        }

        /// <summary>
        /// Gets the percentage of white pixels in a given rectangle of the given frame.
        /// </summary>
        /// <param name="grayImage">The processed frame.</param>
        /// <param name="rect">The rectangle to detect.</param>
        /// <returns>Percentage of white pixels.</returns>
        private double GetWhitePixelPercentage(Image<Gray, byte> grayImage, System.Drawing.Rectangle rect)
        {
            Image<Gray, Byte> grayFrame = grayImage.Copy(rect);
            return CvInvoke.CountNonZero(grayFrame.GetInputArray().GetMat()) / (grayFrame.Width * grayFrame.Height * 1.0);
        }

        #endregion
    }

    /// <summary>
    /// Differentiates the different line detection modes.
    /// </summary>
    public enum LineDetectionMode
    {
        /// <summary>
        /// Indicates line detection using simple blocks.
        /// </summary>
        Blocks,
        /// <summary>
        /// Indicates line detection using hough lines.
        /// </summary>
        Hough,
        /// <summary>
        /// Indicates line detection using contour extraction.
        /// </summary>
        Contours,
        /// <summary>
        /// Indicates line detection using revised contour extraction.
        /// </summary>
        RevisedContour
    }

    /// <summary>
    /// Stores the line detection result of processing one frame.
    /// </summary>
    public class LineRecognitionResult
    {
        /// <summary>
        /// Indicates whether there is any line at all.
        /// </summary>
        public bool AnyLine { get; set; }
        /// <summary>
        /// Indicates whether the line was recognized within the "focus"-area.
        /// </summary>
        public bool LineInFocus { get; set; }
        /// <summary>
        /// The block containing information about where the line was actually located.
        /// </summary>
        public LineFollowerBlock ProminentBlock { get; set; }
        /// <summary>
        /// All blocks used in block detection mode.
        /// </summary>
        public LineFollowerBlock[][] Blocks { get; set; }
        public override string ToString()
        {
            return
                "line:" + AnyLine.ToString() +
                ";prominent:" + ProminentBlock.X.ToString("F2", CultureInfo.InvariantCulture) + "," + ProminentBlock.Y.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Contains information about a block used in block detection mode or the position of the resulting line.
    /// </summary>
    public struct LineFollowerBlock
    {
        /// <summary>
        /// The value indicating whether a line was detected in this block.
        /// </summary>
        public int Value;
        /// <summary>
        /// X-value of the center of the block within the FOV as a value between 0 and 1.
        /// </summary>
        public double X;
        /// <summary>
        /// Y-value of the center of the block within the FOV as a value between 0 and 1.
        /// </summary>
        public double Y;
        /// <summary>
        /// The center of the block in x-pixels.
        /// </summary>
        public Point CenterInPixels;
        public override string ToString() { return "(" + X.ToString("F2", CultureInfo.InvariantCulture) + "," + Y.ToString("F2", CultureInfo.InvariantCulture) + "," + Value.ToString() + ")"; }
    }
}
