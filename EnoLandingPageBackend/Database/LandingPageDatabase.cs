﻿namespace EnoLandingPageBackend.Database
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using EnoLandingPageBackend.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class LandingPageDatabase
    {
        private readonly ILogger<LandingPageDatabase> logger;
        private readonly LandingPageDatabaseContext context;

        public LandingPageDatabase(ILogger<LandingPageDatabase> logger, LandingPageDatabaseContext databaseContext)
        {
            this.logger = logger;
            this.context = databaseContext;
        }

        public void Migrate()
        {
            var pendingMigrations = this.context.Database.GetPendingMigrations().Count();
            if (pendingMigrations > 0)
            {
                this.logger.LogInformation($"Applying {pendingMigrations} migration(s)");
                this.context.Database.Migrate();
                this.context.SaveChanges();
                this.logger.LogDebug($"Database migration complete");
            }
            else
            {
                this.logger.LogDebug($"No pending migrations");
            }
        }

        public async Task<LandingPageTeam> GetTeam(long teamId)
        {
            return await this.context.Teams
                .Where(t => t.Id == teamId)
                .SingleAsync();
        }

        public async Task<LandingPageTeam> UpdateTeam(long? ctftimeId, string name)
        {
            var dbTeam = await this.context.Teams.Where(t => t.CtftimeId == ctftimeId).SingleOrDefaultAsync();
            if (dbTeam == null)
            {
                dbTeam = new LandingPageTeam(
                    0,
                    ctftimeId,
                    false,
                    name);
                this.context.Add(dbTeam);
            }
            else
            {
                dbTeam.Name = name;
                dbTeam.CtftimeId = ctftimeId;
                dbTeam.Name = name;
            }

            await this.context.SaveChangesAsync();
            return dbTeam;
        }
    }
}