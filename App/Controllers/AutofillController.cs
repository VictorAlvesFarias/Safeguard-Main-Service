using Application.Dtos.Autofill;
using Application.Dtos.Default;
using Application.Services.AutofillService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NET_Core_Template.Controllers
{
    /// <summary>Used by the browser extension to fill logins (user, password, authenticator code).</summary>
    [Authorize]
    public class AutofillController : Controller
    {
        private readonly IAutofillService _autofillService;

        public AutofillController(IAutofillService autofillService)
        {
            _autofillService = autofillService;
        }

        /// <summary>Logins available for the site at <paramref name="url"/> (no passwords).</summary>
        [HttpGet("get-autofill-entries")]
        public ActionResult<BaseResponse<AutofillEntriesResponse>> GetEntries(string url)
        {
            return _autofillService.GetEntries(url).Result(this);
        }

        /// <summary>User, password and current code of one login, only if it belongs to the site at <paramref name="url"/>.</summary>
        [HttpGet("get-autofill-fill")]
        public ActionResult<BaseResponse<AutofillFillResponse>> Fill(string url, string kind, int id)
        {
            return _autofillService.Fill(url, kind, id).Result(this);
        }

        /// <summary>Accounts of any platform (no secrets), to pick one for a site that isn't linked yet.</summary>
        [HttpGet("get-autofill-search")]
        public ActionResult<BaseResponse<List<AutofillEntry>>> Search(string? q)
        {
            return _autofillService.Search(q).Result(this);
        }

        /// <summary>Links the site of the given page to a platform.</summary>
        [HttpPost("autofill-link")]
        public ActionResult<BaseResponse<string>> Link([FromBody] AutofillLinkRequest request)
        {
            return _autofillService.Link(request).Result(this);
        }

        /// <summary>One account's values regardless of the site ("only this time").</summary>
        [HttpGet("get-autofill-fill-account")]
        public ActionResult<BaseResponse<AutofillFillResponse>> FillAccount(int id)
        {
            return _autofillService.FillAccount(id).Result(this);
        }
    }
}
