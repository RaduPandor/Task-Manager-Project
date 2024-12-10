using Moq;
using TaskManager.Services;

namespace Tests
{
    public class NativeMethodsFacts
    {
        [Fact]
        public void GetProcessOwnerReturnsCorrectAccountName()
        {
            var mockNativeMethodsService = new Mock<INativeMethodsService>();
            var processHandle = new IntPtr(123);
            var tokenHandle = new IntPtr(456);
            mockNativeMethodsService
                .Setup(service => service.OpenProcess(It.IsAny<uint>(), It.IsAny<bool>(), It.IsAny<int>()))
                .Returns(processHandle);
            mockNativeMethodsService
                .Setup(service => service.OpenProcessToken(processHandle, It.IsAny<uint>(), out tokenHandle))
                .Returns(true);
            mockNativeMethodsService
                .Setup(service => service.GetTokenInformation(tokenHandle, It.IsAny<uint>(), It.IsAny<IntPtr>(), It.IsAny<uint>(), out It.Ref<uint>.IsAny))
                .Returns(true);
            mockNativeMethodsService
                .Setup(service => service.LookupAccountName(It.IsAny<IntPtr>()))
                .Returns("TestUser");
            mockNativeMethodsService
                .Setup(service => service.CloseHandle(It.IsAny<IntPtr>()))
                .Returns(true);

            var performanceMetricsHelper = new PerformanceMetricsService(mockNativeMethodsService.Object);
            var result = performanceMetricsHelper.GetProcessOwner(123);
            Assert.Equal("TestUser", result);
        }
    }
}
