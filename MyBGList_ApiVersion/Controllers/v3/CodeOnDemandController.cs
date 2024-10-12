using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace MyBGList.Controllers.v3;

[Route("v{version:ApiVersion}/[controller]/[action]")]
[ApiController]
[ApiVersion("3.0")]
public class CodeOnDemandController : ControllerBase {
    [HttpGet(Name = "Test")]
    [EnableCors("AnyOrigin_GetOnly")]
    [ResponseCache(NoStore = true)]
    public ContentResult Test() {
        return Content("<script>" +
                       "window.alert('Your client supports JavaScript!" +
                       "\\r\\n\\r\\n" +
                       $"Server time (UTC): {DateTime.UtcNow.ToString("o")}" +
                       "\\r\\n" +
                       "Client time (UTC): ' + new Date().toISOString());" +
                       "</script>" +
                       "<noscript>Your client does not support JavaScript</noscript>",
            "text/html");
    }
    
    [HttpGet(Name = "Test2")]
    [EnableCors("AnyOrigin_GetOnly")]
    [ResponseCache(NoStore = true)]
    public ContentResult Test2(int minutesToAdd = 0) {
        var curTime = DateTime.UtcNow.AddMinutes(minutesToAdd);
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