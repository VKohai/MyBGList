using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MyBGList.Controllers.v2;

[Route("v{version:ApiVersion}/[controller]/[action]")]
[ApiController]
[ApiVersion("2.0")]
public class CodeOnDemandController : ControllerBase {
    [HttpGet(Name = "Test2")]
    [EnableCors("AnyOrigin")]
    [ResponseCache(NoStore = true)]
    public ContentResult Test2(int addMinutes = 0) {
        var curTime = DateTime.UtcNow.AddMinutes(addMinutes);
        return Content("<script>" +
                       "window.alert('Your client supports JavaScript!" +
                       "\\r\\n\\r\\n" +
                       $"Server time (UTC): {curTime.ToString("o")}" +
                       "\\r\\n" +
                       "Client time (UTC): ' + new Date().toISOString());" +
                       "</script>" +
                       "<noscript>Your client does not support JavaScript</noscript>",
            "text/html");
    }
}