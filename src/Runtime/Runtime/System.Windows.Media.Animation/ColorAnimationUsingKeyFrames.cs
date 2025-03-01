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
using System.ComponentModel;
using System.Windows.Markup;
using CSHTML5;
using System.Linq;

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
    /// This class is used to animate a Color property value along a set of key frames.
    /// </summary>
    [ContentProperty("KeyFrames")]
    public partial class ColorAnimationUsingKeyFrames : AnimationTimeline
    {
        private ColorKeyFrameCollection _keyFrames;

        /// <summary>
        /// Gets the collection of <see cref="ColorKeyFrame"/> objects
        /// that define the animation.
        /// </summary>
        /// <returns>
        /// The collection of <see cref="ColorKeyFrame"/> objects that
        /// define the animation. The default is an empty collection.
        /// </returns>
        public ColorKeyFrameCollection KeyFrames
        {
            get => _keyFrames ?? (_keyFrames = new ColorKeyFrameCollection(this));
            [EditorBrowsable(EditorBrowsableState.Never)]
            set => _keyFrames = value;
        }


        private int _appliedKeyFramesCount = -1;
        private INTERNAL_ResolvedKeyFramesEntries<ColorKeyFrame> _resolvedKeyFrames;
        private Dictionary<ColorKeyFrame, Duration> _keyFrameToDurationMap;
        private PropertyMetadata _propertyMetadata;

        // This guid is used to specifically target a particular call to the animation. It prevents the callback which should be called when velocity's animation end 
        // to be called when the callback is called from a previous call to the animation. This could happen when the animation was started quickly multiples times in a row. 
        private Guid _animationID;
        private object _thisLock = new object();


        /// <summary>
        /// Returns the largest time span specified key time from all of the key frames.
        /// If there are not time span key times a time span of one second is returned
        /// to match the default natural duration of the From/To/By animations.
        /// </summary>
        private TimeSpan LargestTimeSpanKeyTime
        {
            get
            {
                if (_keyFrames == null || _keyFrames.Count == 0)
                {
                    return TimeSpan.FromTicks(0);
                }
                if (_resolvedKeyFrames != null)
                {
                    return _keyFrames[_resolvedKeyFrames.GetNextKeyFrameIndex(_keyFrames.Count - 1)].KeyTime.TimeSpan;
                }
                else
                {
                    throw new Exception("ColorAnimationUsingKeyFrames has not been setup yet.");
                }
            }
        }

        private void InitializeKeyFramesSet()
        {
            _resolvedKeyFrames = new INTERNAL_ResolvedKeyFramesEntries<ColorKeyFrame>(_keyFrames);
            _keyFrameToDurationMap = new Dictionary<ColorKeyFrame, Duration>();
            ColorKeyFrame keyFrame;
            for (int i = 0; i < KeyFrames.Count; i++)
            {
                keyFrame = KeyFrames[_resolvedKeyFrames.GetNextKeyFrameIndex(i)];
                _keyFrameToDurationMap.Add(keyFrame, keyFrame.KeyTime.TimeSpan - (i > 0 ? KeyFrames[_resolvedKeyFrames.GetNextKeyFrameIndex(i - 1)].KeyTime.TimeSpan : TimeSpan.Zero));
            }
            _appliedKeyFramesCount = 0;
        }

        private Action OnKeyFrameCompleted(IterationParameters parameters, bool isLastLoop, object value, DependencyObject target, PropertyPath propertyPath, Guid callBackGuid)
        {
            return () =>
            {
                if (!this._isUnapplied)
                {
                    if (_animationID == callBackGuid)
                    {
                        AnimationHelpers.ApplyValue(target, propertyPath, value);
                        _appliedKeyFramesCount++;
                        if (!CheckTimeLineEndAndRaiseCompletedEvent(_parameters))
                        {
                            ApplyKeyFrame(GetNextKeyFrame(), parameters, isLastLoop);
                        }
                    }
                }
            };
        }

        private ColorKeyFrame GetNextKeyFrame()
        {
            int nextKeyFrameIndex = _resolvedKeyFrames.GetNextKeyFrameIndex(_appliedKeyFramesCount);
            if (nextKeyFrameIndex == -1)
            {
                return null;
            }
            else
            {
                return _keyFrames[nextKeyFrameIndex];
            }
        }

        private Duration GetKeyFrameDuration(ColorKeyFrame keyFrame)
        {
            return _keyFrameToDurationMap[keyFrame];
        }

        private void ApplyLastKeyFrame(object sender, EventArgs e)
        {
            if (!_cancelledAnimation)
            {
                ColorKeyFrame lastKeyFrame = _keyFrames[_resolvedKeyFrames.GetNextKeyFrameIndex(_keyFrames.Count - 1)];
                AnimationHelpers.ApplyValue(_propertyContainer, _targetProperty, lastKeyFrame.Value);
            }
        }
        
        private bool CheckTimeLineEndAndRaiseCompletedEvent(IterationParameters parameters)
        {
            bool raiseEvent = false;
            lock (_thisLock)
            {
                if (_appliedKeyFramesCount >= KeyFrames.Count)
                {
                    raiseEvent = true;
                }
            }
            if (raiseEvent || _cancelledAnimation)
            {
                OnIterationCompleted(parameters);
            }
            return raiseEvent || _cancelledAnimation;
        }

        private void ApplyKeyFrame(ColorKeyFrame keyFrame, IterationParameters parameters, bool isLastLoop)
        {
            if (keyFrame != null)
            {
                Color? from = null;
                var to = keyFrame.Value;
                var easingFunction = keyFrame.INTERNAL_GetEasingFunction();

                //Note: we put this line here because the Xaml could use a Color gotten from a StaticResource (which was therefore not converted to a SolidColorbrush by
                // the compiler in the .g.cs file) and led to a wrong type set in a property (Color value in a property of type Brush).
                var castedValue = DynamicCast(to, _propDp.PropertyType);

                //we make a specific name for this animation:
                string specificGroupName = animationInstanceSpecificName.ToString();

                bool cssEquivalentExists = false;
                if (_propertyMetadata.GetCSSEquivalent != null)
                {
                    CSSEquivalent cssEquivalent = _propertyMetadata.GetCSSEquivalent(_propertyContainer);
                    if (cssEquivalent != null)
                    {
                        cssEquivalentExists = true;
                        StartAnimation(_propertyContainer, cssEquivalent, from, to, GetKeyFrameDuration(keyFrame), easingFunction, specificGroupName, _propDp,
                                          OnKeyFrameCompleted(parameters, isLastLoop, castedValue, _propertyContainer, _targetProperty, _animationID));
                    }
                }
                if (_propertyMetadata.GetCSSEquivalents != null)
                {
                    List<CSSEquivalent> cssEquivalents = _propertyMetadata.GetCSSEquivalents(_propertyContainer);
                    bool isFirst = true;
                    foreach (CSSEquivalent equivalent in cssEquivalents)
                    {
                        cssEquivalentExists = true;
                        if (equivalent.CallbackMethod == null)
                        {
                            if (isFirst)
                            {
                                bool updateIsFirst = StartAnimation(_propertyContainer, equivalent, from, to, GetKeyFrameDuration(keyFrame), easingFunction, specificGroupName, _propDp,
                                                                       OnKeyFrameCompleted(parameters, isLastLoop, castedValue, _propertyContainer, _targetProperty, _animationID));
                                if (updateIsFirst)
                                {
                                    isFirst = false;
                                }
                            }
                            else
                            {
                                StartAnimation(_propertyContainer, equivalent, from, to, GetKeyFrameDuration(keyFrame), easingFunction, specificGroupName, _propDp,
                                                  OnKeyFrameCompleted(parameters, isLastLoop, castedValue, _propertyContainer, _targetProperty, _animationID));
                            }
                        }
                        else
                        {
                            OnKeyFrameCompleted(parameters, isLastLoop, castedValue, _propertyContainer, _targetProperty, _animationID)();
                        }
                    }
                }
                if (!cssEquivalentExists)
                {
                    OnKeyFrameCompleted(parameters, isLastLoop, castedValue, _propertyContainer, _targetProperty, _animationID)();
                }
            }
        }

        static bool StartAnimation(DependencyObject target, CSSEquivalent cssEquivalent, Color? from, object to, Duration duration, EasingFunctionBase easingFunction, string visualStateGroupName, DependencyProperty dependencyProperty, Action callbackForWhenfinished = null)
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
                            cssEquivalent.Value = (finalInstance, value) =>
                            {
                                if (value == null)
                                {
                                    return "";
                                }
                                else
                                {
                                    Dictionary<string, object> valuesDict = new Dictionary<string, object>();
                                    foreach (string name in cssEquivalent.Name)
                                    {
                                        if (!name.EndsWith("Alpha"))
                                        {
                                            valuesDict.Add(name, ((Color)value).INTERNAL_ToHtmlStringForVelocity());
                                        }
                                        else
                                        {
                                            valuesDict.Add(name, ((double)((Color)value).A) / 255);
                                        }
                                    }
                                    return valuesDict;
                                }
                                //return value ?? "";
                            }; // Default value
                        }
                        object cssValue = cssEquivalent.Value(target, to);
                        string fromToValues;
                        if (!from.HasValue)
                        {
                            if (cssValue is Dictionary<string, object> cssValueAsDictionary)
                            {
                                fromToValues = "{" +
                                    string.Join(",", cssEquivalent.Name
                                        .Where(name => name != "background")
                                        .Select(name => $"\"{name}\":{INTERNAL_InteropImplementation.GetVariableStringForJS(cssValueAsDictionary[name])}")) + "}";
                            }
                            else
                            {
                                string sCssValue = INTERNAL_InteropImplementation.GetVariableStringForJS(cssValue);
                                fromToValues = "{" +
                                    string.Join(",", cssEquivalent.Name
                                        .Where(name => name != "background")
                                        .Select(name => $"\"{name}\":{sCssValue}")) + "}";
                            }
                        }
                        else
                        {
                            object fromCssValue = cssEquivalent.Value(target, from);
                            fromToValues = "{" +
                                string.Join(",", cssEquivalent.Name
                                    .Where(name => name != "background")
                                    .Select(name =>
                                    {
                                        object currentCssValue = cssValue is Dictionary<string, object> d1 ? d1[name] : cssValue;
                                        object currentFromCssValue = fromCssValue is Dictionary<string, object> d2 ? d2[name] : fromCssValue;
                                        string sCurrentCssValue = INTERNAL_InteropImplementation.GetVariableStringForJS(currentCssValue);
                                        string sCurrentFromCssValue = INTERNAL_InteropImplementation.GetVariableStringForJS(currentFromCssValue);
                                        return $"\"{name}\":[{sCurrentCssValue}, {sCurrentFromCssValue}]";
                                    })) + "}";
                        }

                        AnimationHelpers.CallVelocity(
                            cssEquivalent.DomElement,
                            duration,
                            easingFunction,
                            visualStateGroupName,
                            callbackForWhenfinished,
                            fromToValues);
                        
                        target.DirtyVisualValue(dependencyProperty);
                        return true;
                    }
                }
                return false;
            }
            else
            {
                throw new InvalidOperationException("Please set the Name property of the CSSEquivalent class.");
            }
        }


        internal override void InitializeCore()
        {
            this.Completed -= ApplyLastKeyFrame;
            this.Completed += ApplyLastKeyFrame;
            InitializeKeyFramesSet();
            _propertyMetadata = _propDp.GetTypeMetaData(_propertyContainer.GetType());
        }

        internal override void GetTargetInformation(IterationParameters parameters)
        {
            _parameters = parameters;
            DependencyObject target;
            PropertyPath propertyPath;
            DependencyObject targetBeforePath;
            GetPropertyPathAndTargetBeforePath(parameters, out targetBeforePath, out propertyPath);
            DependencyObject parentElement = targetBeforePath; //this will be the parent of the clonable element (if any).
            foreach (Tuple<DependencyObject, DependencyProperty, int?> element in GoThroughElementsToAccessProperty(propertyPath, targetBeforePath))
            {
                DependencyObject depObject = element.Item1;
                DependencyProperty depProp = element.Item2;
                int? index = element.Item3;
                if (depObject is ICloneOnAnimation)
                {
                    if (!((ICloneOnAnimation)depObject).IsAlreadyAClone())
                    {
                        object clone = ((ICloneOnAnimation)depObject).Clone();
                        if (index != null)
                        {
#if BRIDGE
                            parentElement.GetType().GetProperty("Item").SetValue(parentElement, clone, new object[] { index });
#else
                            //JSIL does not support SetValue(object, object, object[])
#endif
                        }
                        else
                        {
                            parentElement.SetValue(depProp, clone);
                        }
                    }
                    break;
                }
                else
                {
                    parentElement = depObject;
                }
            }

            GetTargetElementAndPropertyInfo(parameters, out target, out propertyPath);

            _propertyContainer = target;
            _targetProperty = propertyPath;
            _propDp = GetProperty(_propertyContainer, _targetProperty);
            _target = Storyboard.GetTarget(this);
            _targetName = Storyboard.GetTargetName(this);
        }
        
        internal override void RestoreDefaultCore()
        {
            _appliedKeyFramesCount = 0;
        }

        protected override Duration GetNaturalDurationCore()
        {
            return new Duration(LargestTimeSpanKeyTime);
        }

        internal override void Apply(IterationParameters parameters, bool isLastLoop)
        {
            _animationID = Guid.NewGuid();
            ApplyKeyFrame(GetNextKeyFrame(), _parameters, isLastLoop);
        }

        internal override void StopAnimation()
        {
            if (_isInitialized)
            {
                DependencyProperty dp = CSHTML5.Internal.INTERNAL_TypeToStringsToDependencyProperties
                        .GetPropertyInTypeOrItsBaseTypes(_propertyContainer.GetType(), _targetProperty.SVI[_targetProperty.SVI.Length - 1].propertyName);

                // - Get the propertyMetadata from the property
                PropertyMetadata propertyMetadata = dp.GetTypeMetaData(_propertyContainer.GetType());
                // - Get the cssPropertyName from the PropertyMetadata

                //we make a specific name for this animation:
                string specificGroupName = animationInstanceSpecificName.ToString();

                bool cssEquivalentExists = false;
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
                                cssEquivalentExists = true;
                                CSHTML5.Interop.ExecuteJavaScriptAsync(@"Velocity($0, ""stop"", $1);", cssEquivalent.DomElement, specificGroupName);
                            }
                        }
                    }
                }
                //todo: use GetCSSEquivalent instead (?)
                if (propertyMetadata.GetCSSEquivalents != null)
                {
                    List<CSSEquivalent> cssEquivalents = propertyMetadata.GetCSSEquivalents(_propertyContainer);
                    foreach (CSSEquivalent equivalent in cssEquivalents)
                    {
                        if (equivalent.DomElement != null && equivalent.CallbackMethod == null)
                        {
                            cssEquivalentExists = true;
                            CSHTML5.Interop.ExecuteJavaScriptAsync(@"Velocity($0, ""stop"", $1);", equivalent.DomElement, specificGroupName);
                        }
                    }
                }
            }
        }        

    }
}
