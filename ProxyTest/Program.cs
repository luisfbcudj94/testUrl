
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V122.Fetch;
using OpenQA.Selenium.DevTools.V122.Network;
using OpenQA.Selenium.Interactions;
using System;


string url = "https://albertsons.okta.com/oauth2/ausp6soxrIyPrm8rS2p6/v1/authorize?client_id=0oap6ku01XJqIRdl42p6&response_type=code&scope=openid%20profile%20email%20offline_access&redirect_uri=https://www.safeway.com/bin/safeway/unified/sso/authorize&state=wasteful-stem-rabid-join&nonce=1d5ce452-f925-461a-92a5-c7f22521b11d&prompt=none";

ChromeOptions options = new ChromeOptions();
options.AddArgument("--disable-features=IsolateOrigins,site-per-process");
options.BinaryLocation = @"C:\Users\aleja\Downloads\chrome-win64\chrome.exe";
options.AddArguments("disable-features=NetworkService");
options.AddArguments("--disable-web-security");
options.AddArguments("--allow-running-insecure-content");
options.AddArguments("--disable-extensions");
options.AddArguments("--ignore-certificate-errors");
options.AddArguments("--disable-notifications");
options.AddArguments("--disable-popup-blocking");


using var driver = new ChromeDriver(options);

String script = "var performance = window.performance || window.mozPerformance || window.msPerformance || window.webkitPerformance || {}; var network = performance.getEntries() || {}; return network;";
var networkdata = ((IJavaScriptExecutor)driver).ExecuteScript(script);

List<string> requests = new List<string>();
List<string> responses = new List<string>();
Dictionary<string, List<JObject>> dataToExcelRequest = new Dictionary<string, List<JObject>>();
Dictionary<string, List<JObject>> dataToExcelResponse = new Dictionary<string, List<JObject>>();

IDevTools devTools = driver;
DevToolsSession session = devTools.GetDevToolsSession();
await session.Domains.Network.EnableNetwork();
session.DevToolsEventReceived += (sender, e) =>
{

    if (e.EventName == "requestWillBeSentExtraInfo")
    {
        JObject jsonObjectRequest = JObject.Parse(e.EventData.ToString());
        JObject resultObjectRequest = new JObject();
        JObject jsonObjectRequestHeaders = JObject.Parse(jsonObjectRequest["headers"].ToString());

        resultObjectRequest["requestId"] = jsonObjectRequest["requestId"];
        resultObjectRequest["authority"] = jsonObjectRequestHeaders[":authority"];
        resultObjectRequest["path"] = jsonObjectRequestHeaders[":path"];
        resultObjectRequest["Action"] = "Request";

        string resultJsonRequest = resultObjectRequest.ToString(Formatting.Indented);

        if (dataToExcelRequest.ContainsKey(resultObjectRequest["requestId"].ToString()))
        {
            dataToExcelRequest[resultObjectRequest["requestId"].ToString()].Add(JObject.Parse(resultJsonRequest));
        }
        else
        {
            dataToExcelRequest.Add(resultObjectRequest["requestId"].ToString(), new List<JObject> { JObject.Parse(resultJsonRequest) });
        }



        Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
        Console.WriteLine($"\n--------EVENT NAME: ----------------------- {e.EventName}\n");
        Console.WriteLine(e.EventData);
        Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
    }

    if (e.EventName == "responseReceivedExtraInfo")
    {
        JObject jsonObjectResponse = JObject.Parse(e.EventData.ToString());
        JObject resultObjectResponse = new JObject();
        JObject jsonObjectResponseHeaders = JObject.Parse(jsonObjectResponse["headers"].ToString());

        resultObjectResponse["requestId"] = jsonObjectResponse["requestId"];
        resultObjectResponse["statusCode"] = jsonObjectResponse["statusCode"];
        resultObjectResponse["Action"] = "Response";

        string resultJsonResponse = resultObjectResponse.ToString(Formatting.Indented);

        if (dataToExcelResponse.ContainsKey(resultObjectResponse["requestId"].ToString()))
        {
            dataToExcelResponse[resultObjectResponse["requestId"].ToString()].Add(JObject.Parse(resultJsonResponse));
        }
        else
        {
            dataToExcelResponse.Add(resultObjectResponse["requestId"].ToString(), new List<JObject> { JObject.Parse(resultJsonResponse) });
        }

        Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
        Console.WriteLine($"\n--------EVENT NAME: ----------------------- {e.EventName}\n");
        Console.WriteLine(e.EventData);
        Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
    }

};



driver.Navigate().GoToUrl(url);


await Task.Delay(TimeSpan.FromSeconds(10));

Console.WriteLine("");

Console.ReadLine();
