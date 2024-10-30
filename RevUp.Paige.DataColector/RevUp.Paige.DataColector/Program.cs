// See https://aka.ms/new-console-template for more information
using System.Net.Http;
using System;
using System.Threading;
using System.Text.Json.Nodes;
using RevUp.Paige.DataColector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;
using Amazon.S3;
using Amazon;
using Amazon.S3.Transfer;
using System.Web;

Console.WriteLine("Data collection starting...");


var knowledgePortalURL = "https://knowledgeportal.pageuppeople.com/api/v2/help_center/en-gb/articles.json";
var sectionIds = new List<string>() {
    "15297902839449",
    "15297937523609",
    "15298443940249",
    "15298412158361",
    "15298331743513",
    "15298663455897",
    "15298247988505",
    "15304492236057",
    "15304488759577"
};
var page = 1;
var pageSize = 100;

var path = Directory.GetCurrentDirectory() + "\\articles";
Console.WriteLine($"{path}");

if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

while (true)
{
    Console.WriteLine("Loading data from zendesk");
    var httpClient = new HttpClient();
    var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{knowledgePortalURL}?page={page}&per_page={pageSize}");

    var userName = "";
    var userPassword = "";
    var authenticationString = $"{userName}:{userPassword}";
    var base64String = Convert.ToBase64String(
       System.Text.Encoding.ASCII.GetBytes(authenticationString));

    httpRequest.Headers.Add("Authorization", $"Basic {base64String}");
    Console.WriteLine("Loading Page: " + page);
    Console.WriteLine();

    var response = await httpClient.SendAsync(httpRequest);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine("Failed to load data from zendesk");
        return -1;
    }

    Console.WriteLine("Loading data from zendesk was successful. Processing the data");

    var responseContent = await response.Content.ReadAsStringAsync();

    var deserializedResponseContent = JsonConvert.DeserializeObject<Response>(responseContent);
    var articles = deserializedResponseContent != null ? deserializedResponseContent.Articles : null;

    if (articles == null || articles.Count == 0)
    {
        break;
    }

    Console.WriteLine("No. of Articles on this page: " + articles.Count);
    foreach (Article article in articles)
    {
        if (!sectionIds.Any(sid => sid.Equals(article.SectionId)))
        {
            continue;
        }

        Console.WriteLine("Article: " + article.Title);
        
        StringWriter writer = new StringWriter();
        HttpUtility.HtmlEncode(article.Body, writer);
        var encodedBodyString = writer.ToString();

        File.WriteAllText(path + $"\\{article.Id}-{article.SectionId}.txt", $"\"title\":\"{article.Title}\", \"body\": \"{encodedBodyString}\", \"section_id\": \"{article.SectionId}\"");
    }

    page++;
}

Console.WriteLine("Uploading files to S3");
var s3Client = new AmazonS3Client(
    awsAccessKeyId: Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
    awsSecretAccessKey: Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
    awsSessionToken: Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"),
    region: RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("AWS_REGION"))
);

var bucketName = "paige-knowledge-base";

var directoryTransferUtility = new TransferUtility(s3Client);

await directoryTransferUtility.UploadDirectoryAsync(path, bucketName, "*.txt", SearchOption.AllDirectories);

return 0;