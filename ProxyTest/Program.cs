
using CsvHelper.Configuration;
using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V122.Fetch;
using OpenQA.Selenium.DevTools.V122.Network;
using OpenQA.Selenium.Interactions;
using System;
using System.Globalization;


//string url = "https://albertsons.okta.com/oauth2/ausp6soxrIyPrm8rS2p6/v1/authorize?client_id=0oap6ku01XJqIRdl42p6&response_type=code&scope=openid%20profile%20email%20offline_access&redirect_uri=https://www.safeway.com/bin/safeway/unified/sso/authorize&state=wasteful-stem-rabid-join&nonce=1d5ce452-f925-461a-92a5-c7f22521b11d&prompt=none";

//string url = "https://mail.google.com/mail/u/0/?pli=1#inbox";

var urlFilePath = "url.txt";
string[] urls = File.ReadAllLines(urlFilePath);

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


//await initSessionChrome();
//await Task.Delay(3000);
//Console.WriteLine("TIME IS DONE");
//await initSessionChrome();

string url = string.Empty;

foreach (var item in urls)
{
    url = item;
    await initSessionChrome();
    await Task.Delay(3000);
    Console.WriteLine("TIME IS DONE");
}

async Task initSessionChrome()
{
    bool firstRequest = false;
    string firstRequestId = string.Empty;
    bool stopSession = false;

    Dictionary<string, List<JObject>> dataToExcelRequest = new Dictionary<string, List<JObject>>();
    Dictionary<string, List<JObject>> dataToExcelResponse = new Dictionary<string, List<JObject>>();

    using var driver = new ChromeDriver(options);
    IDevTools devTools = driver;
    DevToolsSession session = devTools.GetDevToolsSession();
    await session.Domains.Network.EnableNetwork();


    session.DevToolsEventReceived += (sender, e) =>
    {
        if (dataToExcelRequest.Count == 0 && dataToExcelResponse.Count == 0)
        {
            firstRequest = true;
        }
        else
        {
            firstRequest = false;
        }

        if (e.EventName == "requestWillBeSentExtraInfo" && !stopSession)
        {
            JObject jsonObjectRequest = JObject.Parse(e.EventData.ToString());
            JObject resultObjectRequest = new JObject();
            JObject jsonObjectRequestHeaders = JObject.Parse(jsonObjectRequest["headers"].ToString());

            resultObjectRequest["requestId"] = jsonObjectRequest["requestId"];
            resultObjectRequest["authority"] = jsonObjectRequestHeaders[":authority"];
            resultObjectRequest["path"] = jsonObjectRequestHeaders[":path"];
            resultObjectRequest["Action"] = "Request";



            string resultJsonRequest = resultObjectRequest.ToString(Formatting.Indented);

            if (firstRequest == false && firstRequestId != resultObjectRequest["requestId"]?.ToString())
            {
                stopSession = true;
                try
                {
                    Task.Delay(3000);
                    Console.WriteLine("TIME IS DONE DRIVER QUIT");
                    //if (driver != null)
                    //{
                    //    driver.Dispose();
                    //}
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }
            else
            {
                if (dataToExcelRequest.ContainsKey(resultObjectRequest["requestId"].ToString()))
                {
                    dataToExcelRequest[resultObjectRequest["requestId"].ToString()].Add(JObject.Parse(resultJsonRequest));
                }
                else
                {
                    dataToExcelRequest.Add(resultObjectRequest["requestId"].ToString(), new List<JObject> { JObject.Parse(resultJsonRequest) });
                }

                // Validate if is the first request
                if (firstRequest)
                {
                    firstRequestId = resultObjectRequest["requestId"].ToString();
                }
            }


            Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
            Console.WriteLine($"\n--------EVENT NAME: ----------------------- {e.EventName}\n");
            Console.WriteLine(e.EventData);
            Console.WriteLine("\n------------------------- NEW DATA --------------------------------------------------\n");
        }


        if (e.EventName == "responseReceivedExtraInfo" && !stopSession)
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

    await Task.Delay(3000);
    Console.WriteLine("TIME IS DONE GO TO URL");

    processingData(dataToExcelRequest, dataToExcelResponse);
}





/* CREATE NEW DICTIONARY */

void processingData(Dictionary<string, List<JObject>> dataToExcelRequest, Dictionary<string, List<JObject>> dataToExcelResponse)
{
    Dictionary<string, List<JObject>> combinedDictionary = new Dictionary<string, List<JObject>>();


    HashSet<string> allKeys = new HashSet<string>(dataToExcelRequest.Keys.Concat(dataToExcelResponse.Keys));

    foreach (var key in allKeys)
    {
        List<JObject> requestJObjects = dataToExcelRequest.ContainsKey(key) ? dataToExcelRequest[key] : new List<JObject>();

        List<JObject> responseJObjects = dataToExcelResponse.ContainsKey(key) ? dataToExcelResponse[key] : new List<JObject>();

        List<JObject> combinedJObjects = InterleaveLists(requestJObjects, responseJObjects);

        combinedDictionary.Add(key, combinedJObjects);
    }

    foreach (var kvp in combinedDictionary)
    {
        Console.WriteLine($"Clave: {kvp.Key}");

        foreach (var jObject in kvp.Value)
        {
            Console.WriteLine(jObject.ToString());
        }
    }

    string csvFilePath = "output.csv";

    WriteDictionaryToCsv(combinedDictionary, csvFilePath);
}



static List<JObject> InterleaveLists(List<JObject> list1, List<JObject> list2)
{
    List<JObject> interleavedList = new List<JObject>();
    int maxLength = Math.Max(list1.Count, list2.Count);

    for (int i = 0; i < maxLength; i++)
    {
        if (i < list1.Count)
        {
            interleavedList.Add(list1[i]);
        }
        if (i < list2.Count)
        {
            interleavedList.Add(list2[i]);
        }
    }

    return interleavedList;
}

static void WriteDictionaryToCsv(Dictionary<string, List<JObject>> dictionary, string filePath)
{
    var existsFile = File.Exists(filePath);

    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        //HasHeaderRecord = !existsFile,
        Delimiter = ","
    };

    using (var writer = new StreamWriter(filePath, append: true))
    using (var csv = new CsvWriter(writer, csvConfig))
    {
        if (!existsFile)
        {
            csv.WriteField("Request Id");
            csv.WriteField("Action");
            csv.WriteField("URL");
            csv.WriteField("Status Code");
            csv.NextRecord();
        }

        var allKeys = dictionary.Keys.ToList();

        foreach (var key in allKeys)
        {
            var jObjects = dictionary[key];

            foreach (var jObject in jObjects)
            {
                var action = jObject["Action"].ToString();
                var url = action == "Request" ? $"{jObject["authority"]}{jObject["path"]}" : "";
                var statusCode = action == "Response" ? jObject["statusCode"]?.ToString() : "";

                csv.WriteField(key);
                csv.WriteField(action);
                csv.WriteField(url);
                csv.WriteField(statusCode);
                csv.NextRecord();
            }
        }
    }
}
