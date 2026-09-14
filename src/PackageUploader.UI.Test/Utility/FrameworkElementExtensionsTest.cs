// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.Utility;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PackageUploader.UI.Test.Utility;

[TestClass]
public class FrameworkElementExtensionsTest
{
    private static void RaiseLoaded(FrameworkElement element) =>
        element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    [WpfTestMethod]
    public void OnFirstLoad_RunsTheActionWhenTheElementIsLoaded()
    {
        var element = new UserControl();
        int callCount = 0;

        element.OnFirstLoad(() => callCount++);

        Assert.AreEqual(0, callCount, "The action must not run before the element is loaded.");

        RaiseLoaded(element);

        Assert.AreEqual(1, callCount);
    }

    [WpfTestMethod]
    public void OnFirstLoad_DoesNotRunAgainWhenTheElementIsReloaded()
    {
        var element = new UserControl();
        int callCount = 0;

        element.OnFirstLoad(() => callCount++);

        // WPF raises Loaded again whenever the element is re-attached to a live visual tree, which a
        // remote session reconnect can cause without any navigation having happened.
        RaiseLoaded(element);
        RaiseLoaded(element);
        RaiseLoaded(element);

        Assert.AreEqual(1, callCount);
    }

    [WpfTestMethod]
    public void OnFirstLoad_RunsOnceEvenWhenTheActionItselfRaisesLoaded()
    {
        var element = new UserControl();
        int callCount = 0;

        // The handler unsubscribes before invoking, so an action that pumps messages and provokes a
        // nested Loaded - anything that shows a dialog - cannot re-enter.
        element.OnFirstLoad(() =>
        {
            callCount++;
            RaiseLoaded(element);
        });

        RaiseLoaded(element);

        Assert.AreEqual(1, callCount);
    }

    [WpfTestMethod]
    public void OnFirstLoad_KeepsSeparateInstancesIndependent()
    {
        // Views are registered with AddTransient, so each navigation builds a new instance and must
        // get its own first load.
        var first = new UserControl();
        var second = new UserControl();
        int firstCount = 0;
        int secondCount = 0;

        first.OnFirstLoad(() => firstCount++);
        second.OnFirstLoad(() => secondCount++);

        RaiseLoaded(first);
        RaiseLoaded(first);

        Assert.AreEqual(1, firstCount);
        Assert.AreEqual(0, secondCount);

        RaiseLoaded(second);

        Assert.AreEqual(1, firstCount);
        Assert.AreEqual(1, secondCount);
    }

    [WpfTestMethod]
    public void OnFirstLoad_SupportsSeveralIndependentCallbacksOnOneElement()
    {
        var element = new UserControl();
        int firstCount = 0;
        int secondCount = 0;

        element.OnFirstLoad(() => firstCount++);
        element.OnFirstLoad(() => secondCount++);

        RaiseLoaded(element);
        RaiseLoaded(element);

        Assert.AreEqual(1, firstCount);
        Assert.AreEqual(1, secondCount);
    }

    [WpfTestMethod]
    public void OnFirstLoad_RejectsNullArguments()
    {
        var element = new UserControl();

        Assert.ThrowsExactly<ArgumentNullException>(() => ((FrameworkElement)null!).OnFirstLoad(() => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => element.OnFirstLoad(null!));
    }
}
