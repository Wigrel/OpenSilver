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


//TODOBRIDGE: usefull using bridge?
#if !BRIDGE
using JSIL.Meta;
#else
using Bridge;
#endif
using CSHTML5.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using OpenSilver.Internal;
#if MIGRATION
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Automation.Peers;
#else
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Automation.Peers;
using Windows.Foundation;
#endif

#if MIGRATION
namespace System.Windows.Controls
#else
namespace Windows.UI.Xaml.Controls
#endif
{
    /// <summary>
    /// Represents a control that displays an image. The image is specified as an image file in several possible formats, see Remarks.
    /// </summary>
    /// <example>
    /// <code lang="XAML" xmlns:ms-appx="aa" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    /// <StackPanel x:Name="MyStackPanel">
    ///     <Image Source="ms-appx:/MyImage.png"/>
    /// </StackPanel>
    /// </code>
    /// <code lang="C#">
    /// Image image = new Image();
    /// image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:/MyImage.png"));
    /// MyStackPanel.Children.Add(image);
    /// </code>
    /// </example>
    public sealed partial class Image : FrameworkElement
    {
        private const string TransparentGifOnePixel = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";
        object _imageDiv = null;
        double imgWidth = 0; //might be useless, might be useful.
        double imgHeight = 0;


        /*
        /// <summary>
        /// Gets or sets a value for a nine-grid metaphor that controls how the image can be resized.
        /// Returns a  Thickness value that sets the Left, Top, Right, Bottom measurements for the nine-grid resizing metaphor.
        /// </summary>
        public Thickness NineGrid { get; set; }

        /// <summary>
        /// Identifies the NineGrid dependency property.
        /// Returns the identifier for the NineGrid dependency property.
        /// </summary>
        public static DependencyProperty NineGridProperty { get; }

        //
        // Summary:
        //     Gets the information that is transmitted if the Image is used for a "PlayTo"
        //     scenario.
        //
        // Returns:
        //     A reference object that carries the "PlayTo" source information.
        public PlayToSource PlayToSource { get; }
        //
        // Summary:
        //     Identifies the PlayToSource dependency property.
        //
        // Returns:
        //     The identifier for the PlayToSource dependency property.
        public static DependencyProperty PlayToSourceProperty { get; }
         * */

        internal override bool EnablePointerEventsCore
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the source for the image.
        /// 
        /// Returns an object that represents the image source file for the drawn image. Typically
        /// you set this with a BitmapSource object, constructed with the that describes
        /// the path to a valid image source file.
        /// </summary>
        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
        /// <summary>
        /// Identifies the Source dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ImageSource), typeof(Image), new PropertyMetadata(null, Source_Changed)
            { CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet });


        private async static void Source_Changed(DependencyObject i, DependencyPropertyChangedEventArgs e)
        {
            var image = (Image)i;
            ImageSource newValue = (ImageSource)e.NewValue;
            if (INTERNAL_VisualTreeManager.IsElementInVisualTree(image))
            {
                if (newValue is BitmapImage)
                {
                    BitmapImage bmpImage = (BitmapImage)newValue;
                    //we relay the bitmap's events:
                    bmpImage.ImageFailed -= image.bmpImage_ImageFailed;
                    bmpImage.ImageFailed += image.bmpImage_ImageFailed;
                    bmpImage.ImageOpened -= image.bmpImage_ImageOpened;
                    bmpImage.ImageOpened += image.bmpImage_ImageOpened;
                    bmpImage.UriSourceChanged -= image.bmpImage_UriSourceChanged;
                    bmpImage.UriSourceChanged += image.bmpImage_UriSourceChanged;
                }
                await image.RefreshSource();

                image.InvalidateMeasure();
            }
        }

        void bmpImage_UriSourceChanged(object sender, EventArgs e)
        {
            _ = RefreshSource();
        }

        private async Task RefreshSource()
        {
            string sImageDiv = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(_imageDiv);
            if (Source != null)
            {
                Loaded += Image_Loaded;
                var imageSrc = await Source.GetDataStringAsync();
                if (!string.IsNullOrEmpty(imageSrc))
                {
                    INTERNAL_HtmlDomManager.SetDomElementAttribute(_imageDiv, "src", imageSrc, true);
                    //set the width and height to "inherit" so the image takes up the size defined for it (and applied to _imageDiv's parent):
                    OpenSilver.Interop.ExecuteJavaScriptVoid($"{sImageDiv}.style.width = 'inherit'; {sImageDiv}.style.height = 'inherit'");
                }
            }            
            else
            {
                //If Source == null we show empty image to prevent broken image icon
                INTERNAL_HtmlDomManager.SetDomElementAttribute(_imageDiv, "src", TransparentGifOnePixel, true);

                //Set css width and height values to 0 so we don't use space for an image that should not take any. Note: if the size is specifically set in the Xaml, it will still apply on a parent dom element so it won't change the appearance.
                OpenSilver.Interop.ExecuteJavaScriptVoid($"{sImageDiv}.style.width = ''; {sImageDiv}.style.height = ''");
            }
            INTERNAL_HtmlDomManager.SetDomElementAttribute(_imageDiv, "alt", " "); //the text displayed when the image cannot be found. We set it as an empty string since there is nothing in Xaml
        }

        void Image_Loaded(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Loaded -= Image_Loaded;            
            //once the image is loaded, we get the size of the parent of this element and fit the size of the image to it.
            //This will allow us to PARTIALLY fix the problems that come with the fact that display:table is unable to limit the size of its content.

            //todo-perf: we might want to put Parent in a local variable but I doubt this would have a big impact since it only happens once when the image is loaded.
            //              If we move this to a method that will be called whenever there is a size change, we might actually want to do it (probably still minor impact on performance though).

            FrameworkElement parent = (FrameworkElement)VisualTreeHelper.GetParent(this);

            double parentWidth = parent.ActualWidth;
            double parentHeight = parent.ActualHeight;

            string sImageDiv = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(_imageDiv);
            // Hack to improve the Simulator performance by making only one interop call rather than two:
            string concatenated = OpenSilver.Interop.ExecuteJavaScriptString($"{sImageDiv}.naturalWidth + '|' + {sImageDiv}.naturalHeight");
            int sepIndex = concatenated.IndexOf('|');
            string imgWidthAsString = concatenated.Substring(0, sepIndex);
            string imgHeightAsString = concatenated.Substring(sepIndex + 1);
            double.TryParse(imgWidthAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out imgWidth); //todo: verify that the locale is OK. I think that JS by default always produces numbers in invariant culture (with "." separator).
            double.TryParse(imgHeightAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out imgHeight); //todo: read note above

            double currentWidth = ActualWidth;
            double currentHeight = ActualHeight;

            // I think it should be independent from the value of this.Stretch due to object-fit.
            // If one of the sizes is bigger than that of the container, reduce it?
            // -----------------------smaller----------------------------increase it?

            bool isParentLimitingHorizontalSize = !((parent is StackPanel && ((StackPanel)parent).Orientation == Orientation.Horizontal)
                                                    || (parent is WrapPanel && ((WrapPanel)parent).Orientation == Orientation.Horizontal)
                                                    || parent is Canvas); //todo: fill the list.
            bool limitSize = isParentLimitingHorizontalSize
                                && (HorizontalAlignment == HorizontalAlignment.Stretch && (double.IsNaN(Width) && currentWidth != parentWidth)) //is stretch and different size than the parent
                                || ((double.IsNaN(Width) && currentWidth > parentWidth));  //this can only be true if not stretch, meaning that we only want to limit the size to that of the parent.

            if (isParentLimitingHorizontalSize)
            {
                if (double.IsNaN(Width) && currentWidth != parentWidth)
                {
                    imgWidth = OpenSilver.Interop.ExecuteJavaScriptDouble($"{sImageDiv}.style.width = {parentWidth.ToInvariantString()}");
                    //_imageDiv.style.width = parentWidth;
                }
            }

            bool isParentLimitingVerticalSize = !((parent is StackPanel && ((StackPanel)parent).Orientation == Orientation.Vertical)
                                                    || (parent is WrapPanel && ((WrapPanel)parent).Orientation == Orientation.Vertical)
                                                    || parent is Canvas); //todo: fill the list.
            if (isParentLimitingVerticalSize)
            {
                if (double.IsNaN(Height) && currentHeight != parentHeight)
                {
                    imgWidth = OpenSilver.Interop.ExecuteJavaScriptDouble($"{sImageDiv}.style.height = {parentHeight.ToInvariantString()}");
                }
            }

            InvalidateMeasure();
        }

        void bmpImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            OnImageOpened(e);
        }

        void bmpImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            OnImageFailed(e);
        }


        /// <summary>
        /// Gets or sets a value that describes how an Image should be stretched to fill the destination rectangle.
        /// 
        /// Returns a value of the Stretch enumeration that specifies how the source image is applied if the Height and Width of the Image are specified and are different
        /// than the source image's height and width. The default value is Uniform.
        /// </summary>
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }
        /// <summary>
        /// Identifies the Stretch dependency property
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(Image),
                new FrameworkPropertyMetadata(Stretch.Uniform, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange, Stretch_Changed)
            { CallPropertyChangedWhenLoadedIntoVisualTree = WhenToCallPropertyChangedEnum.IfPropertyIsSet });


        static void Stretch_Changed(DependencyObject i, DependencyPropertyChangedEventArgs e)
        {
            var image = (Image)i;
            Stretch newValue = (Stretch)e.NewValue;
            if (INTERNAL_VisualTreeManager.IsElementInVisualTree(image))
            {
                string objectFitvalue = "";
                string objectPosition = "center top"; //todo: see if the values are correct for this and if it should take into consideration the Vertical/HorizontalAlignment.
                switch (newValue)
                {
                    case Stretch.None:
                        //img.width = "auto";
                        //img.height = "auto";
                        objectFitvalue = "none";
                        objectPosition = "left top";
                        break;
                    case Stretch.Fill:
                        // Commented because it should be the same as the one set by the FrameworkElement Width/Height.
                        //img.width = "100%";
                        //img.height = "100%";
                        objectFitvalue = "fill";
                        objectPosition = "center center";
                        break;
                    case Stretch.Uniform: //todo: make it work when the image needs to be made bigger to fill the container
                        //img.maxWidth = "100%";
                        //img.maxHeight = "100%";
                        objectFitvalue = "contain";
                        objectPosition = "center center";
                        break;
                    case Stretch.UniformToFill: //todo: add a negative margin top and left so that the image is centered 
                        //img.minWidth = "100%";
                        //img.minHeight = "100%";
                        objectFitvalue = "cover";
                        objectPosition = "left top";
                        break;
                    default:
                        break;
                }
                string sImageDiv = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(image._imageDiv);
                OpenSilver.Interop.ExecuteJavaScriptVoid(
                    $"{sImageDiv}.style.objectFit = \"{objectFitvalue}\";{sImageDiv}.style.objectPosition = \"{objectPosition}\"");

            }
        }

        #region Image failed event

        private DOMEventManager _imageFailedEventManager;

        private DOMEventManager ImageFailedEventManager
        {
            get
            {
                if (_imageFailedEventManager == null)
                {
                    _imageFailedEventManager = new DOMEventManager(
                        () => _imageDiv,
                        "error",
                        ProcessOnImageFailed);
                }

                return _imageFailedEventManager;
            }
        }

        private EventHandler<ExceptionRoutedEventArgs> _imageFailed;

        /// <summary>
        /// Occurs when there is an error associated with image retrieval or format.
        /// </summary>
        public event EventHandler<ExceptionRoutedEventArgs> ImageFailed
        {
            add
            {
                _imageFailed += value;

                if (_imageFailed != null)
                {
                    ImageFailedEventManager.AttachToDomEvents();
                }
            }
            remove
            {
                _imageFailed -= value;
                
                if (_imageFailed == null && _imageFailedEventManager != null)
                {
                    _imageFailedEventManager.DetachFromDomEvents();
                }
            }
        }

        /// <summary>
        /// Raises the ImageFailed event
        /// </summary>
        void ProcessOnImageFailed(object jsEventArg)
        {
            var eventArgs = new ExceptionRoutedEventArgs() //todo: fill the rest
            {
                OriginalSource = this
            };
            OnImageFailed(eventArgs);
        }

        /// <summary>
        /// Raises the ImageFailed event
        /// </summary>
        void OnImageFailed(ExceptionRoutedEventArgs eventArgs)
        {
            _imageFailed?.Invoke(this, eventArgs);
        }

        #endregion


        #region Image opened event

        private DOMEventManager _imageOpenedEventManager;
        
        private DOMEventManager ImageOpenedEventManager
        {
            get
            {
                if (_imageOpenedEventManager == null)
                {
                    _imageOpenedEventManager = new DOMEventManager(
                        () => _imageDiv, 
                        "load", 
                        ProcessOnImageOpened);
                }

                return _imageOpenedEventManager;
            }
        }

        private EventHandler<RoutedEventArgs> _imageOpened;

        /// <summary>
        /// Occurs when the image source is downloaded and decoded with no failure.
        /// </summary>
        public event EventHandler<RoutedEventArgs> ImageOpened
        {
            add
            {
                _imageOpened += value;

                if (_imageOpened != null)
                {
                    ImageOpenedEventManager.AttachToDomEvents();
                }
            }
            remove
            {
                _imageOpened -= value;

                if (_imageOpened == null && _imageOpenedEventManager != null)
                {
                    _imageOpenedEventManager.DetachFromDomEvents();
                }
            }
        }

        /// <summary>
        /// Raises the ImageOpened event
        /// </summary>
        void ProcessOnImageOpened(object jsEventArg)
        {
            RoutedEventArgs e = new RoutedEventArgs();
            e.OriginalSource = this;
            OnImageOpened(e);
        }

        /// <summary>
        /// Raises the ImageOpened event
        /// </summary>
        void OnImageOpened(RoutedEventArgs eventArgs)
        {
            _imageOpened?.Invoke(this, eventArgs);
        }

        #endregion

        //todo: create a test case for those events.
        public override void INTERNAL_AttachToDomEvents()
        {
            base.INTERNAL_AttachToDomEvents();
            
            if (_imageOpened != null)
            {
                ImageOpenedEventManager.AttachToDomEvents();
            }

            if (_imageFailed != null)
            {
                ImageFailedEventManager.AttachToDomEvents();
            }
        }

        public override void INTERNAL_DetachFromDomEvents()
        {
            base.INTERNAL_DetachFromDomEvents();
            
            _imageOpenedEventManager?.DetachFromDomEvents();
            _imageFailedEventManager?.DetachFromDomEvents();
        }

        public override object CreateDomElement(object parentRef, out object domElementWhereToPlaceChildren)
        {
            //<img style="width: 100%; height: 100%;" src="C:\Users\Sylvain\Documents\Adventure Maker v4.7\Projects\ASA_game\Icons\settings.ico" alt="settings" />
            var div = INTERNAL_HtmlDomManager.CreateDomElementAndAppendIt("div", parentRef, this);
            var intermediaryDomStyle = INTERNAL_HtmlDomManager.GetDomElementStyleForModification(div);
            intermediaryDomStyle.lineHeight = "0px"; //this one is to fix in Firefox the few pixels gap that appears below the image whith certain displays (table, table-cell and possibly some others)

            var img = INTERNAL_HtmlDomManager.CreateImageDomElementAndAppendIt(div, this);
            _imageDiv = img;
            domElementWhereToPlaceChildren = null;
            return div;
        }

        public object INTERNAL_DomImageElement
        {
            get
            {
                return _imageDiv;
            }
        }        

        protected override AutomationPeer OnCreateAutomationPeer()
            => new ImageAutomationPeer(this);

        protected override Size MeasureOverride(Size availableSize)
            => MeasureArrangeHelper(availableSize);

        protected override Size ArrangeOverride(Size finalSize)
            => MeasureArrangeHelper(finalSize);

        /// <summary>
        /// Contains the code common for MeasureOverride and ArrangeOverride.
        /// </summary>
        /// <param name="inputSize">
        /// input size is the parent-provided space that Image should use to "fit in", 
        /// according to other properties.
        /// </param>
        /// <returns>Image's desired size.</returns>
        private Size MeasureArrangeHelper(Size inputSize)
        {
            if (Source == null)
            {
                return new Size();
            }

            Size naturalSize = GetNaturalSize();

            //get computed scale factor
            Size scaleFactor = Viewbox.ComputeScaleFactor(inputSize,
                naturalSize,
                Stretch,
                StretchDirection.Both);

            // Returns our minimum size & sets DesiredSize.
            return new Size(naturalSize.Width * scaleFactor.Width, naturalSize.Height * scaleFactor.Height);
        }

        private Size GetNaturalSize()
        {
            string sDiv = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(_imageDiv);
            var size = OpenSilver.Interop.ExecuteJavaScriptString(
                $"(function(img) {{ return img.naturalWidth + '|' + img.naturalHeight; }})({sDiv});");

            int sepIndex = size.IndexOf('|');
            if (sepIndex > -1)
            {
                double actualWidth, actualHeight = 0;
                double.TryParse(size.Substring(0, sepIndex), NumberStyles.Any, CultureInfo.InvariantCulture, out actualWidth);
                double.TryParse(size.Substring(sepIndex + 1), NumberStyles.Any, CultureInfo.InvariantCulture, out actualHeight);

                return new Size(actualWidth, actualHeight);
            }

            return new Size(0, 0);
        }
    }
}
