#addin nuget:?package=Polly&version=7.2.2

using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using Polly;


public record StorageAccount(
    string Name,
    string Container,
    string Key
);

private static string CreateAuthorization(
    StorageAccount storageAccount,
    string verb,
    long contentLength,
    string contentType,
    string xMsDate,
    string fileName)
{
    string stringToSign = string.Join(
        "\n",
        verb,
        string.Empty, // Content encoding
        string.Empty, // Content language
        contentLength.ToString(CultureInfo.InvariantCulture), // Content length
        string.Empty, // Content MD5
        contentType, //Content type
        string.Empty, // Date
        string.Empty, // If-Modified-Since
        string.Empty, // If-Match
        string.Empty, // If-None-Match
        string.Empty, // If-Unmodified-Since.
        string.Empty, // Range
        "x-ms-blob-type:BlockBlob",
        $"x-ms-date:{xMsDate}",
        "x-ms-version:2015-12-11",
        $"/{storageAccount.Name}/{storageAccount.Container}/{fileName}"
        );

    var keyBytes = Convert.FromBase64String(storageAccount.Key);

    var hmac = new HMACSHA256(keyBytes);

    var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

    var authorization = $"SharedKey {storageAccount.Name}:{signature}";

    return authorization;
}

public static AsyncPolicy UploadToBlobStoragePolicy { get; } = Policy
                                                    .Handle<Exception>()
                                                    .WaitAndRetryAsync(5,
                                                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

public static async Task UploadToBlobStorage(this ICakeContext context,
        StorageAccount storageAccount,
        FilePath sourceFilePath,
        FilePath targetFilePath)
{
    if (context == null)
    {
        throw new ArgumentNullException(nameof(context));
    }

    if (storageAccount == null)
    {
        throw new ArgumentNullException(nameof(storageAccount));
    }

    if (string.IsNullOrEmpty(storageAccount.Name))
    {
        throw new ArgumentNullException($"{nameof(storageAccount)}.{nameof(storageAccount.Name)}");
    }

    if (string.IsNullOrEmpty(storageAccount.Container))
    {
        throw new ArgumentNullException($"{nameof(storageAccount)}.{nameof(storageAccount.Container)}");
    }

    if (string.IsNullOrEmpty(storageAccount.Key))
    {
        throw new ArgumentNullException($"{nameof(storageAccount)}.{nameof(storageAccount.Key)}");
    }

    if (sourceFilePath == null)
    {
        throw new ArgumentNullException(nameof(sourceFilePath));
    }

    if (targetFilePath == null)
    {
        throw new ArgumentNullException(nameof(targetFilePath));
    }

    var url = $"https://{storageAccount.Name}.blob.core.windows.net/{storageAccount.Container}/{targetFilePath.FullPath}";

    string  verb = "PUT",
            contentType = "application/octet-stream", // TODO set correct datatype
            xMsDate = DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture);

    var inputStream  = context.FileSystem.GetFile(sourceFilePath).OpenRead();

    var contentLength = inputStream.Length;

    var authorization = CreateAuthorization(
        storageAccount,
        verb,
        inputStream.Length,
        contentType,
        xMsDate,
        targetFilePath.FullPath
        );

    await UploadToBlobStoragePolicy.ExecuteAsync(async () => {
        var httpClient = new HttpClient {
            DefaultRequestHeaders =
            {
                { "x-ms-version", "2015-12-11" },
                { "x-ms-date", xMsDate },
                { "x-ms-blob-type", "BlockBlob" },
                { "Authorization", authorization }
            },
        };

        var content = new StreamContent(inputStream) {
            Headers = {
                ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType),
                ContentLength = contentLength
            }
        };

        var response = await httpClient.PutAsync(
            url,
            content);

        response.EnsureSuccessStatusCode();
    });
}
