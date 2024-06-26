﻿using FlaUI.Core.AutomationElements;

namespace FlaUI.WebDriver
{
    public class KnownWindow
    {
        public KnownWindow(Window window, string? windowRuntimeId, string windowHandle)
        {
            Window = window;
            WindowRuntimeId = windowRuntimeId;
            WindowHandle = windowHandle;
        }

        public string WindowHandle { get; }

        /// <summary>
        /// A temporarily unique ID, so cannot be used for identity over time, but can be used for improving performance of equality tests.
        /// "The identifier is only guaranteed to be unique to the UI of the desktop on which it was generated. Identifiers can be reused over time."
        /// </summary>
        /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomationelement-getruntimeid"/>
        public string? WindowRuntimeId { get; }

        public Window Window { get; }
    }
}
