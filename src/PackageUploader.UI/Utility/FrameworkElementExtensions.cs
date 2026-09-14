// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Windows;

namespace PackageUploader.UI.Utility
{
    public static class FrameworkElementExtensions
    {
        /// <summary>
        /// Runs <paramref name="onFirstLoad"/> the first time the element is loaded, and never again
        /// for that instance.
        /// </summary>
        /// <remarks>
        /// WPF raises Loaded every time an element is attached to a live visual tree, not just once.
        /// Anything that rebuilds the tree - a remote session disconnect and reconnect, for one -
        /// therefore re-raises it on an element that never went away, which repeats whatever the
        /// handler did. That is harmless for a handler that only refreshes bindings and destructive
        /// for one that starts an upload or registers event handlers.
        ///
        /// This is equivalent to the plain Loaded handler it replaces only because every view is
        /// registered with AddTransient, so navigation always builds a new instance and "first load"
        /// means "once per navigation". Registering a view as a singleton would break that: the
        /// second navigation to it would reuse the instance and the callback would never run again.
        ///
        /// The handler unsubscribes before invoking rather than after, so a callback that pumps
        /// messages - anything showing a dialog - cannot re-enter through a nested Loaded.
        /// </remarks>
        public static void OnFirstLoad(this FrameworkElement element, Action onFirstLoad)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(onFirstLoad);

            void Handler(object sender, RoutedEventArgs e)
            {
                element.Loaded -= Handler;
                onFirstLoad();
            }

            element.Loaded += Handler;
        }
    }
}
