using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace DingtalkDemo.Controllers;

[ApiController]
[Route("robots")]
public class RobotsController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<RobotsController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    public RobotsController(
        IConfiguration configuration,
        ILogger<RobotsController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// ���� Text ��Ϣ
    /// </summary>
    /// <returns></returns>
    [HttpPost("text")]
    public async Task<ActionResult> SendTextInfo()
    {
        var at = new At() { IsAtAll = true, };
        var message = new Text() { Content = $"�Ҿ�����, ���ǲ�һ�����̻�" };
        var result = await SendDingTalkMessage("text", message, at);
        return Ok(result);
    }

    /// <summary>
    /// ���� Text ��Ϣ
    /// </summary>
    /// <returns></returns>
    [HttpPost("link")]
    public async Task<ActionResult> SendLinkInfo()
    {
        var at = new At() { IsAtAll = true, };
        var message = new Link()
        {
            Title = "����Link��Ϣ",
            Text = "����Link��Ϣ",
            PicUrl = $"https://img.alicdn.com/tfs/TB1NwmBEL9TBuNjy1zbXXXpepXa-2400-1218.png",
            MessageUrl = "https://open.dingtalk.com/document/"
        };
        var result = await SendDingTalkMessage("link", message, at);
        return Ok(result);
    }

    /// <summary>
    /// ���� Markdown ��Ϣ
    /// </summary>
    /// <returns></returns>
    [HttpPost("markdown")]
    public async Task<ActionResult> SendMarkdownInfo()
    {
        var at = new At() { IsAtAll = true, };
        var message = new Markdown()
        {
            Title = $"��������",
            Text = $"#### �������� \n> 9�ȣ�������1����������89������¶�73%\n> ![screenshot](https://img.alicdn.com/tfs/TB1NwmBEL9TBuNjy1zbXXXpepXa-2400-1218.png)\n> ###### 10��20�ַ��� [����](https://www.dingalk.com) \n"
        };
        var result = await SendDingTalkMessage("markdown", message, at);
        return Ok(result);
    }

    /// <summary>
    /// ���� ActionCard ��Ϣ
    /// </summary>
    /// <returns></returns>
    [HttpPost("cation-card")]
    public async Task<ActionResult> SendActionCardInfo()
    {
        var at = new At() { IsAtAll = true, };
        var message = new ActionCard()
        {
            Title = $"������תactionCard��Ϣ",
            Text = "@user123  \n  ![����һ��ͼƬ](https://img.alicdn.com/tfs/TB1NwmBEL9TBuNjy1zbXXXpepXa-2400-1218.png)  \n  ����һ��������תactionCard��Ϣ",
            SingleTitle = "�Ķ�ȫ��",
            SingleURL = "https://open.dingtalk.com/document/"
        };
        var result = await SendDingTalkMessage("actionCard", message, at);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> ReceiveInfo(JsonElement json)
    {
        try
        {
            WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ReceiveInfo Request:\n{json}");
            var info = json.Deserialize<OapiRobotInfo>();
            var content = info?.Text?.Content ?? string.Empty;
            var userId = info?.SenderStaffId ?? string.Empty;
            var senderNick = info?.SenderNick ?? string.Empty;
            var sessionWebhook = info?.SessionWebhook ?? string.Empty;
            var at = new At()
            {
                AtUserIds = new string[] { userId },
                IsAtAll = false,
            };
            var message = new Text() { Content = $"Hi {senderNick}���㷢����Ϣ�ǣ�{content}��" };

            //var result = await SendDingTalkMessage("text", message, at, sessionWebhook);
            var result = await Task.FromResult(GetRequestJsonString("text", message, at));
            return Ok(result);
        }
        catch (Exception ex)
        {
            WriteLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ReceiveInfo Exception:\n{ex?.ToString()}");
            return Ok(new { errcode = -1, errmsg = ex?.Message });
        }
    }

    private static string GetRequestJsonString(string msgtype, object message, At at)
    {
        var param = new Dictionary<string, object>
        {
            { "at", at },
            { "msgtype", msgtype },
            { msgtype, message }
        };
        var result = JsonSerializer.Serialize(param);
        return result;
    }

    private async Task<string> SendDingTalkMessage(string msgtype, object message, At at)
    {
        var requestUri = _config["DingtalkRobot:Webhook"];
        var appSecret = _config["DingtalkRobot:Secret"];
        if (!string.IsNullOrWhiteSpace(appSecret))
        {
            var (timestamp, sign) = GetSignInfo(appSecret);
            requestUri = $"{requestUri}&timestamp={timestamp}&sign={sign}";
        }
        var sendMessage = GetRequestJsonString(msgtype, message, at);
        var content = new StringContent(sendMessage, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        var client = _httpClientFactory.CreateClient();
        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();
        //{"errcode":0,"errmsg":"ok"}
        //{"errcode":310000,"errmsg":"sign not match, more: [https://ding-doc.dingtalk.com/doc#/serverapi2/qf2nxq]"}
        return result;
    }
    private static (long timestamp, string sign) GetSignInfo(string appSecret)
    {
        var timestamp = ToUTC(DateTime.Now);
        var stringToSign = $"{timestamp}\n{appSecret}";
        var b64 = GetHmac(stringToSign, appSecret);
        var b64Str = Convert.ToBase64String(b64);
        var sign = HttpUtility.UrlEncode(b64Str);
        return (timestamp, sign);
    }
    private static byte[] GetHmac(string message, string secret)
    {
        byte[] keyByte = Encoding.UTF8.GetBytes(secret);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmacsha256 = new HMACSHA256(keyByte);
        byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
        return hashmessage;
    }
    public static long ToUTC(DateTime time)
    {
        var zts = TimeZoneInfo.Local.BaseUtcOffset;
        var yc = new DateTime(1970, 1, 1).Add(zts);
        return (long)(time - yc).TotalMilliseconds;
    }

    private static void WriteLog(string? text)
    {
        using StreamWriter w = System.IO.File.AppendText("robots.log");
        w.WriteLine(text);
    }
}



public class At
{
    [JsonPropertyName("atMobiles")]
    public string[]? AtMobiles { get; set; }

    [JsonPropertyName("atUserIds")]
    public string[]? AtUserIds { get; set; }

    [JsonPropertyName("isAtAll")]
    public bool IsAtAll { get; set; }
}
public class Text
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
public class Markdown
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
public class ActionCard
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("singleTitle")]
    public string? SingleTitle { get; set; }

    [JsonPropertyName("singleURL")]
    public string? SingleURL { get; set; }
}
public class Link
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("picUrl")]
    public string? PicUrl { get; set; }

    [JsonPropertyName("messageUrl")]
    public string? MessageUrl { get; set; }
}

