﻿namespace EnoLandingPageBackend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using EnoLandingPageBackend.Hetzner;
    using EnoLandingPageCore.Hetzner;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class VMController : ControllerBase
    {
        private readonly ILogger<VMController> logger;

        public VMController(ILogger<VMController> logger)
        {
            this.logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> StartVulnbox()
        {
            long teamId = this.GetTeamId();
            this.logger.LogInformation($"StartVulnbox {teamId}");
            try
            {
                await HetznerCloudApi.Call(teamId, HetznerCloudApiCallType.Create, this.HttpContext.RequestAborted);
            }
            catch (ServerExistsException)
            {
                return this.UnprocessableEntity($"{nameof(ServerExistsException)}");
            }
            catch (OtherRequestRunningException)
            {
                return this.UnprocessableEntity($"{nameof(OtherRequestRunningException)}");
            }

            return this.NoContent();
        }

        [HttpPost]
        public async Task<ActionResult> ResetVulnbox()
        {
            long teamId = this.GetTeamId();
            this.logger.LogInformation($"ResetVulnbox {teamId}");
            await HetznerCloudApi.Call(teamId, HetznerCloudApiCallType.Reset, this.HttpContext.RequestAborted);
            return this.NoContent();
        }
    }
}
