
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PackageUploader.UI.Utility;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace PackageUploader.UI.Test;

[TestClass]
public class DragDropHelperTest
{
    private System.Windows.DragEventArgs MakeDragEvents(DragDropEffects dragDropEffectsAllowed, string dataFormats, bool getDataPresentWithFormats, string[] data)
    {
        Mock<IDataObject> dataObjectMock = new Mock<IDataObject>();
        if (dataFormats != null) 
        { 
            dataObjectMock.Setup(m => m.GetDataPresent(dataFormats)).Returns(getDataPresentWithFormats);
            dataObjectMock.Setup(m => m.GetData(dataFormats)).Returns(data);
        }
        DragDropKeyStates dragDropKeyStates = DragDropKeyStates.LeftMouseButton;
        DragDropEffects allowedEffects = dragDropEffectsAllowed; // I don't know if this is necessary
        DependencyObject target = new DependencyObject();
        Point point = new Point();

        ConstructorInfo constructor = typeof(DragEventArgs).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();

        object[] arguments = new object[]
        {
            dataObjectMock.Object,
            dragDropKeyStates,
            allowedEffects,
            target,
            point
        };

        return (DragEventArgs)constructor.Invoke(arguments);
    }

    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Action_AcceptFolders()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropActionMoq = new Mock<Action<string>>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropActionMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All, 
                                       System.Windows.DataFormats.FileDrop, 
                                       false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.None);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act PreviewDragOver - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All, 
                                       System.Windows.DataFormats.FileDrop, 
                                       true, new string[] {""});
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.Copy);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act Drop - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                               System.Windows.DataFormats.FileDrop,
                               false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act Drop - Invalid Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "Bad Path" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);


        //  Drop - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Once);
        Assert.IsTrue(dragEventArgs.Handled);
    }

    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Action_DontAcceptFolders()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropActionMoq = new Mock<Action<string>>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropActionMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.None);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act PreviewDragOver - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       true, new string[] { "" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.Copy);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act Drop - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                               System.Windows.DataFormats.FileDrop,
                               false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);


        // Act Drop - Invalid Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "Bad Path" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);


        //  Drop - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32\\notepad.exe" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropActionMoq.Verify(action => action(It.IsAny<string>()), Times.Once);
        Assert.IsTrue(dragEventArgs.Handled);
    }

    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_PreviewDragOver_NoData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.None);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_PreviewDragOver_Data()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       true, new string[] { "" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.Copy);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_Drop_NoData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act Drop - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                               System.Windows.DataFormats.FileDrop,
                               false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Never);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_Drop_InvalidData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act Drop - Invalid Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "Bad Path" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Never);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_Drop_Data_ExecuteFalse()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        //  Drop - Data CanExecute = false
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        onDropCommandMoq.Setup(onDropCommandMoq => onDropCommandMoq.CanExecute(It.IsAny<object>())).Returns(false);
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Once);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_AcceptFolders_Drop_Data_ExecuteTrue()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;


        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, true);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);
        //  Drop - Data CanExecute = true
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        onDropCommandMoq.Setup(onDropCommandMoq => onDropCommandMoq.CanExecute(It.IsAny<object>())).Returns(true);
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Once);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Once);
        Assert.IsTrue(dragEventArgs.Handled);
    }

    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_PreviewDragOver_NoData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.None);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_PreviewDragOver_Data()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act PreviewDragOver - Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                                       System.Windows.DataFormats.FileDrop,
                                       true, new string[] { "" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.PreviewDragOverEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        Assert.AreEqual(dragEventArgs.Effects, System.Windows.DragDropEffects.Copy);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_Drop_NoData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act Drop - No Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                               System.Windows.DataFormats.FileDrop,
                               false, null);
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Never);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_Drop_InvalidData()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        // Act Drop - Invalid Data
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "Bad Path" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Never);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_Drop_Data_CanExecuteFalse()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);


        //  Drop - Data CanExecute = false
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32\\notepad.exe" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        onDropCommandMoq.Setup(onDropCommandMoq => onDropCommandMoq.CanExecute(It.IsAny<object>())).Returns(false);
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Once);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Never);
        Assert.IsTrue(dragEventArgs.Handled);
    }
    [WpfTestMethod]
    public void RegisterTextBoxDragDropTest_Command_DontAcceptFolders_Drop_Data_CanExecuteTrue()
    {
        // Arrange
        var textBoxMoq = new Mock<System.Windows.Controls.TextBox>();
        var moqSender = new Mock<DragEventHandler>();
        var onDropCommandMoq = new Mock<System.Windows.Input.ICommand>();
        System.Windows.DragEventArgs dragEventArgs = null;

        // Act Setup
        DragDropHelper.RegisterTextBoxDragDrop(textBoxMoq.Object, onDropCommandMoq.Object, false);

        // Assert
        Assert.IsTrue(textBoxMoq.Object.AllowDrop);

        //  Drop - Data CanExecute = true
        dragEventArgs = MakeDragEvents(DragDropEffects.All,
                       System.Windows.DataFormats.FileDrop,
                       true, new string[] { "C:\\Windows\\System32\\notepad.exe" });
        dragEventArgs.RoutedEvent = System.Windows.UIElement.DropEvent;
        onDropCommandMoq.Setup(onDropCommandMoq => onDropCommandMoq.CanExecute(It.IsAny<object>())).Returns(true);
        textBoxMoq.Object.RaiseEvent(dragEventArgs);
        onDropCommandMoq.Verify(a => a.CanExecute(It.IsAny<object>()), Times.Once);
        onDropCommandMoq.Verify(a => a.Execute(It.IsAny<object>()), Times.Once);
        Assert.IsTrue(dragEventArgs.Handled);
    }
}