public class OapiRobotInfo
{
    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("atUsers")]
    public Atuser[]? AtUsers { get; set; }

    [JsonPropertyName("chatbotCorpId")]
    public string? ChatbotCorpId { get; set; }

    [JsonPropertyName("chatbotUserId")]
    public string? ChatbotUserId { get; set; }

    [JsonPropertyName("msgId")]
    public string? MsgId { get; set; }

    [JsonPropertyName("senderNick")]
    public string? SenderNick { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; set; }

    [JsonPropertyName("senderStaffId")]
    public string? SenderStaffId { get; set; }

    [JsonPropertyName("sessionWebhookExpiredTime")]
    public long? SessionWebhookExpiredTime { get; set; }

    [JsonPropertyName("createAt")]
    public long? CreateAt { get; set; }

    [JsonPropertyName("senderCorpId")]
    public string? SenderCorpId { get; set; }

    [JsonPropertyName("conversationType")]
    public string? ConversationType { get; set; }

    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    [JsonPropertyName("conversationTitle")]
    public string? ConversationTitle { get; set; }

    [JsonPropertyName("isInAtList")]
    public bool IsInAtList { get; set; }

    [JsonPropertyName("sessionWebhook")]
    public string? SessionWebhook { get; set; }

    [JsonPropertyName("text")]
    public Text? Text { get; set; }

    [JsonPropertyName("msgtype")]
    public string? Msgtype { get; set; }

    [JsonPropertyName("robotCode")]
    public string? RobotCode { get; set; }
}

public class Atuser
{
    [JsonPropertyName("dingtalkId")]
    public string? DingtalkId { get; set; }

    [JsonPropertyName("staffId")]
    public string? StaffId { get; set; }
}

