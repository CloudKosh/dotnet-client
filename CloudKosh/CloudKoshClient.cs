using RetroCoreFit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CloudKosh;

public class CloudKoshClient
{

    public static async Task<CloudKoshClient> ConnectAsync(string host, string @namespace, string bucket, string key, string secret)
    {
        var c = new CloudKoshClient(host, @namespace, bucket);
        await c.LoginAsync(key, secret);
        return c;
    }

    private async Task LoginAsync(string key, string secret)
    {

        var nonce = RequestBuilder.Get("auth/nonce").AsTextAsync(client);

        var hash = System.Security.Cryptography.SHA256.Create();
        var sha256Base64 = System.Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(nonce + secret)));

        var r = await RequestBuilder.Post("auth/login")
            .Body(new { key, nonce, sha256Base64 })
            .AsJsonAsync<LoginResponse>(client);

        client.DefaultRequestHeaders.Authorization = System.Net.Http.Headers.AuthenticationHeaderValue.Parse($"Bearer {r.Authorization}");
    }


    private CloudKoshClient(
        string host,
        string @namespace,
        string bucket
    )
    {
        this.BaseUrl = new Uri($"https://{host}/v1/api/");

        this.client = new HttpClient();

        this.@namespace = @namespace;
        this.bucket = bucket;

        this.client.BaseAddress = this.BaseUrl;
    }

    public Uri BaseUrl { get; }

    private readonly HttpClient client;
    private readonly string @namespace;
    private readonly string bucket;

    public async Task UploadAsync(string key, string contentType, Stream stream)
    {
        var tmp = Path.GetTempFileName();
        using var fs = File.OpenWrite(tmp);
        await stream.CopyToAsync(fs);
        await UploadFileAsync(key, contentType, tmp);
        File.Delete(tmp);
    }

    public async Task<FileHeaders> DownloadAsync(string key, Stream stream)
    {
        var body = await RequestBuilder.Get($"files/{@namespace}/{bucket}/{key}")
            .AsResponseMessageAsync(client);
        await body.Content.CopyToAsync(stream);
        
        return new FileHeaders {
            ContentType = body.Content.Headers.ContentType.ToString()
        };
    }

    public async Task UploadFileAsync(string key, string contentType, string tmp)
    {
        // make this parallel....

        var h = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        string sha256Base64 = null;
        using(var fs =  File.OpenRead(tmp))
        {
            var hash = await Sha256Helper.ComputeSha256Async(fs);
            sha256Base64 = System.Convert.ToBase64String(hash);
        }

        using var fs2 = File.OpenRead(tmp);

        await RequestBuilder.Post($"files/{@namespace}/{bucket}/{key}")
            .Multipart("sha256Base64", sha256Base64)
            .MultipartFile("file", fs2, "file.dat", contentType)
            .AsJsonAsync<LoginResponse>(client);
    }
}
