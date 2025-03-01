﻿

/*===================================================================================
* 
*   Copyright (c) Userware/OpenSilver.net
*      
*   This file is part of the OpenSilver Runtime (https://opensilver.net), which is
*   licensed under the MIT license: https://opensource.org/licenses/MIT
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/

using CSHTML5.Internal;
using System;
using OpenSilver.Internal;

#if MIGRATION
using System.Windows.Media;
#else
using Windows.UI.Xaml.Media;
using Windows.Foundation;
#endif

#if MIGRATION
namespace System.Windows.Shapes
#else
namespace Windows.UI.Xaml.Shapes
#endif
{
    /// <summary>
    /// Draws a straight line between two points.
    /// </summary>
    public partial class Line : Shape
    {
        //internal dynamic canvasDomElement;

        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            return INTERNAL_ShapesDrawHelpers.CreateDomElementForPathAndSimilar(this, parentRef, out _canvasDomElement, out domElementWhereToPlaceChildren);

            //domElementWhereToPlaceChildren = null;
            //var canvas = INTERNAL_HtmlDomManager.CreateDomElementAndAppendIt("canvas", parentRef, this);
            //return canvas;
        }

        //protected internal override void INTERNAL_OnAttachedToVisualTree()
        //{
        //    ScheduleRedraw();
        //}

        /// <summary>
        /// Gets or sets the x-coordinate of the Line start point.
        /// The default is 0.
        /// </summary>
        public double X1
        {
            get { return (double)GetValue(X1Property); }
            set { SetValue(X1Property, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Line.X1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X1Property =
            DependencyProperty.Register(
                nameof(X1), 
                typeof(double), 
                typeof(Line), 
                new PropertyMetadata(0d, X1_Changed));

        private static void X1_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Line line = (Line)d;
            line.ScheduleRedraw();
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the Line end point.
        /// The default is 0.
        /// </summary>
        public double X2
        {
            get { return (double)GetValue(X2Property); }
            set { SetValue(X2Property, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Line.X2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty X2Property =
            DependencyProperty.Register(
                nameof(X2), 
                typeof(double), 
                typeof(Line), 
                new PropertyMetadata(0d, X2_Changed));

        private static void X2_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Line line = (Line)d;
            line.ScheduleRedraw();
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the Line start point.
        /// The default is 0.
        /// </summary>
        public double Y1
        {
            get { return (double)GetValue(Y1Property); }
            set { SetValue(Y1Property, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Line.Y1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y1Property =
            DependencyProperty.Register(
                nameof(Y1), 
                typeof(double), 
                typeof(Line), 
                new PropertyMetadata(0d, Y1_Changed));

        private static void Y1_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Line line = (Line)d;
            line.ScheduleRedraw();
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the Line end point.
        /// The default is 0.
        /// </summary>
        public double Y2
        {
            get { return (double)GetValue(Y2Property); }
            set { SetValue(Y2Property, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Line.Y2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Y2Property =
            DependencyProperty.Register(
                nameof(Y2), 
                typeof(double), 
                typeof(Line), 
                new PropertyMetadata(0d, Y2_Changed));

        private static void Y2_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Line line = (Line)d;
            line.ScheduleRedraw();
        }

        internal void GetMinMaxXY(ref double minX, ref double maxX, ref double minY, ref double maxY)
        {
            double maxAbs = X1 > X2 ? X1 : X2;
            double minAbs = X1 < X2 ? X1 : X2;
            double minOrd = Y1 < Y2 ? Y1 : Y2;
            double maxOrd = Y1 > Y2 ? Y1 : Y2;
            if (maxX < maxAbs)
            {
                maxX = maxAbs;
            }
            if (maxY < maxOrd)
            {
                maxY = maxOrd;
            }
            if (minX > minAbs)
            {
                minX = minAbs;
            }
            if (minY > minOrd)
            {
                minY = minOrd;
            }
        }

        override internal protected void Redraw()
        {
            if (INTERNAL_VisualTreeManager.IsElementInVisualTree(this))
            {
                double minX = X1;
                double minY = Y1;
                double maxX = X2;
                double maxY = Y2;
                if (X1 > X2)
                {
                    minX = X2;
                    maxX = X1;
                }
                if (Y1 > Y2)
                {
                    minY = Y2;
                    maxY = Y1;
                }

                Size shapeActualSize;
                INTERNAL_ShapesDrawHelpers.PrepareStretch(this, _canvasDomElement, minX, maxX, minY, maxY, Stretch, out shapeActualSize);

                double horizontalMultiplicator;
                double verticalMultiplicator;
                double xOffsetToApplyBeforeMultiplication;
                double yOffsetToApplyBeforeMultiplication;
                double xOffsetToApplyAfterMultiplication;
                double yOffsetToApplyAfterMultiplication;
                INTERNAL_ShapesDrawHelpers.GetMultiplicatorsAndOffsetForStretch(this, StrokeThickness, minX, maxX, minY, maxY, Stretch, shapeActualSize, out horizontalMultiplicator, out verticalMultiplicator, out xOffsetToApplyBeforeMultiplication, out yOffsetToApplyBeforeMultiplication, out xOffsetToApplyAfterMultiplication, out yOffsetToApplyAfterMultiplication, out _marginOffsets);

                ApplyMarginToFixNegativeCoordinates(new Point());

                if (Stretch == Stretch.None)
                {
                    ApplyMarginToFixNegativeCoordinates(_marginOffsets);
                }

                object context = OpenSilver.Interop.ExecuteJavaScriptAsync($"{CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(_canvasDomElement)}.getContext('2d')"); //Note: we do not use INTERNAL_HtmlDomManager.Get2dCanvasContext here because we need to use the result in ExecuteJavaScript, which requires the value to come from a call of ExecuteJavaScript.
                string sContext = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(context);
                //we remove the previous drawing:
                OpenSilver.Interop.ExecuteJavaScriptFastAsync($"{sContext}.clearRect(0,0, {shapeActualSize.Width.ToInvariantString()}, {shapeActualSize.Height.ToInvariantString()})");


                //double preparedX1 = (X1 + xOffsetToApplyBeforeMultiplication) * horizontalMultiplicator + xOffsetToApplyAfterMultiplication;
                //double preparedX2 = (X2 + xOffsetToApplyBeforeMultiplication) * horizontalMultiplicator + xOffsetToApplyAfterMultiplication;
                //double preparedY1 = (Y1 + yOffsetToApplyBeforeMultiplication) * verticalMultiplicator + yOffsetToApplyAfterMultiplication;
                //double preparedY2 = (Y2 + yOffsetToApplyBeforeMultiplication) * verticalMultiplicator + yOffsetToApplyAfterMultiplication;
                //Note: we replaced the code above with the one below because Bridge.NET has an issue when adding "0" to an Int64 (as of May 1st, 2020), so it is better to first multiply and then add, rather than the contrary:
                double preparedX1 = X1 * horizontalMultiplicator + xOffsetToApplyBeforeMultiplication * horizontalMultiplicator + xOffsetToApplyAfterMultiplication;
                double preparedX2 = X2 * horizontalMultiplicator + xOffsetToApplyBeforeMultiplication * horizontalMultiplicator + xOffsetToApplyAfterMultiplication;
                double preparedY1 = Y1 * verticalMultiplicator + yOffsetToApplyBeforeMultiplication * verticalMultiplicator + yOffsetToApplyAfterMultiplication;
                double preparedY2 = Y2 * verticalMultiplicator + yOffsetToApplyBeforeMultiplication * verticalMultiplicator + yOffsetToApplyAfterMultiplication;

                //todo: if possible, manage strokeStyle and lineWidth in their respective methods (Stroke_Changed and StrokeThickness_Changed) then use context.save() and context.restore() (can't get it to work yet).
                double opacity = Stroke == null ? 1 : Stroke.Opacity;
                object strokeValue = GetHtmlBrush(this, context, Stroke, opacity, minX, minY, maxX, maxY, horizontalMultiplicator, verticalMultiplicator, xOffsetToApplyBeforeMultiplication, yOffsetToApplyBeforeMultiplication, shapeActualSize);

                //we set the StrokeDashArray:
                if (strokeValue != null && StrokeThickness > 0)
                {
                    double thickness = StrokeThickness;
                    OpenSilver.Interop.ExecuteJavaScriptFastAsync($@"
{sContext}.strokeStyle = {CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(strokeValue)}");
                    OpenSilver.Interop.ExecuteJavaScriptFastAsync($@"
{sContext}.lineWidth = {CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(StrokeThickness)}");
                    if (StrokeDashArray != null)
                    {
                        // TODO
                    }
                }


                INTERNAL_ShapesDrawHelpers.PrepareLine(_canvasDomElement, new Point(preparedX1, preparedY1), new Point(preparedX2, preparedY2));

                if (strokeValue != null)
                    OpenSilver.Interop.ExecuteJavaScriptFastAsync($"{sContext}.strokeStyle = {CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(strokeValue)}");

                //context.strokeStyle = strokeAsString; //set the shape's lines color
                OpenSilver.Interop.ExecuteJavaScriptFastAsync($"{sContext}.lineWidth= {CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(StrokeThickness)}");
                //context.lineWidth = StrokeThickness.ToString();
                if (Stroke != null && StrokeThickness > 0)
                {
                    OpenSilver.Interop.ExecuteJavaScriptFastAsync($"{sContext}.stroke()"); //draw the line
                    //context.stroke(); //draw the line
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(Math.Max(0, X2.Max(X1)), Math.Max(0, Y2.Max(Y1)));
        }

    }
}