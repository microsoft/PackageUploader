using PackageUploader.UI.ViewModel;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PackageUploader.UI.Test.ViewModel
{
    [TestClass]
    public sealed class BaseViewModelTest
    {

        private BaseViewModel _baseViewModel;

        [TestInitialize]
        public void Initialize()
        {
            _baseViewModel = new BaseViewModel();
        }

        [TestMethod]
        public void TestMethod1()
        {
            _baseViewModel.PropertyChanged += (sender, args) =>
            {
                Assert.AreEqual("Test", args.PropertyName);
            };
            
        }
    }
}
