﻿#region License
/*
Microsoft Public License (Ms-PL)
XnaTouch - Copyright © 2009 The XnaTouch Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

using Windows.UI.Core;
using Windows.Graphics.Display;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Windows.UI.ViewManagement;


namespace Microsoft.Xna.Framework
{
    public partial class MetroGameWindow : GameWindow
    {
        private DisplayOrientation _orientation;
        private CoreWindow _coreWindow;
        protected Game game;
        private readonly List<Keys> _keys;
        private Rectangle _clientBounds;

        #region Internal Properties

        internal Game Game { get; set; }

        internal bool IsExiting { get; set; }

        #endregion

        #region Public Properties

        public override IntPtr Handle { get { return Marshal.GetIUnknownForObject(_coreWindow); } }

        public override string ScreenDeviceName { get { return String.Empty; } } // window.Title

        public override Rectangle ClientBounds { get { return _clientBounds; } }

        public override bool AllowUserResizing
        {
            get { return false; }
            set 
            {
                // You cannot resize a Metro window!
            }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get { return _orientation; }
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            var supported = DisplayOrientations.None;

            if (orientations == DisplayOrientation.Default)
            {
                // Make the decision based on the preferred backbuffer dimensions.
                var manager = Game.graphicsDeviceManager;
                if (manager.PreferredBackBufferWidth > manager.PreferredBackBufferHeight)
                    supported = DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped;
                else
                    supported = DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped;                    
            }
            else
            {
                if ((orientations & DisplayOrientation.LandscapeLeft) != 0)
                    supported |= DisplayOrientations.Landscape;
                if ((orientations & DisplayOrientation.LandscapeRight) != 0)
                    supported |= DisplayOrientations.LandscapeFlipped;
                if ((orientations & DisplayOrientation.Portrait) != 0)
                    supported |= DisplayOrientations.Portrait;
                if ((orientations & DisplayOrientation.PortraitUpsideDown) != 0)
                    supported |= DisplayOrientations.PortraitFlipped;
            }

            DisplayProperties.AutoRotationPreferences = supported;
        }

        #endregion

        static public MetroGameWindow Instance { get; private set; }

        static MetroGameWindow()
        {
            Instance = new MetroGameWindow();
        }

        private MetroGameWindow()
        {
            _keys = new List<Keys>();
        }

        #region Restricted Methods

        #region Delegates

        private static Keys KeyTranslate(Windows.System.VirtualKey inkey)
        {
            switch (inkey)
            {
                // XNA does not have have 'handless' key values.
                // So, we arebitrarily map those to the 'Left' version.                 
                case Windows.System.VirtualKey.Control:
                    return Keys.LeftControl;
                case Windows.System.VirtualKey.Shift:
                    return Keys.LeftControl;           
                // Note that the Alt key is now refered to as Menu.
                case Windows.System.VirtualKey.Menu:
                    return Keys.LeftAlt;
                default:                    
                    return (Keys)inkey;
            }
        }

        private void Keyboard_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            var xnaKey = KeyTranslate(args.VirtualKey);

            if (_keys.Contains(xnaKey))
                _keys.Remove(xnaKey);
        }

        private void Keyboard_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var xnaKey = KeyTranslate(args.VirtualKey);

            if (!_keys.Contains(xnaKey))
                _keys.Add(xnaKey);
        }

        #endregion

        #endregion

        public void Initialize(CoreWindow coreWindow)
        {
            _coreWindow = coreWindow;

            _orientation = ToOrientation(DisplayProperties.CurrentOrientation);
            DisplayProperties.OrientationChanged += DisplayProperties_OrientationChanged;

            _coreWindow.SizeChanged += Window_SizeChanged;
            _coreWindow.Closed += Window_Closed;

            _coreWindow.KeyDown += Keyboard_KeyDown;
            _coreWindow.KeyUp += Keyboard_KeyUp;
            
            // TODO: Fix for latest WinSDK changes.
            //ApplicationView.Value.ViewStateChanged += Application_ViewStateChanged;

            var bounds = _coreWindow.Bounds;
            SetClientBounds(bounds.Width, bounds.Height);

            InitializeTouch();
        }

        /*
        private void Application_ViewStateChanged(ApplicationView sender, ApplicationViewStateChangedEventArgs args)
        {
            // TODO: We may want to expose this event via GameWindow
            // only in WinRT builds....  not sure yet.
        }
        */

        private void Window_Closed(CoreWindow sender, CoreWindowEventArgs args)
        {
            Game.Exit();
        }

        private void SetClientBounds(double width, double height)
        {
            var dpi = DisplayProperties.LogicalDpi;
            var pwidth = width * dpi / 96.0;
            var pheight = height * dpi / 96.0;

            _clientBounds = new Rectangle(0, 0, (int)pwidth, (int)pheight);
        }

        private void Window_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            SetClientBounds( args.Size.Width, args.Size.Height );

            // If we have a valid client bounds then update the graphics device.
            if (_clientBounds.Width > 0 && _clientBounds.Height > 0)
                UpdateGraphicsDevice();

            OnClientSizeChanged();
        }


        private static DisplayOrientation ToOrientation(DisplayOrientations orientation)
        {
            DisplayOrientation result = (DisplayOrientation)0;

            if (DisplayProperties.NativeOrientation == orientation)
                result |= DisplayOrientation.Default;

            switch (orientation)
            {
                default:
                case DisplayOrientations.None:
                    result |= DisplayOrientation.Default;
                    break;

                case DisplayOrientations.Landscape:
                    result |= DisplayOrientation.LandscapeLeft;
                    break;

                case DisplayOrientations.LandscapeFlipped:
                    result |= DisplayOrientation.LandscapeRight;
                    break;

                case DisplayOrientations.Portrait:
                    result |= DisplayOrientation.Portrait;
                    break;

                case DisplayOrientations.PortraitFlipped:
                    result |= DisplayOrientation.PortraitUpsideDown;
                    break;
            }

            return result;
        }

        private void DisplayProperties_OrientationChanged(object sender)
        {
            // Set the new orientation.
            _orientation = ToOrientation(DisplayProperties.CurrentOrientation);

            // If we have a valid client bounds then update the graphics device.
            if (_clientBounds.Width > 0 && _clientBounds.Height > 0)
                UpdateGraphicsDevice();

            // Call the user callback.
            OnOrientationChanged();
        }

        private void UpdateGraphicsDevice()
        {
            // Is the orientation landscape and is landscape the default?
            var isLandscape = (_orientation & (DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight)) != 0;
            var isDefaultLandscape = DisplayProperties.NativeOrientation == DisplayOrientations.Landscape;

            // Get the new width and height considering that the 
            // orientation changes how we read the client bounds.
            // 
            // TODO: Is the Win8 Simulator broken or is this really correct?
            //
            int newWidth, newHeight;
            if (true) //isLandscape == isDefaultLandscape)
            {
                newWidth = _clientBounds.Width;
                newHeight = _clientBounds.Height;
            }
            else
            {
                newWidth = _clientBounds.Height;
                newHeight = _clientBounds.Width;
            }

            // Update the graphics device.
            var device = Game.GraphicsDevice;
            device.Viewport = new Viewport(0, 0, newWidth, newHeight);
            device.PresentationParameters.BackBufferWidth = newWidth;
            device.PresentationParameters.BackBufferHeight = newHeight;
            device.CreateSizeDependentResources();
            device.ApplyRenderTargets(null);
        }

        protected override void SetTitle(string title)
        {
            // NOTE: There seems to be no concept of a
            // window title in a Metro application.
        }

        internal void SetCursor(bool visible)
        {
            if ( _coreWindow == null )
                return;

            if (visible)
                _coreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            else
                _coreWindow.PointerCursor = null;
        }

        internal void RunLoop()
        {
            SetCursor(Game.IsMouseVisible);
            _coreWindow.Activate();

            while (true)
            {
                // Process events incoming to the window.
                _coreWindow.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

                // Apply the keyboard state gathered from
                // the key events since the last tick.
                Keyboard.State = new KeyboardState(_keys.ToArray());

                // Update and render the game.
                if (Game != null)
                    Game.Tick();

                if (IsExiting)
                    break;
            }
        }

        #region Public Methods

        public void Dispose()
        {
            //window.Dispose();
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {

        }

        #endregion
    }
}

