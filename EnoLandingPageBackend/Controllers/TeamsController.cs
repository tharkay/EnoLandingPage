﻿namespace EnoLandingPageBackend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using EnoLandingPageBackend.Database;
    using EnoLandingPageCore;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;

    [Authorize]
    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class TeamsController : ControllerBase
    {
        private readonly ILogger<TeamsController> logger;
        private readonly LandingPageDatabase db;

        public TeamsController(ILogger<TeamsController> logger, LandingPageDatabase db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<IActionResult> Confirmed()
        {
            var teams = await this.db.GetConfirmedTeams(this.HttpContext.RequestAborted);
            return this.Ok(teams.Select(t => new LandingPageTeam(t.Name, t.CtftimeId)));
        }
    }
}