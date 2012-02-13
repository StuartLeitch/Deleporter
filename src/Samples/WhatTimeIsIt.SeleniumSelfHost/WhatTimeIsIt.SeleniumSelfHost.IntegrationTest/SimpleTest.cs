using System;
using System.Linq;
using System.Net;
using DeleporterCore;
using DeleporterCore.Client;
using DeleporterCore.Configuration;
using DeleporterCore.SelfHosting.SeleniumServer.Servers;
using DeleporterCore.SelfHosting.Servers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using WhatTimeIsIt.SeleniumSelfHost.IntegrationTest.Infrastructure;
using WhatTimeIsIt.SeleniumSelfHost.Services;

namespace WhatTimeIsIt.SeleniumSelfHost.IntegrationTest
{
    [TestClass]
    public class SimpleTest
    {
        private static IWebDriver Driver { get { return DriverFactory.Driver; } }

        [AssemblyCleanup]
        public static void AssemblyCleanup() {
            DriverFactory.DisposeDriver();

            if (!DeleporterConfiguration.BypassSelfHosting) {
                // Cassini must be stopped after Selenium - otherwise Cassini won't release port in a timely fashion
                SeleniumServer.Instance.Stop();
                Cassini.Instance.Stop();
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext testContext) {
            BypassSelfHostingRunFirefoxSingleDriver();
            
            LoggerClient.LoggingEnabled = true;

            if (!DeleporterConfiguration.BypassSelfHosting) {
                Cassini.Instance.Start();
                SeleniumServer.Instance.Start();
            }
            {
                DeleporterUtilities.IterateRemotingPortIfNeeded();
                DeleporterUtilities.RecycleServerAppDomain();
                DeleporterUtilities.PrimeServerHomepage();
            };
        }

        private static void BypassSelfHostingRunFirefoxSingleDriver()
        {
            DeleporterConfiguration.SetBypassSelfHosting();
            DriverFactory.DriverGenerationMethod = () => new FirefoxDriver();
            DriverFactory.KeepDriverForAllTests = true;
        }


        [TestMethod]
        public void DisplaysCurrentYear() {
            Driver.Navigate().GoToUrl(DeleporterConfiguration.SiteBaseUrl);
            var dateElement = Driver.FindElement(By.Id("date"));
            var displayedDate = DateTime.Parse(dateElement.Text);
            Assert.AreEqual(DateTime.Now.Year, displayedDate.Year);

            Console.WriteLine(new WebClient().DownloadString(DeleporterConfiguration.SiteBaseUrl));
        }

        [TestMethod]
        public void DisplaysSpecialMessageIfWebServerHasSomehowGoneBackInTime() {
            // Inject a mock IDateProvider, setting the clock back to 1975
            var dateToSimulate = new DateTime(1975, 1, 1);
            Deleporter.Run(() =>
                               {
                                   var mockDateProvider = new Mock<IDateProvider>();
                                   mockDateProvider.Setup(x => x.CurrentDate).Returns(dateToSimulate);
                                   NinjectControllerFactoryUtils.TemporarilyReplaceBinding(mockDateProvider.Object);
                               });

            // Now see what it displays
            Driver.Navigate().GoToUrl(DeleporterConfiguration.SiteBaseUrl);
            var dateElement = Driver.FindElement(By.Id("date"));
            var displayedDate = DateTime.Parse(dateElement.Text);
            Assert.AreEqual(1975, displayedDate.Year);

            var extraInfo = Driver.FindElement(By.Id("extraInfo")).Text;
            Assert.IsTrue(extraInfo.Contains("The world wide web hasn't been invented yet"));

            Console.WriteLine(new WebClient().DownloadString(DeleporterConfiguration.SiteBaseUrl));
        }

        [TestCleanup]
        public void MyTestCleanup() {
            // Runs any tidy up tasks in both the local and remote appdomains
            TidyupUtils.PerformTidyup();
            Deleporter.Run(TidyupUtils.PerformTidyup);
        }

        [TestInitialize]
        public void TestInit() {
            DriverFactory.GetNewDriverForTestIfNewDriverSet();
        }
    }
}