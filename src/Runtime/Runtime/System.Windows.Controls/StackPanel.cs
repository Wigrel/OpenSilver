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


using System;
using System.Collections.Generic;
using System.Linq;
using CSHTML5.Internal;

#if MIGRATION
using System.Windows.Media;
#else
using Windows.Foundation;
using Windows.UI.Xaml.Media;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Arranges child elements into a single line that can be oriented horizontally
    /// or vertically.
    /// </summary>
    /// <example>
    /// You can add a StackPanel with a Horizontal orientation in the XAML as follows:
    /// <code lang="XAML" xml:space="preserve">
    /// <StackPanel Orientation="Horizontal">
    ///     <!--Add content elements.-->
    /// </StackPanel>
    /// </code>
    /// Or in C#:
    /// <code lang="C#">
    /// StackPanel stackPanel = new StackPanel();
    /// stackPanel.Orientation = Orientation.Horizontal;
    /// </code>
    /// </example>
    public partial class StackPanel : Panel
    {
        Orientation? _renderedOrientation = null;

        /// <summary>
        /// Gets or sets the dimension by which child elements are stacked.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        /// <summary>
        /// Identifies the Orientation dependency property
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(StackPanel),
                new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, Orientation_Changed)
            { CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet });

        static void Orientation_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var stackPanel = (StackPanel)d;
            Orientation newValue = (Orientation)e.NewValue;

            if (INTERNAL_VisualTreeManager.IsElementInVisualTree(stackPanel)
                && stackPanel._renderedOrientation.HasValue
                && stackPanel._renderedOrientation.Value != newValue)
            {
                //todo: refresh the whole stackpanel (so that we display the children in the right orientation)

#if !GD_WIP
                throw new NotSupportedException("Changing the orientation of a StackPanel while it is in the visual tree is not yet supported.");
#endif
            }
        }


        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            //INTERNAL_HtmlDomManager.GetDomElementStyleForModification(div).position = "absolute";
            //div.style.position = "absolute";
            //note: size must be set in the div part only (the rest will follow).

            _renderedOrientation = this.Orientation;

            if (_renderedOrientation == Orientation.Horizontal)
            {
                //------v1------//

                //wrapper for the whole stackpanel:
                //<div>
                //  <table style="height:inherit; border-collapse:collapse">
                //    <tr>
                //          ...
                //    </tr>
                //  </table>
                //</div>

                //var table = INTERNAL_HtmlDomManager.CreateDomElement("table");
                //table.style.height = "inherit";
                //table.style.borderCollapse = "collapse";

                //var tr = INTERNAL_HtmlDomManager.CreateDomElement("tr");
                //tr.style.padding = "0px";

                //INTERNAL_HtmlDomManager.AppendChild(table, tr);
                //INTERNAL_HtmlDomManager.AppendChild(div, table);


                //domElementWhereToPlaceChildren = tr;

                //------v2------//
                //wrapper for the whole StackPanel - v2:
                //  <div style="display:table-row">
                //      <div style="margin-left: 0px; margin-right: auto; height: 100%">
                //          ...
                //      </div>
                //  </div>


                object outerDiv;
                var outerDivStyle = INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle("div", parentRef, this, out outerDiv);

                if (this.IsUnderCustomLayout)
                {
                    domElementWhereToPlaceChildren = outerDiv;

                    return outerDiv;
                }
                else if (this.IsCustomLayoutRoot)
                {
                    outerDivStyle.position = "relative";
                }

                outerDivStyle.display = "table";
                
                object innerDiv;
                var innerDivStyle = INTERNAL_HtmlDomManager.CreateDomElementAppendItAndGetStyle("div", outerDiv, this, out innerDiv);

                innerDivStyle.marginLeft = "0px";
                innerDivStyle.marginRight = "auto";
                innerDivStyle.height = "100%";
                innerDivStyle.display = "table";

                domElementWhereToPlaceChildren = innerDiv;

                return outerDiv;
            }
            else
            {
#if !BRIDGE
                return base.CreateDomElement(parentRef, out domElementWhereToPlaceChildren);
#else
                return CreateDomElement_WorkaroundBridgeInheritanceBug(parentRef, out domElementWhereToPlaceChildren);
#endif
            }
        }

        public override object CreateDomChildWrapper(object parentRef, out object domElementWhereToPlaceChild, int index)
        {
            if (this.IsUnderCustomLayout || this.IsCustomLayoutRoot)
            {
                domElementWhereToPlaceChild = null;
                return null;
            }

            if (Orientation == Orientation.Horizontal)
            {
                //------v1------//


                //NOTE: here, we are in a table

                //wrapper for each child:
                //<td style="padding:0px">
                //  <div style="width: inherit;position:relative">
                //      ...(child)
                //  </div>
                //</td>

                //var td = INTERNAL_HtmlDomManager.CreateDomElement("td");
                //td.style.position = "relative";
                //td.style.padding = "0px";
                ////var div = INTERNAL_HtmlDomManager.CreateDomElement("div");
                ////div.style.height = "inherit"; //todo: find a way to make this div actually inherit the height of the td... (otherwise we cannot set its verticalAlignment)
                ////div.style.position = "relative";
                ////INTERNAL_HtmlDomManager.AppendChild(td, div);

                //domElementWhereToPlaceChild = td;

                //return td;




                //------v2------// = better because we only use divs, it's more simple and verticalAlignment.Stretch works when the stackPanel's size is hard coded (but it still doesn't work when it's not).


                //wrapper for each child - v2
                //<div style="display: table-cell;height:inherit;>
                // ...
                //</div>

                var div = INTERNAL_HtmlDomManager.CreateDomElementAndAppendIt("div", parentRef, this, index);
                var divStyle = INTERNAL_HtmlDomManager.GetDomElementStyleForModification(div);
                divStyle.position = "relative";
                divStyle.display = "table-cell";
                divStyle.height = "100%"; //this allow the stretched items to actually be stretched to the size of the tallest element when the stackpanel's size is only defined by this element.
                divStyle.verticalAlign = "middle"; // We use this as a default value for elements that have a "stretch" vertical alignment

                domElementWhereToPlaceChild = div;


                return div;

            }
            else if (Orientation == Orientation.Vertical) //when we arrive here, it should always be true but we never know...
            {
                //NOTE: here, we are in a div


                //wrapper for each child:
                //<div style="width: inherit">... </div>

                var div = INTERNAL_HtmlDomManager.CreateDomElementAndAppendIt("div", parentRef, this, index);
                var divStyle = INTERNAL_HtmlDomManager.GetDomElementStyleForModification(div);
                divStyle.position = "relative";
                divStyle.width = "100%"; // Makes it possible to do horizontal alignment of the element that will be the child of this div.

                domElementWhereToPlaceChild = div;
                return div;
            }
            else
                throw new NotSupportedException();
        }

        protected internal override void INTERNAL_OnDetachedFromVisualTree()
        {
            _renderedOrientation = null;

            base.INTERNAL_OnDetachedFromVisualTree();
        }

        //internal override dynamic ShowChild(UIElement child)
        //{
        //    dynamic elementToReturn = base.ShowChild(child); //we need to return this so that a class that inherits from this but doesn't create a wrapper (or a different one) is correctly handled 

        //    dynamic domChildWrapper = INTERNAL_VisualChildrenInformation[child].INTERNAL_OptionalChildWrapper_OuterDomElement;
        //    dynamic domChildWrapperStyle = INTERNAL_HtmlDomManager.GetDomElementStyleForModification(domChildWrapper);
        //    //domChildWrapperStyle.visibility = "visible";
        //    domChildWrapperStyle.display = "block"; //todo: verify that it is not necessary to revert to the previous value instead.
        //    if (Orientation == Orientation.Horizontal)
        //    {
        //        domChildWrapperStyle.height = "100%";
        //        domChildWrapperStyle.width = "";
        //    }
        //    else
        //    {
        //        domChildWrapperStyle.height = "";
        //        domChildWrapperStyle.width = "100%";
        //    }

        //    return elementToReturn;
        //}

        private double GetCrossLength(Size size)
        {
            return Orientation == Orientation.Horizontal ? size.Height : size.Width;
        }

        private double GetMainLength(Size size)
        {
            return Orientation == Orientation.Horizontal ? size.Width : size.Height;
        }

        private static Size CreateSize(Orientation orientation, double mainLength, double crossLength)
        {
            return orientation == Orientation.Horizontal ?
                new Size(mainLength, crossLength) :
                new Size(crossLength, mainLength);
        }

        private static Rect CreateRect(Orientation orientation, double mainStart, double crossStart, double mainLength, double crossLength)
        {
            return orientation == Orientation.Horizontal ?
                new Rect(mainStart, crossStart, mainLength, crossLength) :
                new Rect(crossStart, mainStart, crossLength, mainLength);
        }

        internal override bool CheckIsAutoWidth(FrameworkElement child)
        {
            if (!double.IsNaN(child.Width))
            {
                return false;
            }

            if (Orientation == Orientation.Horizontal)
            {
                return true;
            }

            if (child.HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                return true;
            }

            if (VisualTreeHelper.GetParent(child) is FrameworkElement parent)
            {
                return parent.CheckIsAutoWidth(this);
            }

            return false;
        }

        internal override bool CheckIsAutoHeight(FrameworkElement child)
        {
            if (!double.IsNaN(child.Height))
            {
                return false;
            }

            if (Orientation == Orientation.Vertical)
            {
                return true;
            }

            if (child.VerticalAlignment != VerticalAlignment.Stretch)
            {
                return true;
            }

            if (VisualTreeHelper.GetParent(child) is FrameworkElement parent)
            {
                return parent.CheckIsAutoHeight(this);
            }

            return false;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double availableCrossLength = GetCrossLength(availableSize);
            Size measureSize = CreateSize(Orientation, Double.PositiveInfinity, availableCrossLength);

            double mainLength = 0;
            double crossLength = 0;

            UIElement[] childrens = Children.ToArray();
            foreach (UIElement child in childrens)
            {
                child.Measure(measureSize);

                //INTERNAL_HtmlDomElementReference domElementReference = (INTERNAL_HtmlDomElementReference)child.INTERNAL_OuterDomElement;
                //Console.WriteLine($"MeasureOverride StackPanel Child desired Width {domElementReference.UniqueIdentifier} {child.DesiredSize.Width}, Height {child.DesiredSize.Height}");

                mainLength += GetMainLength(child.DesiredSize);
                crossLength = Math.Max(crossLength, GetCrossLength(child.DesiredSize));
            }

            // measuredCrossLength = availableCrossLength;

            return CreateSize(Orientation, mainLength, crossLength);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double panelMainLength = Children.Select(child => GetMainLength(child.DesiredSize)).Sum();
            double panelCrossLength = GetCrossLength(finalSize);

            //Console.WriteLine($"StackPanel panelMainLength {panelMainLength}, panelCrossLength {panelCrossLength}");

            Size measureSize = CreateSize(Orientation, Double.PositiveInfinity, panelCrossLength);
            //Console.WriteLine($"StackPanel ArrangeOverride measureSize {measureSize.Width}, {measureSize.Height}");

            UIElement[] childrens = Children.ToArray();
            foreach (UIElement child in childrens)
            {
                child.Measure(measureSize);
            }
            bool isNormalFlow = FlowDirection == FlowDirection.LeftToRight;
            double childrenMainLength = 0;
            foreach (UIElement child in childrens)
            {
                double childMainLength = GetMainLength(child.DesiredSize);
                double childMainStart = isNormalFlow ? childrenMainLength : panelMainLength - childrenMainLength - childMainLength;

                //Console.WriteLine($"StackPanel ArrangeOverride childMainLength {childMainLength}, childMainStart {childMainStart}");

                child.Arrange(CreateRect(Orientation, childMainStart, 0, childMainLength, panelCrossLength));

                childrenMainLength += childMainLength;
            }

            return CreateSize(Orientation, GetMainLength(finalSize), panelCrossLength);
        }
    }
}
