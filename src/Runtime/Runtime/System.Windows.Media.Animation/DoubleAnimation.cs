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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#if MIGRATION
using System.Windows.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

#if MIGRATION
namespace System.Windows.Media.Animation
#else
namespace Windows.UI.Xaml.Media.Animation
#endif
{

    /// <summary>
    /// Animates the value of a Double property between two target values using linear
    /// interpolation over a specified Duration.
    /// </summary>
    public partial class DoubleAnimation : AnimationTimeline
    {
        public IEasingFunction EasingFunction
        {
            get { return (EasingFunctionBase)GetValue(EasingFunctionProperty); }
            set { SetValue(EasingFunctionProperty, value); }
        }

        /// <summary>
        /// Identifies the EasingFunction dependency property.
        /// </summary>
        public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(DoubleAnimation), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the animation's starting value.
        /// </summary>
        public double? From
        {
            get { return (double?)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }
        /// <summary>
        /// Identifies the From dependency property.
        /// </summary>
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(double?), typeof(DoubleAnimation), new PropertyMetadata(null));



        // Returns:
        //     The ending value of the animation. The default is null. If you are programming
        //     using C# or Visual Basic, the type of this property is projected as double?
        //     (a nullable double).
        /// <summary>
        /// Gets or sets the animation's ending value.
        /// </summary>
        public double? To
        {
            get { return (double?)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }
        /// <summary>
        /// Identifies the To dependency property.
        /// </summary>
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(double?), typeof(DoubleAnimation), new PropertyMetadata(null));

        internal override void GetTargetInformation(IterationParameters parameters)
        {
            _parameters = parameters;
            DependencyObject target;
            PropertyPath propertyPath;

            GetTargetElementAndPropertyInfo(parameters, out target, out propertyPath);

            _propertyContainer = target;
            _targetProperty = propertyPath;
            _propDp = GetProperty(_propertyContainer, _targetProperty);
            _target = Storyboard.GetTarget(this);
            _targetName = Storyboard.GetTargetName(this);
        }

        // This guid is used to specifically target a particular call to the animation. It prevents the callback which should be called when velocity's animation end 
        // to be called when the callback is called from a previous call to the animation. This could happen when the animation was started quickly multiples times in a row. 
        private Guid _animationID;

        internal override void Apply(IterationParameters parameters, bool isLastLoop)
        {
            if (To != null)
            {
                // - Get the propertyMetadata from the property
                PropertyMetadata propertyMetadata = _propDp.GetTypeMetaData(_propertyContainer.GetType());

                //we make a specific name for this animation:
                string specificGroupName = animationInstanceSpecificName.ToString();

                _animationID = Guid.NewGuid();

                bool cssEquivalentExists = false;
                if (propertyMetadata.GetCSSEquivalent != null)
                {
                    CSSEquivalent cssEquivalent = propertyMetadata.GetCSSEquivalent(_propertyContainer);
                    if (cssEquivalent != null)
                    {
                        cssEquivalentExists = true;
                        StartAnimation(_propertyContainer, cssEquivalent, From, To, Duration, (EasingFunctionBase)EasingFunction, specificGroupName, _propDp,
                        OnAnimationCompleted(parameters, isLastLoop, To.Value, _propertyContainer, _targetProperty, _animationID));

                    }
                }
                //todo: use GetCSSEquivalent instead (?)
                if (propertyMetadata.GetCSSEquivalents != null)
                {
                    List<CSSEquivalent> cssEquivalents = propertyMetadata.GetCSSEquivalents(_propertyContainer);
                    foreach (CSSEquivalent equivalent in cssEquivalents)
                    {
                        cssEquivalentExists = true;
                        StartAnimation(_propertyContainer, equivalent, From, To, Duration, (EasingFunctionBase)EasingFunction, specificGroupName, _propDp,
                        OnAnimationCompleted(parameters, isLastLoop, To.Value, _propertyContainer, _targetProperty, _animationID));
                    }
                }

                if (!cssEquivalentExists)
                {
                    OnAnimationCompleted(parameters, isLastLoop, To.Value, _propertyContainer, _targetProperty, _animationID)();
                }
            }
        }

        private Action OnAnimationCompleted(IterationParameters parameters, bool isLastLoop, object value, DependencyObject target, PropertyPath propertyPath, Guid callBackGuid)
        {
            return () =>
            {
                if (!this._isUnapplied)
                {
                    if (isLastLoop && _animationID == callBackGuid)
                    {
                        AnimationHelpers.ApplyValue(target, propertyPath, value);
                    }
                    OnIterationCompleted(parameters);
                }
            };
        }

        static void StartAnimation(DependencyObject target, CSSEquivalent cssEquivalent, double? from, object to, Duration Duration, EasingFunctionBase easingFunction, string visualStateGroupName, DependencyProperty dependencyProperty, Action callbackForWhenfinished = null)
        {
            if (cssEquivalent.Name != null && cssEquivalent.Name.Count != 0)
            {
                UIElement uiElement = cssEquivalent.UIElement ?? (target as UIElement); // If no UIElement is specified, we assume that the property is intended to be applied to the instance on which the PropertyChanged has occurred.

                bool hasTemplate = (uiElement is Control) && ((Control)uiElement).HasTemplate;

                if (!hasTemplate || cssEquivalent.ApplyAlsoWhenThereIsAControlTemplate)
                {
                    if (cssEquivalent.DomElement == null && uiElement != null)
                    {
                        cssEquivalent.DomElement = uiElement.INTERNAL_OuterDomElement; // Default value
                    }
                    if (cssEquivalent.DomElement != null)
                    {
                        if (cssEquivalent.Value == null)
                        {
                            cssEquivalent.Value = (finalInstance, value) => { return value ?? ""; }; // Default value
                        }
                        object cssValue = cssEquivalent.Value(target, to);

                        string sCssValue = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(cssValue);
                        string fromToValues;
                        if (!from.HasValue)
                        {
                            fromToValues = "{" + string.Join(",", cssEquivalent.Name.Select(name => $"\"{name}\":{sCssValue}")) + "}";
                        }
                        else
                        {
                            string sFrom = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(from);
                            fromToValues = "{" + string.Join(",", cssEquivalent.Name.Select(name => $"\"{name}\":[{sCssValue},{sFrom}]")) + "}";
                        }

                        AnimationHelpers.CallVelocity(
                            cssEquivalent.DomElement,
                            Duration,
                            easingFunction,
                            visualStateGroupName,
                            callbackForWhenfinished,
                            fromToValues);
                        target.DirtyVisualValue(dependencyProperty);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Please set the Name property of the CSSEquivalent class.");
            }
        }

        internal override void StopAnimation()
        {
            if (_isInitialized)
            {
                //todo: find out why we put the test on target and put it back? (I removed it because id kept ScaleTransform from working properly)
                if (To != null)// && target is FrameworkElement) //todo: "To" can never be "null", fix this.
                {
                    // - Get the propertyMetadata from the property
                    PropertyMetadata propertyMetadata = _propDp.GetTypeMetaData(_propertyContainer.GetType());

                    //we make a specific name for this animation:
                    string specificGroupName = animationInstanceSpecificName.ToString();

                    // - Get the cssPropertyName from the PropertyMetadata
                    if (propertyMetadata.GetCSSEquivalent != null)
                    {
                        CSSEquivalent cssEquivalent = propertyMetadata.GetCSSEquivalent(_propertyContainer);
                        if (cssEquivalent != null)
                        {
                            UIElement uiElement = cssEquivalent.UIElement ?? (_propertyContainer as UIElement); // If no UIElement is specified, we assume that the property is intended to be applied to the instance on which the PropertyChanged has occurred.

                            bool hasTemplate = (uiElement is Control) && ((Control)uiElement).HasTemplate;

                            if (!hasTemplate || cssEquivalent.ApplyAlsoWhenThereIsAControlTemplate)
                            {
                                if (cssEquivalent.DomElement == null && uiElement != null)
                                {
                                    cssEquivalent.DomElement = uiElement.INTERNAL_OuterDomElement; // Default value
                                }
                                if (cssEquivalent.DomElement != null)
                                {
                                    string sDomElement = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(cssEquivalent.DomElement);
                                    CSHTML5.Interop.ExecuteJavaScriptFastAsync($@"Velocity({sDomElement}, ""stop"", ""{specificGroupName}"");");
                                }
                            }
                        }
                    }
                    if (propertyMetadata.GetCSSEquivalents != null)
                    {
                        List<CSSEquivalent> cssEquivalents = propertyMetadata.GetCSSEquivalents(_propertyContainer);
                        foreach (CSSEquivalent equivalent in cssEquivalents)
                        {
                            if (equivalent.DomElement != null)
                            {
                                string sDomElement = CSHTML5.INTERNAL_InteropImplementation.GetVariableStringForJS(equivalent.DomElement);
                                CSHTML5.Interop.ExecuteJavaScriptFastAsync($@"Velocity({sDomElement}, ""stop"", ""{specificGroupName}"");");
                            }
                        }
                    }
                }
            }
        }

        [OpenSilver.NotImplemented]
        public static readonly DependencyProperty ByProperty = DependencyProperty.Register("By", typeof(double?), typeof(DoubleAnimation), null);
        [OpenSilver.NotImplemented]
        public double? By
        {
            get { return (double?)this.GetValue(ByProperty); }
            set { this.SetValue(ByProperty, value); }
        }
    }
}