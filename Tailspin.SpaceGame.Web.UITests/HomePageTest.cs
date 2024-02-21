using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using System;

namespace UITests
{
    [TestFixture("Chrome")]
    [TestFixture("Firefox")]
    [TestFixture("Edge")]
    public class HomePageTest
    {
        private string browser;
        private IWebDriver driver;

        public HomePageTest(string browser)
        {
            this.browser = browser;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            try
            {
                driver = browser switch
                {
                    "Chrome" => new ChromeDriver(
                        Environment.GetEnvironmentVariable("ChromeWebDriver")
                    ),
                    "Firefox" => new FirefoxDriver(
                        Environment.GetEnvironmentVariable("GeckoWebDriver")
                    ),
                    "Edge" => new  EdgeDriver(
                        Environment.GetEnvironmentVariable("EdgeWebDriver")
                    ),
                    _ => throw new ArgumentException($"'{browser}': Unknown browser")
                };

                // Wait until the page is fully loaded on every page navigation or page reload.
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);

                // Navigate to the site.
                // The site name is stored in the SITE_URL environment variable to make
                // the tests more flexible.
                string url = Environment.GetEnvironmentVariable("SITE_URL");
                driver.Navigate().GoToUrl(url + "/");

                // Wait for the page to be completely loaded.
                new WebDriverWait(driver, TimeSpan.FromSeconds(10))
                    .Until(d => ((IJavaScriptExecutor) d)
                        .ExecuteScript("return document.readyState")
                        .Equals("complete"));
            }
            catch (DriverServiceNotFoundException)
            {
                Console.WriteLine("DriverServiceNotFoundException");
            }
            catch (WebDriverException)
            {
                Console.WriteLine("WebDriverException");
                Cleanup();
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            driver?.Quit();
        }

        // Download game
        [TestCase("download-btn", "pretend-modal")]
        // Screen image
        [TestCase("screen-01", "screen-modal")]
        // Top player on the leaderboard
        [TestCase("profile-1", "profile-modal-1")]
        public void ClickLinkById_ShouldDisplayModalById(string linkId, string modalId)
        {
            // Skip the test if the driver could not be loaded.
            // This happens when the underlying browser is not installed.
            if (driver == null)
            {
                Assert.Ignore();
                return;
            }

            // Locate the link by its ID and then click the link.
            ClickElement(FindElement(By.Id(linkId)));

            // Locate the resulting modal.
            IWebElement modal = FindElement(By.Id(modalId));

            // Record whether the modal was successfully displayed.
            bool modalWasDisplayed = modal?.Displayed ?? false;

            // Close the modal if it was displayed.
            if (modalWasDisplayed)
            {
                // Click the close button that's part of the modal.
                ClickElement(FindElement(By.ClassName("close"), modal));

                // Wait for the modal to close and for the main page to again be clickable.
                FindElement(By.TagName("body"));
            }

            // Assert that the modal was displayed successfully.
            // If it wasn't, this test will be recorded as failed.
            Assert.That(modalWasDisplayed, Is.True);
        }

        private IWebElement FindElement(By locator, IWebElement parent = null, int timeoutSeconds = 10)
        {
            // WebDriverWait enables us to wait for the specified condition to be true
            // within a given time period.
            return new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
                .Until(c => {
                    // If a parent was provided, find its child element.
                    // Otherwise, locate the element from the root of the DOM.
                    IWebElement element = ((ISearchContext)parent ?? driver).FindElement(locator);
                    // Return true after the element is displayed and is able to receive user input.
                    return (element != null && element.Displayed && element.Enabled) ? element : null;
                });
        }

        private void ClickElement(IWebElement element)
        {
            // We expect the driver to implement IJavaScriptExecutor.
            // IJavaScriptExecutor enables us to execute JavaScript code during the tests.
            if (driver is IJavaScriptExecutor js)
            {
                js.ExecuteScript("arguments[0].click();", element);
            }
            else
            {
                throw new NotSupportedException("Driver does not support IJavaScriptExecutor.");
            }
        }
    }
}