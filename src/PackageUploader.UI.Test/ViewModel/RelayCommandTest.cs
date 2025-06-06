using Microsoft.VisualStudio.TestTools.UnitTesting;
using PackageUploader.UI.ViewModel;
using System;

namespace PackageUploader.UI.Test;

[TestClass]
public class RelayCommandTest
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Test_ThrowsException()
    {
        _ = new RelayCommand(null);
        Assert.Fail("Expected an ArgumentNullException to be thrown.");
    }

    [TestMethod]
    public void Test_CanExecute()
    {
        var command = new RelayCommand(() => { }, () => false);
        Assert.IsFalse(command.CanExecute(null));

        command = new RelayCommand(() => { }, () => true);
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public void Test_CanExecuteFromClass()
    {
        var testClass = new TestClass();
        var command = new RelayCommand(() => { }, () => testClass.TestBool);

        testClass.TestBool = false;
        Assert.IsFalse(command.CanExecute(null));

        testClass.TestBool = true;
        Assert.IsTrue(command.CanExecute(null));
    }

    [TestMethod]
    public void Test_Execute()
    {
        bool executed = false;
        var command = new RelayCommand(() => { executed = true; });
        command.Execute(null);

        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void Test_ExecuteWithCanExecute()
    {
        bool executed = false;
        var command = new RelayCommand(() => { executed = true; }, () => false);
        command.Execute(null);
        Assert.IsFalse(executed);

        command = new RelayCommand(() => { executed = true; }, () => true);
        command.Execute(null);
        Assert.IsTrue(executed);
    }

    [TestMethod]
    public void Test_ExecuteWithCanExecuteFromClass()
    {
        // setup 
        var executed = false;
        var testClassChanged = false;
        var testClass = new TestClass();
        
        testClass.TestBool = false;
        var command = new RelayCommand(() => { executed = true; },
                                       () => { testClassChanged = true;
                                           return testClass.TestBool; });
        command.Execute(null);
        Assert.IsTrue(testClassChanged);
        Assert.IsFalse(executed);


        testClassChanged = false;
        testClass.TestBool = true;
        executed = false; // reset executed flag
        command.Execute(null);
        Assert.IsTrue(testClassChanged);
        Assert.IsTrue(executed);
    }

}

public class TestClass
{
    private bool _testBool = true;
    public bool TestBool
    {
        get => _testBool;
        set
        {
            _testBool = value;
        }
    }
}

