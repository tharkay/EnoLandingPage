﻿namespace EnoLandingPageBackend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using EnoLandingPageBackend.Database;
    using EnoLandingPageBackend.CTFTime;
    using EnoLandingPageCore;
    using EnoLandingPageCore.Database;
    using EnoLandingPageCore.Messages;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ILogger<AccountController> logger;
        private readonly LandingPageDatabase db;
        private readonly LandingPageSettings settings;

        public AccountController(ILogger<AccountController> logger, LandingPageDatabase db, LandingPageSettings settings)
        {
            this.logger = logger;
            this.db = db;
            this.settings = settings;
        }

        [HttpGet]
        public ActionResult Login(string redirectUri) // TODO 404 foo makes ReturnUrl out of this
        {
            return this.Challenge(
                new AuthenticationProperties()
                {
                    RedirectUri = $"/api/account/oauth2redirect?redirectUri={HttpUtility.UrlEncode(redirectUri)}",
                },
                "ctftime.org");
        }

        [HttpGet]
        public async Task<ActionResult> OAuth2Redirect(string redirectUri)
        {
            var ctftimeIdClaim = this.HttpContext.User.FindFirst(LandingPageClaimTypes.CtftimeId)?.Value;
            var teamname = this.HttpContext.User.Identity?.Name;
            if (!long.TryParse(ctftimeIdClaim, out long ctftimeId)
                || teamname == null)
            {
                throw new Exception($"OAuth2 failed: ctftimeid={ctftimeIdClaim} teamname={teamname} claims={this.HttpContext.User.Claims.Count()}");
            }

            if (DateTime.UtcNow > this.settings.StartTime.AddHours(-this.settings.RegistrationCloseOffset).ToUniversalTime() &&
                !await this.db.CtftimeTeamExists(ctftimeId, this.HttpContext.RequestAborted))
            {
                return this.Redirect("/registrationclosed");
            }

            CTFTimeTeamInfo? info = null;
            try
            {
                info = await CTFTime.GetTeamInfo(ctftimeId, this.HttpContext.RequestAborted);
            }
            catch (Exception e)
            {
                this.logger.LogError($"CTFtime failed to deliver info for ctftime id {ctftimeId}\n{e.StackTrace}");
            }

            var team = await this.db.GetOrUpdateLandingPageTeam(ctftimeId, teamname, info?.Logo, null, info?.Country, this.HttpContext.RequestAborted);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, $"{team.Id}"),
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await this.HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));
            return this.Redirect(redirectUri);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> Info()
        {
            var team = await this.db.GetTeamAndVulnbox(this.GetTeamId(), this.HttpContext.RequestAborted);
            return this.Ok(new TeamDetailsMessage(
                team.Id,
                team.Confirmed,
                team.Name,
                team.Vulnbox.ExternalAddress != null, // vpnconfig available
                team.Vulnbox.RootPassword,
                team.Vulnbox.ExternalAddress,
                $"10.0.0.{team.Id}", // internal ip
                team.Vulnbox.VulnboxStatus));
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> VpnConfig()
        {
            var team = await this.db.GetTeamAndVulnbox(this.GetTeamId(), this.HttpContext.RequestAborted);
            if (team.Vulnbox.ExternalAddress == null)
            {
                return this.NotFound();
            }

            var config = System.IO.File.ReadAllText($"{LandingPageBackendUtil.TeamDataDirectory}{Path.DirectorySeparatorChar}teamdata{Path.DirectorySeparatorChar}team{team.Id}{Path.DirectorySeparatorChar}client.conf");
            var contentType = "application/force-download";
            return this.File(Encoding.ASCII.GetBytes(config.Replace("REMOTE_IP_PLACEHOLDER", team.Vulnbox.ExternalAddress)), contentType, "client.conf");
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CheckIn()
        {
            long teamId = this.GetTeamId();
            if (DateTime.UtcNow > this.settings.StartTime.AddHours(-this.settings.CheckInEndOffset).ToUniversalTime() ||
                this.settings.StartTime.AddHours(-this.settings.CheckInBeginOffset).ToUniversalTime() > DateTime.UtcNow)
            {
                return this.Forbid();
            }

            await this.db.CheckIn(teamId, this.HttpContext.RequestAborted);
            return this.NoContent();
        }
    }
}
